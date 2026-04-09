using System;
using System.Collections.Generic;
using BreakItAll.Data;

namespace BreakItAll.Gameplay
{
    /// <summary>
    /// Runtime spawn pool cho gameplay.
    /// Chứa danh sách shape cùng weight đã resolve từ data.
    /// </summary>
    public sealed class ShapeSpawnPool
    {
        private readonly List<RuntimeSpawnShapeEntry> _entries = new List<RuntimeSpawnShapeEntry>();

        public IReadOnlyList<RuntimeSpawnShapeEntry> Entries => _entries;

        public ShapeSpawnPool(IEnumerable<RuntimeSpawnShapeEntry> entries)
        {
            if (entries != null)
            {
                _entries.AddRange(entries);
            }

            if (_entries.Count == 0)
            {
                throw new ArgumentException("ShapeSpawnPool must contain at least one entry.");
            }
        }
    }
}