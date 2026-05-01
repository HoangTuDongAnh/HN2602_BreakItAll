using _Game.Scripts.Core;
using _Game.Scripts.Core.Arcade;
using _Game.Scripts.Data;
using _Game.Scripts.Logic;
using _Game.Scripts.Logic.Resolve;
using _Game.Scripts.Modes.Levels;
using _Game.Scripts.Modes.Objectives;
using UnityEngine;

namespace _Game.Scripts.Modes
{
    /// <summary>
    /// Luat Arcade doc LevelDefinition dang duoc chon tu ArcadeSession.
    /// Arcade reads the selected LevelDefinition and creates the matching objective.
    /// </summary>
    public class ArcadeModeRules : MonoBehaviour, IGameModeRules
    {
        #region Inspector
        [Header("Fallback Level")]
        [SerializeField] private LevelDefinition _defaultLevel;

        [Header("Fallback Time Objective")]
        [SerializeField] private int _fallbackTargetScore = 1000;
        #endregion

        #region Runtime
        private LevelDefinition _activeLevel;
        private float _remainingTime;
        private float _totalTime;
        private bool _timerRunning;
        #endregion

        #region Session Flags
        public bool ResetScoreOnStart => true;
        public bool ClearBoardOnStart => true;
        public bool ResetSpawnerOnStart => true;
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

        private void Update()
        {
            if (!_timerRunning) return;
            if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning()) return;
            if (GameManager.Instance.CurrentModeType != GameModeType.Arcade) return;
            if (_activeLevel == null || _activeLevel.LevelType != ArcadeLevelType.Timed) return;

            _remainingTime = Mathf.Max(0f, _remainingTime - Time.deltaTime);
            GameManager.Instance.ReportTimerUpdated(_remainingTime, _totalTime);

            if (_remainingTime <= 0f)
                _timerRunning = false;
        }
        #endregion

        #region Board Hooks
        private void HandleBoardResolved(BoardResolveResult result)
        {
            if (!_timerRunning || _activeLevel == null || _activeLevel.LevelType != ArcadeLevelType.Timed) return;
            if (result == null || !result.HasCollectedItems) return;

            int bonusCount = 0;
            for (int i = 0; i < result.CollectedItems.Count; i++)
            {
                if (result.CollectedItems[i].ItemType == BoardItemType.TimeBonus)
                    bonusCount++;
            }

            if (bonusCount <= 0) return;

            ObjectiveDefinition objective = _activeLevel.Objective;
            float bonusSeconds = objective != null ? Mathf.Max(0f, objective.bonusTimeSeconds) : 5f;
            if (bonusSeconds <= 0f) return;

            float gainedSeconds = bonusSeconds * bonusCount;
            _remainingTime += gainedSeconds;
            GameManager.Instance?.ReportTimerUpdated(_remainingTime, _totalTime);

            GameEvents.RaiseShowFloatingText(
                $"+{Mathf.RoundToInt(gainedSeconds)}s",
                result.EffectCenter,
                new Color(0.2f, 0.95f, 1f, 1f),
                1.15f);
        }
        #endregion

        #region Session Hooks
        public void OnSessionStarting(ModeRuntimeContext context)
        {
            _activeLevel = GameManager.Instance != null && GameManager.Instance.ActiveArcadeLevel != null
                ? GameManager.Instance.ActiveArcadeLevel
                : (ArcadeSession.SelectedLevel != null ? ArcadeSession.SelectedLevel : _defaultLevel);
            _timerRunning = false;
            _remainingTime = 0f;
            _totalTime = 0f;
        }

        public void OnBoardPrepared(ModeRuntimeContext context)
        {
            if (_activeLevel == null || context == null || context.GridManager == null) return;
            context.GridManager.ApplyArcadeLevel(_activeLevel);
            ConfigureBoardRules(context.GridManager, _activeLevel);
        }

        public void OnSessionStarted(ModeRuntimeContext context)
        {
            StartTimerIfNeeded();
        }

        public void OnReturningHome(ModeRuntimeContext context)
        {
            _timerRunning = false;
            if (context != null && context.GridManager != null)
            {
                context.GridManager.SetLineResolveEnabled(true);
                context.GridManager.SetTargetPatternPlacementOnly(false);
            }
        }

        public IGameObjective CreateObjective(ModeRuntimeContext context)
        {
            if (_activeLevel == null)
                return new NoObjective();

            ObjectiveDefinition objective = _activeLevel.Objective;

            switch (_activeLevel.LevelType)
            {
                case ArcadeLevelType.Timed:
                    TimerRuleDefinition timer = _activeLevel.TimerRule;
                    return new TimedObjective(
                        objective != null ? objective.targetScore : _fallbackTargetScore,
                        timer == null || timer.failWhenTimeEnds
                    );

                case ArcadeLevelType.Collectable:
                    return new CollectItemsObjective(
                        objective != null ? objective.targetItemId : "gem",
                        objective != null ? objective.targetGemCount : 1
                    );

                case ArcadeLevelType.Shape:
                    return new ShapeObjective(objective == null || objective.requireExactTargetShape);

                case ArcadeLevelType.Fill:
                    return new FillObjective(objective == null || objective.requireUseAllProvidedShapes);

                default:
                    return new NoObjective();
            }
        }
        #endregion

        #region Helpers
        private void StartTimerIfNeeded()
        {
            if (_activeLevel == null || _activeLevel.LevelType != ArcadeLevelType.Timed)
                return;

            TimerRuleDefinition timer = _activeLevel.TimerRule;
            if (timer == null || !timer.startTimerOnSessionStart)
                return;

            _totalTime = Mathf.Max(1f, timer.totalTimeSeconds);
            _remainingTime = _totalTime;
            _timerRunning = true;

            GameManager.Instance?.ReportTimerUpdated(_remainingTime, _totalTime);
        }

        private void ConfigureBoardRules(GridManager grid, LevelDefinition level)
        {
            if (grid == null || level == null) return;

            bool isFill = level.LevelType == ArcadeLevelType.Fill;
            bool hasTargetMask = grid.HasTargetPatternCells();

            // Shape keeps normal line clears and allows temporary placement outside the target.
            // Fill is the strict puzzle type: no line clear and placement is clipped to the target mask.
            grid.SetLineResolveEnabled(!isFill);
            grid.SetTargetPatternPlacementOnly(isFill && hasTargetMask);
        }
        #endregion
    }
}
