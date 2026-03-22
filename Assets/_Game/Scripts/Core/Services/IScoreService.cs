namespace _Game.Scripts.Core.Services
{
    public interface IScoreService
    {
        int CurrentScore { get; }
        int BestScore { get; }

        void ResetScore();
        void AddPlacementScore(int occupiedCellCount);
        void AddLineClearScore(int totalLines, int combo);
        void SaveHighScore();
    }
}