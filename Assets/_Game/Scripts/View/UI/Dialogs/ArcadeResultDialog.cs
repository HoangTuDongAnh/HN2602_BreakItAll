using _Game.Scripts.Core;
using _Game.Scripts.Modes.Levels;
using _Game.Scripts.Modes.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.Scripts.View.UI.Dialogs
{
    /// <summary>
    /// Compact Arcade result dialog.
    /// Supports the current mockup:
    /// Dialog_ArcadeResult / ArcadeResult_Title / ArcadeResult_Text / Reward_Text / Button_Home / Button_NextLevel / Button_Replay.
    /// Button text references are intentionally optional because the current UI uses image buttons without separate text binding.
    /// </summary>
    public class ArcadeResultDialog : MonoBehaviour
    {
        [Header("Data Display")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _summaryText;
        [SerializeField] private TextMeshProUGUI _rewardText;

        [Header("Legacy Optional Texts")]
        [Tooltip("Optional legacy detail text. Hidden automatically when compact summary text exists.")]
        [SerializeField] private TextMeshProUGUI _levelText;
        [Tooltip("Optional legacy detail text. Hidden automatically when compact summary text exists.")]
        [SerializeField] private TextMeshProUGUI _typeText;
        [Tooltip("Optional legacy detail text. Hidden automatically when compact summary text exists.")]
        [SerializeField] private TextMeshProUGUI _progressText;

        [Header("Buttons")]
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _homeButton;

        private void Awake()
        {
            EnsureReferences();
            WireButtons();
        }

        private void OnEnable()
        {
            EnsureReferences();
            WireButtons();
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

        public void Setup(LevelDefinition level, ObjectiveProgress progress, bool success, int rewardCoins)
        {
            EnsureReferences();
            WireButtons();

            string title = BuildLevelTitle(level);
            string statusLine = success ? "Level Complete" : "Level Failed";
            string typeLine = level != null ? level.LevelType.ToString() : string.Empty;
            string progressLine = BuildProgressText(progress);

            SetText(_titleText, title, true);

            if (_summaryText != null)
            {
                _summaryText.gameObject.SetActive(true);
                _summaryText.text = BuildSummaryText(statusLine, typeLine, progressLine);

                SetText(_levelText, string.Empty, false);
                SetText(_typeText, string.Empty, false);
                SetText(_progressText, string.Empty, false);
            }
            else
            {
                SetText(_levelText, statusLine, !string.IsNullOrEmpty(statusLine));
                SetText(_typeText, typeLine, !string.IsNullOrEmpty(typeLine));
                SetText(_progressText, progressLine, !string.IsNullOrEmpty(progressLine));
            }

            bool showReward = success && rewardCoins > 0;
            SetText(_rewardText, showReward ? $"+{rewardCoins} Coins" : string.Empty, showReward);

            if (_retryButton != null)
                _retryButton.gameObject.SetActive(!success);

            if (_continueButton != null)
                _continueButton.gameObject.SetActive(true);

            if (_homeButton != null)
                _homeButton.gameObject.SetActive(true);
        }

        public void OnRetryClicked()
        {
            GameManager.Instance?.RestartGame();
        }

        public void OnContinueClicked()
        {
            GameManager.Instance?.ReturnToArcadeMap();
        }

        public void OnHomeClicked()
        {
            GameManager.Instance?.ReturnToHome();
        }

        [ContextMenu("Auto Setup References")]
        private void EnsureReferences()
        {
            if (_titleText == null)
                _titleText = FindText("ArcadeResult_Title") ?? FindText("Title") ?? FindText("Text_Title");

            if (_summaryText == null)
                _summaryText = FindText("ArcadeResult_Text") ?? FindText("Text_Summary") ?? FindText("Summary_Text");

            if (_rewardText == null)
                _rewardText = FindText("Reward_Text") ?? FindText("Text_Reward");

            if (_levelText == null)
                _levelText = FindText("Level_Text") ?? FindText("Text_Level");

            if (_typeText == null)
                _typeText = FindText("Type_Text") ?? FindText("Text_Type");

            if (_progressText == null)
                _progressText = FindText("Progress_Text") ?? FindText("Text_Progress");

            if (_retryButton == null)
                _retryButton = FindButton("Button_Replay") ?? FindButton("Button_Retry");

            if (_continueButton == null)
                _continueButton = FindButton("Button_NextLevel") ?? FindButton("Button_Continue") ?? FindButton("Button_Map");

            if (_homeButton == null)
                _homeButton = FindButton("Button_Home");
        }

        private void WireButtons()
        {
            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveListener(OnRetryClicked);
                _retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(OnContinueClicked);
                _continueButton.onClick.AddListener(OnContinueClicked);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.RemoveListener(OnHomeClicked);
                _homeButton.onClick.AddListener(OnHomeClicked);
            }
        }

        private TextMeshProUGUI FindText(string objectName)
        {
            Transform child = FindChild(transform, objectName);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        private Button FindButton(string objectName)
        {
            Transform child = FindChild(transform, objectName);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private static Transform FindChild(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrEmpty(objectName)) return null;
            if (root.name == objectName) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChild(root.GetChild(i), objectName);
                if (result != null) return result;
            }

            return null;
        }

        private static void SetText(TextMeshProUGUI text, string value, bool visible)
        {
            if (text == null) return;
            text.gameObject.SetActive(visible);
            text.text = visible ? value : string.Empty;
        }

        private static string BuildLevelTitle(LevelDefinition level)
        {
            if (level == null)
                return "Arcade";

            if (level.OrderIndex > 0)
                return $"Level {level.OrderIndex}";

            return level.NumberedDisplayName;
        }

        private static string BuildSummaryText(string statusLine, string typeLine, string progressLine)
        {
            bool hasType = !string.IsNullOrWhiteSpace(typeLine);
            bool hasProgress = !string.IsNullOrWhiteSpace(progressLine);

            if (hasType && hasProgress)
                return $"{statusLine}\n{typeLine}\n{progressLine}";

            if (hasType)
                return $"{statusLine}\n{typeLine}";

            if (hasProgress)
                return $"{statusLine}\n{progressLine}";

            return statusLine;
        }

        private static string BuildProgressText(ObjectiveProgress progress)
        {
            if (!string.IsNullOrEmpty(progress.DisplayText))
                return progress.DisplayText;

            return progress.TargetValue > 0 ? $"{progress.CurrentValue}/{progress.TargetValue}" : string.Empty;
        }
    }
}
