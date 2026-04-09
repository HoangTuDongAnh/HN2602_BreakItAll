using System.Collections.Generic;
using UnityEngine;

namespace BreakItAll.Data
{
    [CreateAssetMenu(
        fileName = "GameDataCatalog",
        menuName = "Break It All/Data/Game Data Catalog")]
    public sealed class GameDataCatalog : ScriptableObject
    {
        [Header("Global")]
        public GameSettingsDefinition gameSettings;

        [Header("Modes")]
        public List<ModeDefinition> modes = new List<ModeDefinition>();

        [Header("Gameplay Content")]
        public List<ShapeDefinition> shapes = new List<ShapeDefinition>();
        public List<SpawnProfileDefinition> spawnProfiles = new List<SpawnProfileDefinition>();

        [Header("Arcade Content")]
        public List<LevelDefinition> levels = new List<LevelDefinition>();
    }
}