using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Data;
using _Game.Scripts.Core;
using _Game.Scripts.Core.Services;
using _Game.Scripts.Logic.Spawn;

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
        private BlockColorSelector _colorSelector;

        private bool _isSpawning;
        private Coroutine _gameOverValidationRoutine;

        public bool IsGameOver { get; private set; }
        #endregion

        #region Lifecycle
        private void Start()
        {
            GameEvents.OnMoveCompleted += HandleMoveCompleted;
        }

        private void OnDestroy()
        {
            GameEvents.OnMoveCompleted -= HandleMoveCompleted;
        }
        #endregion

        #region Public API
        public void SpawnInitialBatch()
        {
            ClearActiveBlocks();
            StartCoroutine(SpawnNextBatchRoutine());
        }

        public void ResetSpawner()
        {
            IsGameOver = false;
            ClearActiveBlocks();
            StartCoroutine(SpawnNextBatchRoutine());
        }

        public void ClearSpawner()
        {
            IsGameOver = false;
            _isSpawning = false;

            if (_gameOverValidationRoutine != null)
            {
                StopCoroutine(_gameOverValidationRoutine);
                _gameOverValidationRoutine = null;
            }

            ClearActiveBlocks();
        }

        public void CheckAndSpawnIfNeeded()
        {
            if (IsGameOver) return;
            if (_isSpawning) return;

            PurgeNullBlocks();

            if (_activeBlocks.Count == 0)
            {
                StartCoroutine(SpawnNextBatchRoutine());
                return;
            }

            ScheduleGameOverValidation();
        }
        #endregion

        #region Spawn Flow
        private void HandleMoveCompleted(int totalLines, Vector3 effectCenter)
        {
            if (IsGameOver) return;
            if (_isSpawning) return;

            PurgeNullBlocks();

            if (_activeBlocks.Count == 0)
            {
                StartCoroutine(SpawnNextBatchRoutine());
                return;
            }

            ScheduleGameOverValidation();
        }

        private IEnumerator SpawnNextBatchRoutine()
        {
            _isSpawning = true;

            float delay = GetSpawnDelay();
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            SpawnBatch();

            _isSpawning = false;

            ScheduleGameOverValidation();
        }

        private void SpawnBatch()
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

            int targetCount = Mathf.Min(GetSpawnBatchSize(), _spawnSlots.Length);

            float fillRate = GridManager.Instance != null ? GridManager.Instance.GetFillRate() : 0f;
            List<SpawnRequest> batch = _spawnStrategy.GetNextBatch(targetCount, fillRate);

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

                SpawnSingleBlock(slot, request);
            }
        }

        private void SpawnSingleBlock(Transform slot, SpawnRequest request)
        {
            BlockFactory.RuntimeBlockResult runtimeBlock =
                BlockFactory.CreateBlockInstance(
                    request.ShapeData,
                    request.RotationIndex,
                    GetBoomChance(),
                    GetToolChance()
                );

            Transform parentContainer = slot.parent != null ? slot.parent : slot;

            GameObject blockObj = Instantiate(_blockPrefab, slot.position, Quaternion.identity, parentContainer);
            blockObj.transform.position = slot.position;
            blockObj.transform.localScale = Vector3.one;

            BlockController controller = blockObj.GetComponent<BlockController>();
            if (controller == null)
            {
                Debug.LogError("BlockSpawner: Block prefab missing BlockController!");
                Destroy(blockObj);
                return;
            }

            Color blockColor = _colorSelector != null ? _colorSelector.GetRandomColor() : Color.white;

            controller.Initialize(runtimeBlock.cells, runtimeBlock.width, _cellPrefab, blockColor);
            controller.OnPlaced += OnBlockPlaced;

            _activeBlocks.Add(controller);
        }

        private void OnBlockPlaced(BlockController block)
        {
            if (block == null) return;

            block.OnPlaced -= OnBlockPlaced;
            _activeBlocks.Remove(block);
        }
        #endregion

        #region Game Over Validation
        private void ScheduleGameOverValidation()
        {
            if (IsGameOver) return;

            if (_gameOverValidationRoutine != null)
            {
                StopCoroutine(_gameOverValidationRoutine);
            }

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

            PurgeNullBlocks();

            if (_activeBlocks.Count == 0) return;

            bool hasAnyValidMove = HasAnyValidMoveForActiveBlocks();

            if (!hasAnyValidMove)
            {
                TriggerGameOver();
            }
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

            if (GameServices.BoardQuery != null)
                return GameServices.BoardQuery.CanPlaceBlockAnywhere(shapeOffsets);

            if (GridManager.Instance != null)
                return GridManager.Instance.CanPlaceBlockAnywhere(shapeOffsets);

            return false;
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
        private float GetSpawnDelay()
        {
            if (GameServices.Balance != null)
                return GameServices.Balance.SpawnDelay;

            return _runtimeConfig != null ? _runtimeConfig.SpawnDelay : 0.5f;
        }

        private int GetSpawnBatchSize()
        {
            if (GameServices.Balance != null)
                return GameServices.Balance.SpawnBatchSize;

            return _runtimeConfig != null ? _runtimeConfig.BatchSize : 3;
        }

        private float GetBoomChance()
        {
            if (GameServices.Balance != null)
                return GameServices.Balance.BoomChance;

            return _runtimeConfig != null ? _runtimeConfig.BoomChance : 0f;
        }

        private float GetToolChance()
        {
            if (GameServices.Balance != null)
                return GameServices.Balance.ToolChance;

            return _runtimeConfig != null ? _runtimeConfig.ToolChance : 0f;
        }
        #endregion

        #region Utility
        private void ClearActiveBlocks()
        {
            for (int i = 0; i < _activeBlocks.Count; i++)
            {
                if (_activeBlocks[i] != null)
                    Destroy(_activeBlocks[i].gameObject);
            }

            _activeBlocks.Clear();
        }

        private void PurgeNullBlocks()
        {
            _activeBlocks.RemoveAll(block => block == null);
        }
        #endregion
    }
}