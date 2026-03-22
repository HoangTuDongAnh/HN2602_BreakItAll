using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Data;

namespace _Game.Scripts.Logic
{
    public static class BlockFactory
    {
        #region Data Structures
        // Struct chứa kết quả trả về sau khi tạo khối
        public struct RuntimeBlockResult
        {
            public List<CellData> cells; // Danh sách các ô
            public int width;
            public int height;
        }
        #endregion

        #region Public Factory Method
        /// <summary>
        /// Tạo ra dữ liệu khối Runtime: Xoay theo yêu cầu và thêm Mechanic (Boom, Tool...)
        /// </summary>
        public static RuntimeBlockResult CreateBlockInstance(BlockData template, int targetRotation, float boomChance, float toolChance)
        {
            // 1. Sao chép dữ liệu gốc (Safe Copy)
            // Đảm bảo list mới luôn đủ kích thước để tránh lỗi IndexOutOfRange
            int expectedCount = template.columns * template.rows;
            List<CellData> currentCells = new List<CellData>(expectedCount);
            
            int limit = Mathf.Min(template.boardData.Count, expectedCount);
            for (int i = 0; i < limit; i++)
            {
                var cell = template.boardData[i];
                currentCells.Add(cell != null ? cell.Clone() : new CellData()); 
            }
            // Điền nốt nếu thiếu
            while (currentCells.Count < expectedCount) currentCells.Add(new CellData());

            // 2. Xử lý xoay khối (Nếu cho phép)
            int currentWidth = template.columns;
            int currentHeight = template.rows;

            if (template.allowRotation)
            {
                int safeRotation = targetRotation % 4; // Giới hạn 0-3 lần xoay
                for (int i = 0; i < safeRotation; i++)
                {
                    Rotate90Clockwise(ref currentCells, ref currentWidth, ref currentHeight);
                }
            }

            // 3. Tiêm Mechanic ngẫu nhiên (Boom, Tool...)
            InjectMechanics(currentCells, boomChance, toolChance);

            return new RuntimeBlockResult
            {
                cells = currentCells,
                width = currentWidth,
                height = currentHeight
            };
        }
        #endregion

        #region Internal Logic
        // Thuật toán xoay mảng 1 chiều (90 độ chiều kim đồng hồ)
        private static void Rotate90Clockwise(ref List<CellData> cells, ref int width, ref int height)
        {
            int totalSize = width * height;
            CellData[] newCellsArray = new CellData[totalSize];
            
            // Kích thước mới sau khi xoay
            int newWidth = height;
            int newHeight = width;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int oldIndex = y * width + x;
                    if (oldIndex >= cells.Count) continue;

                    // Công thức xoay 90 độ: (x, y) -> (newHeight - 1 - y, x)
                    int newX = height - 1 - y;
                    int newY = x;
                    int newIndex = newY * newWidth + newX;
                    
                    if (newIndex < newCellsArray.Length)
                    {
                        newCellsArray[newIndex] = cells[oldIndex];
                    }
                }
            }

            cells = new List<CellData>(newCellsArray);
            width = newWidth;
            height = newHeight;
        }

        private static void InjectMechanics(List<CellData> cells, float boomChance, float toolChance)
        {
            // Tìm danh sách các ô có gạch thực sự
            List<int> occupiedIndices = new List<int>();
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] != null && cells[i].isOccupied) occupiedIndices.Add(i);
            }

            if (occupiedIndices.Count == 0) return;

            // Random chọn 1 ô để biến thành Tool hoặc Boom
            float roll = Random.value;
            int idx = occupiedIndices[Random.Range(0, occupiedIndices.Count)];
            CellData target = cells[idx];

            if (target == null) return;

            if (roll < toolChance)
            {
                target.cellType = CellType.Tool;
                target.toolType = ToolType.Hammer;
            }
            else if (roll < toolChance + boomChance)
            {
                target.cellType = CellType.Boom;
            }
        }
        #endregion
    }
}