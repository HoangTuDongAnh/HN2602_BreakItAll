namespace _Game.Scripts.Data
{
    #region Cell & Block Definitions
    /// <summary>
    /// Loại cell nằm trong runtime block. V1 giữ tối giản, không còn Boom/Tool/Ice.
    /// Board mới là nơi giữ item/objective marker cho Arcade.
    /// </summary>
    public enum BlockCellType
    {
        Empty = 0,
        Normal = 1,
        TimeBonus = 2,
        Gem = 3
    }

    /// <summary>
    /// Item/marker nằm trên board cho Arcade objective.
    /// Collect/Time/Shape sẽ đọc dữ liệu board thay vì nhét mechanic vào block.
    /// </summary>
    public enum BoardItemType
    {
        None = 0,
        Gem = 1,
        TimeBonus = 2,
        TargetPattern = 3
    }

    /// <summary>
    /// Phân cấp độ khó của khối gạch, dùng cho spawn strategy.
    /// </summary>
    public enum BlockTier
    {
        Tier1_Easy,
        Tier2_Medium,
        Tier3_Hard
    }
    #endregion

    #region Game Flow
    public enum GameState
    {
        Home,
        Playing,
        Paused,
        GameOver
    }
    #endregion
}
