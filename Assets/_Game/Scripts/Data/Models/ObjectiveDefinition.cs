using System;

namespace BreakItAll.Data
{
    [Serializable]
    public sealed class ObjectiveDefinition
    {
        public ObjectiveType objectiveType;
        public int targetScore;
        public int targetGemCount;
        public string targetPatternId;
        public string displayText;
    }
}