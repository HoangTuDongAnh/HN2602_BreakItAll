using _Game.Scripts.Core;
using _Game.Scripts.Logic.Resolve;
using _Game.Scripts.Modes.Levels;

namespace _Game.Scripts.Modes.Objectives
{
    /// <summary>
    /// Source of truth cho objective cua mot Arcade level.
    /// GameManager chi quan ly session flow; controller nay xu ly progress/pass/fail.
    /// </summary>
    public sealed class ArcadeObjectiveController
    {
        #region Fields
        private IGameObjective _objective;
        private LevelDefinition _level;
        private bool _completedBroadcasted;
        private bool _failedBroadcasted;
        #endregion

        #region Properties
        public IGameObjective ActiveObjective => _objective;
        public ObjectiveProgress Progress => _objective != null ? _objective.Progress : ObjectiveProgress.Empty;
        public bool HasObjective => _objective != null;
        #endregion

        #region Lifecycle
        public void Start(LevelDefinition level, IGameObjective objective, ModeRuntimeContext context)
        {
            _level = level;
            _objective = objective ?? new NoObjective();
            _completedBroadcasted = false;
            _failedBroadcasted = false;
            _objective.Initialize(context);
            BroadcastProgress();
        }

        public void Clear()
        {
            _objective = null;
            _level = null;
            _completedBroadcasted = false;
            _failedBroadcasted = false;
        }
        #endregion

        #region Notify API
        public ObjectiveCheckResult NotifyBoardResolved(BoardResolveResult result)
        {
            if (_objective == null) return ObjectiveCheckResult.None;
            _objective.NotifyBoardResolved(result);
            return RefreshState();
        }

        public ObjectiveCheckResult NotifyScoreChanged(int currentScore)
        {
            if (_objective == null) return ObjectiveCheckResult.None;
            _objective.NotifyScoreChanged(currentScore);
            return RefreshState();
        }

        public ObjectiveCheckResult NotifyTimerUpdated(float remainingTimeSeconds, float totalTimeSeconds)
        {
            if (_objective == null) return ObjectiveCheckResult.None;
            _objective.NotifyTimerUpdated(remainingTimeSeconds, totalTimeSeconds);
            return RefreshState();
        }
        #endregion

        #region State
        public ObjectiveCheckResult RefreshState()
        {
            if (_objective == null) return ObjectiveCheckResult.None;

            BroadcastProgress();

            if (_objective.IsCompleted && !_completedBroadcasted)
            {
                _completedBroadcasted = true;
                GameEvents.RaiseObjectiveCompleted(_objective.Progress);
                return ObjectiveCheckResult.Completed;
            }

            if (_objective.IsFailed && !_failedBroadcasted)
            {
                _failedBroadcasted = true;
                GameEvents.RaiseObjectiveFailed(_objective.Progress);
                return ObjectiveCheckResult.Failed;
            }

            return ObjectiveCheckResult.None;
        }

        private void BroadcastProgress()
        {
            if (_objective != null)
                GameEvents.RaiseObjectiveProgressChanged(_objective.Progress);
        }
        #endregion
    }

    public enum ObjectiveCheckResult
    {
        None = 0,
        Completed = 1,
        Failed = 2
    }
}
