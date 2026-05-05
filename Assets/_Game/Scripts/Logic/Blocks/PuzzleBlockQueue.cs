using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Data;
using _Game.Scripts.Modes.Levels;

namespace _Game.Scripts.Logic
{
    /// <summary>
    /// Owns the data/state for Arcade Puzzle provided blocks.
    /// BlockSpawner still owns Unity object creation, while this class owns queue/set state.
    /// </summary>
    public sealed class PuzzleBlockQueue
    {
        private sealed class Entry
        {
            public BlockData ShapeData;
            public int RotationIndex;
            public bool Used;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly Dictionary<BlockController, int> _visibleBlockIndices = new Dictionary<BlockController, int>();
        private int _currentSetStartIndex;
        private int _visibleSetSize = 3;

        public bool IsActive { get; private set; }
        public int TotalBlocks => _entries.Count;
        public int UsedBlocks => CountUsedBlocks();
        public int RemainingBlocks => Mathf.Max(0, TotalBlocks - UsedBlocks);
        public int CurrentSetStartIndex => _currentSetStartIndex;
        public int CurrentSetIndex => GetSetCount() <= 0 ? -1 : Mathf.Clamp(_currentSetStartIndex / GetVisibleSetSize(), 0, GetSetCount() - 1);
        public int CurrentSetNumber => CurrentSetIndex < 0 ? 0 : CurrentSetIndex + 1;
        public int TotalSets => GetSetCount();
        public bool CanSwitchSet => HasUnusedBlocksInAnotherSet();
        public bool HasUnusedBlocks => FindAnyUnusedBlockIndex() >= 0;

        public void Clear()
        {
            IsActive = false;
            _entries.Clear();
            _visibleBlockIndices.Clear();
            _currentSetStartIndex = 0;
            _visibleSetSize = 3;
        }

        public bool TryInitialize(LevelDefinition level, SmartSpawnStrategy spawnStrategy, int visibleSetSize)
        {
            Clear();
            _visibleSetSize = Mathf.Max(1, visibleSetSize);

            if (level == null || level.LevelType != ArcadeLevelType.Puzzle)
                return false;

            ObjectiveDefinition objective = level.Objective;
            if (objective == null || objective.providedShapeIds == null || objective.providedShapeIds.Count == 0)
                return false;

            if (spawnStrategy == null)
            {
                Debug.LogError("PuzzleBlockQueue: Puzzle level needs a SmartSpawnStrategy to resolve provided shape ids.");
                return false;
            }

            for (int i = 0; i < objective.providedShapeIds.Count; i++)
            {
                string shapeId = objective.providedShapeIds[i];
                BlockData block = spawnStrategy.FindBlockById(shapeId);
                if (block == null)
                {
                    Debug.LogWarning($"PuzzleBlockQueue: Puzzle shape id '{shapeId}' was not found in spawn strategy.");
                    continue;
                }

                _entries.Add(new Entry
                {
                    ShapeData = block,
                    RotationIndex = 0,
                    Used = false
                });
            }

            IsActive = _entries.Count > 0;
            return IsActive;
        }

        public void ClearVisibleBlockBindings()
        {
            _visibleBlockIndices.Clear();
        }

        public void RegisterVisibleBlock(BlockController block, int entryIndex)
        {
            if (block == null) return;
            if (!IsValidEntryIndex(entryIndex)) return;

            _visibleBlockIndices[block] = entryIndex;
        }

        public bool TryGetVisibleBlockEntryIndex(BlockController block, out int entryIndex)
        {
            entryIndex = -1;
            return block != null && _visibleBlockIndices.TryGetValue(block, out entryIndex);
        }

        public bool TryMarkBlockUsed(BlockController block, out int entryIndex)
        {
            entryIndex = -1;
            if (!TryGetVisibleBlockEntryIndex(block, out entryIndex))
                return false;

            _visibleBlockIndices.Remove(block);
            if (!IsValidEntryIndex(entryIndex))
                return false;

            _entries[entryIndex].Used = true;
            return true;
        }

        public bool TryMarkEntryUnused(int entryIndex)
        {
            if (!IsValidEntryIndex(entryIndex)) return false;

            _entries[entryIndex].Used = false;
            return true;
        }

        public bool TryGetSpawnRequest(int entryIndex, out SpawnRequest request)
        {
            request = new SpawnRequest();
            if (!IsValidEntryIndex(entryIndex)) return false;

            Entry entry = _entries[entryIndex];
            if (entry == null || entry.Used || entry.ShapeData == null) return false;

            request = new SpawnRequest
            {
                ShapeData = entry.ShapeData,
                RotationIndex = entry.RotationIndex
            };
            return true;
        }

        public void GetCurrentSetRange(out int startIndex, out int endIndex)
        {
            int setSize = GetVisibleSetSize();
            startIndex = Mathf.Clamp(_currentSetStartIndex, 0, Mathf.Max(0, _entries.Count));
            endIndex = Mathf.Min(_entries.Count, startIndex + setSize);
        }

        public bool TryMoveSet(int direction, bool excludeCurrentSet = true)
        {
            if (!IsActive) return false;
            if (!CanSwitchSet) return false;

            int nextSetStart = FindSetWithUnusedBlocks(_currentSetStartIndex, direction, excludeCurrentSet);
            if (nextSetStart < 0) return false;

            _currentSetStartIndex = nextSetStart;
            return true;
        }

        public bool TryMoveToNextSetWithUnused()
        {
            if (!IsActive) return false;

            int nextSetStart = FindSetWithUnusedBlocks(_currentSetStartIndex, 1, excludeCurrentSet: true);
            if (nextSetStart < 0)
                nextSetStart = FindSetWithUnusedBlocks(_currentSetStartIndex, 1, excludeCurrentSet: false);

            if (nextSetStart < 0) return false;

            _currentSetStartIndex = nextSetStart;
            return true;
        }

        private int FindSetWithUnusedBlocks(int startIndex, int direction, bool excludeCurrentSet)
        {
            int setSize = GetVisibleSetSize();
            int totalSets = GetSetCount();
            if (totalSets <= 0) return -1;

            direction = direction >= 0 ? 1 : -1;
            int currentSet = Mathf.Clamp(startIndex / setSize, 0, totalSets - 1);

            for (int offset = 0; offset < totalSets; offset++)
            {
                if (excludeCurrentSet && offset == 0) continue;

                int set = (currentSet + direction * offset + totalSets * 8) % totalSets;
                int setStart = set * setSize;
                if (SetHasUnusedBlocks(setStart))
                    return setStart;
            }

            return -1;
        }

        private bool HasUnusedBlocksInAnotherSet()
        {
            if (!IsActive) return false;
            return FindSetWithUnusedBlocks(_currentSetStartIndex, 1, excludeCurrentSet: true) >= 0;
        }

        private bool SetHasUnusedBlocks(int setStartIndex)
        {
            int setSize = GetVisibleSetSize();
            int end = Mathf.Min(_entries.Count, setStartIndex + setSize);

            for (int i = setStartIndex; i < end; i++)
            {
                if (_entries[i] != null && !_entries[i].Used)
                    return true;
            }

            return false;
        }

        private int FindAnyUnusedBlockIndex()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i] != null && !_entries[i].Used)
                    return i;
            }

            return -1;
        }

        private int CountUsedBlocks()
        {
            int count = 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i] != null && _entries[i].Used)
                    count++;
            }

            return count;
        }

        private int GetVisibleSetSize() => Mathf.Max(1, _visibleSetSize);

        private int GetSetCount()
        {
            int setSize = GetVisibleSetSize();
            return _entries.Count <= 0 ? 0 : Mathf.CeilToInt(_entries.Count / (float)setSize);
        }

        private bool IsValidEntryIndex(int entryIndex)
        {
            return entryIndex >= 0 && entryIndex < _entries.Count;
        }
    }
}
