using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.View;
using _Game.Scripts.Data;
using _Game.Scripts.Core;
using _Game.Scripts.Logic.Placement;
using _Game.Scripts.Logic.Resolve;
using _Game.Scripts.Logic.Tools;
using _Game.Scripts.Modes;
using _Game.Scripts.Modes.Levels;

namespace _Game.Scripts.Logic
{
    public partial class BlockController
    {
        #region Placement Helpers
        private Vector3 GetAnchorWorldPosition()
        {
            if (_anchorPoint != null) return _anchorPoint.position;
            return transform.TransformPoint(_logicalAnchorLocalPosition);
        }

        private Vector3 GetAnchorWorldPositionAtRoot(Vector3 rootPosition)
        {
            Vector3 anchorOffset = transform.TransformVector(_logicalAnchorLocalPosition);
            return rootPosition + anchorOffset;
        }

        private Vector3 GetRootPositionForAnchor(Vector3 desiredAnchorWorld)
        {
            Vector3 anchorOffset = transform.TransformVector(_logicalAnchorLocalPosition);
            return desiredAnchorWorld - anchorOffset;
        }

        private Vector3 GetHomePosition()
        {
            if (_homeSlot != null) return _homeSlot.position;
            return _homePosition;
        }
        #endregion

        #region Visual & Placement Logic
        private Vector3 ApplySnapAssist(Vector3 rawTarget)
        {
            if (!_enableSnapAssist || GridManager.Instance == null || _shapeOffsets.Count == 0)
                return rawTarget;

            PlacementPreview preview = BuildPreviewAtRoot(rawTarget);
            _lastPreview = preview;

            if (preview == null || !preview.IsNearBoard || !preview.IsValid)
                return rawTarget;

            Vector3 anchorWorld = GetAnchorWorldPositionAtRoot(rawTarget);
            Vector3 snapAnchorWorld = GridManager.Instance.GetAnchorWorldPosition(preview.Anchor);
            float distance = Vector2.Distance(anchorWorld, snapAnchorWorld);

            if (distance > _snapPreviewDistance)
                return rawTarget;

            float t = 1f - Mathf.Clamp01(distance / Mathf.Max(0.001f, _snapPreviewDistance));
            float assist = Mathf.SmoothStep(0f, _snapAssistStrength, t);
            Vector3 snappedRoot = GetRootPositionForAnchor(snapAnchorWorld);
            snappedRoot.z = rawTarget.z;

            preview.WorldSnapPosition = snapAnchorWorld;
            return Vector3.Lerp(rawTarget, snappedRoot, assist);
        }

        private bool ShouldRefreshPreview()
        {
            return _previewRefreshInterval <= 0f || Time.time - _lastPreviewTime >= _previewRefreshInterval;
        }

        private PlacementPreview BuildPreviewAtRoot(Vector3 rootPosition)
        {
            if (GridManager.Instance == null || _shapeOffsets.Count == 0 || IsBombToolBlock())
                return null;

            GridCoord anchor = GridManager.Instance.GetAnchorCoord(GetAnchorWorldPositionAtRoot(rootPosition));
            PlacementPreview preview = _placementService.BuildPreview(anchor, _shapeOffsets, GridManager.Instance.GetBoardState());

            if (preview.IsValid)
                preview.WorldSnapPosition = GridManager.Instance.GetAnchorWorldPosition(preview.Anchor);

            return preview;
        }

        private void UpdateShadowAtRoot(Vector3 rootPosition)
        {
            if (GridManager.Instance == null) return;

            if (IsBombToolBlock())
            {
                Vector2Int center = GridManager.Instance.GetGridPosition(GetAnchorWorldPositionAtRoot(rootPosition));
                GridManager.Instance.ShowToolSquarePreview(center, 1, _toolPreviewColor);
                return;
            }

            PlacementPreview preview = BuildPreviewAtRoot(rootPosition);
            if (preview == null) return;

            GridManager.Instance.ShowGhostPreview(preview, _themeColor);
        }

        private void CheckAndPlace()
        {
            if (GridManager.Instance == null || _shapeOffsets.Count == 0)
            {
                ReturnAfterFailedPlacement();
                return;
            }

            if (_isGameplayToolBlock)
            {
                CheckAndApplyToolBlock();
                return;
            }

            GridCoord anchor = GridManager.Instance.GetAnchorCoord(GetAnchorWorldPosition());
            PlacementResult result = _placementService.Evaluate(anchor, _shapeOffsets, GridManager.Instance.GetBoardState());

            if (!result.IsValid)
            {
                ReturnAfterFailedPlacement();
                return;
            }

            SnapVisuallyToPlacement(result.Anchor);

            BuildPlacementPayload(result, out List<Vector2Int> targetCoords, out List<CellData> activeData);
            bool success = GridManager.Instance.TryPlaceBlock(targetCoords, activeData, _themeColor);

            if (!success)
            {
                ReturnAfterFailedPlacement();
                return;
            }

            if (IsPuzzleArcadeSession())
            {
                CompletePuzzleGridPlacement(targetCoords, activeData, result.Anchor, notifyQueueUsed: !_hasNotifiedPuzzleQueueUsed);
                return;
            }

            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddPlacementScore(GetOccupiedCellCount());

            OnPlaced?.Invoke(this);
            Destroy(gameObject);
        }

        private void BuildPlacementPayload(PlacementResult result, out List<Vector2Int> targetCoords, out List<CellData> activeData)
        {
            targetCoords = new List<Vector2Int>();
            activeData = new List<CellData>();

            int logicalIndex = 0;
            for (int i = 0; i < _runtimeData.Count; i++)
            {
                if (_runtimeData[i] == null || !_runtimeData[i].isOccupied) continue;

                GridCoord coord = result.OccupiedCoords[logicalIndex++];
                targetCoords.Add(coord.ToVector2Int());
                activeData.Add(_runtimeData[i] != null ? _runtimeData[i].Clone() : new CellData());
            }
        }

        private void CheckAndApplyToolBlock()
        {
            Vector2Int anchor = GridManager.Instance.GetGridPosition(GetAnchorWorldPosition());
            bool success = false;

            if (_gameplayToolType == GameplayToolType.PlaceSingleCell)
            {
                success = GridManager.Instance.TryPlaceSingleCell(anchor, BlockCellType.Normal, _themeColor);
            }
            else if (IsBombToolBlock())
            {
                success = GridManager.Instance.TryExplodeSquare(anchor, 1);
            }

            if (!success)
            {
                ReturnToSlotSmooth();
                return;
            }

            OnPlaced?.Invoke(this);

            if (ToolController.Instance != null)
                ToolController.Instance.NotifyToolActionCompleted();

            Destroy(gameObject);
        }

        private bool IsBombToolBlock()
        {
            return _isGameplayToolBlock && (_gameplayToolType == GameplayToolType.BombSquare);
        }

        private bool CanRuntimeRotate()
        {
            return _allowRuntimeRotation
                   && !_isGameplayToolBlock
                   && _sourceTemplate != null
                   && _sourceTemplate.allowRotation
                   && _sourceCellPrefab != null
                   && !_isDragging;
        }

        private void RotateRuntimeBlock()
        {
            if (!CanRuntimeRotate()) return;

            _runtimeRotationIndex = (_runtimeRotationIndex + 1) % 4;
            BlockFactory.RuntimeBlockResult runtimeBlock = BlockFactory.CreateBlockInstance(_sourceTemplate, _runtimeRotationIndex);
            Initialize(runtimeBlock.cells, runtimeBlock.width, _sourceCellPrefab, _themeColor);

            if (_hasPuzzleGridPlacement)
            {
                transform.localScale = Vector3.one * _dragScale;
                _targetScale = _dragScale;
                SnapVisuallyToPlacement(_currentPuzzleGridAnchor);
            }
            else
            {
                _targetScale = _currentSlotScale;
                transform.localScale = Vector3.one * _currentSlotScale;
                Vector3 home = GetHomePosition();
                if (!_isDragging && !_isReturning && Vector3.Distance(transform.position, home) < 1.25f)
                {
                    home.z = 0f;
                    transform.position = home;
                    _targetDragPosition = home;
                }
            }

            CalculateDragBounds();
        }

        private void SnapVisuallyToPlacement(GridCoord anchor)
        {
            if (GridManager.Instance == null) return;

            Vector3 anchorWorld = GridManager.Instance.GetAnchorWorldPosition(anchor);
            Vector3 rootPosition = GetRootPositionForAnchor(anchorWorld);
            rootPosition.z = 0f;
            transform.position = rootPosition;
            _targetDragPosition = rootPosition;
        }

        private void ReturnAfterFailedPlacement()
        {
            if (_isRepositioningPuzzleGridBlock)
            {
                if (TryReturnPuzzleBlockToSpawnSlot())
                    return;

                RestorePuzzleGridPlacement();
                return;
            }

            ReturnToSlotSmooth();
        }

        private void ReturnToSlotSmooth()
        {
            StopReturnRoutine();
            _returnRoutine = StartCoroutine(ReturnRoutine());
        }

        private IEnumerator ReturnRoutine()
        {
            _isReturning = true;
            _targetScale = _currentSlotScale * Mathf.Max(1f, _returnScaleOvershoot);

            Vector3 start = transform.position;
            Vector3 end = GetHomePosition();
            end.z = 0f;

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, _returnSmoothTime);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float rawT = Mathf.Clamp01(elapsed / duration);
                float t = _returnEase != null ? _returnEase.Evaluate(rawT) : Mathf.SmoothStep(0f, 1f, rawT);
                transform.position = Vector3.LerpUnclamped(start, end, t);

                if (rawT > 0.45f)
                    _targetScale = _currentSlotScale;

                yield return null;
            }

            transform.position = end;
            _targetDragPosition = end;
            _targetScale = _currentSlotScale;
            transform.localScale = Vector3.one * _currentSlotScale;
            _isReturning = false;
        }

        private void StopReturnRoutine()
        {
            if (_returnRoutine != null)
            {
                StopCoroutine(_returnRoutine);
                _returnRoutine = null;
            }

            _isReturning = false;
        }
        #endregion
    }
}
