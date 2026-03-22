using _Game.Scripts.Data;

namespace _Game.Scripts.Logic
{
    /// <summary>
    /// Class đại diện cho dữ liệu logic của một ô trên bàn cờ.
    /// Class này không kế thừa MonoBehaviour để tối ưu hiệu năng bộ nhớ.
    /// </summary>
    [System.Serializable]
    public class GridCell
    {
        #region Properties
        // Tọa độ (Immutable)
        public int x { get; private set; }
        public int y { get; private set; }

        // Trạng thái
        public bool IsOccupied { get; private set; }
        public CellType Type { get; private set; }
        public ToolType Tool { get; private set; }
        #endregion

        #region Constructor
        public GridCell(int x, int y)
        {
            this.x = x;
            this.y = y;
            Clear(); // Mặc định là trống
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gán dữ liệu mới cho ô (khi đặt gạch vào)
        /// </summary>
        public void SetData(CellType type, ToolType tool = ToolType.None)
        {
            IsOccupied = true;
            Type = type;
            Tool = tool;
        }

        /// <summary>
        /// Reset ô về trạng thái trống (khi ăn hàng)
        /// </summary>
        public void Clear()
        {
            IsOccupied = false;
            Type = CellType.Empty;
            Tool = ToolType.None;
        }
        #endregion
    }
}