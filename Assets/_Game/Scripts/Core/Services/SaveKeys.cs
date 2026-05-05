namespace _Game.Scripts.Core.Services
{
    /// <summary>
    /// Centralized persistent key names.
    /// Keep existing values stable to preserve compatibility with old PlayerPrefs saves.
    /// </summary>
    public static class SaveKeys
    {
        public const string HighScore = "high_score";

        public const string MusicEnabled = "music_enabled";
        public const string SfxEnabled = "sfx_enabled";
        public const string MusicVolume = "music_volume";
        public const string SfxVolume = "sfx_volume";
        public const string BgmIndex = "bgm_index";

        public const string ArcadeCoins = "arcade_coins";
        public const string ArcadeLevelPassedPrefix = "arcade_level_passed_";
        public const string ArcadeLevelCompletedLegacyPrefix = "arcade_level_completed_";

        public const string ToolSingleCellCount = "tool_single_cell_count";
        public const string ToolRemoveSpawnBlockCount = "tool_remove_spawn_block_count";
        public const string ToolBombSquareCount = "tool_bomb_square_count";
    }
}
