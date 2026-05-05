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
        #region Puzzle Placement/Edit
        private bool IsPuzzleArcadeSession()
        {
            GameManager manager = GameManager.Instance;
            return manager != null
                   && manager.CurrentModeType == GameModeType.Arcade
                   && manager.ActiveArcadeLevel != null
                   && manager.ActiveArcadeLevel.LevelType == ArcadeLevelType.Puzzle;
        }

        private void BeginPuzzleGridReposition()
        {
            if (!_hasPuzzleGridPlacement || _isRepositioningPuzzleGridBlock) return;
            if (GridManager.Instance == null) return;

            SetPuzzlePlacedVisualVisible(true);
            transform.localScale = Vector3.one * _dragScale;
            _targetScale = _dragScale;

            GridManager.Instance.ClearCellsForPuzzleReposition(_currentPuzzleGridCoords);
            _isRepositioningPuzzleGridBlock = true;
        }

        private void CompletePuzzleGridPlacement(List<Vector2Int> targetCoords, List<CellData> activeData, GridCoord anchor, bool notifyQueueUsed)
        {
            _hasPuzzleGridPlacement = true;
            _isRepositioningPuzzleGridBlock = false;
            _currentPuzzleGridAnchor = anchor;

            _currentPuzzleGridCoords.Clear();
            _currentPuzzleGridData.Clear();

            if (targetCoords != null)
                _currentPuzzleGridCoords.AddRange(targetCoords);

            if (activeData != null)
            {
                for (int i = 0; i < activeData.Count; i++)
                    _currentPuzzleGridData.Add(activeData[i] != null ? activeData[i].Clone() : null);
            }

            SnapVisuallyToPlacement(anchor);
            transform.localScale = Vector3.one * _dragScale;
            _targetScale = _dragScale;
            _homePosition = transform.position;
            _targetDragPosition = transform.position;

            if (_homeSlot != null)
            {
                transform.SetParent(null, true);
                _homeSlot = null;
            }

            SetPuzzlePlacedVisualVisible(false);

            if (notifyQueueUsed)
            {
                _hasNotifiedPuzzleQueueUsed = true;
                OnPlaced?.Invoke(this);
            }
        }

        private bool TryReturnPuzzleBlockToSpawnSlot()
        {
            if (!_hasPuzzleGridPlacement) return false;
            if (_puzzleQueueOwner == null) return false;
            if (_puzzleQueueEntryIndex < 0) return false;

            bool returned = _puzzleQueueOwner.TryReturnPlacedPuzzleBlockToQueue(
                this,
                _puzzleQueueEntryIndex,
                transform.position,
                _homeSlot
            );

            if (!returned) return false;

            _isRepositioningPuzzleGridBlock = false;
            GameEvents.RaiseBoardResolved(new BoardResolveResult());
            GameEvents.RaiseMoveCompleted(0, Vector3.zero);
            return true;
        }

        private void RestorePuzzleGridPlacement()
        {
            _isRepositioningPuzzleGridBlock = false;

            if (GridManager.Instance != null)
            {
                GridManager.Instance.TryRestoreCellsForPuzzleReposition(
                    _currentPuzzleGridCoords,
                    _currentPuzzleGridData,
                    _themeColor
                );
            }

            SnapVisuallyToPlacement(_currentPuzzleGridAnchor);
            transform.localScale = Vector3.one * _dragScale;
            _targetScale = _dragScale;
            _homePosition = transform.position;
            _targetDragPosition = transform.position;
            SetPuzzlePlacedVisualVisible(false);
        }

        private void RotatePlacedPuzzleGridBlock()
        {
            if (!_hasPuzzleGridPlacement) return;
            if (GridManager.Instance == null) return;

            int previousRotationIndex = _runtimeRotationIndex;
            GridCoord previousAnchor = _currentPuzzleGridAnchor;
            List<Vector2Int> previousCoords = new List<Vector2Int>(_currentPuzzleGridCoords);
            List<CellData> previousData = CloneCellDataList(_currentPuzzleGridData);

            Vector3 previousCenter = GetCoordsWorldCenter(previousCoords);

            GridManager.Instance.ClearCellsForPuzzleReposition(previousCoords);
            SetPuzzlePlacedVisualVisible(true);

            _runtimeRotationIndex = (_runtimeRotationIndex + 1) % 4;
            BlockFactory.RuntimeBlockResult rotatedBlock = BlockFactory.CreateBlockInstance(_sourceTemplate, _runtimeRotationIndex);
            Initialize(rotatedBlock.cells, rotatedBlock.width, _sourceCellPrefab, _themeColor);
            transform.localScale = Vector3.one * _dragScale;
            _targetScale = _dragScale;

            PlacementResult bestResult = FindBestValidPlacementNear(previousAnchor, previousCenter, searchRadius: 2);

            if (bestResult != null && bestResult.IsValid)
            {
                BuildPlacementPayload(bestResult, out List<Vector2Int> targetCoords, out List<CellData> activeData);
                if (GridManager.Instance.TryPlaceBlock(targetCoords, activeData, _themeColor))
                {
                    CompletePuzzleGridPlacement(targetCoords, activeData, bestResult.Anchor, notifyQueueUsed: false);
                    return;
                }
            }

            _runtimeRotationIndex = previousRotationIndex;
            BlockFactory.RuntimeBlockResult previousBlock = BlockFactory.CreateBlockInstance(_sourceTemplate, _runtimeRotationIndex);
            Initialize(previousBlock.cells, previousBlock.width, _sourceCellPrefab, _themeColor);
            transform.localScale = Vector3.one * _dragScale;
            _targetScale = _dragScale;

            _currentPuzzleGridAnchor = previousAnchor;
            _currentPuzzleGridCoords.Clear();
            _currentPuzzleGridCoords.AddRange(previousCoords);
            _currentPuzzleGridData.Clear();
            _currentPuzzleGridData.AddRange(CloneCellDataList(previousData));

            GridManager.Instance.TryRestoreCellsForPuzzleReposition(previousCoords, previousData, _themeColor);
            SnapVisuallyToPlacement(previousAnchor);
            SetPuzzlePlacedVisualVisible(false);
        }

        private PlacementResult FindBestValidPlacementNear(GridCoord anchor, Vector3 previousWorldCenter, int searchRadius)
        {
            PlacementResult best = null;
            float bestScore = float.MaxValue;

            for (int dy = -searchRadius; dy <= searchRadius; dy++)
            {
                for (int dx = -searchRadius; dx <= searchRadius; dx++)
                {
                    GridCoord candidate = new GridCoord(anchor.X + dx, anchor.Y + dy);
                    PlacementResult result = _placementService.Evaluate(candidate, _shapeOffsets, GridManager.Instance.GetBoardState());
                    if (result == null || !result.IsValid) continue;

                    Vector3 center = GetCoordsWorldCenter(result.OccupiedCoords);
                    float score = Vector2.SqrMagnitude((Vector2)(center - previousWorldCenter)) + (Mathf.Abs(dx) + Mathf.Abs(dy)) * 0.015f;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = result;
                    }
                }
            }

            return best;
        }

        private Vector3 GetCoordsWorldCenter(List<Vector2Int> coords)
        {
            if (GridManager.Instance == null || coords == null || coords.Count == 0)
                return transform.position;

            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < coords.Count; i++)
            {
                if (!GridManager.Instance.IsValidCoordinate(coords[i].x, coords[i].y)) continue;
                sum += GridManager.Instance.GetWorldPosition(coords[i].x, coords[i].y);
                count++;
            }

            return count > 0 ? sum / count : transform.position;
        }

        private Vector3 GetCoordsWorldCenter(List<GridCoord> coords)
        {
            if (GridManager.Instance == null || coords == null || coords.Count == 0)
                return transform.position;

            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < coords.Count; i++)
            {
                if (!GridManager.Instance.IsValidCoordinate(coords[i].X, coords[i].Y)) continue;
                sum += GridManager.Instance.GetWorldPosition(coords[i].X, coords[i].Y);
                count++;
            }

            return count > 0 ? sum / count : transform.position;
        }

        private List<CellData> CloneCellDataList(List<CellData> source)
        {
            List<CellData> clone = new List<CellData>();
            if (source == null) return clone;

            for (int i = 0; i < source.Count; i++)
                clone.Add(source[i] != null ? source[i].Clone() : null);

            return clone;
        }

        private void SetPuzzlePlacedVisualVisible(bool visible)
        {
            if (!_hasPuzzleGridPlacement && !visible) return;
            _isPuzzlePlacedVisualHidden = !visible;

            for (int i = 0; i < _childCells.Count; i++)
            {
                Transform cell = _childCells[i];
                if (cell == null) continue;

                SpriteRenderer[] renderers = cell.GetComponentsInChildren<SpriteRenderer>(true);
                for (int r = 0; r < renderers.Length; r++)
                    renderers[r].enabled = visible;

                if (visible)
                {
                    GridCellView cellView = cell.GetComponent<GridCellView>();
                    if (cellView != null)
                        cellView.UpdateVisual();

                    Transform marker = cell.Find("Item_Marker");
                    if (marker != null)
                    {
                        SpriteRenderer markerRenderer = marker.GetComponent<SpriteRenderer>();
                        if (markerRenderer != null)
                            markerRenderer.enabled = false;
                    }
                }
            }
        }
        #endregion
    }
}
