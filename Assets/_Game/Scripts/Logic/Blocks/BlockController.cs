using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.View;
using _Game.Scripts.Data;
using _Game.Scripts.Core;
using _Game.Scripts.Logic.Placement;
using _Game.Scripts.Logic.Tools;

namespace _Game.Scripts.Logic
{
    /// <summary>
    /// Runtime controller cua mot block trong queue.
    /// Chiu trach nhiem dung visual, drag feel, preview placement va gui yeu cau dat block.
    /// </summary>
    public class BlockController : MonoBehaviour
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
        private float _lastTapTime = -999f;

        private bool _isGameplayToolBlock;
        private GameplayToolType _gameplayToolType = GameplayToolType.None;
        private Color _toolPreviewColor = Color.white;
        private Coroutine _spawnToolAttentionRoutine;

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

            float padding = 1.5f;
            _dragAreaMin = new Vector2(minX - padding, minY - padding);
            _dragAreaMax = new Vector2(maxX + padding, maxY + padding);
        }

        public void Initialize(List<CellData> injectedData, int columns, GameObject cellPrefab, Color themeColor)
        {
            _runtimeData.Clear();
            if (injectedData != null)
                _runtimeData.AddRange(injectedData);

            _shapeOffsets.Clear();
            _themeColor = themeColor;

            ClearOldVisuals();
            BuildVisualCells(columns, cellPrefab, themeColor);
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

        private void ClearOldVisuals()
        {
            foreach (Transform child in transform)
            {
                if (_anchorPoint != null && child == _anchorPoint) continue;
                Destroy(child.gameObject);
            }

            _childCells.Clear();
        }

        private void BuildVisualCells(int columns, GameObject cellPrefab, Color themeColor)
        {
            if (_runtimeData.Count == 0 || columns <= 0 || cellPrefab == null)
                return;

            List<int> occupiedIndices = CollectOccupiedIndices(columns, out int minX, out int maxX, out int minY, out int maxY);
            if (occupiedIndices.Count == 0) return;

            int blockWidth = maxX - minX + 1;
            int blockHeight = maxY - minY + 1;
            _currentSlotScale = CalculateSlotScale(blockWidth, blockHeight);
            _targetScale = _currentSlotScale;
            transform.localScale = Vector3.one * _currentSlotScale;

            float visualCenterX = (minX + maxX) / 2.0f;
            float visualCenterY = (minY + maxY) / 2.0f;
            Vector2Int logicAnchor = ResolveBottomLeftAnchor(occupiedIndices, columns);
            _logicalAnchorLocalPosition = new Vector3(
                (logicAnchor.x - visualCenterX) * _cellSpacing,
                (logicAnchor.y - visualCenterY) * _cellSpacing,
                0f
            );

            if (_anchorPoint != null)
                _anchorPoint.localPosition = _logicalAnchorLocalPosition;

            foreach (int index in occupiedIndices)
            {
                CellData cell = _runtimeData[index];
                int x = index % columns;
                int y = index / columns;

                GameObject cellObj = Instantiate(cellPrefab, transform);
                cellObj.transform.localPosition = new Vector3(
                    (x - visualCenterX) * _cellSpacing,
                    (y - visualCenterY) * _cellSpacing,
                    0f
                );

                _shapeOffsets.Add(new Vector2Int(x - logicAnchor.x, y - logicAnchor.y));
                InitCellView(cellObj, cell, themeColor);
                _childCells.Add(cellObj.transform);
            }
        }

        private float CalculateSlotScale(int blockWidth, int blockHeight)
        {
            float maxDimension = Mathf.Max(1f, Mathf.Max(blockWidth, blockHeight));
            float maxVisualCells = Mathf.Max(1f, _slotMaxVisualCells);
            float scaleMultiplier = Mathf.Min(1f, maxVisualCells / maxDimension);
            float scale = _baseSlotScale * scaleMultiplier;
            return Mathf.Max(_minSlotScale, scale);
        }

        private List<int> CollectOccupiedIndices(int columns, out int minX, out int maxX, out int minY, out int maxY)
        {
            minX = int.MaxValue;
            maxX = int.MinValue;
            minY = int.MaxValue;
            maxY = int.MinValue;

            List<int> occupied = new List<int>();

            for (int i = 0; i < _runtimeData.Count; i++)
            {
                if (_runtimeData[i] == null || !_runtimeData[i].isOccupied) continue;

                occupied.Add(i);
                int col = i % columns;
                int row = i / columns;

                minX = Mathf.Min(minX, col);
                maxX = Mathf.Max(maxX, col);
                minY = Mathf.Min(minY, row);
                maxY = Mathf.Max(maxY, row);
            }

            return occupied;
        }

        private Vector2Int ResolveBottomLeftAnchor(List<int> occupiedIndices, int columns)
        {
            Vector2Int best = Vector2Int.zero;
            bool hasBest = false;

            foreach (int index in occupiedIndices)
            {
                Vector2Int candidate = new Vector2Int(index % columns, index / columns);
                if (!hasBest || candidate.y < best.y || (candidate.y == best.y && candidate.x < best.x))
                {
                    best = candidate;
                    hasBest = true;
                }
            }

            return best;
        }

        private void InitCellView(GameObject cellObj, CellData cell, Color themeColor)
        {
            GridCellView view = cellObj.GetComponent<GridCellView>();
            if (view == null) return;

            GridCell tempCell = new GridCell(0, 0);
            tempCell.SetData(cell.blockCellType);
            view.Init(tempCell, themeColor);
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
                RotateRuntimeBlock();
                return;
            }

            if (_isReturning) return;

            if (ToolController.Instance != null && ToolController.Instance.ActiveTool == GameplayToolType.RemoveSpawnBlock)
            {
                ToolController.Instance.TryRemoveSpawnBlock(this);
                return;
            }

            _lastTapTime = Time.unscaledTime;

            StopReturnRoutine();
            CalculateDragBounds();

            _isDragging = true;
            _lastPreview = null;
            _lastPreviewTime = -999f;

            Vector3 rawOffset = transform.position - mousePos;
            _dragOffset = Vector3.Lerp(rawOffset, Vector3.zero, _pickupCentering);
            _targetScale = _dragScale;

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
            if (_blockLayer.value == 0)
                return Physics2D.Raycast(worldPos, Vector2.zero);

            return Physics2D.Raycast(worldPos, Vector2.zero, Mathf.Infinity, _blockLayer);
        }

        private Vector3 GetMouseWorldPos()
        {
            if (_cam == null) _cam = Camera.main;
            Vector3 pos = _cam != null ? _cam.ScreenToWorldPoint(Input.mousePosition) : Vector3.zero;
            pos.z = 0f;
            return pos;
        }
        #endregion

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
                ReturnToSlotSmooth();
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
                ReturnToSlotSmooth();
                return;
            }

            SnapVisuallyToPlacement(result.Anchor);

            List<Vector2Int> targetCoords = new List<Vector2Int>();
            List<CellData> activeData = new List<CellData>();

            int logicalIndex = 0;
            for (int i = 0; i < _runtimeData.Count; i++)
            {
                if (_runtimeData[i] == null || !_runtimeData[i].isOccupied) continue;

                GridCoord coord = result.OccupiedCoords[logicalIndex++];
                targetCoords.Add(coord.ToVector2Int());
                activeData.Add(_runtimeData[i]);
            }

            bool success = GridManager.Instance.TryPlaceBlock(targetCoords, activeData, _themeColor);

            if (success)
            {
                if (ScoreManager.Instance != null)
                    ScoreManager.Instance.AddPlacementScore(GetOccupiedCellCount());

                OnPlaced?.Invoke(this);
                Destroy(gameObject);
            }
            else
            {
                ReturnToSlotSmooth();
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
            _targetScale = _currentSlotScale;
            transform.localScale = Vector3.one * _currentSlotScale;
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

        #region Smooth Updates
        private void SmoothDragFollow()
        {
            if (!_isDragging) return;

            if (_dragFollowSmoothTime <= 0f)
            {
                transform.position = _targetDragPosition;
                return;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                _targetDragPosition,
                ref _dragVelocity,
                _dragFollowSmoothTime,
                _maxFollowSpeed
            );
        }

        private void SmoothScale()
        {
            if (_targetScale <= 0f) return;

            float current = transform.localScale.x;
            float next = Mathf.Lerp(current, _targetScale, Time.deltaTime * _scaleSmoothSpeed);
            transform.localScale = Vector3.one * next;
        }

        private void PlayPickupPunch()
        {
            if (_pickupPunchRoutine != null)
                StopCoroutine(_pickupPunchRoutine);

            _pickupPunchRoutine = StartCoroutine(PickupPunchRoutine());
        }

        private IEnumerator PickupPunchRoutine()
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, _pickupPunchDuration);
            float baseScale = _targetScale;
            float punchScale = baseScale * Mathf.Max(1f, _pickupPunchScale);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(t * Mathf.PI);
                transform.localScale = Vector3.one * Mathf.Lerp(baseScale, punchScale, wave);
                yield return null;
            }
        }
        #endregion

        #region Spawn Tool Attention
        private IEnumerator SpawnToolAttentionRoutine()
        {
            Vector3 baseScale = transform.localScale;

            while (true)
            {
                float wave = (Mathf.Sin(Time.time * 7.5f) + 1f) * 0.5f;
                float alpha = Mathf.Lerp(0.38f, 1f, wave);
                RestoreChildRendererAlpha(alpha);

                float scalePulse = Mathf.Lerp(0.97f, 1.03f, wave);
                transform.localScale = baseScale * scalePulse;
                yield return null;
            }
        }

        private void RestoreChildRendererAlpha(float alpha)
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Color color = renderers[i].color;
                color.a = alpha;
                renderers[i].color = color;
            }
        }
        #endregion

        #region Input Gate
        private bool IsInputAllowed()
        {
            if (GameManager.Instance == null) return true;
            return GameManager.Instance.IsInputAllowed();
        }
        #endregion
    }
}
