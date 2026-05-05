using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Data;
using _Game.Scripts.View;

namespace _Game.Scripts.Logic
{
    /// <summary>
    /// Builds the runtime visual cells for a draggable block and reports the data BlockController
    /// needs for input, snapping, rotation and placement. Keeping this outside BlockController
    /// makes BlockController focus on interaction state instead of prefab/child-cell construction.
    /// </summary>
    public sealed class BlockVisualBuilder
    {
        public readonly struct BuildResult
        {
            public readonly bool Success;
            public readonly float SlotScale;
            public readonly Vector3 LogicalAnchorLocalPosition;

            public BuildResult(bool success, float slotScale, Vector3 logicalAnchorLocalPosition)
            {
                Success = success;
                SlotScale = slotScale;
                LogicalAnchorLocalPosition = logicalAnchorLocalPosition;
            }
        }

        public BuildResult Rebuild(
            Transform root,
            Transform anchorPoint,
            IReadOnlyList<CellData> runtimeData,
            int columns,
            GameObject cellPrefab,
            Color themeColor,
            float baseSlotScale,
            float minSlotScale,
            float slotMaxVisualCells,
            float cellSpacing,
            List<Transform> childCells,
            List<Vector2Int> shapeOffsets)
        {
            ClearOldVisuals(root, anchorPoint, childCells);
            shapeOffsets?.Clear();

            if (root == null || runtimeData == null || runtimeData.Count == 0 || columns <= 0 || cellPrefab == null)
                return new BuildResult(false, 0f, Vector3.zero);

            List<int> occupiedIndices = CollectOccupiedIndices(runtimeData, columns, out int minX, out int maxX, out int minY, out int maxY);
            if (occupiedIndices.Count == 0)
                return new BuildResult(false, 0f, Vector3.zero);

            int blockWidth = maxX - minX + 1;
            int blockHeight = maxY - minY + 1;
            float slotScale = CalculateSlotScale(blockWidth, blockHeight, baseSlotScale, minSlotScale, slotMaxVisualCells);
            root.localScale = Vector3.one * slotScale;

            float visualCenterX = (minX + maxX) / 2.0f;
            float visualCenterY = (minY + maxY) / 2.0f;
            Vector2Int logicAnchor = ResolveBottomLeftAnchor(occupiedIndices, columns);
            Vector3 logicalAnchorLocalPosition = new Vector3(
                (logicAnchor.x - visualCenterX) * cellSpacing,
                (logicAnchor.y - visualCenterY) * cellSpacing,
                0f
            );

            if (anchorPoint != null)
                anchorPoint.localPosition = logicalAnchorLocalPosition;

            for (int i = 0; i < occupiedIndices.Count; i++)
            {
                int index = occupiedIndices[i];
                CellData cell = runtimeData[index];
                int x = index % columns;
                int y = index / columns;

                GameObject cellObj = Object.Instantiate(cellPrefab, root);
                cellObj.transform.localPosition = new Vector3(
                    (x - visualCenterX) * cellSpacing,
                    (y - visualCenterY) * cellSpacing,
                    0f
                );

                shapeOffsets?.Add(new Vector2Int(x - logicAnchor.x, y - logicAnchor.y));
                InitCellView(cellObj, cell, themeColor);
                childCells?.Add(cellObj.transform);
            }

            return new BuildResult(true, slotScale, logicalAnchorLocalPosition);
        }

        private static void ClearOldVisuals(Transform root, Transform anchorPoint, List<Transform> childCells)
        {
            if (root == null) return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (anchorPoint != null && child == anchorPoint) continue;
                Object.Destroy(child.gameObject);
            }

            childCells?.Clear();
        }

        private static float CalculateSlotScale(int blockWidth, int blockHeight, float baseSlotScale, float minSlotScale, float slotMaxVisualCells)
        {
            float maxDimension = Mathf.Max(1f, Mathf.Max(blockWidth, blockHeight));
            float maxVisualCells = Mathf.Max(1f, slotMaxVisualCells);
            float scaleMultiplier = Mathf.Min(1f, maxVisualCells / maxDimension);
            float scale = baseSlotScale * scaleMultiplier;
            return Mathf.Max(minSlotScale, scale);
        }

        private static List<int> CollectOccupiedIndices(
            IReadOnlyList<CellData> runtimeData,
            int columns,
            out int minX,
            out int maxX,
            out int minY,
            out int maxY)
        {
            minX = int.MaxValue;
            maxX = int.MinValue;
            minY = int.MaxValue;
            maxY = int.MinValue;

            List<int> occupied = new List<int>();

            for (int i = 0; i < runtimeData.Count; i++)
            {
                if (runtimeData[i] == null || !runtimeData[i].isOccupied) continue;

                occupied.Add(i);
                int col = i % columns;
                int row = i / columns;

                minX = Mathf.Min(minX, col);
                maxX = Mathf.Max(maxX, col);
                minY = Mathf.Min(minY, row);
                maxY = Mathf.Max(maxY, row);
            }

            return occupied;
        }

        private static Vector2Int ResolveBottomLeftAnchor(List<int> occupiedIndices, int columns)
        {
            Vector2Int best = Vector2Int.zero;
            bool hasBest = false;

            for (int i = 0; i < occupiedIndices.Count; i++)
            {
                int index = occupiedIndices[i];
                Vector2Int candidate = new Vector2Int(index % columns, index / columns);
                if (!hasBest || candidate.y < best.y || (candidate.y == best.y && candidate.x < best.x))
                {
                    best = candidate;
                    hasBest = true;
                }
            }

            return best;
        }

        private static void InitCellView(GameObject cellObj, CellData cell, Color themeColor)
        {
            GridCellView view = cellObj != null ? cellObj.GetComponent<GridCellView>() : null;
            if (view == null) return;

            GridCell tempCell = new GridCell(0, 0);
            tempCell.SetData(cell.blockCellType);
            view.Init(tempCell, themeColor);
        }
    }
}
