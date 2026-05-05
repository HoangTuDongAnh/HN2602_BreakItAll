using System;
using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Logic.Tools;

namespace _Game.Scripts.Logic
{
    /// <summary>
    /// Owns the temporary queue replacement used by gameplay tools.
    /// BlockSpawner still creates/destroys block GameObjects, but this class owns the
    /// state transition between the normal queue and a one-block tool queue.
    /// </summary>
    public sealed class ToolQueueOverrideController
    {
        private readonly List<BlockController> _storedQueueBlocks = new List<BlockController>();
        private BlockController _activeToolBlock;

        public bool IsActive { get; private set; }
        public BlockController ActiveToolBlock => _activeToolBlock;

        public bool BeginToolBlock(
            GameplayToolType toolType,
            Color blockColor,
            Color previewColor,
            Transform[] spawnSlots,
            List<BlockController> activeBlocks,
            Func<Transform, Color, GameplayToolType, Color, BlockController> spawnToolBlock,
            Action purgeNullBlocks,
            Action<BlockController> destroyBlock
        )
        {
            if (spawnSlots == null || spawnSlots.Length == 0) return false;
            if (activeBlocks == null) return false;
            if (spawnToolBlock == null) return false;

            CancelAndRestore(activeBlocks, destroyBlock);
            purgeNullBlocks?.Invoke();

            _storedQueueBlocks.Clear();
            _storedQueueBlocks.AddRange(activeBlocks);

            for (int i = 0; i < _storedQueueBlocks.Count; i++)
            {
                BlockController block = _storedQueueBlocks[i];
                if (block != null)
                    block.gameObject.SetActive(false);
            }

            activeBlocks.Clear();
            IsActive = true;

            Transform middleSlot = spawnSlots[Mathf.Clamp(spawnSlots.Length / 2, 0, spawnSlots.Length - 1)];
            if (middleSlot == null)
            {
                Restore(activeBlocks);
                return false;
            }

            _activeToolBlock = spawnToolBlock(middleSlot, blockColor, toolType, previewColor);
            if (_activeToolBlock == null)
            {
                Restore(activeBlocks);
                return false;
            }

            activeBlocks.Add(_activeToolBlock);
            return true;
        }

        public void CancelAndRestore(List<BlockController> activeBlocks, Action<BlockController> destroyBlock)
        {
            if (_activeToolBlock != null)
            {
                destroyBlock?.Invoke(_activeToolBlock);
                _activeToolBlock = null;
            }

            Restore(activeBlocks);
        }

        public void Restore(List<BlockController> activeBlocks)
        {
            if (!IsActive && _storedQueueBlocks.Count == 0) return;
            if (activeBlocks == null) return;

            activeBlocks.Clear();
            for (int i = 0; i < _storedQueueBlocks.Count; i++)
            {
                BlockController block = _storedQueueBlocks[i];
                if (block == null) continue;

                block.gameObject.SetActive(true);
                block.SetSpawnToolAttention(false);
                if (!activeBlocks.Contains(block))
                    activeBlocks.Add(block);
            }

            _storedQueueBlocks.Clear();
            IsActive = false;
        }

        public void MarkToolBlockPlaced(BlockController block)
        {
            if (block != null && block == _activeToolBlock)
                _activeToolBlock = null;
        }

        public bool BeginRemoveToolFeedback(List<BlockController> activeBlocks, Action cancelActiveToolAndRestoreQueue, Action purgeNullBlocks)
        {
            cancelActiveToolAndRestoreQueue?.Invoke();
            purgeNullBlocks?.Invoke();

            if (activeBlocks == null) return false;
            for (int i = 0; i < activeBlocks.Count; i++)
            {
                if (activeBlocks[i] != null)
                    activeBlocks[i].SetSpawnToolAttention(true);
            }

            return activeBlocks.Count > 0;
        }

        public void EndRemoveToolFeedback(List<BlockController> activeBlocks)
        {
            if (activeBlocks == null) return;

            for (int i = 0; i < activeBlocks.Count; i++)
            {
                if (activeBlocks[i] != null)
                    activeBlocks[i].SetSpawnToolAttention(false);
            }
        }

        public void ClearAll(List<BlockController> activeBlocks, Action<BlockController> destroyBlock)
        {
            if (activeBlocks != null)
            {
                for (int i = 0; i < activeBlocks.Count; i++)
                {
                    if (activeBlocks[i] != null)
                        destroyBlock?.Invoke(activeBlocks[i]);
                }

                activeBlocks.Clear();
            }

            for (int i = 0; i < _storedQueueBlocks.Count; i++)
            {
                if (_storedQueueBlocks[i] != null)
                    destroyBlock?.Invoke(_storedQueueBlocks[i]);
            }

            if (_activeToolBlock != null)
                destroyBlock?.Invoke(_activeToolBlock);

            _storedQueueBlocks.Clear();
            _activeToolBlock = null;
            IsActive = false;
        }

        public void PurgeNullBlocks()
        {
            _storedQueueBlocks.RemoveAll(block => block == null);
            if (_activeToolBlock == null && IsActive && _storedQueueBlocks.Count == 0)
                IsActive = false;
        }
    }
}
