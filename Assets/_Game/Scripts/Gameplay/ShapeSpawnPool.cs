using System;
using System.Collections.Generic;

namespace BreakItAll.Gameplay
{
    /// <summary>
    /// Runtime pool tạm cho M1.3.
    /// M2 sẽ thay bằng data-driven definitions/repository.
    /// </summary>
    public sealed class ShapeSpawnPool
    {
        private readonly List<ShapeData> _shapes = new List<ShapeData>();

        public IReadOnlyList<ShapeData> Shapes => _shapes;

        public ShapeSpawnPool(IEnumerable<ShapeData> shapes)
        {
            if (shapes != null)
            {
                _shapes.AddRange(shapes);
            }

            if (_shapes.Count == 0)
            {
                throw new ArgumentException("ShapeSpawnPool must contain at least one shape.");
            }
        }
    }
}