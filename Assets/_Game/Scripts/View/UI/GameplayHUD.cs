using TMPro;
using UnityEngine;

namespace _Game.Scripts.View.UI
{
    public class GameplayHUD : MonoBehaviour
    {
        [Header("Text References")]
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _highScoreText;

        public void UpdateScore(int currentScore, int highScore)
        {
            if (_scoreText) _scoreText.text = currentScore.ToString();
            if (_highScoreText) _highScoreText.text = highScore.ToString();
        }

        public void OnPauseClicked()
        {
            if (UIManager.Instance != null) UIManager.Instance.OnPauseSettingBtnClicked();
        }
        
        public void OnChangeMusicClicked()
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMusicChangeDialog();
        }
    }
}