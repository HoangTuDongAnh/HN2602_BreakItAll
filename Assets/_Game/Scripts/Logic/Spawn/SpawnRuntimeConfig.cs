using System;
using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Data;
using _Game.Scripts.Logic.Placement;
using _Game.Scripts.Modes.Levels;

namespace _Game.Scripts.Logic.Spawn
{
    /// <summary>
    /// Runtime config nhẹ cho hệ spawn hiện tại.
    /// Dùng được cho Endless ngay, và có thể bị override bởi Arcade SpawnProfile sau này.
    /// </summary>
    [Serializable]
    public class SpawnRuntimeConfig
    {
        #region Queue
        [Header("Queue")]
        [SerializeField] private float _spawnDelay = 0.5f;
        [SerializeField] private int _batchSize = 3;
        #endregion

        #region Mode Filters
        [Header("Mode Filters")]
        [SerializeField] private bool _allowEndlessBlocks = true;
        [SerializeField] private bool _allowArcadeBlocks = true;
        #endregion

        #region Difficulty Bias
        [Header("Difficulty Bias")]
        [Range(-1f, 1f)] [SerializeField] private float _difficultyBias;
        [Tooltip("Bật để spawn ưu tiên ít nhất một block còn đặt được khi board nguy hiểm.")]
        [SerializeField] private bool _preferPlayableBatch = true;
        [Tooltip("Cố gắng tránh sinh 3 shape cùng một id trong một queue.")]
        [SerializeField] private bool _preferDistinctShapes = true;
        #endregion

        #region Tag Filters
        [Header("Optional Tag Filters")]
        [SerializeField] private List<string> _allowedTags = new List<string>();
        [SerializeField] private List<string> _blockedTags = new List<string>();

        [Header("Optional Shape Filters")]
        [SerializeField] private List<string> _allowedShapeIds = new List<string>();
        [SerializeField] private List<string> _blockedShapeIds = new List<string>();
        [SerializeField] private List<string> _preferredTags = new List<string>();
        #endregion

        #region Properties
        public float SpawnDelay => Mathf.Max(0f, _spawnDelay);
        public int BatchSize => Mathf.Max(1, _batchSize);
        public bool AllowEndlessBlocks => _allowEndlessBlocks;
        public bool AllowArcadeBlocks => _allowArcadeBlocks;
        public float DifficultyBias => Mathf.Clamp(_difficultyBias, -1f, 1f);
        public bool PreferPlayableBatch => _preferPlayableBatch;
        public bool PreferDistinctShapes => _preferDistinctShapes;
        public IReadOnlyList<string> AllowedTags => _allowedTags;
        public IReadOnlyList<string> BlockedTags => _blockedTags;
        public IReadOnlyList<string> AllowedShapeIds => _allowedShapeIds;
        public IReadOnlyList<string> BlockedShapeIds => _blockedShapeIds;
        public IReadOnlyList<string> PreferredTags => _preferredTags;
        #endregion

        #region Constructors
        public SpawnRuntimeConfig() { }

        private SpawnRuntimeConfig(
            float spawnDelay,
            int batchSize,
            bool allowEndlessBlocks,
            bool allowArcadeBlocks,
            float difficultyBias,
            bool preferPlayableBatch,
            bool preferDistinctShapes,
            IEnumerable<string> allowedTags,
            IEnumerable<string> blockedTags,
            IEnumerable<string> allowedShapeIds,
            IEnumerable<string> blockedShapeIds,
            IEnumerable<string> preferredTags)
        {
            _spawnDelay = Mathf.Max(0f, spawnDelay);
            _batchSize = Mathf.Max(1, batchSize);
            _allowEndlessBlocks = allowEndlessBlocks;
            _allowArcadeBlocks = allowArcadeBlocks;
            _difficultyBias = Mathf.Clamp(difficultyBias, -1f, 1f);
            _preferPlayableBatch = preferPlayableBatch;
            _preferDistinctShapes = preferDistinctShapes;
            _allowedTags = CopyList(allowedTags);
            _blockedTags = CopyList(blockedTags);
            _allowedShapeIds = CopyList(allowedShapeIds);
            _blockedShapeIds = CopyList(blockedShapeIds);
            _preferredTags = CopyList(preferredTags);
        }

        public static SpawnRuntimeConfig FromArcadeProfile(SpawnRuntimeConfig baseConfig, SpawnProfileDefinition profile)
        {
            baseConfig ??= new SpawnRuntimeConfig();
            if (profile == null)
                return baseConfig;

            int batchSize = profile.queueSizeOverride > 0 ? profile.queueSizeOverride : baseConfig.BatchSize;
            float difficultyBias = Mathf.Clamp(profile.difficultyBias, -1f, 1f);

            return new SpawnRuntimeConfig(
                baseConfig.SpawnDelay,
                batchSize,
                baseConfig.AllowEndlessBlocks,
                baseConfig.AllowArcadeBlocks,
                difficultyBias,
                baseConfig.PreferPlayableBatch,
                baseConfig.PreferDistinctShapes,
                baseConfig.AllowedTags,
                MergeLists(baseConfig.BlockedTags, profile.blockedTags),
                profile.allowedShapeIds,
                profile.blockedShapeIds,
                profile.preferredTags
            );
        }

        private static List<string> CopyList(IEnumerable<string> source)
        {
            List<string> result = new List<string>();
            if (source == null) return result;

            foreach (string value in source)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                string normalized = value.Trim();
                if (!ContainsIgnoreCase(result, normalized))
                    result.Add(normalized);
            }

            return result;
        }

        private static List<string> MergeLists(IEnumerable<string> first, IEnumerable<string> second)
        {
            List<string> result = CopyList(first);
            if (second == null) return result;

            foreach (string value in second)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                string normalized = value.Trim();
                if (!ContainsIgnoreCase(result, normalized))
                    result.Add(normalized);
            }

            return result;
        }

        private static bool ContainsIgnoreCase(List<string> values, string target)
        {
            if (values == null || string.IsNullOrWhiteSpace(target)) return false;

            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], target, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
        #endregion
    }

    /// <summary>
    /// Context truyền vào SmartSpawnStrategy để strategy không phụ thuộc trực tiếp vào Mode/Level controller.
    /// Arcade về sau chỉ cần map SpawnProfileDefinition sang context này.
    /// </summary>
    public readonly struct SpawnSelectionContext
    {
        #region Data
        public readonly BoardStateSnapshot Board;
        public readonly SpawnRuntimeConfig RuntimeConfig;
        public readonly bool IsArcade;
        #endregion

        #region Constructor
        public SpawnSelectionContext(BoardStateSnapshot board, SpawnRuntimeConfig runtimeConfig, bool isArcade = false)
        {
            Board = board;
            RuntimeConfig = runtimeConfig;
            IsArcade = isArcade;
        }
        #endregion
    }

    /// <summary>
    /// Snapshot tối giản của board cho spawn heuristic.
    /// Không expose GridManager/GridCell để giữ spawn strategy dễ test.
    /// </summary>
    public readonly struct BoardStateSnapshot
    {
        #region Data
        private readonly bool[,] _occupied;
        public readonly int Width;
        public readonly int Height;
        public readonly float FillRate;
        #endregion

        #region Constructor
        public BoardStateSnapshot(bool[,] occupied, int width, int height)
        {
            _occupied = occupied;
            Width = Mathf.Max(0, width);
            Height = Mathf.Max(0, height);

            int total = Width * Height;
            int filled = 0;

            if (_occupied != null)
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        if (_occupied[x, y]) filled++;
                    }
                }
            }

            FillRate = total > 0 ? filled / (float)total : 0f;
        }
        #endregion

        #region Public API
        public bool IsValid => _occupied != null && Width > 0 && Height > 0;

        public bool IsInside(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool IsOccupied(int x, int y)
        {
            if (!IsInside(x, y)) return true;
            return _occupied != null && _occupied[x, y];
        }

        public static BoardStateSnapshot FromBoardState(BoardState board)
        {
            if (board == null)
                return Empty(9, 9);

            bool[,] occupied = new bool[board.Width, board.Height];

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    occupied[x, y] = board.IsOccupied(new GridCoord(x, y));
                }
            }

            return new BoardStateSnapshot(occupied, board.Width, board.Height);
        }

        public static BoardStateSnapshot Empty(int width, int height)
        {
            return new BoardStateSnapshot(new bool[Mathf.Max(0, width), Mathf.Max(0, height)], width, height);
        }
        #endregion
    }
}
