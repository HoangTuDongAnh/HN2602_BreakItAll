namespace _Game.Scripts.Core.Services
{
    public interface IGameSessionService
    {
        void StartNewGame();
        void ReturnToHome();
        void RestartCurrentGame();
        void HandleGameOver();

        bool IsGameRunning();
        bool IsGameOverState();
    }
}