using System;
using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Logic.Spawn;

namespace _Game.Scripts.Logic
{
    /// <summary>
    /// Handles normal Endless/Arcade spawn-batch selection and slot dispatch.
    /// BlockSpawner remains the owner of lifecycle/state; this class only builds a batch.
    /// </summary>
    public sealed class NormalBlockBatchSpawner
    {
        public bool SpawnBatch(
            SpawnRuntimeConfig runtimeConfig,
            Transform[] spawnSlots,
            SmartSpawnStrategy spawnStrategy,
            bool isArcadeSession,
            Func<Transform, SpawnRequest, bool, BlockController> spawnSingleBlock)
        {
            if (spawnSlots == null || spawnSlots.Length == 0)
            {
                Debug.LogError("NormalBlockBatchSpawner: Missing spawn slots!");
                return false;
            }

            if (spawnStrategy == null)
            {
                Debug.LogError("NormalBlockBatchSpawner: Missing spawn strategy!");
                return false;
            }

            if (spawnSingleBlock == null)
            {
                Debug.LogError("NormalBlockBatchSpawner: Missing spawn delegate!");
                return false;
            }

            int targetCount = Mathf.Min(runtimeConfig != null ? runtimeConfig.BatchSize : 3, spawnSlots.Length);
            if (targetCount <= 0)
                return false;

            BoardStateSnapshot boardSnapshot = GridManager.Instance != null
                ? BoardStateSnapshot.FromBoardState(GridManager.Instance.GetBoardState())
                : BoardStateSnapshot.Empty(8, 8);

            SpawnSelectionContext spawnContext = new SpawnSelectionContext(boardSnapshot, runtimeConfig, isArcadeSession);
            List<SpawnRequest> batch = spawnStrategy.GetNextBatch(targetCount, spawnContext);

            if (batch == null || batch.Count == 0)
            {
                Debug.LogError("NormalBlockBatchSpawner: Spawn strategy returned empty batch!");
                return false;
            }

            bool spawnedAny = false;
            for (int i = 0; i < targetCount && i < batch.Count; i++)
            {
                SpawnRequest request = batch[i];
                Transform slot = spawnSlots[i];

                if (request.ShapeData == null || slot == null)
                    continue;

                BlockController spawnedBlock = spawnSingleBlock(slot, request, false);
                spawnedAny |= spawnedBlock != null;
            }

            return spawnedAny;
        }
    }
}
