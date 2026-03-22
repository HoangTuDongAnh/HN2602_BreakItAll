using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.View;
using _Game.Scripts.Data;
using _Game.Scripts.Core;
using _Game.Scripts.Core.Services;
using _Game.Scripts.Logic.Placement;
using _Game.Scripts.Logic.Resolve;

namespace _Game.Scripts.Logic
{
    public class GridManager : MonoBehaviour, IBoardQueryService
    {
        #region Singleton
        public static GridManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            _lineClearDetector = new LineClearDetector();
            _boardResolver = new BoardResolver(_lineClearDetector, _clearStepDelay);

            GameServices.RegisterBoardQuery(this);
            GameServices.RegisterPlacement(_placementService);

            GenerateGrid();
        }

        private void OnDestroy()
        {
            if (GameServices.BoardQuery == this)
                GameServices.RegisterBoardQuery(null);

            if (ReferenceEquals(GameServices.Placement, _placementService))
                GameServices.RegisterPlacement(null);
        }
        #endregion

        #region Configuration
        [Header("Grid Settings")]
        [SerializeField] private int _width = 8;
        [SerializeField] private int _height = 8;
        [SerializeField] private float _cellSize = 1.0f;
        [SerializeField] private float _spacing = 0.05f;

        [Header("Resolve Settings")]
        [SerializeField] private float _clearStepDelay = 0.02f;

        [Header("References")]
        [SerializeField] private GridCellView _cellPrefab;
        [SerializeField] private Transform _boardContainer;
        #endregion

        #region Data Storage
        private GridCell[,] _gridData;
        private GridCellView[,] _gridViews;

        private Vector3 _originPosition;
        private readonly List<Vector2Int> _currentShadowCells = new List<Vector2Int>();

        private bool _isProcessing = false;

        private readonly BlockPlacementService _placementService = new BlockPlacementService();

        private LineClearDetector _lineClearDetector;
        private BoardResolver _boardResolver;
        #endregion

        #region Initialization
        private void GenerateGrid()
        {
            if (_gridData != null) return;

            _gridData = new GridCell[_width, _height];
            _gridViews = new GridCellView[_width, _height];

            float totalWidth = _width * (_cellSize + _spacing);
            float totalHeight = _height * (_cellSize + _spacing);
            _originPosition = new Vector3(
                -totalWidth / 2 + _cellSize / 2,
                -totalHeight / 2 + _cellSize / 2,
                0f
            );

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    GridCell newCell = new GridCell(x, y);
                    _gridData[x, y] = newCell;

                    Vector3 pos = GetWorldPosition(x, y);
                    GridCellView newView = Instantiate(_cellPrefab, pos, Quaternion.identity, _boardContainer);

                    newView.Init(newCell, Color.white);
                    _gridViews[x, y] = newView;
                }
            }

            Debug.Log($"Grid {_width}x{_height} generated.");
        }
        #endregion

        #region Coordinates Helpers
        public Vector3 GetWorldPosition(int x, int y)
        {
            return _originPosition + new Vector3(x * (_cellSize + _spacing), y * (_cellSize + _spacing), 0f);
        }

        public Vector2Int GetGridPosition(Vector3 worldPosition)
        {
            Vector3 localPos = worldPosition - _boardContainer.position - _originPosition;
            float percentX = (localPos.x + (_cellSize + _spacing) / 2f) / (_cellSize + _spacing);
            float percentY = (localPos.y + (_cellSize + _spacing) / 2f) / (_cellSize + _spacing);

            return new Vector2Int(
                Mathf.FloorToInt(percentX),
                Mathf.FloorToInt(percentY)
            );
        }

        public GridCoord GetAnchorCoord(Vector3 worldPosition)
        {
            Vector2Int raw = GetGridPosition(worldPosition);
            return GridCoord.FromVector2Int(raw);
        }

        public BoardState GetBoardState()
        {
            return new BoardState(_gridData, _width, _height);
        }

        public bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        public (Vector2 min, Vector2 max) GetBoardWorldBounds()
        {
            Vector3 minPos = GetWorldPosition(0, 0);
            Vector3 maxPos = GetWorldPosition(_width - 1, _height - 1);
            float margin = _cellSize / 2f + _spacing;

            return (
                new Vector2(minPos.x - margin, minPos.y - margin),
                new Vector2(maxPos.x + margin, maxPos.y + margin)
            );
        }
        #endregion

        #region Core Gameplay Logic
        public bool TryPlaceBlock(List<Vector2Int> targetCoords, List<CellData> sourceData, Color blockColor)
        {
            if (_isProcessing) return false;
            if (targetCoords == null || sourceData == null) return false;
            if (targetCoords.Count != sourceData.Count) return false;

            HashSet<Vector2Int> uniqueCoords = new HashSet<Vector2Int>(targetCoords);
            if (uniqueCoords.Count != targetCoords.Count) return false;

            for (int i = 0; i < targetCoords.Count; i++)
            {
                Vector2Int coord = targetCoords[i];

                if (!IsValidCoordinate(coord.x, coord.y)) return false;
                if (_gridData[coord.x, coord.y].IsOccupied) return false;
            }

            for (int i = 0; i < targetCoords.Count; i++)
            {
                Vector2Int coord = targetCoords[i];
                CellData source = sourceData[i];

                _gridData[coord.x, coord.y].SetData(source.cellType, source.toolType);
                _gridViews[coord.x, coord.y].Init(_gridData[coord.x, coord.y], blockColor);
            }

            StartCoroutine(ResolveBoardRoutine());
            return true;
        }

        private IEnumerator ResolveBoardRoutine()
        {
            _isProcessing = true;

            yield return null;

            yield return StartCoroutine(_boardResolver.Resolve(
                _gridData,
                _gridViews,
                _width,
                _height,
                GetWorldPosition,
                OnBoardResolved
            ));

            _isProcessing = false;
        }

        private void OnBoardResolved(BoardResolveResult result)
        {
            if (result == null)
            {
                GameEvents.OnMoveCompleted?.Invoke(0, Vector3.zero);
                return;
            }

            if (result.HasAnyClear)
            {
                Debug.Log($"Cleared {result.TotalLines} lines.");
                GameEvents.OnMoveCompleted?.Invoke(result.TotalLines, result.EffectCenter);
            }
            else
            {
                GameEvents.OnMoveCompleted?.Invoke(0, Vector3.zero);
            }
        }
        #endregion

        #region Ghost / Shadow Logic
        public void ShowGhostPreview(PlacementPreview preview)
        {
            ClearGhost();

            if (preview == null || !preview.IsValid) return;

            foreach (GridCoord coord in preview.OccupiedCoords)
            {
                if (!IsValidCoordinate(coord.X, coord.Y)) continue;

                _gridViews[coord.X, coord.Y].ShowShadowState();
                _currentShadowCells.Add(new Vector2Int(coord.X, coord.Y));
            }
        }

        public void ClearGhost()
        {
            foreach (Vector2Int coord in _currentShadowCells)
            {
                if (!IsValidCoordinate(coord.x, coord.y)) continue;
                _gridViews[coord.x, coord.y].UpdateVisual();
            }

            _currentShadowCells.Clear();
        }
        #endregion

        #region Utility
        public void ClearBoard()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _gridData[x, y].Clear();
                    _gridViews[x, y].UpdateVisual();
                }
            }
        }

        public float GetFillRate()
        {
            int occupiedCount = 0;
            foreach (GridCell cell in _gridData)
            {
                if (cell.IsOccupied) occupiedCount++;
            }

            return (float)occupiedCount / (_width * _height);
        }

        public bool CanPlaceBlockAnywhere(List<Vector2Int> shapeOffsets)
        {
            if (shapeOffsets == null || shapeOffsets.Count == 0) return false;
            return _placementService.CanPlaceAnywhere(shapeOffsets, GetBoardState());
        }
        #endregion
    }
}