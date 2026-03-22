namespace _Game.Scripts.Data
{
    #region Cell & Block Definitions
    /// <summary>
    /// Các loại ô gạch đặc biệt trên bàn cờ
    /// </summary>
    public enum CellType
    {
        Empty = 0,      // Ô trống (Không dùng, nhưng giữ làm mặc định)
        Normal = 1,     // Gạch thường
        Boom = 2,       // Gạch chứa bom (Nổ khi ăn hàng)
        Tool = 3,       // Gạch chứa công cụ (Thu thập khi ăn hàng)
        Ice = 4         // Gạch băng (Cần ăn 2 lần - Tính năng mở rộng)
    }

    /// <summary>
    /// Các loại công cụ hỗ trợ (Power-ups) người chơi có thể thu thập
    /// </summary>
    public enum ToolType
    {
        None = 0,
        Hammer = 1,     // Búa: Phá 1 ô bất kỳ
        Refresh = 2,    // Làm mới: Đổi 3 khối đang chờ trên tay
    }
    
    /// <summary>
    /// Phân cấp độ khó của khối gạch (Dùng cho AI sinh khối)
    /// </summary>
    public enum BlockTier
    {
        Tier1_Easy,   // Khối nhỏ (1-2 ô) -> Dễ lấp lỗ hổng, dùng để cứu thua.
        Tier2_Medium, // Khối trung bình (3-4 ô) -> Xuất hiện thường xuyên.
        Tier3_Hard    // Khối lớn/dị (3x3, 5 ô dài) -> Khó đặt, dùng để tạo thử thách.
    }
    #endregion

    #region Game Flow
    /// <summary>
    /// Các trạng thái chính của vòng đời game
    /// </summary>
    public enum GameState
    {
        Home,       // Màn hình chờ
        Playing,    // Đang chơi
        Paused,     // Tạm dừng
        GameOver    // Kết thúc game
    }
    #endregion
}