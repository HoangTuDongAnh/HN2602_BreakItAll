using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Logic
{
    /// <summary>
    /// Checks whether the current visible spawn queue has at least one legal board move.
    /// Kept separate from BlockSpawner so spawn flow does not own board-fit validation details.
    /// </summary>
    public sealed class GameOverMoveValidator
    {
        public bool HasAnyValidMoveForActiveBlocks(IReadOnlyList<BlockController> activeBlocks)
        {
            if (activeBlocks == null || activeBlocks.Count == 0)
                return false;

            for (int i = 0; i < activeBlocks.Count; i++)
            {
                BlockController block = activeBlocks[i];
                if (block == null) continue;

                if (CanBlockFitAnywhere(block))
                    return true;
            }

            return false;
        }

        private bool CanBlockFitAnywhere(BlockController block)
        {
            if (block == null) return false;

            List<Vector2Int> shapeOffsets = block.GetShapeOffsets();
            if (shapeOffsets == null || shapeOffsets.Count == 0) return false;

            return GridManager.Instance != null && GridManager.Instance.CanPlaceBlockAnywhere(shapeOffsets);
        }
    }
}
