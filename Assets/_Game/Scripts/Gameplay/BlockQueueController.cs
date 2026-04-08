using System;
using System.Collections.Generic;

namespace BreakItAll.Gameplay
{
    /// <summary>
    /// Quản lý queue block hiện tại.
    /// V1: refill cả batch khi queue rỗng hoàn toàn.
    /// </summary>
    public sealed class BlockQueueController
    {
        private readonly SpawnSystem _spawnSystem;
        private readonly int _queueSize;
        private readonly List<ShapeData> _currentQueue = new List<ShapeData>();

        public IReadOnlyList<ShapeData> CurrentQueue => _currentQueue;
        public int QueueSize => _queueSize;
        public bool IsEmpty => _currentQueue.Count == 0;

        public BlockQueueController(SpawnSystem spawnSystem, int queueSize)
        {
            _spawnSystem = spawnSystem ?? throw new ArgumentNullException(nameof(spawnSystem));

            if (queueSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(queueSize));
            }

            _queueSize = queueSize;
        }

        public void Initialize()
        {
            _currentQueue.Clear();
            RefillIfEmpty();
        }

        public ShapeData PeekAt(int index)
        {
            if (index < 0 || index >= _currentQueue.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _currentQueue[index];
        }

        public ShapeData ConsumeAt(int index)
        {
            if (index < 0 || index >= _currentQueue.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            ShapeData shape = _currentQueue[index];
            _currentQueue.RemoveAt(index);
            return shape;
        }

        public void RefillIfEmpty()
        {
            if (_currentQueue.Count > 0)
            {
                return;
            }

            List<ShapeData> batch = _spawnSystem.GenerateBatch(_queueSize);
            _currentQueue.AddRange(batch);
        }

        public void ForceRefill()
        {
            _currentQueue.Clear();
            RefillIfEmpty();
        }
    }
}