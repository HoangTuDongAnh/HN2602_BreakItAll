using UnityEngine;

namespace BreakItAll.Data
{
    [CreateAssetMenu(
        fileName = "GameSettingsDefinition",
        menuName = "Break It All/Data/Game Settings Definition")]
    public sealed class GameSettingsDefinition : ScriptableObject
    {
        [Header("Board")]
        public int boardWidth = 8;
        public int boardHeight = 8;
        public int queueSize = 3;

        [Header("Scoring")]
        public int scorePerLine = 100;
        public int comboBonusPerStep = 25;
        public int comboResetAfterMoves = 1;

        [Header("Economy")]
        public int startingCoins = 0;
        public int continuePrice = 100;
        public int rewardAdCoins = 25;

        [Header("Defaults")]
        public string defaultEndlessSpawnProfileId;
    }
}