using _Game.Scripts.Modes.Objectives;

namespace _Game.Scripts.Modes
{
    public interface IGameModeRules
    {
        #region Session Flags
        bool ResetScoreOnStart { get; }
        bool ClearBoardOnStart { get; }
        bool ResetSpawnerOnStart { get; }
        #endregion

        #region Session Hooks
        void OnSessionStarting(ModeRuntimeContext context);
        void OnBoardPrepared(ModeRuntimeContext context);
        void OnSessionStarted(ModeRuntimeContext context);
        void OnReturningHome(ModeRuntimeContext context);
        IGameObjective CreateObjective(ModeRuntimeContext context);
        #endregion
    }
}
