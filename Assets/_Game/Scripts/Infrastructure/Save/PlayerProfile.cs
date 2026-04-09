using System;
using System.Collections.Generic;

namespace BreakItAll.Infrastructure.Save
{
    [Serializable]
    public sealed class PlayerProfile
    {
        public int version = 1;

        public int coins;
        public int bestEndlessScore;
        public int currentWorldUnlocked = 1;

        public List<string> completedLevels = new List<string>();
        public List<LevelStarRecord> levelStars = new List<LevelStarRecord>();

        public string lastSelectedMode;
        public List<string> settingsFlags = new List<string>();

        public ContinueUsageMetrics continueUsageMetrics = new ContinueUsageMetrics();
    }

    [Serializable]
    public sealed class LevelStarRecord
    {
        public string levelId;
        public int stars;
    }

    [Serializable]
    public sealed class ContinueUsageMetrics
    {
        public int totalContinuesUsed;
    }
}