using System;
using UnityEngine;

namespace _Game.Scripts.Modes.Levels
{
    [Serializable]
    public class TimerRuleDefinition
    {
        #region Fields
        [Min(1f)] public float totalTimeSeconds = 90f;
        public bool failWhenTimeEnds = true;
        public bool startTimerOnSessionStart = true;
        #endregion
    }
}
