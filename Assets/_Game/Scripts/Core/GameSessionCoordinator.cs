using UnityEngine;
using _Game.Scripts.Core.Services;
using _Game.Scripts.Logic;
using _Game.Scripts.View.UI;
using _Game.Scripts.Data;

namespace _Game.Scripts.Core
{
    public class GameSessionCoordinator : MonoBehaviour, IGameSessionService
    {
        private void Awake()
        {
            GameServices.RegisterSession(this);
        }

        private void OnDestroy()
        {
            if (GameServices.Session == this)
                GameServices.RegisterSession(null);
        }

        public bool IsGameRunning()
        {
            return GameServices.GameState != null &&
                   GameServices.GameState.CurrentState == GameState.Playing;
        }

        public bool IsGameOverState()
        {
            return GameServices.GameState != null &&
                   GameServices.GameState.CurrentState == GameState.GameOver;
        }

        public void StartNewGame()
        {
            if (GameServices.GameState != null)
                GameServices.GameState.SetState(GameState.Playing);

            ResetGameplayWorld();
            ShowGameplayUI();

            GameEvents.OnGameStarted?.Invoke();
        }

        public void ReturnToHome()
        {
            if (GameServices.GameState != null)
                GameServices.GameState.SetState(GameState.Home);

            ClearGameplayWorld();
            ShowHomeUI();
        }

        public void RestartCurrentGame()
        {
            StartNewGame();
        }

        public void HandleGameOver()
        {
            if (IsGameOverState())
                return;

            if (GameServices.GameState != null)
                GameServices.GameState.SetState(GameState.GameOver);

            ShowGameOverUI();
            GameEvents.OnGameOver?.Invoke();
        }

        #region Internal Session Flow
        private void ResetGameplayWorld()
        {
            if (GameServices.Score != null)
                GameServices.Score.ResetScore();

            if (GridManager.Instance != null)
                GridManager.Instance.ClearBoard();

            if (BlockSpawner.Instance != null)
                BlockSpawner.Instance.ResetSpawner();
        }

        private void ClearGameplayWorld()
        {
            if (BlockSpawner.Instance != null)
                BlockSpawner.Instance.ClearSpawner();

            if (GridManager.Instance != null)
                GridManager.Instance.ClearBoard();
        }

        private void ShowGameplayUI()
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowGameplay();
        }

        private void ShowHomeUI()
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowHome();
        }

        private void ShowGameOverUI()
        {
            if (UIManager.Instance != null)
            {
                int currentScore = GameServices.Score != null ? GameServices.Score.CurrentScore : 0;
                int bestScore = GameServices.Score != null ? GameServices.Score.BestScore : 0;
                UIManager.Instance.ShowGameOver(currentScore, bestScore);
            }
        }
        #endregion
    }
}