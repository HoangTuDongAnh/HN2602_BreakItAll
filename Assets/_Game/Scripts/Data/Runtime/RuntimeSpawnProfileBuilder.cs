using System;
using System.Collections.Generic;
using BreakItAll.Gameplay;
using UnityEngine;

namespace BreakItAll.Data
{
    public static class RuntimeSpawnProfileBuilder
    {
        public static RuntimeSpawnProfile Build(
            IReadOnlyList<ShapeDefinition> shapeDefinitions,
            SpawnProfileDefinition spawnProfileDefinition)
        {
            List<RuntimeSpawnShapeEntry> entries = new List<RuntimeSpawnShapeEntry>();

            if (shapeDefinitions == null)
            {
                throw new ArgumentNullException(nameof(shapeDefinitions));
            }

            HashSet<string> allowedIds = BuildIdSet(spawnProfileDefinition != null ? spawnProfileDefinition.allowedShapeIds : null);
            HashSet<string> blockedIds = BuildIdSet(spawnProfileDefinition != null ? spawnProfileDefinition.blockedShapeIds : null);

            foreach (ShapeDefinition shapeDefinition in shapeDefinitions)
            {
                if (shapeDefinition == null)
                {
                    continue;
                }

                string shapeId = shapeDefinition.GetResolvedId();

                if (!IsAllowed(shapeId, allowedIds))
                {
                    continue;
                }

                if (blockedIds.Contains(shapeId))
                {
                    continue;
                }

                try
                {
                    ShapeData runtimeShape = shapeDefinition.ToRuntimeShape();
                    int finalWeight = CalculateFinalWeight(shapeDefinition, spawnProfileDefinition);

                    if (finalWeight <= 0)
                    {
                        continue;
                    }

                    entries.Add(new RuntimeSpawnShapeEntry(
                        shapeId,
                        runtimeShape,
                        finalWeight,
                        shapeDefinition.difficultyTier));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RuntimeSpawnProfileBuilder] Skip invalid shape '{shapeDefinition.name}'. Reason: {ex.Message}");
                }
            }

            if (entries.Count == 0)
            {
                throw new InvalidOperationException("No valid shapes available after applying spawn profile filters.");
            }

            return new RuntimeSpawnProfile(entries);
        }

        private static bool IsAllowed(string shapeId, HashSet<string> allowedIds)
        {
            if (allowedIds.Count == 0)
            {
                return true;
            }

            return allowedIds.Contains(shapeId);
        }

        private static HashSet<string> BuildIdSet(List<string> ids)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (ids == null)
            {
                return result;
            }

            foreach (string id in ids)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    result.Add(id.Trim());
                }
            }

            return result;
        }

        private static int CalculateFinalWeight(ShapeDefinition shapeDefinition, SpawnProfileDefinition spawnProfileDefinition)
        {
            int weight = Mathf.Max(1, shapeDefinition.spawnWeight);

            if (spawnProfileDefinition != null)
            {
                weight += CalculateTagModifier(shapeDefinition, spawnProfileDefinition);
                weight += CalculateDifficultyModifier(shapeDefinition, spawnProfileDefinition);
            }

            return Mathf.Max(0, weight);
        }

        private static int CalculateTagModifier(ShapeDefinition shapeDefinition, SpawnProfileDefinition spawnProfileDefinition)
        {
            if (shapeDefinition.tags == null || shapeDefinition.tags.Count == 0)
            {
                return 0;
            }

            if (spawnProfileDefinition.tagWeightModifiers == null || spawnProfileDefinition.tagWeightModifiers.Count == 0)
            {
                return 0;
            }

            int modifier = 0;

            foreach (TagWeightModifierData tagModifier in spawnProfileDefinition.tagWeightModifiers)
            {
                if (string.IsNullOrWhiteSpace(tagModifier.tag))
                {
                    continue;
                }

                for (int i = 0; i < shapeDefinition.tags.Count; i++)
                {
                    if (string.Equals(shapeDefinition.tags[i], tagModifier.tag, StringComparison.OrdinalIgnoreCase))
                    {
                        modifier += tagModifier.weightDelta;
                    }
                }
            }

            return modifier;
        }

        private static int CalculateDifficultyModifier(ShapeDefinition shapeDefinition, SpawnProfileDefinition spawnProfileDefinition)
        {
            int bias = spawnProfileDefinition.difficultyBias;

            if (bias == 0)
            {
                return 0;
            }

            return shapeDefinition.difficultyTier * bias;
        }
    }
}
