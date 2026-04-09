using System.Collections.Generic;
using BreakItAll.Gameplay;

namespace BreakItAll.Data
{
    public interface IDataRepository
    {
        GameSettingsDefinition GetGameSettings();
        ModeDefinition GetMode(string modeId);
        SpawnProfileDefinition GetSpawnProfile(string profileId);
        LevelDefinition GetLevel(string levelId);

        IReadOnlyList<ShapeDefinition> GetAllShapeDefinitions();
        IReadOnlyList<ShapeDefinition> GetShapesForMode(GameModeType modeType);
        IReadOnlyList<ShapeData> BuildRuntimeShapesForMode(GameModeType modeType);
        RuntimeSpawnProfile BuildRuntimeSpawnProfile(GameModeType modeType, string spawnProfileId);
    }
}