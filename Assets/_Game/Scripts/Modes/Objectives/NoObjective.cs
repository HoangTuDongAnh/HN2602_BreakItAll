using _Game.Scripts.Logic.Resolve;
using _Game.Scripts.Modes;

namespace _Game.Scripts.Modes.Objectives
{
    public sealed class NoObjective : IGameObjective
    {
        #region State
        public ObjectiveProgress Progress => ObjectiveProgress.Empty;
        public bool IsCompleted => false;
        public bool IsFailed => false;
        #endregion

        #region Lifecycle
        public void Initialize(ModeRuntimeContext context) { }
        public void NotifyBoardResolved(BoardResolveResult result) { }
        public void NotifyScoreChanged(int currentScore) { }
        public void NotifyTimerUpdated(float remainingTimeSeconds, float totalTimeSeconds) { }
        #endregion
    }
}
