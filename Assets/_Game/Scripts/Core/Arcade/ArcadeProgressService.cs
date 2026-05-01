using _Game.Scripts.Modes.Levels;
using _Game.Scripts.Modes.Map;
using UnityEngine;

namespace _Game.Scripts.Core.Arcade
{
    /// <summary>
    /// Lưu progress Arcade tối giản bằng PlayerPrefs.
    /// Progress chỉ có 3 trạng thái cấp level: Locked / Unlocked / Passed.
    /// Không dùng star rating trong phiên bản hiện tại.
    /// </summary>
    public static class ArcadeProgressService
    {
        #region Keys
        private const string PassedPrefix = "arcade_level_passed_";
        private const string LegacyCompletedPrefix = "arcade_level_completed_";
        private const string CoinsKey = "arcade_coins";
        #endregion

        #region Query
        public static bool IsLevelPassed(LevelDefinition level)
        {
            return level != null && (PlayerPrefs.GetInt(PassedPrefix + level.LevelId, 0) == 1 || PlayerPrefs.GetInt(LegacyCompletedPrefix + level.LevelId, 0) == 1);
        }

        // Alias giữ tương thích với code cũ nếu còn nơi đang gọi Completed.
        public static bool IsLevelCompleted(LevelDefinition level) => IsLevelPassed(level);

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
        public static void MarkLevelPassed(LevelDefinition level)
        {
            if (level == null) return;

            bool wasPassed = IsLevelPassed(level);
            PlayerPrefs.SetInt(PassedPrefix + level.LevelId, 1);

            if (!wasPassed && level.RewardCoins > 0)
                PlayerPrefs.SetInt(CoinsKey, PlayerPrefs.GetInt(CoinsKey, 0) + level.RewardCoins);

            PlayerPrefs.Save();
        }

        // Alias giữ tương thích tạm thời. Tham số stars bị bỏ qua.
        public static void CompleteLevel(LevelDefinition level, int stars = 0)
        {
            MarkLevelPassed(level);
        }
        #endregion
    }
}
