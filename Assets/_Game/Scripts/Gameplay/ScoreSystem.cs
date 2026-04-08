namespace BreakItAll.Gameplay
{
    /// <summary>
    /// Score system tạm cho M1.4.
    /// M2 sẽ chuyển sang nhận config từ GameSettingsDefinition.
    /// </summary>
    public sealed class ScoreSystem
    {
        private readonly int _scorePerLine;
        private readonly int _comboBonusPerStep;
        private readonly int _comboResetAfterMoves;

        public int CurrentScore { get; private set; }
        public int CurrentComboStep { get; private set; }
        public int MovesSinceLastClear { get; private set; }

        public ScoreSystem(int scorePerLine = 100, int comboBonusPerStep = 25, int comboResetAfterMoves = 1)
        {
            _scorePerLine = scorePerLine;
            _comboBonusPerStep = comboBonusPerStep;
            _comboResetAfterMoves = comboResetAfterMoves;
        }

        public void Reset()
        {
            CurrentScore = 0;
            CurrentComboStep = 0;
            MovesSinceLastClear = 0;
        }

        public int ApplyMove(ClearResolutionResult clearResult)
        {
            if (!clearResult.HasAnyClear)
            {
                MovesSinceLastClear++;

                if (MovesSinceLastClear >= _comboResetAfterMoves)
                {
                    CurrentComboStep = 0;
                }

                return 0;
            }

            int linesCleared = clearResult.ClearedRowCount + clearResult.ClearedColumnCount;
            CurrentComboStep++;
            MovesSinceLastClear = 0;

            int baseScore = linesCleared * _scorePerLine;
            int comboBonus = CurrentComboStep * _comboBonusPerStep;
            int gained = baseScore + comboBonus;

            CurrentScore += gained;
            return gained;
        }
    }
}