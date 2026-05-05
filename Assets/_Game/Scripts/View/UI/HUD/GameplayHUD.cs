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
    /// Gameplay HUD shared by Endless and Arcade. Arcade labels are split by level type so
    /// each objective can own its own layout without leaking controls into other types.
    /// </summary>
    public partial class GameplayHUD : MonoBehaviour
    {
        #region Text References
        [Header("Mode Roots")]
        [SerializeField] private GameObject _endlessRoot;

        [FormerlySerializedAs("_objectiveRoot")]
        [SerializeField] private GameObject _arcadeRoot;

        [Header("Endless")]
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _highScoreText;

        [Header("Arcade Common")]
        [SerializeField] private GameObject _levelLabelRoot;

        [FormerlySerializedAs("_levelText")]
        [SerializeField] private TextMeshProUGUI _levelText;

        [Header("Arcade Type Labels")]
        [FormerlySerializedAs("_timeLabelRoot")]
        [SerializeField] private GameObject _scoreLabelRoot;
        [SerializeField] private GameObject _collectableLabelRoot;
        [SerializeField] private GameObject _shapeLabelRoot;
        [SerializeField] private GameObject _puzzleLabelRoot;

        [Header("Score Label")]
        [FormerlySerializedAs("_timerText")]
        [SerializeField] private TextMeshProUGUI _scoreTimerText;

        [FormerlySerializedAs("_arcadeProgressText")]
        [FormerlySerializedAs("_objectiveProgressText")]
        [SerializeField] private TextMeshProUGUI _scoreProgressText;

        [Header("Collectable Label")]
        [SerializeField] private TextMeshProUGUI _collectableTimerText;
        [SerializeField] private TextMeshProUGUI _collectableProgressText;

        [Header("Shape Label")]
        [SerializeField] private Button _shapeOverlayButton;
        [SerializeField] private TextMeshProUGUI _shapeOverlayButtonText;
        [SerializeField] private TextMeshProUGUI _shapeProgressText;

        [Header("Puzzle Queue")]
        [SerializeField] private TextMeshProUGUI _puzzleTimerText;
        [HideInInspector, SerializeField] private TextMeshProUGUI _puzzleProgressText;
        [SerializeField] private Button _puzzleSwitchButton;
        [SerializeField] private TextMeshProUGUI _puzzleSwitchButtonText;
        [SerializeField] private TextMeshProUGUI _puzzleRemainingText;

        [FormerlySerializedAs("_fillPreviousButton")]
        [HideInInspector, SerializeField] private Button _puzzlePreviousButton;

        [FormerlySerializedAs("_fillNextButton")]
        [HideInInspector, SerializeField] private Button _puzzleNextButton;

        [FormerlySerializedAs("_fillPageText")]
        [HideInInspector, SerializeField] private TextMeshProUGUI _puzzlePageText;

        [Header("Legacy Arcade")]
        [FormerlySerializedAs("_arcadeLabelText")]
        [FormerlySerializedAs("_objectiveText")]
        [SerializeField] private TextMeshProUGUI _legacyArcadeLabelText;
        #endregion

        #region Runtime
        private LevelDefinition _activeArcadeLevel;
        private bool _shapeOverlayVisible;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            EnsureReferences();
            ConfigurePuzzleQueueReferences();
            NormalizeRuntimeVisibility();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;

                EnsureReferences();
                ConfigurePuzzleQueueReferences();
                NormalizeRuntimeVisibility();
                UnityEditor.EditorUtility.SetDirty(this);
            };
        }

        [ContextMenu("Auto Setup Puzzle Queue UI")]
        private void AutoSetupPuzzleQueueUI()
        {
            EnsureReferences();
            ConfigurePuzzleQueueReferences();
            NormalizeRuntimeVisibility();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        private void OnEnable()
        {
            EnsureReferences();
            ConfigurePuzzleQueueReferences();
            NormalizeRuntimeVisibility();

            GameEvents.OnArcadeLevelStarted += HandleArcadeLevelStarted;
            GameEvents.OnObjectiveProgressChanged += HandleObjectiveProgressChanged;
            GameEvents.OnTimerUpdated += UpdateTimer;
            GameEvents.OnGameStarted += RefreshForCurrentSession;
            GameEvents.OnGameOver += HideTimerIfNotNeeded;
            GameEvents.OnPuzzleQueueChanged += UpdatePuzzleNavigation;

            if (_puzzleSwitchButton != null)
            {
                _puzzleSwitchButton.onClick.RemoveListener(OnPuzzleSwitchBlocksClicked);
                _puzzleSwitchButton.onClick.AddListener(OnPuzzleSwitchBlocksClicked);
            }

            if (_shapeOverlayButton != null)
            {
                _shapeOverlayButton.onClick.RemoveListener(OnShapeOverlayClicked);
                _shapeOverlayButton.onClick.AddListener(OnShapeOverlayClicked);
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
            GameEvents.OnPuzzleQueueChanged -= UpdatePuzzleNavigation;

            if (_puzzleSwitchButton != null)
                _puzzleSwitchButton.onClick.RemoveListener(OnPuzzleSwitchBlocksClicked);

            if (_shapeOverlayButton != null)
                _shapeOverlayButton.onClick.RemoveListener(OnShapeOverlayClicked);
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
            OnPuzzleSwitchBlocksClicked();
        }

        public void OnFillNextClicked()
        {
            OnPuzzleSwitchBlocksClicked();
        }

        public void OnPuzzlePreviousClicked()
        {
            OnPuzzleSwitchBlocksClicked();
        }

        public void OnPuzzleNextClicked()
        {
            OnPuzzleSwitchBlocksClicked();
        }

        public void OnPuzzleSwitchBlocksClicked()
        {
            BlockSpawner spawner = BlockSpawner.Instance;
            if (spawner == null || !spawner.CanSwitchPuzzleBlockSet) return;

            spawner.ShowNextPuzzleBlockSet();
            UpdatePuzzleNavigation();
        }

        public void OnShapeOverlayClicked()
        {
            if (!IsShapeArcadeLevel()) return;

            _shapeOverlayVisible = !_shapeOverlayVisible;
            ApplyShapeOverlayVisible(_shapeOverlayVisible);
            UpdateShapeOverlayButton();
            GameEvents.RaiseShapeOverlayToggled(_shapeOverlayVisible);
        }
        #endregion

    }
}
