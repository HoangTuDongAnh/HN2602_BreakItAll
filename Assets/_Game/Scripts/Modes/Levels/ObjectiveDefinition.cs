using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Modes.Levels
{
    [Serializable]
    public class ObjectiveDefinition
    {
        #region Score
        [Header("Score")]
        [Tooltip("Only used by Score levels. Reach the target score before time runs out.")]
        [Min(1)] public int targetScore = 1000;
        [Tooltip("Seconds gained for each Time Bonus item collected in timed Arcade levels.")]
        [Min(0f)] public float bonusTimeSeconds = 5f;
        #endregion

        #region Collect
        [Header("Collect")]
        public string targetItemId = "gem";
        [Min(1)] public int targetGemCount = 5;
        #endregion

        #region Shape
        [Header("Shape")]
        public string targetPatternId;
        [Min(0)] public int helperToolCount;
        public bool requireExactTargetShape = true;
        #endregion

        #region Puzzle
        [Header("Puzzle")]
        public List<string> providedShapeIds = new List<string>();
        public bool allowRotation = true;
        public bool requireUseAllProvidedShapes = true;
        #endregion
    }
}
