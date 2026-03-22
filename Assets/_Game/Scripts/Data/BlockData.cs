using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Data
{
    /// <summary>
    /// Dữ liệu khuôn mẫu của một khối gạch (chưa bao gồm logic runtime).
    /// </summary>
    [CreateAssetMenu(fileName = "NewBlockShape", menuName = "Game/Block Shape Template")]
    public class BlockData : ScriptableObject
    {
        #region Configuration (Cấu hình Editor)
        [Header("Classification")]
        [Tooltip("Độ khó của khối này (Dùng cho thuật toán sinh khối thông minh)")]
        public BlockTier tier = BlockTier.Tier1_Easy;
        
        [Header("Shape Dimensions")]
        [Tooltip("Số cột của ma trận khối (Width)")]
        public int columns = 5;
        
        [Tooltip("Số hàng của ma trận khối (Height)")]
        public int rows = 5;
        
        [Tooltip("Cho phép xoay khối này khi sinh ra? (Tắt với khối vuông/đối xứng)")]
        public bool allowRotation = true;
        #endregion

        #region Raw Data
        // Lưu trữ cấu trúc hình dáng dưới dạng danh sách phẳng (Flat List 1 chiều)
        // Unity không serialize được mảng 2 chiều nên phải dùng cách này.
        [HideInInspector]
        public List<CellData> boardData = new List<CellData>();
        #endregion

        #region Helper Methods (Truy xuất dữ liệu)
        
        // Reset dữ liệu về rỗng (Dùng cho Custom Editor nếu có)
        public void ClearData()
        {
            boardData.Clear();
            for (int i = 0; i < columns * rows; i++)
            {
                boardData.Add(new CellData());
            }
        }

        // Lấy dữ liệu ô tại tọa độ (x, y)
        public CellData GetCell(int x, int y)
        {
            int index = y * columns + x;
            if (index >= 0 && index < boardData.Count)
                return boardData[index];
            return null;
        }

        // Trả về danh sách tọa độ các ô có gạch (Dùng để tính toán va chạm)
        public List<Vector2Int> GetOccupiedCoordinates()
        {
            List<Vector2Int> occupied = new List<Vector2Int>();
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (GetCell(x, y).isOccupied)
                    {
                        occupied.Add(new Vector2Int(x, y));
                    }
                }
            }
            return occupied;
        }
        #endregion
    }

    #region Inner Class (Dữ liệu ô)
    [System.Serializable]
    public class CellData
    {
        [Tooltip("Ô này có gạch hay là ô rỗng?")]
        public bool isOccupied = false;
        
        // Mechanic thực tế sẽ được random tại Runtime, ở đây chỉ giữ chỗ
        [HideInInspector] public CellType cellType = CellType.Normal; 
        [HideInInspector] public ToolType toolType = ToolType.None;

        // Tạo bản sao (Deep Copy) để không ảnh hưởng dữ liệu gốc
        public CellData Clone()
        {
            return new CellData
            {
                isOccupied = this.isOccupied,
                cellType = this.cellType,
                toolType = this.toolType
            };
        }
    }
    #endregion
}