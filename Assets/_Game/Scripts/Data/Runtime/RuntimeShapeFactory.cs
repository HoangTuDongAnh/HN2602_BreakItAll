using System;
using BreakItAll.Gameplay;

namespace BreakItAll.Data
{
    public static class RuntimeShapeFactory
    {
        public static ShapeSpawnPool BuildSpawnPool(
            IDataRepository dataRepository,
            GameModeType modeType,
            string spawnProfileId)
        {
            if (dataRepository == null)
            {
                throw new ArgumentNullException(nameof(dataRepository));
            }

            RuntimeSpawnProfile runtimeSpawnProfile =
                dataRepository.BuildRuntimeSpawnProfile(modeType, spawnProfileId);

            return new ShapeSpawnPool(runtimeSpawnProfile.Entries);
        }
    }
}