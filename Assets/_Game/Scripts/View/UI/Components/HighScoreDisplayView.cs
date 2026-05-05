using _Game.Scripts.Core;
using _Game.Scripts.Core.Services;
using TMPro;
using UnityEngine;

namespace _Game.Scripts.View.UI.Components
{
    /// <summary>
    /// Home-only Endless high-score display.
    /// Suggested hierarchy: HighScoreDisplay / Txt_HighScore.
    /// This is intentionally independent from ArcadeResultDialog; Arcade results should show reward/progress, not Endless score.
    /// </summary>
    public class HighScoreDisplayView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI _highScoreText;

        [Header("Format")]
        [SerializeField] private string _prefix = "BEST ";
        [SerializeField] private string _suffix;
        [SerializeField] private bool _useCompactFormat = true;
        [SerializeField] private bool _refreshOnEnable = true;

        private int _lastValue = int.MinValue;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            GameEvents.OnScoreChanged += HandleScoreChanged;

            if (_refreshOnEnable)
                Refresh();
        }

        private void OnDisable()
        {
            GameEvents.OnScoreChanged -= HandleScoreChanged;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                EnsureReferences();
                UnityEditor.EditorUtility.SetDirty(this);
            };
        }
#endif

        [ContextMenu("Refresh")]
        public void Refresh()
        {
            int bestScore = ScoreManager.Instance != null
                ? ScoreManager.Instance.BestScore
                : GameSave.GetInt(SaveKeys.HighScore, 0);

            SetHighScore(bestScore);
        }

        private void HandleScoreChanged(int currentScore, int highScore)
        {
            SetHighScore(highScore);
        }

        private void SetHighScore(int highScore)
        {
            if (_lastValue == highScore && _highScoreText != null && !string.IsNullOrEmpty(_highScoreText.text))
                return;

            _lastValue = highScore;

            if (_highScoreText != null)
                _highScoreText.text = $"{_prefix}{FormatScore(highScore)}{_suffix}";
        }

        private string FormatScore(int score)
        {
            if (!_useCompactFormat)
                return score.ToString();

            if (score >= 1000000)
                return $"{score / 1000000f:0.#}M";

            if (score >= 1000)
                return $"{score / 1000f:0.#}K";

            return score.ToString();
        }

        [ContextMenu("Auto Setup References")]
        private void EnsureReferences()
        {
            if (_highScoreText == null)
                _highScoreText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }
}
