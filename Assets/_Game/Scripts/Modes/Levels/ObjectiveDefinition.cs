using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Modes.Levels
{
    [Serializable]
    public class ObjectiveDefinition
    {
        #region Time
        [Header("Time")]
        [Tooltip("Chi dung cho Time level. Dat target score truoc khi het gio.")]
        [Min(1)] public int targetScore = 1000;
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

        #region Fill
        [Header("Fill")]
        public List<string> providedShapeIds = new List<string>();
        public bool allowRotation = true;
        public bool allowMovePlacedBlocks = true;
        public bool requireUseAllProvidedShapes = true;
        #endregion
    }
}
