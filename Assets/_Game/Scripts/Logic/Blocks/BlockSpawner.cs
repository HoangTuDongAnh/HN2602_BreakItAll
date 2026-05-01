using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Data;
using _Game.Scripts.Core;
using _Game.Scripts.Logic.Spawn;
using _Game.Scripts.Logic.Tools;
using _Game.Scripts.Modes;
using _Game.Scripts.Modes.Levels;

namespace _Game.Scripts.Logic
{
    public class BlockSpawner : MonoBehaviour
    {
        #region Singleton
        public static BlockSpawner Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            _colorSelector = new BlockColorSelector(_blockPalette);
        }
        #endregion

        #region Config
        [Header("References")]
        [SerializeField] private Transform[] _spawnSlots;
        [SerializeField] private GameObject _blockPrefab;
        [SerializeField] private GameObject _cellPrefab;
        [SerializeField] private SmartSpawnStrategy _spawnStrategy;

        [Header("Legacy Theme Palette")]
        [SerializeField] private List<Color> _blockPalette = new List<Color>();

        [Header("Runtime Settings")]
        [SerializeField] private SpawnRuntimeConfig _runtimeConfig = new SpawnRuntimeConfig();
        #endregion

        #region Runtime
        private readonly List<BlockController> _activeBlocks = new List<BlockController>();
        private readonly List<BlockController> _storedToolQueueBlocks = new List<BlockController>();
        private readonly List<FixedFillEntry> _fixedFillEntries = new List<FixedFillEntry>();
        private readonly Dictionary<BlockController, int> _fixedFillBlockIndices = new Dictionary<BlockController, int>();
        private BlockColorSelector _colorSelector;
        private BlockController _activeToolBlock;

        private bool _isSpawning;
        private bool _isToolQueueOverrideActive;
        private bool _isFixedFillQueueActive;
        private int _fixedFillPageStartIndex;
        private Coroutine _spawnRoutine;
        private Coroutine _gameOverValidationRoutine;

        public bool IsGameOver { get; private set; }
        public bool IsFixedFillQueueActive => _isFixedFillQueueActive;
        public int TotalFixedFillBlocks => _fixedFillEntries.Count;
        public int UsedFixedFillBlocks => CountUsedFixedFillBlocks();
        public int UnusedFixedFillBlockCount => Mathf.Max(0, TotalFixedFillBlocks - UsedFixedFillBlocks);
        public int CurrentFillPage => GetFillPageCount() <= 0 ? 0 : (_fixedFillPageStartIndex / GetVisibleFillPageSize()) + 1;
        public int TotalFillPages => GetFillPageCount();
        #endregion

        #region Lifecycle
        private void Start()
        {
            GameEvents.OnMoveCompleted += HandleMoveCompleted;
        }

        private void OnDestroy()
        {
            GameEvents.OnMoveCompleted -= HandleMoveCompleted;
            StopSpawnRoutine();
            StopGameOverValidationRoutine();
        }
        #endregion

        #region Public API
        public void SpawnInitialBatch()
        {
            StopSpawnRoutine();
            StopGameOverValidationRoutine();
            _spawnStrategy?.ResetRuntimeState();
            ClearActiveBlocks();
            if (TryStartFixedFillQueue())
                return;

            StartSpawnRoutine();
        }

        public void ResetSpawner()
        {
            StopSpawnRoutine();
            StopGameOverValidationRoutine();
            _spawnStrategy?.ResetRuntimeState();
            IsGameOver = false;
            ClearActiveBlocks();
            if (TryStartFixedFillQueue())
                return;

            StartSpawnRoutine();
        }

        public void ClearSpawner()
        {
            IsGameOver = false;
            StopSpawnRoutine();
            StopGameOverValidationRoutine();
            ClearActiveBlocks();
        }

        public bool RemoveSpawnBlock(BlockController block, bool refillWhenQueueEmpty)
        {
            if (block == null) return false;
            if (_isFixedFillQueueActive) return false;

            PurgeNullBlocks();
            if (!_activeBlocks.Contains(block)) return false;

            block.SetSpawnToolAttention(false);
            block.OnPlaced -= OnBlockPlaced;
            _activeBlocks.Remove(block);
            Destroy(block.gameObject);

            if (refillWhenQueueEmpty && _activeBlocks.Count == 0 && !_isSpawning && !IsGameOver)
                StartSpawnRoutine();
            else
                ScheduleGameOverValidation();

            return true;
        }

        public void CheckAndSpawnIfNeeded()
        {
            if (IsGameOver) return;
            if (_isSpawning) return;
            if (_isToolQueueOverrideActive) return;
            if (_isFixedFillQueueActive)
            {
                RefreshFixedFillPageIfNeeded();
                return;
            }

            PurgeNullBlocks();

            if (_activeBlocks.Count == 0)
            {
                StartSpawnRoutine();
                return;
            }

            ScheduleGameOverValidation();
        }
        #endregion

        #region Gameplay Tool Queue Override
        public bool BeginToolBlock(GameplayToolType toolType, Color blockColor, Color previewColor)
        {
            if (_isFixedFillQueueActive) return false;
            if (_isSpawning || IsGameOver) return false;
            if (_spawnSlots == null || _spawnSlots.Length == 0 || _blockPrefab == null || _cellPrefab == null) return false;

            CancelActiveToolAndRestoreQueue();
            PurgeNullBlocks();

            _storedToolQueueBlocks.Clear();
            _storedToolQueueBlocks.AddRange(_activeBlocks);

            for (int i = 0; i < _storedToolQueueBlocks.Count; i++)
            {
                if (_storedToolQueueBlocks[i] != null)
                    _storedToolQueueBlocks[i].gameObject.SetActive(false);
            }

            _activeBlocks.Clear();
            _isToolQueueOverrideActive = true;

            Transform middleSlot = _spawnSlots[Mathf.Clamp(_spawnSlots.Length / 2, 0, _spawnSlots.Length - 1)];
            if (middleSlot == null)
            {
                RestoreStoredQueueBlocks();
                return false;
            }

            _activeToolBlock = SpawnRuntimeOneCellBlock(middleSlot, blockColor, toolType, previewColor);
            if (_activeToolBlock == null)
            {
                RestoreStoredQueueBlocks();
                return false;
            }

            _activeBlocks.Add(_activeToolBlock);
            return true;
        }

        public void CancelActiveToolAndRestoreQueue()
        {
            if (_activeToolBlock != null)
            {
                _activeToolBlock.OnPlaced -= OnBlockPlaced;
                Destroy(_activeToolBlock.gameObject);
                _activeToolBlock = null;
            }

            RestoreStoredQueueBlocks();
        }

        public void RestoreQueueAfterToolUse()
        {
            RestoreStoredQueueBlocks();
            ScheduleGameOverValidation();
        }

        public bool BeginRemoveToolFeedback()
        {
            if (_isSpawning || IsGameOver) return false;

            CancelActiveToolAndRestoreQueue();
            PurgeNullBlocks();

            for (int i = 0; i < _activeBlocks.Count; i++)
            {
                if (_activeBlocks[i] != null)
                    _activeBlocks[i].SetSpawnToolAttention(true);
            }

            return _activeBlocks.Count > 0;
        }

        public void EndRemoveToolFeedback()
        {
            for (int i = 0; i < _activeBlocks.Count; i++)
            {
                if (_activeBlocks[i] != null)
                    _activeBlocks[i].SetSpawnToolAttention(false);
            }
        }
        #endregion

        #region Spawn Flow
        private void HandleMoveCompleted(int totalLines, Vector3 effectCenter)
        {
            if (IsGameOver) return;
            if (_isSpawning) return;
            if (_isToolQueueOverrideActive) return;
            if (_isFixedFillQueueActive)
            {
                RefreshFixedFillPageIfNeeded();
                ScheduleGameOverValidation();
                return;
            }

            PurgeNullBlocks();

            if (_activeBlocks.Count == 0)
            {
                StartSpawnRoutine();
                return;
            }

            ScheduleGameOverValidation();
        }

        private IEnumerator SpawnNextBatchRoutine()
        {
            SpawnRuntimeConfig runtimeConfig = GetEffectiveRuntimeConfig();
            _isSpawning = true;

            float delay = runtimeConfig != null ? runtimeConfig.SpawnDelay : 0.5f;
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (!IsGameOver && !_isToolQueueOverrideActive)
                SpawnBatch(runtimeConfig);

            _isSpawning = false;
            _spawnRoutine = null;

            if (!IsGameOver && !_isToolQueueOverrideActive)
                ScheduleGameOverValidation();
        }

        private void SpawnBatch(SpawnRuntimeConfig runtimeConfig)
        {
            if (_spawnSlots == null || _spawnSlots.Length == 0)
            {
                Debug.LogError("BlockSpawner: Missing spawn slots!");
                return;
            }

            if (_spawnStrategy == null)
            {
                Debug.LogError("BlockSpawner: Missing spawn strategy!");
                return;
            }

            int targetCount = Mathf.Min(runtimeConfig != null ? runtimeConfig.BatchSize : 3, _spawnSlots.Length);

            BoardStateSnapshot boardSnapshot = GridManager.Instance != null
                ? BoardStateSnapshot.FromBoardState(GridManager.Instance.GetBoardState())
                : BoardStateSnapshot.Empty(8, 8);

            var spawnContext = new SpawnSelectionContext(boardSnapshot, runtimeConfig, IsArcadeSession());
            List<SpawnRequest> batch = _spawnStrategy.GetNextBatch(targetCount, spawnContext);

            if (batch == null || batch.Count == 0)
            {
                Debug.LogError("BlockSpawner: Spawn strategy returned empty batch!");
                return;
            }

            for (int i = 0; i < targetCount && i < batch.Count; i++)
            {
                SpawnRequest request = batch[i];
                Transform slot = _spawnSlots[i];

                if (request.ShapeData == null || slot == null) continue;

                SpawnSingleBlock(slot, request, allowRuntimeRotation: false);
            }
        }

        private BlockController SpawnSingleBlock(Transform slot, SpawnRequest request, bool allowRuntimeRotation)
        {
            Transform parentContainer = slot.parent != null ? slot.parent : slot;

            GameObject blockObj = Instantiate(_blockPrefab, slot.position, Quaternion.identity, parentContainer);
            blockObj.transform.position = slot.position;
            blockObj.transform.localScale = Vector3.one;

            BlockController controller = blockObj.GetComponent<BlockController>();
            if (controller == null)
            {
                Debug.LogError("BlockSpawner: Block prefab missing BlockController!");
                Destroy(blockObj);
                return null;
            }

            Color blockColor = _colorSelector != null ? _colorSelector.GetRandomColor() : Color.white;

            controller.SetHome(slot);
            controller.InitializeFromTemplate(request.ShapeData, request.RotationIndex, _cellPrefab, blockColor, allowRuntimeRotation);
            controller.OnPlaced += OnBlockPlaced;

            _activeBlocks.Add(controller);
            return controller;
        }

        private BlockController SpawnRuntimeOneCellBlock(Transform slot, Color blockColor, GameplayToolType toolType, Color previewColor)
        {
            Transform parentContainer = slot.parent != null ? slot.parent : slot;
            GameObject blockObj = Instantiate(_blockPrefab, slot.position, Quaternion.identity, parentContainer);
            blockObj.transform.position = slot.position;
            blockObj.transform.localScale = Vector3.one;

            BlockController controller = blockObj.GetComponent<BlockController>();
            if (controller == null)
            {
                Destroy(blockObj);
                return null;
            }

            List<CellData> cells = new List<CellData>
            {
                new CellData { isOccupied = true, blockCellType = BlockCellType.Normal }
            };

            controller.SetHome(slot);
            controller.Initialize(cells, 1, _cellPrefab, blockColor);
            controller.ConfigureAsGameplayToolBlock(toolType, previewColor);
            controller.OnPlaced += OnBlockPlaced;
            return controller;
        }

        private void OnBlockPlaced(BlockController block)
        {
            if (block == null) return;

            block.OnPlaced -= OnBlockPlaced;
            _activeBlocks.Remove(block);

            if (block == _activeToolBlock)
                _activeToolBlock = null;

            if (_fixedFillBlockIndices.TryGetValue(block, out int fixedIndex))
            {
                _fixedFillBlockIndices.Remove(block);
                if (fixedIndex >= 0 && fixedIndex < _fixedFillEntries.Count)
                    _fixedFillEntries[fixedIndex].Used = true;

                RefreshFixedFillPageIfNeeded();
                GameEvents.RaiseFillQueueChanged();
            }
        }
        #endregion

        #region Fill Fixed Queue
        public void ShowPreviousFillPage()
        {
            if (!_isFixedFillQueueActive) return;

            int pageSize = GetVisibleFillPageSize();
            int totalPages = GetFillPageCount();
            if (totalPages <= 1) return;

            int currentPage = Mathf.Clamp(_fixedFillPageStartIndex / pageSize, 0, totalPages - 1);
            int nextPage = (currentPage - 1 + totalPages) % totalPages;
            _fixedFillPageStartIndex = nextPage * pageSize;
            SpawnFixedFillPage();
        }

        public void ShowNextFillPage()
        {
            if (!_isFixedFillQueueActive) return;

            int pageSize = GetVisibleFillPageSize();
            int totalPages = GetFillPageCount();
            if (totalPages <= 1) return;

            int currentPage = Mathf.Clamp(_fixedFillPageStartIndex / pageSize, 0, totalPages - 1);
            int nextPage = (currentPage + 1) % totalPages;
            _fixedFillPageStartIndex = nextPage * pageSize;
            SpawnFixedFillPage();
        }

        private bool TryStartFixedFillQueue()
        {
            _isFixedFillQueueActive = false;
            _fixedFillEntries.Clear();
            _fixedFillBlockIndices.Clear();
            _fixedFillPageStartIndex = 0;

            LevelDefinition level = GameManager.Instance != null ? GameManager.Instance.ActiveArcadeLevel : null;
            if (level == null || level.LevelType != ArcadeLevelType.Fill)
                return false;

            ObjectiveDefinition objective = level.Objective;
            if (objective == null || objective.providedShapeIds == null || objective.providedShapeIds.Count == 0)
                return false;

            if (_spawnStrategy == null)
            {
                Debug.LogError("BlockSpawner: Fill level needs a SmartSpawnStrategy to resolve provided shape ids.");
                return false;
            }

            for (int i = 0; i < objective.providedShapeIds.Count; i++)
            {
                string shapeId = objective.providedShapeIds[i];
                BlockData block = _spawnStrategy.FindBlockById(shapeId);
                if (block == null)
                {
                    Debug.LogWarning($"BlockSpawner: Fill shape id '{shapeId}' was not found in spawn strategy.");
                    continue;
                }

                _fixedFillEntries.Add(new FixedFillEntry
                {
                    ShapeData = block,
                    RotationIndex = 0,
                    Used = false
                });
            }

            if (_fixedFillEntries.Count == 0)
                return false;

            _isFixedFillQueueActive = true;
            SpawnFixedFillPage();
            return true;
        }

        private void SpawnFixedFillPage()
        {
            if (!_isFixedFillQueueActive) return;
            if (_spawnSlots == null || _spawnSlots.Length == 0 || _blockPrefab == null || _cellPrefab == null) return;

            ClearVisibleBlocks();
            _fixedFillBlockIndices.Clear();

            int pageSize = GetVisibleFillPageSize();
            int end = Mathf.Min(_fixedFillEntries.Count, _fixedFillPageStartIndex + pageSize);
            int slotIndex = 0;

            for (int i = _fixedFillPageStartIndex; i < end && slotIndex < _spawnSlots.Length; i++)
            {
                FixedFillEntry entry = _fixedFillEntries[i];
                if (entry == null || entry.Used || entry.ShapeData == null) continue;

                SpawnRequest request = new SpawnRequest
                {
                    ShapeData = entry.ShapeData,
                    RotationIndex = entry.RotationIndex
                };

                BlockController controller = SpawnSingleBlock(_spawnSlots[slotIndex], request, AllowsFillRotation());
                if (controller != null)
                    _fixedFillBlockIndices[controller] = i;

                slotIndex++;
            }

            if (_activeBlocks.Count == 0 && HasUnusedFixedFillBlocks())
            {
                _fixedFillPageStartIndex = FindNextPageWithUnusedBlocks(_fixedFillPageStartIndex);
                if (_fixedFillPageStartIndex >= 0)
                    SpawnFixedFillPage();
                return;
            }

            GameEvents.RaiseFillQueueChanged();
        }

        private void RefreshFixedFillPageIfNeeded()
        {
            if (!_isFixedFillQueueActive) return;

            PurgeNullBlocks();
            if (_activeBlocks.Count > 0 || !HasUnusedFixedFillBlocks())
            {
                GameEvents.RaiseFillQueueChanged();
                return;
            }

            int nextPage = FindNextPageWithUnusedBlocks(_fixedFillPageStartIndex);
            if (nextPage >= 0)
            {
                _fixedFillPageStartIndex = nextPage;
                SpawnFixedFillPage();
            }
            else
            {
                GameEvents.RaiseFillQueueChanged();
            }
        }

        private int FindNextPageWithUnusedBlocks(int startIndex)
        {
            int pageSize = GetVisibleFillPageSize();
            int totalPages = GetFillPageCount();
            if (totalPages <= 0) return -1;

            int currentPage = Mathf.Clamp(startIndex / pageSize, 0, totalPages - 1);
            for (int offset = 1; offset <= totalPages; offset++)
            {
                int page = (currentPage + offset) % totalPages;
                int pageStart = page * pageSize;
                int pageEnd = Mathf.Min(_fixedFillEntries.Count, pageStart + pageSize);

                for (int i = pageStart; i < pageEnd; i++)
                {
                    if (_fixedFillEntries[i] != null && !_fixedFillEntries[i].Used)
                        return pageStart;
                }
            }

            return -1;
        }

        private bool AllowsFillRotation()
        {
            LevelDefinition level = GameManager.Instance != null ? GameManager.Instance.ActiveArcadeLevel : null;
            return level == null || level.Objective == null || level.Objective.allowRotation;
        }

        private bool HasUnusedFixedFillBlocks()
        {
            for (int i = 0; i < _fixedFillEntries.Count; i++)
            {
                if (_fixedFillEntries[i] != null && !_fixedFillEntries[i].Used)
                    return true;
            }

            return false;
        }

        private int CountUsedFixedFillBlocks()
        {
            int count = 0;
            for (int i = 0; i < _fixedFillEntries.Count; i++)
            {
                if (_fixedFillEntries[i] != null && _fixedFillEntries[i].Used)
                    count++;
            }

            return count;
        }

        private int GetVisibleFillPageSize()
        {
            return Mathf.Max(1, _spawnSlots != null && _spawnSlots.Length > 0 ? _spawnSlots.Length : 3);
        }

        private int GetFillPageCount()
        {
            int pageSize = GetVisibleFillPageSize();
            return _fixedFillEntries.Count <= 0 ? 0 : Mathf.CeilToInt(_fixedFillEntries.Count / (float)pageSize);
        }
        #endregion

        #region Game Over Validation
        private void ScheduleGameOverValidation()
        {
            if (IsGameOver) return;
            if (_isToolQueueOverrideActive) return;

            StopGameOverValidationRoutine();

            _gameOverValidationRoutine = StartCoroutine(ValidateGameOverAfterBoardSettlesRoutine());
        }

        private IEnumerator ValidateGameOverAfterBoardSettlesRoutine()
        {
            while (_isSpawning)
                yield return null;

            yield return new WaitForEndOfFrame();
            yield return null;

            _gameOverValidationRoutine = null;
            CheckGameOverConditionImmediate();
        }

        private void CheckGameOverConditionImmediate()
        {
            if (IsGameOver) return;
            if (_isToolQueueOverrideActive) return;

            PurgeNullBlocks();
            if (_activeBlocks.Count == 0)
            {
                if (_isFixedFillQueueActive && !HasUnusedFixedFillBlocks())
                    TriggerGameOver();

                return;
            }

            if (!HasAnyValidMoveForActiveBlocks())
                TriggerGameOver();
        }

        private bool HasAnyValidMoveForActiveBlocks()
        {
            for (int i = 0; i < _activeBlocks.Count; i++)
            {
                BlockController block = _activeBlocks[i];
                if (block == null) continue;

                if (CanBlockFitAnywhere(block))
                    return true;
            }

            return false;
        }

        private bool CanBlockFitAnywhere(BlockController block)
        {
            if (block == null) return false;

            List<Vector2Int> shapeOffsets = block.GetShapeOffsets();
            if (shapeOffsets == null || shapeOffsets.Count == 0) return false;

            return GridManager.Instance != null && GridManager.Instance.CanPlaceBlockAnywhere(shapeOffsets);
        }

        private void TriggerGameOver()
        {
            if (IsGameOver) return;

            IsGameOver = true;

            if (GameManager.Instance != null)
                GameManager.Instance.TriggerGameOver();
        }
        #endregion

        #region Config Helpers
        private void StartSpawnRoutine()
        {
            if (_spawnRoutine != null) return;
            if (IsGameOver || _isToolQueueOverrideActive) return;

            _spawnRoutine = StartCoroutine(SpawnNextBatchRoutine());
        }

        private void StopSpawnRoutine()
        {
            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }

            _isSpawning = false;
        }

        private void StopGameOverValidationRoutine()
        {
            if (_gameOverValidationRoutine == null) return;

            StopCoroutine(_gameOverValidationRoutine);
            _gameOverValidationRoutine = null;
        }

        private SpawnRuntimeConfig GetEffectiveRuntimeConfig()
        {
            SpawnRuntimeConfig baseConfig = _runtimeConfig ?? new SpawnRuntimeConfig();
            if (!IsArcadeSession())
                return baseConfig;

            LevelDefinition level = GameManager.Instance != null ? GameManager.Instance.ActiveArcadeLevel : null;
            return level != null ? SpawnRuntimeConfig.FromArcadeProfile(baseConfig, level.SpawnProfileOverride) : baseConfig;
        }

        private bool IsArcadeSession()
        {
            return GameManager.Instance != null && GameManager.Instance.CurrentModeType == GameModeType.Arcade;
        }
        #endregion

        #region Utility
        private void RestoreStoredQueueBlocks()
        {
            if (!_isToolQueueOverrideActive && _storedToolQueueBlocks.Count == 0) return;

            if (_activeToolBlock != null)
            {
                _activeToolBlock.OnPlaced -= OnBlockPlaced;
                Destroy(_activeToolBlock.gameObject);
                _activeToolBlock = null;
            }

            _activeBlocks.Clear();
            for (int i = 0; i < _storedToolQueueBlocks.Count; i++)
            {
                BlockController block = _storedToolQueueBlocks[i];
                if (block == null) continue;

                block.gameObject.SetActive(true);
                block.SetSpawnToolAttention(false);
                if (!_activeBlocks.Contains(block))
                    _activeBlocks.Add(block);
            }

            _storedToolQueueBlocks.Clear();
            _isToolQueueOverrideActive = false;
        }

        private void ClearActiveBlocks()
        {
            ClearVisibleBlocks();

            for (int i = 0; i < _storedToolQueueBlocks.Count; i++)
            {
                if (_storedToolQueueBlocks[i] == null) continue;

                _storedToolQueueBlocks[i].OnPlaced -= OnBlockPlaced;
                _storedToolQueueBlocks[i].SetSpawnToolAttention(false);
                Destroy(_storedToolQueueBlocks[i].gameObject);
            }

            if (_activeToolBlock != null)
            {
                _activeToolBlock.OnPlaced -= OnBlockPlaced;
                Destroy(_activeToolBlock.gameObject);
            }

            _storedToolQueueBlocks.Clear();
            _activeToolBlock = null;
            _isToolQueueOverrideActive = false;
            _isFixedFillQueueActive = false;
            _fixedFillEntries.Clear();
            _fixedFillBlockIndices.Clear();
            _fixedFillPageStartIndex = 0;
        }

        private void ClearVisibleBlocks()
        {
            for (int i = 0; i < _activeBlocks.Count; i++)
            {
                if (_activeBlocks[i] == null) continue;

                _activeBlocks[i].OnPlaced -= OnBlockPlaced;
                _activeBlocks[i].SetSpawnToolAttention(false);
                Destroy(_activeBlocks[i].gameObject);
            }

            if (_activeToolBlock != null)
            {
                _activeToolBlock.OnPlaced -= OnBlockPlaced;
                Destroy(_activeToolBlock.gameObject);
            }

            _activeBlocks.Clear();
            _activeToolBlock = null;
            _fixedFillBlockIndices.Clear();
        }

        private void PurgeNullBlocks()
        {
            _activeBlocks.RemoveAll(block => block == null);
            _storedToolQueueBlocks.RemoveAll(block => block == null);
        }
        #endregion

        private sealed class FixedFillEntry
        {
            public BlockData ShapeData;
            public int RotationIndex;
            public bool Used;
        }
    }
}
