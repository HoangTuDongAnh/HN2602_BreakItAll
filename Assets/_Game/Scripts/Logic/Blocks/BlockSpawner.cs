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
        private readonly PuzzleBlockQueue _puzzleQueue = new PuzzleBlockQueue();
        private readonly GameOverMoveValidator _gameOverMoveValidator = new GameOverMoveValidator();
        private readonly ToolQueueOverrideController _toolQueueOverride = new ToolQueueOverrideController();
        private readonly NormalBlockBatchSpawner _normalBlockBatchSpawner = new NormalBlockBatchSpawner();
        private readonly BlockSpawnFactory _blockSpawnFactory = new BlockSpawnFactory();
        private BlockColorSelector _colorSelector;

        private bool _isSpawning;
        private Coroutine _spawnRoutine;
        private Coroutine _gameOverValidationRoutine;

        public bool IsGameOver { get; private set; }
        public bool IsFixedFillQueueActive => _puzzleQueue.IsActive;
        public int TotalFixedFillBlocks => _puzzleQueue.TotalBlocks;
        public int UsedFixedFillBlocks => _puzzleQueue.UsedBlocks;
        public int UnusedFixedFillBlockCount => _puzzleQueue.RemainingBlocks;
        public int CurrentFillPage => CurrentPuzzleBlockSet;
        public int TotalFillPages => TotalPuzzleBlockSets;
        public int RemainingPuzzleBlocks => _puzzleQueue.RemainingBlocks;
        public int CurrentPuzzleSetIndex => _puzzleQueue.CurrentSetIndex;
        public int CurrentPuzzleBlockSet => _puzzleQueue.CurrentSetNumber;
        public int TotalPuzzleBlockSets => _puzzleQueue.TotalSets;
        public bool CanSwitchPuzzleBlockSet => _puzzleQueue.CanSwitchSet;
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
            if (_puzzleQueue.IsActive) return false;

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
            if (_toolQueueOverride.IsActive) return;
            if (_puzzleQueue.IsActive)
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
            if (_puzzleQueue.IsActive) return false;
            if (_isSpawning || IsGameOver) return false;
            if (_spawnSlots == null || _spawnSlots.Length == 0 || _blockPrefab == null || _cellPrefab == null) return false;

            return _toolQueueOverride.BeginToolBlock(
                toolType,
                blockColor,
                previewColor,
                _spawnSlots,
                _activeBlocks,
                SpawnRuntimeOneCellBlock,
                PurgeNullBlocks,
                DestroySpawnedBlock
            );
        }

        public void CancelActiveToolAndRestoreQueue()
        {
            _toolQueueOverride.CancelAndRestore(_activeBlocks, DestroySpawnedBlock);
        }

        public void RestoreQueueAfterToolUse()
        {
            _toolQueueOverride.Restore(_activeBlocks);
            ScheduleGameOverValidation();
        }

        public bool BeginRemoveToolFeedback()
        {
            if (_isSpawning || IsGameOver) return false;

            return _toolQueueOverride.BeginRemoveToolFeedback(
                _activeBlocks,
                CancelActiveToolAndRestoreQueue,
                PurgeNullBlocks
            );
        }

        public void EndRemoveToolFeedback()
        {
            _toolQueueOverride.EndRemoveToolFeedback(_activeBlocks);
        }
        #endregion

        #region Spawn Flow
        private void HandleMoveCompleted(int totalLines, Vector3 effectCenter)
        {
            if (IsGameOver) return;
            if (_isSpawning) return;
            if (_toolQueueOverride.IsActive) return;
            if (_puzzleQueue.IsActive)
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

            if (!IsGameOver && !_toolQueueOverride.IsActive)
                SpawnBatch(runtimeConfig);

            _isSpawning = false;
            _spawnRoutine = null;

            if (!IsGameOver && !_toolQueueOverride.IsActive)
                ScheduleGameOverValidation();
        }

        private void SpawnBatch(SpawnRuntimeConfig runtimeConfig)
        {
            _normalBlockBatchSpawner.SpawnBatch(
                runtimeConfig,
                _spawnSlots,
                _spawnStrategy,
                IsArcadeSession(),
                SpawnSingleBlock
            );
        }

        private BlockController SpawnSingleBlock(Transform slot, SpawnRequest request, bool allowRuntimeRotation)
        {
            BlockController controller = _blockSpawnFactory.SpawnBlock(
                slot,
                _blockPrefab,
                _cellPrefab,
                _colorSelector,
                request,
                allowRuntimeRotation,
                OnBlockPlaced
            );

            if (controller != null)
                _activeBlocks.Add(controller);

            return controller;
        }

        private BlockController SpawnRuntimeOneCellBlock(Transform slot, Color blockColor, GameplayToolType toolType, Color previewColor)
        {
            return _blockSpawnFactory.SpawnRuntimeOneCellBlock(
                slot,
                _blockPrefab,
                _cellPrefab,
                blockColor,
                toolType,
                previewColor,
                OnBlockPlaced
            );
        }

        private void OnBlockPlaced(BlockController block)
        {
            if (block == null) return;

            block.OnPlaced -= OnBlockPlaced;
            _activeBlocks.Remove(block);

            _toolQueueOverride.MarkToolBlockPlaced(block);

            if (_puzzleQueue.TryMarkBlockUsed(block, out int fixedIndex))
            {
                RefreshFixedFillPageIfNeeded();
                GameEvents.RaisePuzzleQueueChanged();
            }
        }
        #endregion

        #region Puzzle Queue
        public void ShowNextPuzzleBlockSet()
        {
            ShowPuzzleBlockSetOffset(1);
        }

        public void ShowPreviousFillPage()
        {
            ShowPuzzleBlockSetOffset(-1);
        }

        public void ShowNextFillPage()
        {
            ShowPuzzleBlockSetOffset(1);
        }

        private void ShowPuzzleBlockSetOffset(int direction)
        {
            if (!_puzzleQueue.IsActive) return;
            if (!_puzzleQueue.TryMoveSet(direction, excludeCurrentSet: true)) return;

            SpawnPuzzleQueueSet();
            GameEvents.RaisePuzzleBlockSetSwitched();
        }

        private bool TryStartFixedFillQueue()
        {
            _puzzleQueue.Clear();

            LevelDefinition level = GameManager.Instance != null ? GameManager.Instance.ActiveArcadeLevel : null;
            int setSize = GetVisiblePuzzleBlockSetSize();
            if (!_puzzleQueue.TryInitialize(level, _spawnStrategy, setSize))
                return false;

            SpawnPuzzleQueueSet();
            return true;
        }

        private void SpawnPuzzleQueueSet()
        {
            if (!_puzzleQueue.IsActive) return;
            if (_spawnSlots == null || _spawnSlots.Length == 0 || _blockPrefab == null || _cellPrefab == null) return;

            ClearVisibleBlocks();
            _puzzleQueue.ClearVisibleBlockBindings();

            _puzzleQueue.GetCurrentSetRange(out int startIndex, out int endIndex);
            int slotIndex = 0;

            for (int i = startIndex; i < endIndex && slotIndex < _spawnSlots.Length; i++)
            {
                if (!_puzzleQueue.TryGetSpawnRequest(i, out SpawnRequest request))
                    continue;

                BlockController controller = SpawnSingleBlock(_spawnSlots[slotIndex], request, AllowsPuzzleRotation());
                if (controller != null)
                {
                    _puzzleQueue.RegisterVisibleBlock(controller, i);
                    controller.ConfigurePuzzleQueue(this, i);
                }

                slotIndex++;
            }

            if (_activeBlocks.Count == 0 && _puzzleQueue.HasUnusedBlocks)
            {
                if (_puzzleQueue.TryMoveToNextSetWithUnused())
                    SpawnPuzzleQueueSet();
                return;
            }

            GameEvents.RaisePuzzleQueueChanged();
        }

        public bool TryReturnPlacedPuzzleBlockToQueue(BlockController block, int fixedEntryIndex, Vector3 releaseWorldPosition, Transform preferredSlot)
        {
            if (!_puzzleQueue.IsActive) return false;
            if (block == null) return false;
            if (!IsWorldPositionInSpawnArea(releaseWorldPosition)) return false;

            Transform returnSlot = ResolveReturnSlot(preferredSlot);
            if (returnSlot == null) return false;
            if (!_puzzleQueue.TryMarkEntryUnused(fixedEntryIndex)) return false;

            block.ResetAsPuzzleQueueBlock(returnSlot);
            block.OnPlaced -= OnBlockPlaced;
            block.OnPlaced += OnBlockPlaced;

            if (!_activeBlocks.Contains(block))
                _activeBlocks.Add(block);

            _puzzleQueue.RegisterVisibleBlock(block, fixedEntryIndex);
            GameEvents.RaisePuzzleQueueChanged();
            return true;
        }

        private Transform ResolveReturnSlot(Transform preferredSlot)
        {
            if (preferredSlot != null && IsSlotAvailable(preferredSlot))
                return preferredSlot;

            if (_spawnSlots == null) return null;

            for (int i = 0; i < _spawnSlots.Length; i++)
            {
                Transform slot = _spawnSlots[i];
                if (slot != null && IsSlotAvailable(slot))
                    return slot;
            }

            return null;
        }

        private bool IsSlotAvailable(Transform slot)
        {
            if (slot == null) return false;

            for (int i = 0; i < _activeBlocks.Count; i++)
            {
                BlockController active = _activeBlocks[i];
                if (active == null) continue;
                if (Vector2.Distance(active.transform.position, slot.position) < 0.35f)
                    return false;
            }

            return true;
        }

        private bool IsWorldPositionInSpawnArea(Vector3 worldPosition)
        {
            if (_spawnSlots == null || _spawnSlots.Length == 0) return false;

            bool hasAny = false;
            Vector2 min = Vector2.zero;
            Vector2 max = Vector2.zero;

            for (int i = 0; i < _spawnSlots.Length; i++)
            {
                Transform slot = _spawnSlots[i];
                if (slot == null) continue;

                Vector2 pos = slot.position;
                if (!hasAny)
                {
                    min = pos;
                    max = pos;
                    hasAny = true;
                }
                else
                {
                    min = Vector2.Min(min, pos);
                    max = Vector2.Max(max, pos);
                }
            }

            if (!hasAny) return false;

            float paddingX = 1.5f;
            float paddingY = 1.1f;
            return worldPosition.x >= min.x - paddingX
                   && worldPosition.x <= max.x + paddingX
                   && worldPosition.y >= min.y - paddingY
                   && worldPosition.y <= max.y + paddingY;
        }

        private void RefreshFixedFillPageIfNeeded()
        {
            if (!_puzzleQueue.IsActive) return;

            PurgeNullBlocks();
            if (_activeBlocks.Count > 0 || !_puzzleQueue.HasUnusedBlocks)
            {
                GameEvents.RaisePuzzleQueueChanged();
                return;
            }

            if (_puzzleQueue.TryMoveToNextSetWithUnused())
                SpawnPuzzleQueueSet();
            else
                GameEvents.RaisePuzzleQueueChanged();
        }

        private bool AllowsPuzzleRotation()
        {
            LevelDefinition level = GameManager.Instance != null ? GameManager.Instance.ActiveArcadeLevel : null;
            return level == null || level.Objective == null || level.Objective.allowRotation;
        }

        private int GetVisiblePuzzleBlockSetSize()
        {
            return Mathf.Max(1, _spawnSlots != null && _spawnSlots.Length > 0 ? _spawnSlots.Length : 3);
        }
        #endregion

        #region Game Over Validation
        private void ScheduleGameOverValidation()
        {
            if (IsGameOver) return;
            if (_toolQueueOverride.IsActive) return;
            if (IsPuzzleArcadeSession()) return;

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
            if (_toolQueueOverride.IsActive) return;
            if (IsPuzzleArcadeSession()) return;

            PurgeNullBlocks();
            if (_activeBlocks.Count == 0)
            {
                if (_puzzleQueue.IsActive && !_puzzleQueue.HasUnusedBlocks)
                    TriggerGameOver();

                return;
            }

            if (!_gameOverMoveValidator.HasAnyValidMoveForActiveBlocks(_activeBlocks))
                TriggerGameOver();
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
            if (IsGameOver || _toolQueueOverride.IsActive) return;

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

        private bool IsPuzzleArcadeSession()
        {
            GameManager manager = GameManager.Instance;
            return manager != null
                   && manager.CurrentModeType == GameModeType.Arcade
                   && manager.ActiveArcadeLevel != null
                   && manager.ActiveArcadeLevel.LevelType == ArcadeLevelType.Puzzle;
        }
        #endregion

        #region Utility
        private void ClearActiveBlocks()
        {
            _toolQueueOverride.ClearAll(_activeBlocks, DestroySpawnedBlock);
            _puzzleQueue.Clear();
        }

        private void ClearVisibleBlocks()
        {
            for (int i = 0; i < _activeBlocks.Count; i++)
            {
                if (_activeBlocks[i] == null) continue;
                DestroySpawnedBlock(_activeBlocks[i]);
            }

            if (_toolQueueOverride.ActiveToolBlock != null)
                DestroySpawnedBlock(_toolQueueOverride.ActiveToolBlock);

            _activeBlocks.Clear();
            _puzzleQueue.ClearVisibleBlockBindings();
        }

        private void DestroySpawnedBlock(BlockController block)
        {
            if (block == null) return;

            block.OnPlaced -= OnBlockPlaced;
            block.SetSpawnToolAttention(false);
            Destroy(block.gameObject);
        }

        private void PurgeNullBlocks()
        {
            _activeBlocks.RemoveAll(block => block == null);
            _toolQueueOverride.PurgeNullBlocks();
        }
        #endregion
    }
}
