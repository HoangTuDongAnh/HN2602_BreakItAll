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
        #region Input Handling
        private void HandleInputDown()
        {
            Vector3 mousePos = GetMouseWorldPos();
            RaycastHit2D hit = RaycastBlock(mousePos);

            if (hit.collider == null || !hit.collider.transform.IsChildOf(transform))
                return;

            if (CanRuntimeRotate() && Time.unscaledTime - _lastTapTime <= _doubleTapRotateThreshold)
            {
                _lastTapTime = -999f;
                StopReturnRoutine();

                if (_hasPuzzleGridPlacement)
                    RotatePlacedPuzzleGridBlock();
                else
                    RotateRuntimeBlock();

                return;
            }

            if (_isReturning) return;

            if (!_hasPuzzleGridPlacement && ToolController.Instance != null && ToolController.Instance.ActiveTool == GameplayToolType.RemoveSpawnBlock)
            {
                ToolController.Instance.TryRemoveSpawnBlock(this);
                return;
            }

            _lastTapTime = Time.unscaledTime;

            StopReturnRoutine();
            CalculateDragBounds();

            if (_hasPuzzleGridPlacement)
                BeginPuzzleGridReposition();

            _isDragging = true;
            _lastPreview = null;
            _lastPreviewTime = -999f;

            Vector3 rawOffset = transform.position - mousePos;
            _dragOffset = Vector3.Lerp(rawOffset, Vector3.zero, _pickupCentering);
            _targetScale = _dragScale;
            transform.localScale = Vector3.one * _dragScale;

            Vector3 currentPos = transform.position;
            currentPos.z = -1f;
            transform.position = currentPos;
            _targetDragPosition = currentPos;
            _dragVelocity = Vector3.zero;

            PlayPickupPunch();
        }

        private void HandleInputDrag()
        {
            Vector3 mousePos = GetMouseWorldPos();
            Vector3 rawTarget = mousePos + _dragOffset + new Vector3(0f, _dragOffsetY, 0f);

            rawTarget.x = Mathf.Clamp(rawTarget.x, _dragAreaMin.x, _dragAreaMax.x);
            rawTarget.y = Mathf.Clamp(rawTarget.y, _dragAreaMin.y, _dragAreaMax.y);
            rawTarget.z = -1f;

            Vector3 assistedTarget = ApplySnapAssist(rawTarget);
            _targetDragPosition = assistedTarget;

            if (ShouldRefreshPreview())
            {
                UpdateShadowAtRoot(assistedTarget);
                _lastPreviewTime = Time.time;
            }
        }

        private void HandleInputUp()
        {
            _isDragging = false;

            if (GridManager.Instance != null)
            {
                GridManager.Instance.ClearGhost();
                GridManager.Instance.ClearToolPreview();
            }

            Vector3 releasePosition = _targetDragPosition;
            releasePosition.z = 0f;
            transform.position = releasePosition;

            CheckAndPlace();
        }

        private RaycastHit2D RaycastBlock(Vector3 worldPos)
        {
            RaycastHit2D[] hits = _blockLayer.value == 0
                ? Physics2D.RaycastAll(worldPos, Vector2.zero)
                : Physics2D.RaycastAll(worldPos, Vector2.zero, Mathf.Infinity, _blockLayer);

            if (hits == null || hits.Length == 0)
                return new RaycastHit2D();

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider != null && hits[i].collider.transform.IsChildOf(transform))
                    return hits[i];
            }

            return hits[0];
        }

        private Vector3 GetMouseWorldPos()
        {
            if (_cam == null) _cam = Camera.main;
            Vector3 pos = _cam != null ? _cam.ScreenToWorldPoint(Input.mousePosition) : Vector3.zero;
            pos.z = 0f;
            return pos;
        }
        #endregion
    }
}
