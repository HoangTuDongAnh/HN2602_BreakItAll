using System;
using System.Collections.Generic;

namespace BreakItAll.Gameplay
{
    [Serializable]
    public sealed class ShapeData
    {
        public string Id { get; }
        public IReadOnlyList<CellCoord> Cells => _cells;

        private readonly List<CellCoord> _cells;

        public ShapeData(string id, IEnumerable<CellCoord> cells)
        {
            Id = string.IsNullOrWhiteSpace(id) ? "shape" : id;
            _cells = new List<CellCoord>();

            if (cells != null)
            {
                _cells.AddRange(cells);
            }

            if (_cells.Count == 0)
            {
                throw new ArgumentException("ShapeData must contain at least one cell.");
            }
        }
    }
}