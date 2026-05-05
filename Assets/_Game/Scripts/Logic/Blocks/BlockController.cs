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
    /// <summary>
    /// Runtime controller cua mot block trong queue/drag ghost.
    /// Với Puzzle mode, board cell la visual that sau khi commit; BlockController chi giu vai tro input handle/ghost tam thoi.
    /// </summary>
    public partial class BlockController : MonoBehaviour
    {
        #region Configuration
        [Header("Visual Settings")]
        [SerializeField] private float _baseSlotScale = 0.8f;
        [SerializeField] private float _dragScale = 1.0f;
        [SerializeField] private float _cellSpacing = 1.0f;
        [SerializeField] private float _slotMaxVisualCells = 3.25f;
        [SerializeField] private float _minSlotScale = 0.52f;

        [Header("Drag Feel - Follow")]
        [SerializeField] private float _dragOffsetY = 0.75f;
        [SerializeField] private float _dragFollowSmoothTime = 0.025f;
        [SerializeField] private float _maxFollowSpeed = 45f;
        [SerializeField, Range(0f, 1f)] private float _pickupCentering = 0.35f;

        [Header("Drag Feel - Scale")]
        [SerializeField] private float _scaleSmoothSpeed = 18f;
        [SerializeField] private float _pickupPunchScale = 1.06f;
        [SerializeField] private float _pickupPunchDuration = 0.08f;
        [SerializeField] private float _returnScaleOvershoot = 1.03f;

        [Header("Drag Feel - Snap")]
        [SerializeField] private bool _enableSnapAssist = true;
        [SerializeField] private float _snapPreviewDistance = 0.75f;
        [SerializeField, Range(0f, 1f)] private float _snapAssistStrength = 0.65f;
        [SerializeField] private float _previewRefreshInterval = 0.015f;

        [Header("Drag Feel - Return")]
        [SerializeField] private float _returnSmoothTime = 0.12f;
        [SerializeField] private AnimationCurve _returnEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Input")]
        [SerializeField] private LayerMask _blockLayer;
        [SerializeField] private float _doubleTapRotateThreshold = 0.28f;

        [Header("Placement Anchor")]
        [SerializeField] private Transform _anchorPoint;
        #endregion

        #region Runtime Data
        private readonly List<CellData> _runtimeData = new List<CellData>();
        private readonly List<Transform> _childCells = new List<Transform>();
        private readonly List<Vector2Int> _shapeOffsets = new List<Vector2Int>();

        private float _currentSlotScale;
        private float _targetScale;
        private Color _themeColor;
        private Vector3 _logicalAnchorLocalPosition;
        private BlockData _sourceTemplate;
        private GameObject _sourceCellPrefab;
        private bool _allowRuntimeRotation;
        private int _runtimeRotationIndex;

        private Vector2 _dragAreaMin;
        private Vector2 _dragAreaMax;
        #endregion

        #region State Variables
        private Vector3 _homePosition;
        private Transform _homeSlot;
        private bool _isDragging;
        private bool _isReturning;
        private Vector3 _dragOffset;
        private Vector3 _targetDragPosition;
        private Vector3 _dragVelocity;
        private Camera _cam;
        private Coroutine _returnRoutine;
        private Coroutine _pickupPunchRoutine;
        private float _lastPreviewTime;
        private PlacementPreview _lastPreview;
        private readonly BlockPlacementService _placementService = new BlockPlacementService();
        private readonly BlockVisualBuilder _visualBuilder = new BlockVisualBuilder();
        private float _lastTapTime = -999f;

        private bool _isGameplayToolBlock;
        private GameplayToolType _gameplayToolType = GameplayToolType.None;
        private Color _toolPreviewColor = Color.white;
        private Coroutine _spawnToolAttentionRoutine;

        private bool _hasPuzzleGridPlacement;
        private bool _isRepositioningPuzzleGridBlock;
        private bool _isPuzzlePlacedVisualHidden;
        private readonly List<Vector2Int> _currentPuzzleGridCoords = new List<Vector2Int>();
        private readonly List<CellData> _currentPuzzleGridData = new List<CellData>();
        private GridCoord _currentPuzzleGridAnchor;
        private BlockSpawner _puzzleQueueOwner;
        private int _puzzleQueueEntryIndex = -1;
        private bool _hasNotifiedPuzzleQueueUsed;

        public event Action<BlockController> OnPlaced;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _cam = Camera.main;

            if (_homePosition == Vector3.zero)
                _homePosition = transform.position;

            _targetDragPosition = transform.position;
            _targetScale = _currentSlotScale > 0f ? _currentSlotScale : transform.localScale.x;
            CalculateDragBounds();
        }

        private void Update()
        {
            if (!IsInputAllowed())
            {
                if (_isDragging) HandleInputUp();
                SmoothScale();
                return;
            }

            if (Input.GetMouseButtonDown(0)) HandleInputDown();
            else if (_isDragging && Input.GetMouseButton(0)) HandleInputDrag();
            else if (_isDragging && Input.GetMouseButtonUp(0)) HandleInputUp();

            SmoothDragFollow();
            SmoothScale();
        }
        #endregion

        #region Initialization
        public void SetHome(Transform slot)
        {
            _homeSlot = slot;
            _homePosition = slot != null ? slot.position : transform.position;
            _targetDragPosition = _homePosition;
        }

        public void ConfigurePuzzleQueue(BlockSpawner owner, int entryIndex)
        {
            _puzzleQueueOwner = owner;
            _puzzleQueueEntryIndex = entryIndex;
            _hasNotifiedPuzzleQueueUsed = false;
        }

        public void ResetAsPuzzleQueueBlock(Transform slot)
        {
            StopReturnRoutine();
            _hasPuzzleGridPlacement = false;
            _isRepositioningPuzzleGridBlock = false;
            _hasNotifiedPuzzleQueueUsed = false;
            _currentPuzzleGridCoords.Clear();
            _currentPuzzleGridData.Clear();

            SetPuzzlePlacedVisualVisible(true);
            SetHome(slot);

            Transform parentContainer = slot != null && slot.parent != null ? slot.parent : slot;
            if (parentContainer != null)
                transform.SetParent(parentContainer, true);

            Vector3 home = GetHomePosition();
            home.z = 0f;
            transform.position = home;
            _targetDragPosition = home;
            _targetScale = _currentSlotScale;
            transform.localScale = Vector3.one * _currentSlotScale;
            CalculateDragBounds();
        }

        private void CalculateDragBounds()
        {
            if (GridManager.Instance == null)
            {
                _dragAreaMin = new Vector2(-20f, -20f);
                _dragAreaMax = new Vector2(20f, 20f);
                return;
            }

            var boardBounds = GridManager.Instance.GetBoardWorldBounds();
            Vector3 home = GetHomePosition();

            float minX = Mathf.Min(boardBounds.min.x, home.x);
            float maxX = Mathf.Max(boardBounds.max.x, home.x);
            float minY = Mathf.Min(boardBounds.min.y, home.y);
            float maxY = Mathf.Max(boardBounds.max.y, home.y);

            float padding = _hasPuzzleGridPlacement ? 5.0f : 1.5f;
            _dragAreaMin = new Vector2(minX - padding, minY - padding);
            _dragAreaMax = new Vector2(maxX + padding, maxY + padding);
        }

        public void Initialize(List<CellData> injectedData, int columns, GameObject cellPrefab, Color themeColor)
        {
            _runtimeData.Clear();
            if (injectedData != null)
            {
                for (int i = 0; i < injectedData.Count; i++)
                    _runtimeData.Add(injectedData[i] != null ? injectedData[i].Clone() : new CellData());
            }

            _shapeOffsets.Clear();
            _themeColor = themeColor;

            RebuildVisualCells(columns, cellPrefab, themeColor);
        }

        public void InitializeFromTemplate(BlockData template, int rotationIndex, GameObject cellPrefab, Color themeColor, bool allowRuntimeRotation)
        {
            _sourceTemplate = template;
            _sourceCellPrefab = cellPrefab;
            _allowRuntimeRotation = allowRuntimeRotation;
            _runtimeRotationIndex = Mathf.Abs(rotationIndex) % 4;

            BlockFactory.RuntimeBlockResult runtimeBlock = BlockFactory.CreateBlockInstance(template, _runtimeRotationIndex);
            Initialize(runtimeBlock.cells, runtimeBlock.width, cellPrefab, themeColor);
        }

        public void ConfigureAsGameplayToolBlock(GameplayToolType toolType, Color previewColor)
        {
            _isGameplayToolBlock = true;
            _gameplayToolType = toolType;
            _toolPreviewColor = previewColor;
        }

        public void SetSpawnToolAttention(bool active)
        {
            if (active)
            {
                if (_spawnToolAttentionRoutine == null)
                    _spawnToolAttentionRoutine = StartCoroutine(SpawnToolAttentionRoutine());
            }
            else
            {
                if (_spawnToolAttentionRoutine != null)
                {
                    StopCoroutine(_spawnToolAttentionRoutine);
                    _spawnToolAttentionRoutine = null;
                }

                RestoreChildRendererAlpha(1f);
                _targetScale = _currentSlotScale > 0f ? _currentSlotScale : transform.localScale.x;
            }
        }

        private void RebuildVisualCells(int columns, GameObject cellPrefab, Color themeColor)
        {
            BlockVisualBuilder.BuildResult result = _visualBuilder.Rebuild(
                transform,
                _anchorPoint,
                _runtimeData,
                columns,
                cellPrefab,
                themeColor,
                _baseSlotScale,
                _minSlotScale,
                _slotMaxVisualCells,
                _cellSpacing,
                _childCells,
                _shapeOffsets
            );

            if (!result.Success)
                return;

            _currentSlotScale = result.SlotScale;
            _targetScale = _currentSlotScale;
            _logicalAnchorLocalPosition = result.LogicalAnchorLocalPosition;

            if (_hasPuzzleGridPlacement && !_isRepositioningPuzzleGridBlock)
                SetPuzzlePlacedVisualVisible(false);
        }

        public List<Vector2Int> GetShapeOffsets() => _shapeOffsets;

        public int GetOccupiedCellCount()
        {
            int count = 0;
            foreach (CellData data in _runtimeData)
            {
                if (data != null && data.isOccupied) count++;
            }
            return count;
        }
        #endregion
    }
}
