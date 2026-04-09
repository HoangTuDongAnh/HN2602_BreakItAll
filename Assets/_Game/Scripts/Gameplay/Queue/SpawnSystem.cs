using System;
using System.Collections.Generic;
using BreakItAll.Data;

namespace BreakItAll.Gameplay
{
    /// <summary>
    /// Spawn system dùng weighted random từ shape spawn pool.
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
            IReadOnlyList<RuntimeSpawnShapeEntry> entries = _shapeSpawnPool.Entries;

            int totalWeight = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                totalWeight += Math.Max(0, entries[i].FinalWeight);
            }

            if (totalWeight <= 0)
            {
                throw new InvalidOperationException("Spawn pool has no positive total weight.");
            }

            int roll = _random.Next(0, totalWeight);
            int cumulative = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                cumulative += Math.Max(0, entries[i].FinalWeight);

                if (roll < cumulative)
                {
                    return entries[i].RuntimeShape;
                }
            }

            return entries[entries.Count - 1].RuntimeShape;
        }
    }
}