using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Data;

namespace _Game.Scripts.Logic
{
    /// <summary>
    /// Tạo runtime block data từ BlockData asset.
    /// Factory chỉ clone/rotate shape, không còn inject mechanic ngẫu nhiên.
    /// Arcade mechanic như Gem/Time/Target nên được đặt ở board/level data.
    /// </summary>
    public static class BlockFactory
    {
        #region Data Structures
        public struct RuntimeBlockResult
        {
            public string templateId;
            public string displayName;
            public List<string> tags;
            public List<CellData> cells;
            public int width;
            public int height;
        }
        #endregion

        #region Public Factory Method
        public static RuntimeBlockResult CreateBlockInstance(BlockData template, int targetRotation)
        {
            if (template == null)
            {
                return new RuntimeBlockResult
                {
                    templateId = string.Empty,
                    displayName = string.Empty,
                    tags = new List<string>(),
                    cells = new List<CellData>(),
                    width = 0,
                    height = 0
                };
            }

            template.EnsureDataSize();

            int expectedCount = Mathf.Max(0, template.columns * template.rows);
            List<CellData> currentCells = new List<CellData>(expectedCount);

            for (int i = 0; i < expectedCount; i++)
            {
                CellData source = i < template.boardData.Count ? template.boardData[i] : null;
                currentCells.Add(source != null ? source.Clone() : new CellData());
            }

            int currentWidth = Mathf.Max(1, template.columns);
            int currentHeight = Mathf.Max(1, template.rows);

            if (template.allowRotation)
            {
                int safeRotation = Mathf.Abs(targetRotation) % 4;
                for (int i = 0; i < safeRotation; i++)
                    Rotate90Clockwise(ref currentCells, ref currentWidth, ref currentHeight);
            }

            return new RuntimeBlockResult
            {
                templateId = template.Id,
                displayName = template.DisplayName,
                tags = template.tags != null ? new List<string>(template.tags) : new List<string>(),
                cells = currentCells,
                width = currentWidth,
                height = currentHeight
            };
        }
        #endregion

        #region Internal Logic
        private static void Rotate90Clockwise(ref List<CellData> cells, ref int width, ref int height)
        {
            int newWidth = height;
            int newHeight = width;
            CellData[] newCellsArray = new CellData[newWidth * newHeight];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int oldIndex = y * width + x;
                    if (oldIndex < 0 || oldIndex >= cells.Count) continue;

                    int newX = height - 1 - y;
                    int newY = x;
                    int newIndex = newY * newWidth + newX;

                    if (newIndex >= 0 && newIndex < newCellsArray.Length)
                        newCellsArray[newIndex] = cells[oldIndex] != null ? cells[oldIndex].Clone() : new CellData();
                }
            }

            for (int i = 0; i < newCellsArray.Length; i++)
                if (newCellsArray[i] == null) newCellsArray[i] = new CellData();

            cells = new List<CellData>(newCellsArray);
            width = newWidth;
            height = newHeight;
        }
        #endregion
    }
}
