using UnityEngine;

namespace _Game.Scripts.Modes
{
    public class GameModeRunner : MonoBehaviour
    {
        #region Inspector
        [SerializeField] private MonoBehaviour _endlessRulesBehaviour;
        [SerializeField] private MonoBehaviour _arcadeRulesBehaviour;
        #endregion

        #region Runtime
        private IGameModeRules _fallbackEndlessRules;
        private IGameModeRules _fallbackArcadeRules;
        #endregion

        #region Public API
        public IGameModeRules ResolveRules(GameModeType modeType)
        {
            return modeType == GameModeType.Arcade ? ResolveArcadeRules() : ResolveEndlessRules();
        }
        #endregion

        #region Resolve Helpers
        private IGameModeRules ResolveEndlessRules()
        {
            if (_endlessRulesBehaviour is IGameModeRules rules) return rules;
            if (_fallbackEndlessRules == null) _fallbackEndlessRules = gameObject.AddComponent<EndlessModeRules>();
            return _fallbackEndlessRules;
        }

        private IGameModeRules ResolveArcadeRules()
        {
            if (_arcadeRulesBehaviour is IGameModeRules rules) return rules;
            if (_fallbackArcadeRules == null) _fallbackArcadeRules = gameObject.AddComponent<ArcadeModeRules>();
            return _fallbackArcadeRules;
        }
        #endregion
    }
}
