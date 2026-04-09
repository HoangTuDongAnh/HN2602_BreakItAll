using System.Collections.Generic;
using UnityEngine;

namespace BreakItAll.Data
{
    [CreateAssetMenu(
        fileName = "SpawnProfileDefinition",
        menuName = "Break It All/Data/Spawn Profile Definition")]
    public sealed class SpawnProfileDefinition : ScriptableObject
    {
        public string profileId;
        public int queueSizeOverride = 0;

        public List<string> allowedShapeIds = new List<string>();
        public List<string> blockedShapeIds = new List<string>();
        public List<TagWeightModifierData> tagWeightModifiers = new List<TagWeightModifierData>();

        public int difficultyBias = 0;
    }
}