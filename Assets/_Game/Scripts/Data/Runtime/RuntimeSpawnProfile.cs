using System;
using System.Collections.Generic;

namespace BreakItAll.Data
{
    public sealed class RuntimeSpawnProfile
    {
        private readonly List<RuntimeSpawnShapeEntry> _entries = new List<RuntimeSpawnShapeEntry>();

        public IReadOnlyList<RuntimeSpawnShapeEntry> Entries => _entries;

        public RuntimeSpawnProfile(IEnumerable<RuntimeSpawnShapeEntry> entries)
        {
            if (entries != null)
            {
                _entries.AddRange(entries);
            }

            if (_entries.Count == 0)
            {
                throw new ArgumentException("RuntimeSpawnProfile must contain at least one spawn entry.");
            }
        }
    }
}