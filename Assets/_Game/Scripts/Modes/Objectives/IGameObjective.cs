using _Game.Scripts.Logic.Resolve;
using _Game.Scripts.Modes;

namespace _Game.Scripts.Modes.Objectives
{
    public interface IGameObjective
    {
        #region State
        ObjectiveProgress Progress { get; }
        bool IsCompleted { get; }
        bool IsFailed { get; }
        #endregion

        #region Lifecycle
        void Initialize(ModeRuntimeContext context);
        void NotifyBoardResolved(BoardResolveResult result);
        void NotifyScoreChanged(int currentScore);
        void NotifyTimerUpdated(float remainingTimeSeconds, float totalTimeSeconds);
        #endregion
    }
}
