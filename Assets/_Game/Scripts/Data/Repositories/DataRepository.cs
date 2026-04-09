using System;
using System.Collections.Generic;
using BreakItAll.Gameplay;

namespace BreakItAll.Data
{
    public sealed class DataRepository : IDataRepository
    {
        private readonly GameDataCatalog _catalog;
        private readonly Dictionary<string, ModeDefinition> _modesById = new Dictionary<string, ModeDefinition>();
        private readonly Dictionary<string, SpawnProfileDefinition> _spawnProfilesById = new Dictionary<string, SpawnProfileDefinition>();
        private readonly Dictionary<string, LevelDefinition> _levelsById = new Dictionary<string, LevelDefinition>();

        public DataRepository(GameDataCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            IndexCatalog();
        }

        public GameSettingsDefinition GetGameSettings()
        {
            return _catalog.gameSettings;
        }

        public ModeDefinition GetMode(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId))
            {
                return null;
            }

            _modesById.TryGetValue(modeId, out ModeDefinition result);
            return result;
        }

        public SpawnProfileDefinition GetSpawnProfile(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return null;
            }

            _spawnProfilesById.TryGetValue(profileId, out SpawnProfileDefinition result);
            return result;
        }

        public LevelDefinition GetLevel(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                return null;
            }

            _levelsById.TryGetValue(levelId, out LevelDefinition result);
            return result;
        }

        public IReadOnlyList<ShapeDefinition> GetAllShapeDefinitions()
        {
            return _catalog.shapes;
        }

        public IReadOnlyList<ShapeDefinition> GetShapesForMode(GameModeType modeType)
        {
            List<ShapeDefinition> result = new List<ShapeDefinition>();

            if (_catalog.shapes == null)
            {
                return result;
            }

            foreach (ShapeDefinition shape in _catalog.shapes)
            {
                if (shape == null)
                {
                    continue;
                }

                bool allowed =
                    modeType == GameModeType.Endless ? shape.enabledInEndless : shape.enabledInArcade;

                if (allowed)
                {
                    result.Add(shape);
                }
            }

            return result;
        }

        public IReadOnlyList<ShapeData> BuildRuntimeShapesForMode(GameModeType modeType)
        {
            IReadOnlyList<ShapeDefinition> definitions = GetShapesForMode(modeType);
            List<ShapeData> runtimeShapes = new List<ShapeData>(definitions.Count);

            foreach (ShapeDefinition definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                try
                {
                    runtimeShapes.Add(definition.ToRuntimeShape());
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[DataRepository] Skip invalid ShapeDefinition '{definition.name}'. Reason: {ex.Message}");
                }
            }

            return runtimeShapes;
        }


        public RuntimeSpawnProfile BuildRuntimeSpawnProfile(GameModeType modeType, string spawnProfileId)
        {
            IReadOnlyList<ShapeDefinition> shapesForMode = GetShapesForMode(modeType);
            SpawnProfileDefinition spawnProfile = GetSpawnProfile(spawnProfileId);

            return RuntimeSpawnProfileBuilder.Build(shapesForMode, spawnProfile);
        }

        private void IndexCatalog()
        {
            _modesById.Clear();
            _spawnProfilesById.Clear();
            _levelsById.Clear();

            if (_catalog.modes != null)
            {
                foreach (ModeDefinition mode in _catalog.modes)
                {
                    if (mode == null || string.IsNullOrWhiteSpace(mode.modeId))
                    {
                        continue;
                    }

                    _modesById[mode.modeId] = mode;
                }
            }

            if (_catalog.spawnProfiles != null)
            {
                foreach (SpawnProfileDefinition profile in _catalog.spawnProfiles)
                {
                    if (profile == null || string.IsNullOrWhiteSpace(profile.profileId))
                    {
                        continue;
                    }

                    _spawnProfilesById[profile.profileId] = profile;
                }
            }

            if (_catalog.levels != null)
            {
                foreach (LevelDefinition level in _catalog.levels)
                {
                    if (level == null || string.IsNullOrWhiteSpace(level.levelId))
                    {
                        continue;
                    }

                    _levelsById[level.levelId] = level;
                }
            }
        }
    }
}