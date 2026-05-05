using System;
using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Data;
using _Game.Scripts.Logic.Spawn;
using _Game.Scripts.Logic.Tools;

namespace _Game.Scripts.Logic
{
    /// <summary>
    /// Creates runtime block controllers for spawn slots and tool overrides.
    /// Keeps prefab/cell initialization details out of BlockSpawner.
    /// </summary>
    public sealed class BlockSpawnFactory
    {
        public BlockController SpawnBlock(
            Transform slot,
            GameObject blockPrefab,
            GameObject cellPrefab,
            BlockColorSelector colorSelector,
            SpawnRequest request,
            bool allowRuntimeRotation,
            Action<BlockController> onPlaced)
        {
            if (slot == null || blockPrefab == null || cellPrefab == null || request.ShapeData == null)
                return null;

            BlockController controller = CreateController(slot, blockPrefab);
            if (controller == null)
                return null;

            Color blockColor = colorSelector != null ? colorSelector.GetRandomColor() : Color.white;

            controller.SetHome(slot);
            controller.InitializeFromTemplate(request.ShapeData, request.RotationIndex, cellPrefab, blockColor, allowRuntimeRotation);

            if (onPlaced != null)
                controller.OnPlaced += onPlaced;

            return controller;
        }

        public BlockController SpawnRuntimeOneCellBlock(
            Transform slot,
            GameObject blockPrefab,
            GameObject cellPrefab,
            Color blockColor,
            GameplayToolType toolType,
            Color previewColor,
            Action<BlockController> onPlaced)
        {
            if (slot == null || blockPrefab == null || cellPrefab == null)
                return null;

            BlockController controller = CreateController(slot, blockPrefab);
            if (controller == null)
                return null;

            List<CellData> cells = new List<CellData>
            {
                new CellData { isOccupied = true, blockCellType = BlockCellType.Normal }
            };

            controller.SetHome(slot);
            controller.Initialize(cells, 1, cellPrefab, blockColor);
            controller.ConfigureAsGameplayToolBlock(toolType, previewColor);

            if (onPlaced != null)
                controller.OnPlaced += onPlaced;

            return controller;
        }

        private static BlockController CreateController(Transform slot, GameObject blockPrefab)
        {
            Transform parentContainer = slot.parent != null ? slot.parent : slot;
            GameObject blockObj = UnityEngine.Object.Instantiate(blockPrefab, slot.position, Quaternion.identity, parentContainer);
            blockObj.transform.position = slot.position;
            blockObj.transform.localScale = Vector3.one;

            BlockController controller = blockObj.GetComponent<BlockController>();
            if (controller != null)
                return controller;

            Debug.LogError("BlockSpawnFactory: Block prefab missing BlockController!");
            UnityEngine.Object.Destroy(blockObj);
            return null;
        }
    }
}
