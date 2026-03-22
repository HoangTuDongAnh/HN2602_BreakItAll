using UnityEngine;
using _Game.Scripts.Data;

namespace _Game.Scripts.Core
{
    public class ScoreManager : MonoBehaviour
    {
        #region Singleton
        public static ScoreManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        #endregion

        #region Configuration
        [Header("Scoring Config")]
        [SerializeField] private int _baseLineScore = 100;       // Điểm cơ bản mỗi dòng
        [SerializeField] private int _comboBonusPerStreak = 50;  // Điểm thưởng combo
        #endregion

        #region Runtime Data
        private int _currentScore;
        private int _highScore;
        private int _currentStreak; // Chuỗi combo liên tiếp
        #endregion

        #region Unity Lifecycle
        private void Start() => LoadHighScore();

        private void OnEnable() => GameEvents.OnMoveCompleted += CalculateScore;
        private void OnDisable() => GameEvents.OnMoveCompleted -= CalculateScore;
        #endregion

        #region Score Logic
        public void ResetScore()
        {
            _currentScore = 0;
            _currentStreak = 0;
            UpdateUI();
        }

        // Tính toán điểm sau mỗi nước đi
        private void CalculateScore(int linesCleared, Vector3 position)
        {
            // Không ăn được dòng nào -> Mất Combo
            if (linesCleared == 0)
            {
                _currentStreak = 0;
                GameEvents.OnComboUpdated?.Invoke(0);
                return;
            }

            _currentStreak++;

            // Công thức tính điểm
            int rawScore = linesCleared * _baseLineScore;
            int multiLineBonus = rawScore * linesCleared; // Ăn nhiều dòng cùng lúc điểm càng cao
            int streakBonus = (_currentStreak - 1) * _comboBonusPerStreak;
            int finalMoveScore = multiLineBonus + streakBonus;

            AddScore(finalMoveScore);
            
            // Trigger hiệu ứng rung Camera
            GameEvents.OnComboUpdated?.Invoke(_currentStreak);

            // Trigger hiệu ứng chữ bay (Floating Text)
            TriggerFloatingText(finalMoveScore, linesCleared, position);
        }

        private void TriggerFloatingText(int score, int lines, Vector3 pos)
        {
            string textToShow = $"+{score}";
            float scale = 1f;
            Color textColor = Color.yellow;

            // Logic hiển thị chữ "Ngầu"
            if (_currentStreak > 1)
            {
                textToShow += $"\nCOMBO x{_currentStreak}!";
                scale = 1.2f + (_currentStreak * 0.1f);
                textColor = new Color(1f, 0.5f, 0f); // Màu cam
            }
            else if (lines >= 2)
            {
                textToShow += "\nGREAT!";
                scale = 1.2f;
                textColor = Color.cyan;
            }

            GameEvents.OnShowFloatingText?.Invoke(textToShow, pos, textColor, scale);
        }

        private void AddScore(int amount)
        {
            _currentScore += amount;
            if (_currentScore > _highScore)
            {
                _highScore = _currentScore;
                SaveHighScore();
            }
            UpdateUI();
        }
        #endregion

        #region Data Persistence
        private void UpdateUI() => GameEvents.OnScoreChanged?.Invoke(_currentScore, _highScore);
        
        private void SaveHighScore() => PlayerPrefs.SetInt("HighScore", _highScore);
        
        private void LoadHighScore() => _highScore = PlayerPrefs.GetInt("HighScore", 0);
        
        public int GetCurrentScore() => _currentScore;
        public int GetHighScore() => _highScore;
        #endregion
    }
}