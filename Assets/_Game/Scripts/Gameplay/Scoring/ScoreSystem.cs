namespace BreakItAll.Gameplay
{
    /// <summary>
    /// Score system dùng config runtime, không hardcode ở flow bên ngoài.
    /// </summary>
    public sealed class ScoreSystem
    {
        private readonly int _scorePerLine;
        private readonly int _comboBonusPerStep;
        private readonly int _comboResetAfterMoves;

        public int CurrentScore { get; private set; }
        public int CurrentComboStep { get; private set; }
        public int MovesSinceLastClear { get; private set; }

        public ScoreSystem(int scorePerLine, int comboBonusPerStep, int comboResetAfterMoves)
        {
            _scorePerLine = scorePerLine;
            _comboBonusPerStep = comboBonusPerStep;
            _comboResetAfterMoves = comboResetAfterMoves <= 0 ? 1 : comboResetAfterMoves;
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
            int gainedScore = baseScore + comboBonus;

            CurrentScore += gainedScore;
            return gainedScore;
        }
    }
}