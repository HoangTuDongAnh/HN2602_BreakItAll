using System;
using System.Collections.Generic;

namespace BreakItAll.Gameplay
{
    /// <summary>
    /// Spawn system tối thiểu cho M1.3.
    /// Chưa có spawn weight/profile; chỉ random uniform từ pool.
    /// </summary>
    public sealed class SpawnSystem
    {
        private readonly ShapeSpawnPool _shapeSpawnPool;
        private readonly Random _random;

        public SpawnSystem(ShapeSpawnPool shapeSpawnPool, int? seed = null)
        {
            _shapeSpawnPool = shapeSpawnPool ?? throw new ArgumentNullException(nameof(shapeSpawnPool));
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public List<ShapeData> GenerateBatch(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            List<ShapeData> result = new List<ShapeData>(count);

            for (int i = 0; i < count; i++)
            {
                result.Add(GetRandomShape());
            }

            return result;
        }

        private ShapeData GetRandomShape()
        {
            int index = _random.Next(0, _shapeSpawnPool.Shapes.Count);
            return _shapeSpawnPool.Shapes[index];
        }
    }
}