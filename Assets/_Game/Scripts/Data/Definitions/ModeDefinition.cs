using UnityEngine;

namespace BreakItAll.Data
{
    [CreateAssetMenu(
        fileName = "ModeDefinition",
        menuName = "Break It All/Data/Mode Definition")]
    public sealed class ModeDefinition : ScriptableObject
    {
        public string modeId;
        public GameModeType modeType;
        public string displayName;

        public string defaultSpawnProfileId;
        public TimerRuleData defaultTimerRule;
        public bool usesMapProgression;
    }
}