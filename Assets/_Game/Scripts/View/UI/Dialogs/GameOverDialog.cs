using TMPro;
using UnityEngine;
using _Game.Scripts.Core;

namespace _Game.Scripts.View.UI.Dialogs
{
    public class GameOverDialog : MonoBehaviour
    {
        [Header("Data Display")]
        [SerializeField] private TextMeshProUGUI _finalScoreText; 
        [SerializeField] private TextMeshProUGUI _finalHighScoreText;

        public void Setup(int score, int highScore)
        {
            if (_finalScoreText) _finalScoreText.text = "Score: " + score.ToString();
            if (_finalHighScoreText) _finalHighScoreText.text = "Highscore: " + highScore.ToString();
        }

        public void OnReplayClicked()
        {
            GameManager.Instance?.StartGame();
        }

        public void OnHomeClicked()
        {
            GameManager.Instance?.EndGame();
        }
    }
}
