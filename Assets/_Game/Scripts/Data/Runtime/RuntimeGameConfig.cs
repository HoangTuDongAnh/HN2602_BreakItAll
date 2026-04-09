namespace BreakItAll.Data
{
    /// <summary>
    /// Cấu hình runtime đã resolve từ authored data.
    /// Dùng để khởi tạo gameplay session một cách gọn và rõ ràng.
    /// </summary>
    public sealed class RuntimeGameConfig
    {
        public int BoardWidth { get; }
        public int BoardHeight { get; }
        public int QueueSize { get; }

        public int ScorePerLine { get; }
        public int ComboBonusPerStep { get; }
        public int ComboResetAfterMoves { get; }

        public RuntimeGameConfig(
            int boardWidth,
            int boardHeight,
            int queueSize,
            int scorePerLine,
            int comboBonusPerStep,
            int comboResetAfterMoves)
        {
            BoardWidth = boardWidth;
            BoardHeight = boardHeight;
            QueueSize = queueSize;
            ScorePerLine = scorePerLine;
            ComboBonusPerStep = comboBonusPerStep;
            ComboResetAfterMoves = comboResetAfterMoves;
        }
    }
}