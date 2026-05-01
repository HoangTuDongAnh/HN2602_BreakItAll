using System;
using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Data;
using _Game.Scripts.Logic.Spawn;

namespace _Game.Scripts.Logic
{
    [Serializable]
    public struct SpawnRequest
    {
        public BlockData ShapeData;
        public int RotationIndex;
    }

    /// <summary>
    /// Hybrid spawn strategy:
    /// - Bag randomizer để giảm lặp shape khó chịu.
    /// - Board-aware scoring để block sinh ra hợp tình trạng board.
    /// - Safety filter để hạn chế queue không có nước đi.
    /// - Difficulty bias/profile-ready cho Endless và Arcade.
    /// </summary>
    [CreateAssetMenu(fileName = "SmartSpawnStrategy", menuName = "Game/Spawn Strategy")]
    public class SmartSpawnStrategy : ScriptableObject
    {
        #region Database
        [Header("Block Database")]
        [Tooltip("Danh sách các khối Tier 1 (dễ/nhỏ/rescue).")]
        [SerializeField] private List<BlockData> _tier1Blocks = new List<BlockData>();

        [Tooltip("Danh sách các khối Tier 2 (trung bình).")]
        [SerializeField] private List<BlockData> _tier2Blocks = new List<BlockData>();

        [Tooltip("Danh sách các khối Tier 3 (khó/lớn).")]
        [SerializeField] private List<BlockData> _tier3Blocks = new List<BlockData>();
        #endregion

        #region Tuning
        [Header("Board Difficulty Thresholds")]
        [Range(0f, 1f)] [SerializeField] private float _panicThreshold = 0.7f;
        [Range(0f, 1f)] [SerializeField] private float _safeThreshold = 0.3f;

        [Header("Bag Randomizer")]
        [SerializeField] private bool _useBagRandomizer = true;
        [Min(1)] [SerializeField] private int _recentShapeMemory = 6;
        [Range(0f, 1f)] [SerializeField] private float _recentRepeatPenalty = 0.45f;

        [Header("Board Aware Scoring")]
        [Tooltip("Trọng số cho số vị trí có thể đặt được.")]
        [SerializeField] private float _fitCountWeight = 1.25f;
        [Tooltip("Trọng số cho khả năng tạo line clear ngay.")]
        [SerializeField] private float _clearPotentialWeight = 1.6f;
        [Tooltip("Giảm điểm block lớn khi board đang nguy hiểm.")]
        [SerializeField] private float _panicLargeBlockPenalty = 0.35f;
        [Tooltip("Tăng điểm block lớn khi board còn trống để tạo thử thách.")]
        [SerializeField] private float _safeLargeBlockBonus = 0.25f;

        [Header("Safety")]
        [SerializeField] private bool _guaranteeAtLeastOnePlayable = true;
        [Range(0f, 1f)] [SerializeField] private float _unplayableCandidatePenalty = 0.05f;
        #endregion

        #region Runtime
        private readonly Queue<string> _recentShapeIds = new Queue<string>();
        private readonly List<BlockData> _bag = new List<BlockData>();
        #endregion

        #region Public API
        public List<SpawnRequest> GetNextBatch(int count, float currentFillRate)
        {
            BoardStateSnapshot board = BoardStateSnapshot.Empty(9, 9);
            var fallbackConfig = new SpawnRuntimeConfig();
            var context = new SpawnSelectionContext(board, fallbackConfig, false);
            return GetNextBatch(count, context, currentFillRate);
        }

        public List<SpawnRequest> GetNextBatch(int count, SpawnSelectionContext context)
        {
            float fillRate = context.Board.IsValid ? context.Board.FillRate : 0f;
            return GetNextBatch(count, context, fillRate);
        }

        public BlockData GetGuaranteedBlock()
        {
            List<BlockData> pool = BuildRawPool();
            BlockData best = null;
            int bestSize = int.MaxValue;

            for (int i = 0; i < pool.Count; i++)
            {
                BlockData block = pool[i];
                if (block == null) continue;

                int size = CountOccupiedCells(block);
                if (size < bestSize)
                {
                    best = block;
                    bestSize = size;
                }
            }

            return best;
        }

        public BlockData FindBlockById(string shapeId)
        {
            if (string.IsNullOrWhiteSpace(shapeId)) return null;

            List<BlockData> pool = BuildRawPool();
            for (int i = 0; i < pool.Count; i++)
            {
                BlockData block = pool[i];
                if (block != null && string.Equals(GetBlockId(block), shapeId.Trim(), StringComparison.OrdinalIgnoreCase))
                    return block;
            }

            return null;
        }

        public void ResetRuntimeState()
        {
            _recentShapeIds.Clear();
            _bag.Clear();
        }
        #endregion

        #region Batch Selection
        private List<SpawnRequest> GetNextBatch(int count, SpawnSelectionContext context, float fillRate)
        {
            int safeCount = Mathf.Max(1, count);
            List<SpawnRequest> result = new List<SpawnRequest>(safeCount);
            HashSet<string> batchShapeIds = new HashSet<string>();

            List<BlockData> candidatePool = BuildFilteredPool(context);
            if (candidatePool.Count == 0)
                return result;

            bool hasPlayableInBatch = false;
            bool shouldGuaranteePlayable = _guaranteeAtLeastOnePlayable &&
                                          (context.RuntimeConfig == null || context.RuntimeConfig.PreferPlayableBatch);

            for (int i = 0; i < safeCount; i++)
            {
                bool forcePlayable = shouldGuaranteePlayable && i == safeCount - 1 && !hasPlayableInBatch;
                SpawnCandidate selected = PickCandidate(candidatePool, context, fillRate, batchShapeIds, forcePlayable);

                if (selected.ShapeData == null)
                    continue;

                result.Add(new SpawnRequest
                {
                    ShapeData = selected.ShapeData,
                    RotationIndex = selected.RotationIndex
                });

                if (selected.FitCount > 0)
                    hasPlayableInBatch = true;

                string id = GetBlockId(selected.ShapeData);
                if (!string.IsNullOrEmpty(id))
                {
                    batchShapeIds.Add(id);
                    RememberShape(id);
                }
            }

            return result;
        }

        private SpawnCandidate PickCandidate(List<BlockData> pool, SpawnSelectionContext context, float fillRate, HashSet<string> batchShapeIds, bool forcePlayable)
        {
            List<SpawnCandidate> candidates = BuildCandidates(pool, context, fillRate, batchShapeIds, forcePlayable);

            if (candidates.Count == 0 && forcePlayable)
                candidates = BuildCandidates(pool, context, fillRate, batchShapeIds, false);

            if (candidates.Count == 0)
                return default;

            float totalWeight = 0f;
            for (int i = 0; i < candidates.Count; i++)
                totalWeight += Mathf.Max(0f, candidates[i].Weight);

            if (totalWeight <= 0f)
                return candidates[UnityEngine.Random.Range(0, candidates.Count)];

            float roll = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;

            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative += Mathf.Max(0f, candidates[i].Weight);
                if (roll <= cumulative)
                    return candidates[i];
            }

            return candidates[candidates.Count - 1];
        }
        #endregion

        #region Candidate Building
        private List<SpawnCandidate> BuildCandidates(List<BlockData> pool, SpawnSelectionContext context, float fillRate, HashSet<string> batchShapeIds, bool forcePlayable)
        {
            List<SpawnCandidate> candidates = new List<SpawnCandidate>();
            bool preferDistinct = context.RuntimeConfig == null || context.RuntimeConfig.PreferDistinctShapes;

            for (int i = 0; i < pool.Count; i++)
            {
                BlockData block = pool[i];
                if (block == null) continue;

                string blockId = GetBlockId(block);
                if (preferDistinct && batchShapeIds.Contains(blockId))
                    continue;

                int rotationVariants = block.allowRotation ? 4 : 1;
                for (int rotation = 0; rotation < rotationVariants; rotation++)
                {
                    ShapeRuntimeInfo shape = BuildRuntimeShape(block, rotation);
                    SpawnCandidate candidate = ScoreCandidate(block, rotation, shape, context, fillRate, batchShapeIds);

                    if (forcePlayable && candidate.FitCount <= 0)
                        continue;

                    if (candidate.Weight > 0f)
                        candidates.Add(candidate);
                }
            }

            return candidates;
        }

        private SpawnCandidate ScoreCandidate(BlockData block, int rotation, ShapeRuntimeInfo shape, SpawnSelectionContext context, float fillRate, HashSet<string> batchShapeIds)
        {
            int fitCount = CountFitPositions(shape, context.Board);
            int bestClearPotential = EstimateBestClearPotential(shape, context.Board);
            int cellCount = Mathf.Max(1, shape.OccupiedOffsets.Count);

            float weight = Mathf.Max(0.01f, block.spawnWeight);
            weight *= GetTierWeight(block.tier, fillRate, context.RuntimeConfig);
            weight *= GetTagWeight(block, context.RuntimeConfig);
            weight *= GetBagWeight(block);

            if (fitCount <= 0)
            {
                weight *= _unplayableCandidatePenalty;
            }
            else
            {
                weight *= 1f + Mathf.Log(1f + fitCount) * _fitCountWeight;
                weight *= 1f + bestClearPotential * _clearPotentialWeight;
            }

            if (fillRate >= _panicThreshold)
                weight *= Mathf.Lerp(1f, _panicLargeBlockPenalty, Mathf.InverseLerp(2f, 6f, cellCount));
            else if (fillRate <= _safeThreshold)
                weight *= 1f + Mathf.InverseLerp(2f, 6f, cellCount) * _safeLargeBlockBonus;

            return new SpawnCandidate
            {
                ShapeData = block,
                RotationIndex = rotation,
                Weight = Mathf.Max(0f, weight),
                FitCount = fitCount,
                ClearPotential = bestClearPotential,
                CellCount = cellCount
            };
        }
        #endregion

        #region Pool Filtering
        private List<BlockData> BuildRawPool()
        {
            List<BlockData> pool = new List<BlockData>();
            AddRange(pool, _tier1Blocks);
            AddRange(pool, _tier2Blocks);
            AddRange(pool, _tier3Blocks);
            return pool;
        }

        private List<BlockData> BuildFilteredPool(SpawnSelectionContext context)
        {
            List<BlockData> rawPool = BuildRawPool();
            List<BlockData> filtered = new List<BlockData>();

            for (int i = 0; i < rawPool.Count; i++)
            {
                BlockData block = rawPool[i];
                if (block == null) continue;
                if (!IsModeAllowed(block, context)) continue;
                if (!PassesShapeFilters(block, context.RuntimeConfig)) continue;
                if (!PassesTagFilters(block, context.RuntimeConfig)) continue;
                if (block.spawnWeight <= 0f) continue;

                filtered.Add(block);
            }

            if (_useBagRandomizer)
                RefillBagIfNeeded(filtered);

            return filtered;
        }

        private bool IsModeAllowed(BlockData block, SpawnSelectionContext context)
        {
            if (context.IsArcade)
                return block.enabledInArcade && (context.RuntimeConfig == null || context.RuntimeConfig.AllowArcadeBlocks);

            return block.enabledInEndless && (context.RuntimeConfig == null || context.RuntimeConfig.AllowEndlessBlocks);
        }

        private bool PassesShapeFilters(BlockData block, SpawnRuntimeConfig config)
        {
            if (block == null || config == null) return true;

            string id = GetBlockId(block);
            IReadOnlyList<string> blocked = config.BlockedShapeIds;
            if (blocked != null && blocked.Count > 0 && ContainsIgnoreCase(blocked, id))
                return false;

            IReadOnlyList<string> allowed = config.AllowedShapeIds;
            if (allowed != null && allowed.Count > 0 && !ContainsIgnoreCase(allowed, id))
                return false;

            return true;
        }

        private bool PassesTagFilters(BlockData block, SpawnRuntimeConfig config)
        {
            if (block == null || config == null) return true;

            IReadOnlyList<string> blocked = config.BlockedTags;
            if (blocked != null && blocked.Count > 0 && HasAnyTag(block, blocked))
                return false;

            IReadOnlyList<string> allowed = config.AllowedTags;
            if (allowed != null && allowed.Count > 0 && !HasAnyTag(block, allowed))
                return false;

            return true;
        }

        private void RefillBagIfNeeded(List<BlockData> filtered)
        {
            for (int i = _bag.Count - 1; i >= 0; i--)
            {
                if (filtered == null || _bag[i] == null || !filtered.Contains(_bag[i]))
                    _bag.RemoveAt(i);
            }

            if (_bag.Count > 0) return;

            _bag.Clear();
            _bag.AddRange(filtered);
            Shuffle(_bag);
        }
        #endregion

        #region Heuristics
        private float GetTierWeight(BlockTier tier, float fillRate, SpawnRuntimeConfig config)
        {
            float bias = config != null ? config.DifficultyBias : 0f;

            if (fillRate > _panicThreshold)
            {
                switch (tier)
                {
                    case BlockTier.Tier1_Easy: return 2.6f - bias;
                    case BlockTier.Tier2_Medium: return 1.2f;
                    case BlockTier.Tier3_Hard: return 0.45f + bias;
                }
            }

            if (fillRate < _safeThreshold)
            {
                switch (tier)
                {
                    case BlockTier.Tier1_Easy: return 0.75f - bias * 0.25f;
                    case BlockTier.Tier2_Medium: return 1.15f;
                    case BlockTier.Tier3_Hard: return 1.7f + bias;
                }
            }

            switch (tier)
            {
                case BlockTier.Tier1_Easy: return 1.15f - bias * 0.25f;
                case BlockTier.Tier2_Medium: return 1.35f;
                case BlockTier.Tier3_Hard: return 0.95f + bias * 0.5f;
                default: return 1f;
            }
        }

        private float GetTagWeight(BlockData block, SpawnRuntimeConfig config)
        {
            if (block == null || config == null)
                return 1f;

            IReadOnlyList<string> preferredTags = config.PreferredTags;
            if (preferredTags != null && preferredTags.Count > 0 && HasAnyTag(block, preferredTags))
                return 1.35f;

            return 1f;
        }

        private float GetBagWeight(BlockData block)
        {
            if (!_useBagRandomizer || block == null)
                return 1f;

            string id = GetBlockId(block);
            if (string.IsNullOrEmpty(id))
                return 1f;

            float weight = _bag.Contains(block) ? 1.35f : 0.85f;

            if (_recentShapeIds.Contains(id))
                weight *= Mathf.Clamp01(1f - _recentRepeatPenalty);

            return Mathf.Max(0.01f, weight);
        }

        private int CountFitPositions(ShapeRuntimeInfo shape, BoardStateSnapshot board)
        {
            if (!board.IsValid) return 1;

            int count = 0;
            for (int anchorX = 0; anchorX < board.Width; anchorX++)
            {
                for (int anchorY = 0; anchorY < board.Height; anchorY++)
                {
                    if (CanPlace(shape, board, anchorX, anchorY))
                        count++;
                }
            }

            return count;
        }

        private int EstimateBestClearPotential(ShapeRuntimeInfo shape, BoardStateSnapshot board)
        {
            if (!board.IsValid) return 0;

            int best = 0;
            for (int anchorX = 0; anchorX < board.Width; anchorX++)
            {
                for (int anchorY = 0; anchorY < board.Height; anchorY++)
                {
                    if (!CanPlace(shape, board, anchorX, anchorY))
                        continue;

                    int clearCount = EstimateLineClearsAfterPlacement(shape, board, anchorX, anchorY);
                    if (clearCount > best) best = clearCount;
                }
            }

            return best;
        }

        private bool CanPlace(ShapeRuntimeInfo shape, BoardStateSnapshot board, int anchorX, int anchorY)
        {
            for (int i = 0; i < shape.OccupiedOffsets.Count; i++)
            {
                Vector2Int offset = shape.OccupiedOffsets[i];
                int x = anchorX + offset.x;
                int y = anchorY + offset.y;

                if (!board.IsInside(x, y)) return false;
                if (board.IsOccupied(x, y)) return false;
            }

            return true;
        }

        private int EstimateLineClearsAfterPlacement(ShapeRuntimeInfo shape, BoardStateSnapshot board, int anchorX, int anchorY)
        {
            HashSet<int> rowsToCheck = new HashSet<int>();
            HashSet<int> colsToCheck = new HashSet<int>();
            HashSet<Vector2Int> virtualPlaced = new HashSet<Vector2Int>();

            for (int i = 0; i < shape.OccupiedOffsets.Count; i++)
            {
                Vector2Int offset = shape.OccupiedOffsets[i];
                int x = anchorX + offset.x;
                int y = anchorY + offset.y;

                virtualPlaced.Add(new Vector2Int(x, y));
                rowsToCheck.Add(y);
                colsToCheck.Add(x);
            }

            int clears = 0;

            foreach (int row in rowsToCheck)
            {
                bool full = true;
                for (int x = 0; x < board.Width; x++)
                {
                    if (!board.IsOccupied(x, row) && !virtualPlaced.Contains(new Vector2Int(x, row)))
                    {
                        full = false;
                        break;
                    }
                }
                if (full) clears++;
            }

            foreach (int col in colsToCheck)
            {
                bool full = true;
                for (int y = 0; y < board.Height; y++)
                {
                    if (!board.IsOccupied(col, y) && !virtualPlaced.Contains(new Vector2Int(col, y)))
                    {
                        full = false;
                        break;
                    }
                }
                if (full) clears++;
            }

            return clears;
        }
        #endregion

        #region Shape Runtime
        private ShapeRuntimeInfo BuildRuntimeShape(BlockData block, int rotation)
        {
            BlockFactory.RuntimeBlockResult runtime = BlockFactory.CreateBlockInstance(block, rotation);
            List<Vector2Int> occupied = new List<Vector2Int>();

            for (int y = 0; y < runtime.height; y++)
            {
                for (int x = 0; x < runtime.width; x++)
                {
                    int index = y * runtime.width + x;
                    if (index < 0 || index >= runtime.cells.Count) continue;

                    CellData cell = runtime.cells[index];
                    if (cell != null && cell.isOccupied)
                        occupied.Add(new Vector2Int(x, y));
                }
            }

            Vector2Int anchor = ResolveBottomLeftAnchor(occupied);
            for (int i = 0; i < occupied.Count; i++)
                occupied[i] -= anchor;

            return new ShapeRuntimeInfo
            {
                Width = runtime.width,
                Height = runtime.height,
                OccupiedOffsets = occupied
            };
        }

        private Vector2Int ResolveBottomLeftAnchor(List<Vector2Int> occupied)
        {
            if (occupied == null || occupied.Count == 0) return Vector2Int.zero;

            Vector2Int best = occupied[0];
            for (int i = 1; i < occupied.Count; i++)
            {
                Vector2Int current = occupied[i];
                if (current.y < best.y || (current.y == best.y && current.x < best.x))
                    best = current;
            }

            return best;
        }

        private int CountOccupiedCells(BlockData block)
        {
            if (block == null) return 0;
            block.EnsureDataSize();

            int count = 0;
            for (int i = 0; i < block.boardData.Count; i++)
            {
                if (block.boardData[i] != null && block.boardData[i].isOccupied)
                    count++;
            }

            return count;
        }
        #endregion

        #region Utility
        private void RememberShape(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            _recentShapeIds.Enqueue(id);
            while (_recentShapeIds.Count > Mathf.Max(1, _recentShapeMemory))
                _recentShapeIds.Dequeue();

            for (int i = _bag.Count - 1; i >= 0; i--)
            {
                if (GetBlockId(_bag[i]) == id)
                {
                    _bag.RemoveAt(i);
                    break;
                }
            }
        }

        private string GetBlockId(BlockData block)
        {
            return block == null ? string.Empty : block.Id;
        }

        private void AddRange(List<BlockData> target, List<BlockData> source)
        {
            if (target == null || source == null) return;

            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null && !target.Contains(source[i]))
                    target.Add(source[i]);
            }
        }

        private bool HasAnyTag(BlockData block, IReadOnlyList<string> tags)
        {
            if (block == null || block.tags == null || tags == null) return false;

            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                if (string.IsNullOrWhiteSpace(tag)) continue;

                for (int j = 0; j < block.tags.Count; j++)
                {
                    if (string.Equals(block.tags[j], tag, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        private bool ContainsIgnoreCase(IReadOnlyList<string> values, string target)
        {
            if (values == null || string.IsNullOrWhiteSpace(target)) return false;

            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], target, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private void Shuffle<T>(List<T> list)
        {
            if (list == null) return;

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
        #endregion

        #region Internal Types
        private struct SpawnCandidate
        {
            public BlockData ShapeData;
            public int RotationIndex;
            public float Weight;
            public int FitCount;
            public int ClearPotential;
            public int CellCount;
        }

        private struct ShapeRuntimeInfo
        {
            public int Width;
            public int Height;
            public List<Vector2Int> OccupiedOffsets;
        }
        #endregion
    }
}
