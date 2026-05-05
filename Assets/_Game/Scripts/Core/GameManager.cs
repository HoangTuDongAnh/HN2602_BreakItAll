using _Game.Scripts.Core.Arcade;
using _Game.Scripts.Core.Services;
using _Game.Scripts.Data;
using _Game.Scripts.Logic;
using _Game.Scripts.Logic.Resolve;
using _Game.Scripts.Modes;
using _Game.Scripts.Modes.Levels;
using _Game.Scripts.Modes.Objectives;
using _Game.Scripts.View.UI;
using UnityEngine;

namespace _Game.Scripts.Core
{
    /// <summary>
    /// Dieu phoi vong doi game: Home / Map / Gameplay / Result.
    /// GameManager chi quan ly session flow. Objective progress/pass/fail duoc xu ly boi ArcadeObjectiveController.
    /// </summary>
    public class GameManager : MonoBehaviour
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
            GameServices.RegisterSession(this);
        }
        #endregion

        #region Inspector
        [Header("Mode")]
        [SerializeField] private GameModeRunner _modeRunner;
        [SerializeField] private GameModeType _defaultMode = GameModeType.Endless;
        #endregion

        #region State
        [field: SerializeField] public GameState CurrentState { get; private set; } = GameState.Home;
        public GameModeType CurrentModeType { get; private set; }
        public LevelDefinition ActiveArcadeLevel { get; private set; }
        public IGameObjective ActiveObjective => _arcadeObjectiveController.ActiveObjective;
        public ObjectiveProgress CurrentObjectiveProgress => _arcadeObjectiveController.Progress;

        private readonly ArcadeObjectiveController _arcadeObjectiveController = new ArcadeObjectiveController();
        private bool _arcadeLevelWon;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            GameEvents.OnBoardResolved += HandleBoardResolved;
        }

        private void OnDisable()
        {
            GameEvents.OnBoardResolved -= HandleBoardResolved;
        }

        private void Start()
        {
            CurrentModeType = _defaultMode;
            ReturnToHome();
        }
        #endregion

        #region State API
        public bool IsInputAllowed() => CurrentState == GameState.Playing;
        public bool IsGameRunning() => CurrentState == GameState.Playing;
        public bool IsGameOverState() => CurrentState == GameState.GameOver;

        public void SetState(GameState state)
        {
            CurrentState = state;
            Time.timeScale = state == GameState.Paused ? 0f : 1f;
        }
        #endregion

        #region Public Controls
        public void SelectMode(GameModeType modeType)
        {
            CurrentModeType = modeType;
        }

        public void StartGame()
        {
            StartNewGame(CurrentModeType);
        }

        public void StartEndless()
        {
            ArcadeSession.ClearSelection();
            ActiveArcadeLevel = null;
            StartNewGame(GameModeType.Endless);
        }

        public void OpenArcadeMap()
        {
            CurrentModeType = GameModeType.Arcade;
            SetState(GameState.Home);
            UIManager.Instance?.ShowArcadeMap();
        }

        public void StartArcade()
        {
            StartNewGame(GameModeType.Arcade);
        }

        public void StartArcadeLevel(LevelDefinition level)
        {
            if (level == null)
            {
                Debug.LogWarning("GameManager.StartArcadeLevel: level is null.");
                return;
            }

            ActiveArcadeLevel = level;
            StartNewGame(GameModeType.Arcade);
            GameEvents.RaiseArcadeLevelStarted(level);
        }

        public void StartNewGame(GameModeType modeType)
        {
            CurrentModeType = modeType;
            _arcadeLevelWon = false;

            if (modeType != GameModeType.Arcade)
                ActiveArcadeLevel = null;
            else if (ArcadeSession.SelectedLevel != null)
                ActiveArcadeLevel = ArcadeSession.SelectedLevel;

            SetState(GameState.Home);

            ModeRuntimeContext context = BuildRuntimeContext();
            IGameModeRules rules = ResolveRules();

            rules?.OnSessionStarting(context);
            ResetGameplayWorld(rules, context);
            ActivateObjective(rules, context);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameplay(() => CompleteGameplayStart(rules, context));
            }
            else
            {
                CompleteGameplayStart(rules, context);
            }
        }

        private void CompleteGameplayStart(IGameModeRules rules, ModeRuntimeContext context)
        {
            SetState(GameState.Playing);
            rules?.OnSessionStarted(context);
            GameEvents.RaiseGameStarted();
        }

        public void RestartGame()
        {
            StartNewGame(CurrentModeType);
        }

        public void EndGame()
        {
            ReturnToHome();
        }

        public void ReturnToHome()
        {
            SetState(GameState.Home);
            IGameModeRules rules = ResolveRules();
            rules?.OnReturningHome(BuildRuntimeContext());
            BlockSpawner.Instance?.ClearSpawner();
            GridManager.Instance?.ClearBoard();
            ClearObjective();
            ActiveArcadeLevel = null;
            ArcadeSession.ClearSelection();
            UIManager.Instance?.ShowHome();
        }

        public void ReturnToArcadeMap()
        {
            SetState(GameState.Home);
            IGameModeRules rules = ResolveRules();
            rules?.OnReturningHome(BuildRuntimeContext());
            BlockSpawner.Instance?.ClearSpawner();
            GridManager.Instance?.ClearBoard();
            ClearObjective();
            ActiveArcadeLevel = null;
            ArcadeSession.ClearSelection();
            UIManager.Instance?.ShowArcadeMap();
        }

        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;
            SetState(GameState.Paused);
            GameEvents.RaiseGamePaused();
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;
            SetState(GameState.Playing);
            GameEvents.RaiseGameResumed();
        }

        public void TriggerGameOver()
        {
            HandleGameOver();
        }

        public void HandleGameOver()
        {
            if (CurrentState == GameState.GameOver) return;

            SetState(GameState.GameOver);

            bool arcadeFailure = CurrentModeType == GameModeType.Arcade && ActiveArcadeLevel != null && !_arcadeLevelWon;
            if (arcadeFailure)
            {
                LevelDefinition failedLevel = ActiveArcadeLevel;
                ObjectiveProgress failedProgress = CurrentObjectiveProgress;
                GameEvents.RaiseArcadeLevelFailed(failedLevel);
                UIManager.Instance?.ShowArcadeResult(failedLevel, failedProgress, success: false, rewardCoins: 0);
            }
            else
            {
                UIManager.Instance?.ShowGameOver(
                    ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0,
                    ScoreManager.Instance != null ? ScoreManager.Instance.BestScore : 0);
            }

            GameEvents.RaiseGameOver();
        }
        #endregion

        #region Objective Report API
        public void ReportScoreChanged(int currentScore)
        {
            if (!CanUpdateArcadeObjective()) return;
            HandleObjectiveResult(_arcadeObjectiveController.NotifyScoreChanged(currentScore));
        }

        public void ReportItemCollected(string itemId, int amount)
        {
            // Compatibility method. Collect objective now reads BoardResolveResult directly.
        }

        public void ReportTimerUpdated(float remainingTimeSeconds, float totalTimeSeconds)
        {
            GameEvents.RaiseTimerUpdated(remainingTimeSeconds, totalTimeSeconds);
            if (!CanUpdateArcadeObjective()) return;
            HandleObjectiveResult(_arcadeObjectiveController.NotifyTimerUpdated(remainingTimeSeconds, totalTimeSeconds));
        }
        #endregion

        #region Internal Flow
        private void HandleBoardResolved(BoardResolveResult result)
        {
            if (!CanUpdateArcadeObjective()) return;
            HandleObjectiveResult(_arcadeObjectiveController.NotifyBoardResolved(result));
        }

        private bool CanUpdateArcadeObjective()
        {
            return CurrentState == GameState.Playing
                   && CurrentModeType == GameModeType.Arcade
                   && ActiveArcadeLevel != null
                   && _arcadeObjectiveController.HasObjective;
        }

        private void HandleObjectiveResult(ObjectiveCheckResult result)
        {
            switch (result)
            {
                case ObjectiveCheckResult.Completed:
                    CompleteArcadeLevelIfNeeded();
                    break;
                case ObjectiveCheckResult.Failed:
                    HandleGameOver();
                    break;
            }
        }

        private IGameModeRules ResolveRules()
        {
            if (_modeRunner == null) _modeRunner = FindFirstObjectByType<GameModeRunner>();
            if (_modeRunner == null) _modeRunner = gameObject.AddComponent<GameModeRunner>();
            return _modeRunner.ResolveRules(CurrentModeType);
        }

        private ModeRuntimeContext BuildRuntimeContext()
        {
            return new ModeRuntimeContext(this, ScoreManager.Instance, GameServices.Balance, GridManager.Instance, BlockSpawner.Instance);
        }

        private void ResetGameplayWorld(IGameModeRules rules, ModeRuntimeContext context)
        {
            if (rules == null || rules.ResetScoreOnStart)
                ScoreManager.Instance?.ResetScore();
            if (rules == null || rules.ClearBoardOnStart)
                GridManager.Instance?.ClearBoard();
            rules?.OnBoardPrepared(context);
            if (rules == null || rules.ResetSpawnerOnStart)
                BlockSpawner.Instance?.ResetSpawner();
        }

        private void ActivateObjective(IGameModeRules rules, ModeRuntimeContext context)
        {
            ClearObjective();
            IGameObjective objective = rules != null ? rules.CreateObjective(context) : new NoObjective();
            _arcadeObjectiveController.Start(ActiveArcadeLevel, objective, context);
        }

        private void ClearObjective()
        {
            _arcadeObjectiveController.Clear();
        }

        private void CompleteArcadeLevelIfNeeded()
        {
            if (CurrentModeType != GameModeType.Arcade || ActiveArcadeLevel == null) return;

            LevelDefinition completedLevel = ActiveArcadeLevel;
            ObjectiveProgress completedProgress = CurrentObjectiveProgress;
            _arcadeLevelWon = true;

            bool newlyPassed = ArcadeProgressService.MarkLevelPassed(completedLevel);
            int rewardCoins = newlyPassed ? completedLevel.RewardCoins : 0;
            GameEvents.RaiseArcadeLevelCompleted(completedLevel);

            SetState(GameState.Home);
            BlockSpawner.Instance?.ClearSpawner();
            GridManager.Instance?.ClearBoard();
            ClearObjective();
            ActiveArcadeLevel = null;
            ArcadeSession.ClearSelection();
            UIManager.Instance?.ShowArcadeResult(completedLevel, completedProgress, success: true, rewardCoins: rewardCoins);
        }
        #endregion
    }
}
