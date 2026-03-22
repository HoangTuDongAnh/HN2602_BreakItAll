using System;
using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.View;
using _Game.Scripts.Data;
using _Game.Scripts.Core.Services;
using _Game.Scripts.Logic.Placement;

namespace _Game.Scripts.Logic
{
    public class BlockController : MonoBehaviour
    {
        #region Configuration
        [Header("Visual Settings")]
        [SerializeField] private float _baseSlotScale = 0.8f;
        [SerializeField] private float _dragScale = 1.0f;
        [SerializeField] private float _cellSpacing = 1.0f;

        [Header("Drag Settings")]
        [SerializeField] private float _dragOffsetY = 0.3f;

        [SerializeField] private LayerMask _blockLayer;

        [Header("Placement Anchor")]
        [SerializeField] private Transform _anchorPoint;
        #endregion

        #region Runtime Data
        private List<CellData> _runtimeData = new List<CellData>();
        private List<Transform> _childCells = new List<Transform>();
        private List<Vector2Int> _shapeOffsets = new List<Vector2Int>();

        private float _currentSlotScale;
        private Color _themeColor;

        private Vector2 _dragAreaMin;
        private Vector2 _dragAreaMax;
        #endregion

        #region State Variables
        private Vector3 _startPosition;
        private bool _isDragging = false;
        private Vector3 _dragOffset;
        private Camera _cam;

        public event Action<BlockController> OnPlaced;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _cam = Camera.main;
            _startPosition = transform.position;
            CalculateDragBounds();
        }

        private void Update()
        {
            if (GameServices.GameState == null || !GameServices.GameState.IsInputAllowed())
            {
                if (_isDragging) HandleInputUp();
                return;
            }

            if (Input.GetMouseButtonDown(0)) HandleInputDown();
            else if (_isDragging && Input.GetMouseButton(0)) HandleInputDrag();
            else if (_isDragging && Input.GetMouseButtonUp(0)) HandleInputUp();
        }
        #endregion

        #region Initialization
        private void CalculateDragBounds()
        {
            if (GameServices.BoardQuery == null) return;

            var boardBounds = GameServices.BoardQuery.GetBoardWorldBounds();

            float minX = Mathf.Min(boardBounds.min.x, _startPosition.x);
            float maxX = Mathf.Max(boardBounds.max.x, _startPosition.x);
            float minY = Mathf.Min(boardBounds.min.y, _startPosition.y);
            float maxY = Mathf.Max(boardBounds.max.y, _startPosition.y);

            float padding = 0.5f;
            _dragAreaMin = new Vector2(minX - padding, minY - padding);
            _dragAreaMax = new Vector2(maxX + padding, maxY + padding);
        }

        public void Initialize(List<CellData> injectedData, int columns, GameObject cellPrefab, Color themeColor)
        {
            _runtimeData = injectedData;
            _shapeOffsets.Clear();
            _themeColor = themeColor;

            foreach (Transform child in transform)
            {
                if (_anchorPoint != null && child == _anchorPoint) continue;
                Destroy(child.gameObject);
            }

            _childCells.Clear();

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            List<int> occupiedIndices = new List<int>();

            for (int i = 0; i < injectedData.Count; i++)
            {
                if (!injectedData[i].isOccupied) continue;

                occupiedIndices.Add(i);

                int col = i % columns;
                int row = i / columns;

                if (col < minX) minX = col;
                if (col > maxX) maxX = col;
                if (row < minY) minY = row;
                if (row > maxY) maxY = row;
            }

            if (occupiedIndices.Count == 0) return;

            int blockWidth = maxX - minX + 1;
            int blockHeight = maxY - minY + 1;
            float sizePenalty = (blockWidth > 3 || blockHeight > 3) ? 0.75f : 1.0f;
            _currentSlotScale = _baseSlotScale * sizePenalty;

            transform.localScale = Vector3.one * _currentSlotScale;

            float visualCenterX = (minX + maxX) / 2.0f;
            float visualCenterY = (minY + maxY) / 2.0f;

            foreach (int i in occupiedIndices)
            {
                CellData cell = injectedData[i];
                int x = i % columns;
                int y = i / columns;

                GameObject cellObj = Instantiate(cellPrefab, transform);

                float posX = (x - visualCenterX) * _cellSpacing;
                float posY = (y - visualCenterY) * _cellSpacing;
                cellObj.transform.localPosition = new Vector3(posX, posY, 0f);

                int pivotX = Mathf.RoundToInt(visualCenterX);
                int pivotY = Mathf.RoundToInt(visualCenterY);
                _shapeOffsets.Add(new Vector2Int(x - pivotX, y - pivotY));

                GridCellView view = cellObj.GetComponent<GridCellView>();
                if (view != null)
                {
                    GridCell tempCell = new GridCell(0, 0);
                    tempCell.SetData(cell.cellType, cell.toolType);
                    view.Init(tempCell, themeColor);
                }

                _childCells.Add(cellObj.transform);
            }
        }

        public List<Vector2Int> GetShapeOffsets() => _shapeOffsets;
        public int GetOccupiedCellCount()
        {
            int count = 0;
            foreach (var data in _runtimeData)
            {
                if (data.isOccupied) count++;
            }
            return count;
        }
        #endregion

        #region Input Handling
        private void HandleInputDown()
        {
            Vector3 mousePos = GetMouseWorldPos();
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, _blockLayer);

            if (hit.collider != null && hit.collider.transform == transform)
            {
                _isDragging = true;
                _dragOffset = transform.position - mousePos;

                transform.localScale = Vector3.one * _dragScale;

                Vector3 currentPos = transform.position;
                currentPos.z = -1f;
                transform.position = currentPos;
            }
        }

        private void HandleInputDrag()
        {
            Vector3 mousePos = GetMouseWorldPos();
            Vector3 targetPos = mousePos + _dragOffset + new Vector3(0f, _dragOffsetY, 0f);

            targetPos.x = Mathf.Clamp(targetPos.x, _dragAreaMin.x, _dragAreaMax.x);
            targetPos.y = Mathf.Clamp(targetPos.y, _dragAreaMin.y, _dragAreaMax.y);
            targetPos.z = -1f;

            transform.position = targetPos;
            UpdateShadow();
        }

        private void HandleInputUp()
        {
            _isDragging = false;

            if (GridManager.Instance != null)
                GridManager.Instance.ClearGhost();

            Vector3 pos = transform.position;
            pos.z = 0f;
            transform.position = pos;

            CheckAndPlace();
        }

        private Vector3 GetMouseWorldPos()
        {
            Vector3 pos = _cam.ScreenToWorldPoint(Input.mousePosition);
            pos.z = 0f;
            return pos;
        }
        #endregion

        #region Placement Helpers
        private Vector3 GetAnchorWorldPosition()
        {
            return _anchorPoint != null ? _anchorPoint.position : transform.position;
        }
        #endregion

        #region Visual & Placement Logic
        private void UpdateShadow()
        {
            if (GameServices.BoardQuery == null || GameServices.Placement == null) return;
            if (_shapeOffsets == null || _shapeOffsets.Count == 0) return;

            GridCoord anchor = GameServices.BoardQuery.GetAnchorCoord(GetAnchorWorldPosition());

            PlacementPreview preview = GameServices.Placement.BuildPreview(
                anchor,
                _shapeOffsets,
                GameServices.BoardQuery.GetBoardState()
            );

            if (GridManager.Instance != null)
                GridManager.Instance.ShowGhostPreview(preview);
        }

        private void CheckAndPlace()
        {
            if (GameServices.BoardQuery == null || GameServices.Placement == null)
            {
                ResetToStartPosition();
                return;
            }

            if (_shapeOffsets == null || _shapeOffsets.Count == 0)
            {
                ResetToStartPosition();
                return;
            }

            GridCoord anchor = GameServices.BoardQuery.GetAnchorCoord(GetAnchorWorldPosition());

            PlacementResult result = GameServices.Placement.Evaluate(
                anchor,
                _shapeOffsets,
                GameServices.BoardQuery.GetBoardState()
            );

            if (!result.IsValid)
            {
                ResetToStartPosition();
                return;
            }

            List<Vector2Int> targetCoords = new List<Vector2Int>();
            List<CellData> activeData = new List<CellData>();

            int logicalIndex = 0;
            for (int i = 0; i < _runtimeData.Count; i++)
            {
                if (!_runtimeData[i].isOccupied) continue;

                GridCoord coord = result.OccupiedCoords[logicalIndex++];
                targetCoords.Add(coord.ToVector2Int());
                activeData.Add(_runtimeData[i]);
            }

            bool success = GridManager.Instance != null &&
                           GridManager.Instance.TryPlaceBlock(targetCoords, activeData, _themeColor);

            if (success)
            {
                GameServices.Score?.AddPlacementScore(GetOccupiedCellCount());
                OnPlaced?.Invoke(this);
                Destroy(gameObject);
            }
            else
            {
                ResetToStartPosition();
            }
        }

        private void ResetToStartPosition()
        {
            transform.position = _startPosition;
            transform.localScale = Vector3.one * _currentSlotScale;
        }
        #endregion
    }
}