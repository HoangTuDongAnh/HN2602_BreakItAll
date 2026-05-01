using _Game.Scripts.Core;
using _Game.Scripts.Core.Arcade;
using _Game.Scripts.Logic;
using _Game.Scripts.Modes;
using _Game.Scripts.Modes.Levels;
using _Game.Scripts.Modes.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace _Game.Scripts.View.UI
{
    /// <summary>
    /// Gameplay HUD shared by Endless and Arcade. The HUD owns mode visibility only;
    /// objective state remains in ArcadeObjectiveController/objective classes.
    /// </summary>
    public class GameplayHUD : MonoBehaviour
    {
        #region Text References
        [Header("Mode Roots")]
        [SerializeField] private GameObject _endlessRoot;

        [FormerlySerializedAs("_objectiveRoot")]
        [SerializeField] private GameObject _arcadeRoot;

        [Header("Endless")]
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _highScoreText;

        [Header("Arcade Labels")]
        [SerializeField] private GameObject _timeLabelRoot;
        [SerializeField] private GameObject _levelLabelRoot;

        [FormerlySerializedAs("_levelText")]
        [SerializeField] private TextMeshProUGUI _levelText;

        [FormerlySerializedAs("_objectiveText")]
        [SerializeField] private TextMeshProUGUI _arcadeLabelText;

        [FormerlySerializedAs("_objectiveProgressText")]
        [SerializeField] private TextMeshProUGUI _arcadeProgressText;

        [SerializeField] private TextMeshProUGUI _timerText;

        [Header("Fill Navigation")]
        [SerializeField] private Button _fillPreviousButton;
        [SerializeField] private Button _fillNextButton;
        [SerializeField] private TextMeshProUGUI _fillPageText;
        #endregion

        #region Runtime
        private LevelDefinition _activeArcadeLevel;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();

            GameEvents.OnArcadeLevelStarted += HandleArcadeLevelStarted;
            GameEvents.OnObjectiveProgressChanged += HandleObjectiveProgressChanged;
            GameEvents.OnTimerUpdated += UpdateTimer;
            GameEvents.OnGameStarted += RefreshForCurrentSession;
            GameEvents.OnGameOver += HideTimerIfNotNeeded;
            GameEvents.OnFillQueueChanged += UpdateFillNavigation;

            if (_fillPreviousButton != null)
            {
                _fillPreviousButton.onClick.RemoveListener(OnFillPreviousClicked);
                _fillPreviousButton.onClick.AddListener(OnFillPreviousClicked);
            }

            if (_fillNextButton != null)
            {
                _fillNextButton.onClick.RemoveListener(OnFillNextClicked);
                _fillNextButton.onClick.AddListener(OnFillNextClicked);
            }

            RefreshForCurrentSession();
        }

        private void OnDisable()
        {
            GameEvents.OnArcadeLevelStarted -= HandleArcadeLevelStarted;
            GameEvents.OnObjectiveProgressChanged -= HandleObjectiveProgressChanged;
            GameEvents.OnTimerUpdated -= UpdateTimer;
            GameEvents.OnGameStarted -= RefreshForCurrentSession;
            GameEvents.OnGameOver -= HideTimerIfNotNeeded;
            GameEvents.OnFillQueueChanged -= UpdateFillNavigation;

            if (_fillPreviousButton != null)
                _fillPreviousButton.onClick.RemoveListener(OnFillPreviousClicked);

            if (_fillNextButton != null)
                _fillNextButton.onClick.RemoveListener(OnFillNextClicked);
        }
        #endregion

        #region Score API
        public void UpdateScore(int currentScore, int highScore)
        {
            bool showScore = ShouldShowEndlessRoot();

            if (_scoreText)
            {
                _scoreText.gameObject.SetActive(showScore);
                _scoreText.text = currentScore.ToString();
            }

            if (_highScoreText)
            {
                _highScoreText.gameObject.SetActive(showScore);
                _highScoreText.text = highScore.ToString();
            }
        }
        #endregion

        #region Button API
        public void OnPauseClicked()
        {
            if (UIManager.Instance != null) UIManager.Instance.OnPauseSettingBtnClicked();
        }

        public void OnChangeMusicClicked()
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMusicChangeDialog();
        }

        public void OnFillPreviousClicked()
        {
            BlockSpawner.Instance?.ShowPreviousFillPage();
            UpdateFillNavigation();
        }

        public void OnFillNextClicked()
        {
            BlockSpawner.Instance?.ShowNextFillPage();
            UpdateFillNavigation();
        }
        #endregion

        #region Event Handlers
        private void HandleArcadeLevelStarted(LevelDefinition level)
        {
            _activeArcadeLevel = level;
            SetModeRoots(isArcade: true);
            RefreshArcadeLabels(level);
            UpdateFillNavigation();
        }

        private void HandleObjectiveProgressChanged(ObjectiveProgress progress)
        {
            bool isArcade = IsArcadeSession();
            SetModeRoots(isArcade);

            if (_arcadeProgressText)
            {
                _arcadeProgressText.gameObject.SetActive(isArcade);
                _arcadeProgressText.text = BuildArcadeProgress(_activeArcadeLevel, progress);
            }

            UpdateFillNavigation();
        }

        private void RefreshForCurrentSession()
        {
            bool isArcade = IsArcadeSession();
            SetModeRoots(isArcade);

            if (!isArcade)
            {
                _activeArcadeLevel = null;
                ClearArcadeText();
                SetTimerVisible(false);
                SetFillNavigationVisible(false);
                return;
            }

            GameManager manager = GameManager.Instance;
            LevelDefinition level = manager != null && manager.ActiveArcadeLevel != null
                ? manager.ActiveArcadeLevel
                : ArcadeSession.SelectedLevel;

            _activeArcadeLevel = level;
            RefreshArcadeLabels(level);

            if (manager != null)
                HandleObjectiveProgressChanged(manager.CurrentObjectiveProgress);
        }

        private void HideTimerIfNotNeeded()
        {
            if (!IsTimedArcadeLevel())
                SetTimerVisible(false);
        }
        #endregion

        #region Public Timer API
        public void UpdateTimer(float remainingSeconds, float totalSeconds)
        {
            if (_timerText == null || !IsTimedArcadeLevel()) return;

            SetTimerVisible(true);
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds));
            int minutes = seconds / 60;
            int remainder = seconds % 60;
            _timerText.text = $"{minutes:00}:{remainder:00}";
        }
        #endregion

        #region UI State
        private void RefreshArcadeLabels(LevelDefinition level)
        {
            bool hasLevel = level != null;

            if (_levelText)
                _levelText.text = hasLevel ? level.DisplayName : "ARCADE";

            if (_arcadeLabelText)
            {
                _arcadeLabelText.gameObject.SetActive(true);
                _arcadeLabelText.text = BuildArcadeLabel(level);
            }

            if (_arcadeProgressText)
            {
                _arcadeProgressText.gameObject.SetActive(true);
                _arcadeProgressText.text = string.Empty;
            }

            bool isTimed = level != null && level.LevelType == ArcadeLevelType.Timed;
            SetTimerVisible(isTimed);

            if (_levelLabelRoot != null)
                _levelLabelRoot.SetActive(true);

            UpdateFillNavigation();
        }

        private void ClearArcadeText()
        {
            if (_levelText) _levelText.text = string.Empty;
            if (_arcadeLabelText) _arcadeLabelText.text = string.Empty;
            if (_arcadeProgressText) _arcadeProgressText.text = string.Empty;
        }

        private void SetModeRoots(bool isArcade)
        {
            if (_endlessRoot)
                _endlessRoot.SetActive(!isArcade);

            if (_arcadeRoot)
                _arcadeRoot.SetActive(isArcade);
        }

        private void SetTimerVisible(bool visible)
        {
            if (_timeLabelRoot)
                _timeLabelRoot.SetActive(visible);

            if (_timerText)
                _timerText.gameObject.SetActive(visible);
        }

        private void UpdateFillNavigation()
        {
            bool visible = IsFillArcadeLevel();
            SetFillNavigationVisible(visible);
            if (!visible) return;

            BlockSpawner spawner = BlockSpawner.Instance;
            int currentPage = spawner != null ? spawner.CurrentFillPage : 0;
            int totalPages = spawner != null ? spawner.TotalFillPages : 0;
            int unused = spawner != null ? spawner.UnusedFixedFillBlockCount : 0;

            if (_fillPageText != null)
                _fillPageText.text = totalPages > 0 ? $"{currentPage}/{totalPages}  {unused} left" : string.Empty;

            bool canSwitch = totalPages > 1;
            if (_fillPreviousButton != null)
                _fillPreviousButton.interactable = canSwitch;

            if (_fillNextButton != null)
                _fillNextButton.interactable = canSwitch;
        }

        private void SetFillNavigationVisible(bool visible)
        {
            if (_fillPreviousButton != null)
                _fillPreviousButton.gameObject.SetActive(visible);

            if (_fillNextButton != null)
                _fillNextButton.gameObject.SetActive(visible);

            if (_fillPageText != null)
                _fillPageText.gameObject.SetActive(visible);
        }
        #endregion

        #region Reference Helpers
        private void EnsureReferences()
        {
            if (_endlessRoot == null)
                _endlessRoot = FindChildGameObject("Endless_Root");

            if (_arcadeRoot == null)
                _arcadeRoot = FindChildGameObject("Arcade_Root");

            if (_timeLabelRoot == null)
                _timeLabelRoot = FindChildGameObject("Label_Time");

            if (_levelLabelRoot == null)
                _levelLabelRoot = FindChildGameObject("Label_Level");

            if (_levelText == null)
                _levelText = FindText("Text_Level");

            if (_timerText == null)
                _timerText = FindText("Text_Time");

            Transform arcadeRoot = _arcadeRoot != null ? _arcadeRoot.transform : transform;
            if (_arcadeLabelText == null)
                _arcadeLabelText = EnsureText(arcadeRoot, "Text_ArcadeType", new Vector2(0.5f, 1f), new Vector2(0f, -128f), 26f);

            if (_arcadeProgressText == null)
                _arcadeProgressText = EnsureText(arcadeRoot, "Text_ArcadeProgress", new Vector2(0.5f, 1f), new Vector2(0f, -166f), 24f);

            if (_fillPreviousButton == null)
                _fillPreviousButton = EnsureButton(arcadeRoot, "Button_FillPrev", "<", new Vector2(0.5f, 0f), new Vector2(-148f, 92f));

            if (_fillNextButton == null)
                _fillNextButton = EnsureButton(arcadeRoot, "Button_FillNext", ">", new Vector2(0.5f, 0f), new Vector2(148f, 92f));

            if (_fillPageText == null)
                _fillPageText = EnsureText(arcadeRoot, "Text_FillPage", new Vector2(0.5f, 0f), new Vector2(0f, 92f), 20f);
        }

        private GameObject FindChildGameObject(string childName)
        {
            Transform child = FindChild(transform, childName);
            return child != null ? child.gameObject : null;
        }

        private TextMeshProUGUI FindText(string childName)
        {
            Transform child = FindChild(transform, childName);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        private static Transform FindChild(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName)) return null;
            if (root.name == childName) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static TextMeshProUGUI EnsureText(Transform parent, string name, Vector2 anchor, Vector2 position, float fontSize)
        {
            Transform existing = FindChild(parent, name);
            TextMeshProUGUI text = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
            if (text != null) return text;

            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.transform as RectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(280f, 36f);

            text = obj.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            return text;
        }

        private static Button EnsureButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position)
        {
            Transform existing = FindChild(parent, name);
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (button != null) return button;

            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.transform as RectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(58f, 46f);

            Image image = obj.GetComponent<Image>();
            image.color = new Color(0.08f, 0.45f, 0.9f, 0.9f);

            button = obj.GetComponent<Button>();

            TextMeshProUGUI text = EnsureText(obj.transform, "Text", new Vector2(0.5f, 0.5f), Vector2.zero, 28f);
            text.text = label;
            text.raycastTarget = false;

            RectTransform textRect = text.transform as RectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }
        #endregion

        #region Logic Helpers
        private bool IsArcadeSession()
        {
            return GameManager.Instance != null && GameManager.Instance.CurrentModeType == GameModeType.Arcade;
        }

        private bool ShouldShowEndlessRoot()
        {
            return !IsArcadeSession();
        }

        private bool IsTimedArcadeLevel()
        {
            return _activeArcadeLevel != null && _activeArcadeLevel.LevelType == ArcadeLevelType.Timed;
        }

        private bool IsFillArcadeLevel()
        {
            return IsArcadeSession()
                   && _activeArcadeLevel != null
                   && _activeArcadeLevel.LevelType == ArcadeLevelType.Fill;
        }

        private string BuildArcadeLabel(LevelDefinition level)
        {
            if (level == null) return "ARCADE";

            switch (level.LevelType)
            {
                case ArcadeLevelType.Collectable: return "COLLECT";
                case ArcadeLevelType.Timed: return "TIME";
                case ArcadeLevelType.Shape: return "SHAPE";
                case ArcadeLevelType.Fill: return "FILL";
                default: return "ARCADE";
            }
        }

        private string BuildArcadeProgress(LevelDefinition level, ObjectiveProgress progress)
        {
            string raw = !string.IsNullOrEmpty(progress.DisplayText)
                ? progress.DisplayText
                : (progress.TargetValue > 0 ? $"{progress.CurrentValue}/{progress.TargetValue}" : string.Empty);

            if (level == null || string.IsNullOrEmpty(raw))
                return raw;

            switch (level.LevelType)
            {
                case ArcadeLevelType.Timed: return $"Score {raw}";
                case ArcadeLevelType.Collectable: return $"Gem {raw}";
                case ArcadeLevelType.Shape: return $"Shape {raw}";
                case ArcadeLevelType.Fill: return $"Fill {raw}";
                default: return raw;
            }
        }
        #endregion
    }
}
