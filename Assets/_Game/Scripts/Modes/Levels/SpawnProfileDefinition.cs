using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Modes.Levels
{
    /// <summary>
    /// Data override cho spawn trong Arcade. Phase này mới lưu data để Map/Level flow dùng được;
    /// việc map chi tiết sang SmartSpawnStrategy có thể polish tiếp ở bước balancing.
    /// </summary>
    [Serializable]
    public class SpawnProfileDefinition
    {
        #region Fields
        public string profileId;
        [Min(1)] public int queueSizeOverride = 3;
        [Range(-1f, 1f)] public float difficultyBias;
        public List<string> allowedShapeIds = new List<string>();
        public List<string> blockedShapeIds = new List<string>();
        public List<string> preferredTags = new List<string>();
        public List<string> blockedTags = new List<string>();
        #endregion
    }
}
