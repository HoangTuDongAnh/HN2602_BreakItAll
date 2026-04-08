using BreakItAll.Gameplay;

namespace BreakItAll.Modes
{
    /// <summary>
    /// Runtime state cho một session Endless.
    /// </summary>
    public sealed class EndlessSessionContext
    {
        public BoardController BoardController { get; }
        public ScoreSystem ScoreSystem { get; }

        public bool IsGameOver { get; private set; }
        public int LastGainedScore { get; private set; }

        public int CurrentScore => ScoreSystem.CurrentScore;
        public int CurrentComboStep => ScoreSystem.CurrentComboStep;

        public EndlessSessionContext(BoardController boardController, ScoreSystem scoreSystem)
        {
            BoardController = boardController;
            ScoreSystem = scoreSystem;
        }

        public void Reset()
        {
            IsGameOver = false;
            LastGainedScore = 0;
            BoardController.Initialize();
            ScoreSystem.Reset();
        }

        public void SetLastGainedScore(int value)
        {
            LastGainedScore = value;
        }

        public void MarkGameOver()
        {
            IsGameOver = true;
        }
    }
}