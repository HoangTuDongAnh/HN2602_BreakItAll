using System;
using _Game.Scripts.Core.Services;
using _Game.Scripts.Modes.Levels;
using _Game.Scripts.Modes.Map;

namespace _Game.Scripts.Core.Arcade
{
    /// <summary>
    /// Arcade progress service.
    /// Uses the centralized save service instead of touching PlayerPrefs directly.
    /// Progress currently has three practical states: Locked / Unlocked / Passed.
    /// </summary>
    public static class ArcadeProgressService
    {
        public static event Action<int> OnCoinsChanged;

        #region Query
        public static bool IsLevelPassed(LevelDefinition level)
        {
            if (level == null) return false;

            return GameSave.GetInt(SaveKeys.ArcadeLevelPassedPrefix + level.LevelId, 0) == 1
                   || GameSave.GetInt(SaveKeys.ArcadeLevelCompletedLegacyPrefix + level.LevelId, 0) == 1;
        }

        // Alias giữ tương thích với code cũ nếu còn nơi đang gọi Completed.
        public static bool IsLevelCompleted(LevelDefinition level) => IsLevelPassed(level);

        public static int GetCoins()
        {
            return GameSave.GetInt(SaveKeys.ArcadeCoins, 0);
        }

        public static bool IsNodeUnlocked(MapDefinition map, MapLevelNodeDefinition node)
        {
            if (node == null || node.Level == null) return false;
            if (node.RequiredCompletedNodeIds == null || node.RequiredCompletedNodeIds.Count == 0)
                return IsSequentialNodeUnlocked(map, node);

            for (int i = 0; i < node.RequiredCompletedNodeIds.Count; i++)
            {
                MapLevelNodeDefinition requiredNode = map != null ? map.FindNode(node.RequiredCompletedNodeIds[i]) : null;
                if (requiredNode == null || requiredNode.Level == null) return false;
                if (!IsLevelPassed(requiredNode.Level)) return false;
            }

            return true;
        }

        private static bool IsSequentialNodeUnlocked(MapDefinition map, MapLevelNodeDefinition node)
        {
            if (map == null || map.Nodes == null)
                return true;

            for (int i = 0; i < map.Nodes.Count; i++)
            {
                MapLevelNodeDefinition current = map.Nodes[i];
                if (current != node)
                    continue;

                if (i == 0)
                    return true;

                MapLevelNodeDefinition previous = map.Nodes[i - 1];
                return previous != null && previous.Level != null && IsLevelPassed(previous.Level);
            }

            return true;
        }
        #endregion

        #region Mutate
        public static bool MarkLevelPassed(LevelDefinition level)
        {
            if (level == null) return false;

            bool wasPassed = IsLevelPassed(level);
            GameSave.SetInt(SaveKeys.ArcadeLevelPassedPrefix + level.LevelId, 1);

            if (!wasPassed && level.RewardCoins > 0)
                AddCoins(level.RewardCoins, saveImmediately: false);

            GameSave.Save();
            return !wasPassed;
        }

        public static void AddCoins(int amount, bool saveImmediately = true)
        {
            if (amount <= 0) return;

            SetCoins(GetCoins() + amount, saveImmediately);
        }

        public static void SetCoins(int amount, bool saveImmediately = true)
        {
            int clampedAmount = amount < 0 ? 0 : amount;
            GameSave.SetInt(SaveKeys.ArcadeCoins, clampedAmount);

            if (saveImmediately)
                GameSave.Save();

            OnCoinsChanged?.Invoke(clampedAmount);
        }

        public static void RefreshCoinsChanged()
        {
            OnCoinsChanged?.Invoke(GetCoins());
        }

        // Alias giữ tương thích tạm thời. Tham số stars bị bỏ qua.
        public static void CompleteLevel(LevelDefinition level, int stars = 0)
        {
            MarkLevelPassed(level);
        }
        #endregion
    }
}
