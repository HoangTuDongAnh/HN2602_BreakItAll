using _Game.Scripts.Core;
using _Game.Scripts.Core.Arcade;
using _Game.Scripts.Modes.Levels;
using _Game.Scripts.Modes.Map;
using _Game.Scripts.View.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.Scripts.View.UI.Dialogs
{
    /// <summary>
    /// Lightweight Arcade level preview shown from the map before entering gameplay.
    /// Suggested hierarchy:
    /// Dialog_LevelPreview / Preview_Title / Preview_Type / Preview_Objective / Preview_Timer / Preview_Reward / Preview_Tools / Button_Play / Button_Close.
    /// All references are optional and auto-bound by name.
    /// </summary>
    public class LevelPreviewDialog : MonoBehaviour
    {
        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _typeText;
        [SerializeField] private TextMeshProUGUI _objectiveText;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _rewardText;
        [SerializeField] private TextMeshProUGUI _toolRuleText;

        [Header("Buttons")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _closeButton;

        private MapDefinition _map;
        private MapLevelNodeDefinition _node;
        private LevelDefinition _level;

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

        public void Setup(MapDefinition map, MapLevelNodeDefinition node)
        {
            _map = map;
            _node = node;
            _level = node != null ? node.Level : null;

            EnsureReferences();
            WireButtons();
            RefreshTexts();
        }

        public void OnPlayClicked()
        {
            if (_level == null)
                return;

            if (_map != null && _node != null)
                ArcadeSession.SelectLevel(_map, _node);

            gameObject.SetActive(false);
            UIManager.Instance?.CloseAllDialogs();
            GameManager.Instance?.StartArcadeLevel(_level);
        }

        public void OnCloseClicked()
        {
            gameObject.SetActive(false);
            UIManager.Instance?.OnDialogCloseClicked();
        }

        [ContextMenu("Auto Setup References")]
        private void EnsureReferences()
        {
            if (_titleText == null)
                _titleText = FindText("Preview_Title") ?? FindText("LevelPreview_Title") ?? FindText("Text_Title");

            if (_typeText == null)
                _typeText = FindText("Preview_Type") ?? FindText("LevelPreview_Type") ?? FindText("Text_Type");

            if (_objectiveText == null)
                _objectiveText = FindText("Preview_Objective") ?? FindText("LevelPreview_Objective") ?? FindText("Text_Objective");

            if (_timerText == null)
                _timerText = FindText("Preview_Timer") ?? FindText("LevelPreview_Timer") ?? FindText("Text_Timer");

            if (_rewardText == null)
                _rewardText = FindText("Preview_Reward") ?? FindText("LevelPreview_Reward") ?? FindText("Text_Reward");

            if (_toolRuleText == null)
                _toolRuleText = FindText("Preview_Tools") ?? FindText("LevelPreview_Tools") ?? FindText("Text_Tools");

            if (_playButton == null)
                _playButton = FindButton("Button_Play") ?? FindButton("Button_Start");

            if (_closeButton == null)
                _closeButton = FindButton("Button_Close") ?? FindButton("Button_Back") ?? FindButton("Button_Cancel");
        }

        private void WireButtons()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayClicked);
                _playButton.onClick.AddListener(OnPlayClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
                _closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        private void RefreshTexts()
        {
            if (_level == null)
            {
                SetText(_titleText, "Level", true);
                SetText(_typeText, string.Empty, false);
                SetText(_objectiveText, string.Empty, false);
                SetText(_timerText, string.Empty, false);
                SetText(_rewardText, string.Empty, false);
                SetText(_toolRuleText, string.Empty, false);
                return;
            }

            SetText(_titleText, _level.DisplayName, true);
            SetText(_typeText, $"Type: {_level.LevelType}", true);
            SetText(_objectiveText, BuildObjectiveText(_level), true);
            SetText(_timerText, BuildTimerText(_level), _level.UsesTimer);
            SetText(_rewardText, _level.RewardCoins > 0 ? $"Reward: {_level.RewardCoins} Coins" : "Reward: None", true);
            SetText(_toolRuleText, BuildToolRuleText(_level), true);
        }

        private static string BuildObjectiveText(LevelDefinition level)
        {
            ObjectiveDefinition objective = level.Objective;
            if (objective == null)
                return "Goal: Complete the level";

            switch (level.LevelType)
            {
                case ArcadeLevelType.Score:
                    return $"Goal: Reach {objective.targetScore} points";
                case ArcadeLevelType.Collectable:
                    return $"Goal: Collect {objective.targetGemCount} {ReadableItemName(objective.targetItemId)}";
                case ArcadeLevelType.Shape:
                    return objective.requireExactTargetShape
                        ? "Goal: Fill only the highlighted shape"
                        : "Goal: Fill the highlighted shape";
                case ArcadeLevelType.Puzzle:
                    return objective.requireUseAllProvidedShapes
                        ? "Goal: Fill the highlighted area using all blocks"
                        : "Goal: Fill the highlighted area";
                default:
                    return "Goal: Complete the level";
            }
        }

        private static string BuildTimerText(LevelDefinition level)
        {
            if (level == null || !level.UsesTimer || level.TimerRule == null)
                return string.Empty;

            int seconds = Mathf.CeilToInt(level.TimerRule.totalTimeSeconds);
            int minutes = seconds / 60;
            int remainingSeconds = seconds % 60;
            return $"Time: {minutes:00}:{remainingSeconds:00}";
        }

        private static string BuildToolRuleText(LevelDefinition level)
        {
            if (level == null)
                return string.Empty;

            ToolRuleDefinition rule = level.ToolRule;
            if (rule == null)
                return "Tools: Default";

            bool anyAllowed = rule.IsToolAllowed(level.LevelType, _Game.Scripts.Logic.Tools.GameplayToolType.PlaceSingleCell)
                              || rule.IsToolAllowed(level.LevelType, _Game.Scripts.Logic.Tools.GameplayToolType.RemoveSpawnBlock)
                              || rule.IsToolAllowed(level.LevelType, _Game.Scripts.Logic.Tools.GameplayToolType.BombSquare);

            if (!anyAllowed)
                return "Tools: Disabled";

            if (!rule.UseCustomRules)
                return "Tools: Enabled";

            return "Tools: Custom";
        }

        private static string ReadableItemName(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return "items";

            return itemId.Trim();
        }

        private static void SetText(TextMeshProUGUI text, string value, bool visible)
        {
            if (text == null) return;
            text.gameObject.SetActive(visible);
            text.text = visible ? value : string.Empty;
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
    }
}
