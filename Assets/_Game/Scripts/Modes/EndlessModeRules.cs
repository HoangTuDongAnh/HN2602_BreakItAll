using _Game.Scripts.Modes.Objectives;
using UnityEngine;

namespace _Game.Scripts.Modes
{
    public class EndlessModeRules : MonoBehaviour, IGameModeRules
    {
        #region Session Flags
        public bool ResetScoreOnStart => true;
        public bool ClearBoardOnStart => true;
        public bool ResetSpawnerOnStart => true;
        #endregion

        #region Session Hooks
        public void OnSessionStarting(ModeRuntimeContext context) { }
        public void OnBoardPrepared(ModeRuntimeContext context) { }
        public void OnSessionStarted(ModeRuntimeContext context) { }
        public void OnReturningHome(ModeRuntimeContext context) { }
        public IGameObjective CreateObjective(ModeRuntimeContext context) => new NoObjective();
        #endregion
    }
}
