using UnityEngine;
using _Game.Scripts.Data;
using _Game.Scripts.Logic;
using _Game.Scripts.View.UI;

namespace _Game.Scripts.Core
{
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        #endregion

        #region State Management
        [field: SerializeField] 
        public GameState CurrentState { get; private set; }

        private void Start()
        {
            // Mặc định vào game là ở màn hình Home
            SetState(GameState.Home);
        }

        public void SetState(GameState newState)
        {
            CurrentState = newState;
            switch (newState)
            {
                case GameState.Home:
                    Time.timeScale = 1; 
                    UIManager.Instance.ShowHome();
                    break;
                    
                case GameState.Playing:
                    Time.timeScale = 1; 
                    UIManager.Instance.ShowGameplay();
                    break;
                    
                case GameState.Paused:
                    Time.timeScale = 0; // Dừng thời gian để pause
                    break;
                    
                case GameState.GameOver:
                    // Hiển thị dialog thua cuộc kèm điểm số
                    int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.GetCurrentScore() : 0;
                    int finalHighScore = ScoreManager.Instance != null ? ScoreManager.Instance.GetHighScore() : 0;
                    
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayGameOverSound();
                    }
                    
                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowGameOver(finalScore, finalHighScore);
                    break;
            }
        }
        #endregion

        #region Game Flow Control (Điều khiển luồng)

        public void StartGame()
        {
            // Reset toàn bộ dữ liệu để chơi ván mới
            if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
            if (GridManager.Instance != null) GridManager.Instance.ClearBoard();
            if (BlockSpawner.Instance != null) BlockSpawner.Instance.ResetSpawner();

            SetState(GameState.Playing);
        }

        public void PauseGame()
        {
            SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            SetState(GameState.Playing);

            // Kiểm tra xem có cần sinh khối không (phòng trường hợp pause lúc đang sinh)
            if (BlockSpawner.Instance != null)
            {
                BlockSpawner.Instance.CheckAndSpawnIfNeeded();
            }
        }

        public void EndGame() 
        {
            SetState(GameState.Home);
        }
        
        public void TriggerGameOver()
        {
            if (CurrentState != GameState.Playing) return;
            SetState(GameState.GameOver);
        }

        #endregion
    }
}