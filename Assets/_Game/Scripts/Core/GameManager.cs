using UnityEngine;
using _Game.Scripts.Data;
using _Game.Scripts.Core.Services;
using _Game.Scripts.Logic;
using _Game.Scripts.View.UI;

namespace _Game.Scripts.Core
{
    public class GameManager : MonoBehaviour, IGameStateService
    {
        #region Singleton
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            GameServices.RegisterGameState(this);
        }

        private void OnDestroy()
        {
            if (GameServices.GameState == this)
                GameServices.RegisterGameState(null);
        }
        #endregion

        #region State
        [field: SerializeField]
        public GameState CurrentState { get; private set; } = GameState.Home;
        #endregion

        #region Lifecycle
        private void Start()
        {
            SetState(GameState.Home);

            if (BlockSpawner.Instance != null)
                BlockSpawner.Instance.ClearSpawner();

            if (GridManager.Instance != null)
                GridManager.Instance.ClearBoard();

            if (UIManager.Instance != null)
                UIManager.Instance.ShowHome();
        }
        #endregion

        #region IGameStateService
        public bool IsInputAllowed()
        {
            return CurrentState == GameState.Playing;
        }

        public void SetState(GameState state)
        {
            CurrentState = state;

            switch (CurrentState)
            {
                case GameState.Home:
                    Time.timeScale = 1f;
                    break;

                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;

                case GameState.GameOver:
                    Time.timeScale = 1f;
                    break;
            }
        }
        #endregion

        #region Public Controls
        public void StartGame()
        {
            if (GameServices.Session != null)
            {
                GameServices.Session.StartNewGame();
                return;
            }

            FallbackStartGame();
        }

        public void EndGame()
        {
            if (GameServices.Session != null)
            {
                GameServices.Session.ReturnToHome();
                return;
            }

            FallbackReturnHome();
        }

        public void RestartGame()
        {
            if (GameServices.Session != null)
            {
                GameServices.Session.RestartCurrentGame();
                return;
            }

            FallbackStartGame();
        }

        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;

            SetState(GameState.Paused);
            GameEvents.OnGamePaused?.Invoke();
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;

            SetState(GameState.Playing);
            GameEvents.OnGameResumed?.Invoke();
        }

        public void TriggerGameOver()
        {
            if (GameServices.Session != null)
            {
                GameServices.Session.HandleGameOver();
                return;
            }

            FallbackTriggerGameOver();
        }
        #endregion

        #region Fallback Flow
        private void FallbackStartGame()
        {
            SetState(GameState.Playing);

            if (GameServices.Score != null)
                GameServices.Score.ResetScore();

            if (GridManager.Instance != null)
                GridManager.Instance.ClearBoard();

            if (UIManager.Instance != null)
                UIManager.Instance.ShowGameplay();

            if (BlockSpawner.Instance != null)
                BlockSpawner.Instance.ResetSpawner();

            GameEvents.OnGameStarted?.Invoke();
        }

        private void FallbackReturnHome()
        {
            SetState(GameState.Home);

            if (BlockSpawner.Instance != null)
                BlockSpawner.Instance.ClearSpawner();

            if (GridManager.Instance != null)
                GridManager.Instance.ClearBoard();

            if (UIManager.Instance != null)
                UIManager.Instance.ShowHome();
        }

        private void FallbackTriggerGameOver()
        {
            if (CurrentState == GameState.GameOver) return;

            SetState(GameState.GameOver);

            if (UIManager.Instance != null)
            {
                int currentScore = GameServices.Score != null ? GameServices.Score.CurrentScore : 0;
                int bestScore = GameServices.Score != null ? GameServices.Score.BestScore : 0;
                UIManager.Instance.ShowGameOver(currentScore, bestScore);
            }

            GameEvents.OnGameOver?.Invoke();
        }
        #endregion
    }
}