using System.Collections.Generic;
using UnityEngine;

namespace BreakItAll.Data
{
    [CreateAssetMenu(
        fileName = "LevelDefinition",
        menuName = "Break It All/Data/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string levelId;
        public string worldId;
        public int orderIndex;

        [Header("Type")]
        public ArcadeLevelType levelType;

        [Header("Board")]
        public int boardWidth = 8;
        public int boardHeight = 8;
        public List<BoardCellData> boardCells = new List<BoardCellData>();

        [Header("Objective")]
        public ObjectiveDefinition objectiveDefinition = new ObjectiveDefinition();
        public TimerRuleData timerRule;

        [Header("Spawn")]
        public string spawnProfileOverrideId;

        [Header("Rewards")]
        public int rewardCoins = 0;
        public List<int> starThresholds = new List<int>();
    }
}