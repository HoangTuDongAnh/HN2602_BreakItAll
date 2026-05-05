using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.View;
using _Game.Scripts.Data;
using _Game.Scripts.Core;
using _Game.Scripts.Logic.Placement;
using _Game.Scripts.Logic.Resolve;
using _Game.Scripts.Modes.Levels;

namespace _Game.Scripts.Logic
{
    /// <summary>
    /// Quản lý board runtime: tạo grid, chuyển đổi tọa độ, đặt block, preview và resolve line clear.
    /// Không chứa logic mode; Arcade đọc dữ liệu qua BoardResolveResult/Event.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        private const int FixedBoardWidth = LevelDefinition.FixedBoardWidth;
        private const int FixedBoardHeight = LevelDefinition.FixedBoardHeight;

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

            _width = FixedBoardWidth;
            _height = FixedBoardHeight;
            _lineClearDetector = new LineClearDetector();
            _boardResolver = new BoardResolver(_lineClearDetector, _clearStepDelay, _clearAnimationDuration);

            GenerateGrid();
        }

        private void OnDestroy()
        {
            StopResolveRoutine();
        }

        private void OnValidate()
        {
            _width = FixedBoardWidth;
            _height = FixedBoardHeight;
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
        [SerializeField] private float _clearAnimationDuration = 0.16f;

        [Header("References")]
        [SerializeField] private GridCellView _cellPrefab;
        [SerializeField] private Transform _boardContainer;
        #endregion

        #region Data Storage
        private GridCell[,] _gridData;
        private GridCellView[,] _gridViews;

        private Vector3 _originPosition;
        private readonly List<Vector2Int> _currentShadowCells = new List<Vector2Int>();
        private readonly List<Vector2Int> _currentToolPreviewCells = new List<Vector2Int>();

        private bool _isProcessing;
        private bool _lineResolveEnabled = true;
        private bool _targetPatternPlacementOnly;
        private bool _targetPatternOverlayVisible = true;
        private readonly BlockPlacementService _placementService = new BlockPlacementService();

        private LineClearDetector _lineClearDetector;
        private BoardResolver _boardResolver;
        private List<Vector2Int> _lastPlacedCells = new List<Vector2Int>();
        private Coroutine _resolveRoutine;
        #endregion

        #region Properties
        public int Width => _width;
        public int Height => _height;
        public bool IsTargetPatternOverlayVisible => _targetPatternOverlayVisible;
        #endregion

        #region Initialization
        private void GenerateGrid()
        {
            if (_gridData != null) return;

            if (_boardContainer == null)
                _boardContainer = transform;

            _gridData = new GridCell[_width, _height];
            _gridViews = new GridCellView[_width, _height];

            float step = _cellSize + _spacing;
            float totalWidth = _width * step;
            float totalHeight = _height * step;
            _originPosition = new Vector3(
                -totalWidth / 2f + _cellSize / 2f,
                -totalHeight / 2f + _cellSize / 2f,
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
            return _boardContainer.TransformPoint(_originPosition + new Vector3(x * (_cellSize + _spacing), y * (_cellSize + _spacing), 0f));
        }

        public Vector2Int GetGridPosition(Vector3 worldPosition)
        {
            Vector3 localPos = _boardContainer.InverseTransformPoint(worldPosition) - _originPosition;
            float step = _cellSize + _spacing;

            int x = Mathf.RoundToInt(localPos.x / step);
            int y = Mathf.RoundToInt(localPos.y / step);

            return new Vector2Int(x, y);
        }

        public GridCoord GetAnchorCoord(Vector3 worldPosition)
        {
            return GridCoord.FromVector2Int(GetGridPosition(worldPosition));
        }

        public Vector3 GetAnchorWorldPosition(GridCoord anchor)
        {
            return GetWorldPosition(anchor.X, anchor.Y);
        }

        public BoardState GetBoardState()
        {
            return new BoardState(
                _gridData,
                _width,
                _height,
                _targetPatternPlacementOnly ? cell => cell != null && !cell.IsTargetPatternCell : null);
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
                new Vector2(Mathf.Min(minPos.x, maxPos.x) - margin, Mathf.Min(minPos.y, maxPos.y) - margin),
                new Vector2(Mathf.Max(minPos.x, maxPos.x) + margin, Mathf.Max(minPos.y, maxPos.y) + margin)
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
                if (_targetPatternPlacementOnly && !_gridData[coord.x, coord.y].IsTargetPatternCell) return false;
            }

            _lastPlacedCells = new List<Vector2Int>(targetCoords);

            for (int i = 0; i < targetCoords.Count; i++)
            {
                Vector2Int coord = targetCoords[i];
                CellData source = sourceData[i];

                _gridData[coord.x, coord.y].SetData(source.blockCellType);
                _gridViews[coord.x, coord.y].Init(_gridData[coord.x, coord.y], blockColor);
            }

            StartResolveRoutine();
            return true;
        }


        public void ClearCellsForPuzzleReposition(IReadOnlyList<Vector2Int> coords)
        {
            if (coords == null) return;

            ClearGhost();
            ClearToolPreview();

            for (int i = 0; i < coords.Count; i++)
            {
                Vector2Int coord = coords[i];
                if (!IsValidCoordinate(coord.x, coord.y)) continue;

                _gridData[coord.x, coord.y].Clear();
                _gridViews[coord.x, coord.y].UpdateVisual();
            }
        }

        public bool TryRestoreCellsForPuzzleReposition(IReadOnlyList<Vector2Int> coords, IReadOnlyList<CellData> sourceData, Color blockColor)
        {
            if (coords == null || sourceData == null) return false;
            if (coords.Count != sourceData.Count) return false;

            for (int i = 0; i < coords.Count; i++)
            {
                Vector2Int coord = coords[i];
                if (!IsValidCoordinate(coord.x, coord.y)) return false;

                GridCell cell = _gridData[coord.x, coord.y];
                if (cell.IsOccupied) return false;
                if (_targetPatternPlacementOnly && !cell.IsTargetPatternCell) return false;
            }

            _lastPlacedCells = new List<Vector2Int>(coords);

            for (int i = 0; i < coords.Count; i++)
            {
                Vector2Int coord = coords[i];
                CellData source = sourceData[i];
                if (source == null || !source.isOccupied) continue;

                _gridData[coord.x, coord.y].SetData(source.blockCellType);
                _gridViews[coord.x, coord.y].Init(_gridData[coord.x, coord.y], blockColor);
            }

            GameEvents.RaiseBoardResolved(new BoardResolveResult { PlacedCells = new List<Vector2Int>(_lastPlacedCells) });
            GameEvents.RaiseMoveCompleted(0, Vector3.zero);
            return true;
        }

        private void StartResolveRoutine()
        {
            StopResolveRoutine();
            _resolveRoutine = StartCoroutine(ResolveBoardRoutine());
        }

        private void StopResolveRoutine()
        {
            if (_resolveRoutine != null)
            {
                StopCoroutine(_resolveRoutine);
                _resolveRoutine = null;
            }

            _isProcessing = false;
        }

        private IEnumerator ResolveBoardRoutine()
        {
            _isProcessing = true;

            yield return null;

            if (_lineResolveEnabled)
            {
                yield return StartCoroutine(_boardResolver.Resolve(
                    _gridData,
                    _gridViews,
                    _width,
                    _height,
                    GetWorldPosition,
                    _lastPlacedCells,
                    OnBoardResolved
                ));
            }
            else
            {
                OnBoardResolved(new BoardResolveResult
                {
                    PlacedCells = _lastPlacedCells != null ? new List<Vector2Int>(_lastPlacedCells) : new List<Vector2Int>()
                });
            }

            _isProcessing = false;
            _resolveRoutine = null;
        }

        private void OnBoardResolved(BoardResolveResult result)
        {
            result ??= new BoardResolveResult();

            GameEvents.RaiseBoardResolved(result);

            if (result.HasAnyClear)
            {
                Debug.Log($"Cleared {result.TotalLines} lines.");
                GameEvents.RaiseMoveCompleted(result.TotalLines, result.EffectCenter);
            }
            else
            {
                GameEvents.RaiseMoveCompleted(0, Vector3.zero);
            }
        }
        #endregion

        #region Ghost / Shadow Logic
        public void ShowGhostPreview(PlacementPreview preview)
        {
            ShowGhostPreview(preview, Color.white);
        }

        public void ShowGhostPreview(PlacementPreview preview, Color blockColor)
        {
            ClearGhost();

            if (preview == null || !preview.IsNearBoard)
                return;

            if (preview.IsValid)
            {
                PaintPreviewCells(preview.ValidCoords, true);
                if (_lineResolveEnabled)
                    ShowClearPreviewIfNeeded(preview, blockColor);
                return;
            }

            PaintPreviewCells(preview.ValidCoords, false);
            PaintPreviewCells(preview.BlockedCoords, false);
        }
        private void PaintPreviewCells(List<GridCoord> coords, bool isValid)
        {
            if (coords == null) return;

            foreach (GridCoord coord in coords)
            {
                if (!IsValidCoordinate(coord.X, coord.Y)) continue;

                _gridViews[coord.X, coord.Y].ShowPreviewState(isValid);
                Vector2Int cell = new Vector2Int(coord.X, coord.Y);
                if (!_currentShadowCells.Contains(cell))
                    _currentShadowCells.Add(cell);
            }
        }

        private void ShowClearPreviewIfNeeded(PlacementPreview preview, Color blockColor)
        {
            if (preview == null || !preview.IsValid || preview.OccupiedCoords == null) return;

            HashSet<Vector2Int> virtualPlaced = new HashSet<Vector2Int>();
            foreach (GridCoord coord in preview.OccupiedCoords)
            {
                if (!IsValidCoordinate(coord.X, coord.Y)) continue;
                virtualPlaced.Add(new Vector2Int(coord.X, coord.Y));
            }

            List<int> rowsToClear = new List<int>();
            List<int> colsToClear = new List<int>();

            for (int y = 0; y < _height; y++)
            {
                bool isFull = true;
                for (int x = 0; x < _width; x++)
                {
                    if (!_gridData[x, y].IsOccupied && !virtualPlaced.Contains(new Vector2Int(x, y)))
                    {
                        isFull = false;
                        break;
                    }
                }
                if (isFull) rowsToClear.Add(y);
            }

            for (int x = 0; x < _width; x++)
            {
                bool isFull = true;
                for (int y = 0; y < _height; y++)
                {
                    if (!_gridData[x, y].IsOccupied && !virtualPlaced.Contains(new Vector2Int(x, y)))
                    {
                        isFull = false;
                        break;
                    }
                }
                if (isFull) colsToClear.Add(x);
            }

            if (rowsToClear.Count == 0 && colsToClear.Count == 0) return;

            HashSet<Vector2Int> clearCells = new HashSet<Vector2Int>();
            foreach (int row in rowsToClear)
            {
                for (int x = 0; x < _width; x++) clearCells.Add(new Vector2Int(x, row));
            }
            foreach (int col in colsToClear)
            {
                for (int y = 0; y < _height; y++) clearCells.Add(new Vector2Int(col, y));
            }

            foreach (Vector2Int coord in clearCells)
            {
                if (!IsValidCoordinate(coord.x, coord.y)) continue;
                _gridViews[coord.x, coord.y].ShowClearPreview(blockColor);
                if (!_currentShadowCells.Contains(coord)) _currentShadowCells.Add(coord);
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

        #region Gameplay Tool API
        public bool TryPlaceSingleCell(Vector2Int coord, BlockCellType cellType, Color cellColor)
        {
            if (_isProcessing) return false;
            if (!IsValidCoordinate(coord.x, coord.y)) return false;
            if (_gridData[coord.x, coord.y].IsOccupied) return false;
            if (_targetPatternPlacementOnly && !_gridData[coord.x, coord.y].IsTargetPatternCell) return false;

            ClearToolPreview();

            _lastPlacedCells = new List<Vector2Int> { coord };
            BlockCellType resolvedType = cellType == BlockCellType.Empty ? BlockCellType.Normal : cellType;
            _gridData[coord.x, coord.y].SetData(resolvedType);
            _gridViews[coord.x, coord.y].Init(_gridData[coord.x, coord.y], cellColor);

            StartResolveRoutine();
            return true;
        }

        public bool TryExplodeSquare(Vector2Int center, int radius)
        {
            if (_isProcessing) return false;
            if (!IsValidCoordinate(center.x, center.y)) return false;

            ClearToolPreview();

            List<Vector2Int> affectedCells = GetSquareCells(center, radius);
            BoardResolveResult result = new BoardResolveResult
            {
                EffectCenter = GetWorldPosition(center.x, center.y)
            };

            bool changed = false;

            for (int i = 0; i < affectedCells.Count; i++)
            {
                Vector2Int coord = affectedCells[i];
                if (!IsValidCoordinate(coord.x, coord.y)) continue;

                GridCell cell = _gridData[coord.x, coord.y];
                if (!cell.IsOccupied && cell.ItemType == BoardItemType.None)
                    continue;

                result.ClearedCells.Add(coord);

                if (cell.IsTargetPatternCell)
                    result.ClearedTargetCells.Add(coord);

                cell.Clear();
                cell.ClearBoardItem();
                _gridViews[coord.x, coord.y].UpdateVisual();
                changed = true;
            }

            if (!changed) return false;

            GameEvents.RaiseBoardResolved(result);
            GameEvents.RaiseMoveCompleted(0, result.EffectCenter);
            return true;
        }

        public void ShowToolSquarePreview(Vector2Int center, int radius, Color previewColor)
        {
            ClearToolPreview();

            List<Vector2Int> cells = GetSquareCells(center, radius);
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int coord = cells[i];
                if (!IsValidCoordinate(coord.x, coord.y)) continue;

                _gridViews[coord.x, coord.y].ShowCustomPreview(previewColor);
                if (!_currentToolPreviewCells.Contains(coord))
                    _currentToolPreviewCells.Add(coord);
            }
        }

        public void ClearToolPreview()
        {
            for (int i = 0; i < _currentToolPreviewCells.Count; i++)
            {
                Vector2Int coord = _currentToolPreviewCells[i];
                if (!IsValidCoordinate(coord.x, coord.y)) continue;
                _gridViews[coord.x, coord.y].UpdateVisual();
            }

            _currentToolPreviewCells.Clear();
        }

        private List<Vector2Int> GetSquareCells(Vector2Int center, int radius)
        {
            radius = Mathf.Max(0, radius);
            List<Vector2Int> cells = new List<Vector2Int>();

            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                for (int y = center.y - radius; y <= center.y + radius; y++)
                    cells.Add(new Vector2Int(x, y));
            }

            return cells;
        }
        #endregion

        #region Arcade Board Marker API
        public void SetBoardItem(int x, int y, BoardItemType itemType, string itemId = null, string markerTag = null)
        {
            if (!IsValidCoordinate(x, y)) return;

            _gridData[x, y].SetBoardItem(itemType, itemId, markerTag);
            _gridViews[x, y].UpdateVisual();
        }

        public void SetTargetPatternCell(int x, int y, bool isTarget)
        {
            if (!IsValidCoordinate(x, y)) return;

            _gridData[x, y].SetTargetPatternCell(isTarget);
            _gridViews[x, y].UpdateVisual();
        }

        public GridCell GetCellData(int x, int y)
        {
            return IsValidCoordinate(x, y) ? _gridData[x, y] : null;
        }

        public void SetLineResolveEnabled(bool enabled)
        {
            _lineResolveEnabled = enabled;
        }

        public void SetTargetPatternPlacementOnly(bool enabled)
        {
            _targetPatternPlacementOnly = enabled;
        }

        public void SetTargetPatternOverlayVisible(bool visible)
        {
            _targetPatternOverlayVisible = visible;
            if (_gridViews == null) return;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_gridViews[x, y] != null)
                        _gridViews[x, y].SetTargetPatternOverlayVisible(visible);
                }
            }
        }

        public bool HasTargetPatternCells()
        {
            if (_gridData == null) return false;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_gridData[x, y] != null && _gridData[x, y].IsTargetPatternCell)
                        return true;
                }
            }

            return false;
        }
        #endregion

        #region Utility
        public void ClearBoard()
        {
            StopResolveRoutine();
            ClearGhost();
            ClearToolPreview();
            _lineResolveEnabled = true;
            _targetPatternPlacementOnly = false;
            SetTargetPatternOverlayVisible(true);

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _gridData[x, y].ClearAll();
                    _gridViews[x, y].UpdateVisual();
                }
            }
        }


        public void ApplyArcadeLevel(LevelDefinition level)
        {
            if (level == null) return;

            if (level.BoardWidth != _width || level.BoardHeight != _height)
            {
                Debug.LogWarning($"Level {level.LevelId} is {level.BoardWidth}x{level.BoardHeight}, but runtime board is fixed at {_width}x{_height}.");
            }

            ClearBoard();

            IReadOnlyList<BoardCellData> cells = level.BoardCells;
            if (cells == null) return;

            for (int i = 0; i < cells.Count; i++)
            {
                BoardCellData cell = cells[i];
                if (cell == null || !IsValidCoordinate(cell.x, cell.y)) continue;

                if (cell.occupiedAtStart)
                {
                    _gridData[cell.x, cell.y].SetData(cell.occupiedCellType);
                    _gridViews[cell.x, cell.y].Init(_gridData[cell.x, cell.y], cell.occupiedColor);
                }

                if (cell.itemType != BoardItemType.None)
                    _gridData[cell.x, cell.y].SetBoardItem(cell.itemType, cell.itemId, cell.markerTag);

                if (cell.targetPatternFilled)
                    _gridData[cell.x, cell.y].SetTargetPatternCell(true);

                _gridViews[cell.x, cell.y].UpdateVisual();
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
