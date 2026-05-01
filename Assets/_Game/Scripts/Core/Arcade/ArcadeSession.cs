using _Game.Scripts.Modes.Levels;
using _Game.Scripts.Modes.Map;

namespace _Game.Scripts.Core.Arcade
{
    /// <summary>
    /// Bộ nhớ phiên tạm để Map truyền level được chọn sang Gameplay.
    /// Không lưu lâu dài; progress lưu bằng ArcadeProgressService.
    /// </summary>
    public static class ArcadeSession
    {
        #region Properties
        public static MapDefinition CurrentMap { get; private set; }
        public static MapLevelNodeDefinition SelectedNode { get; private set; }
        public static LevelDefinition SelectedLevel => SelectedNode != null ? SelectedNode.Level : null;
        public static bool HasSelectedLevel => SelectedLevel != null;
        #endregion

        #region API
        public static void SelectLevel(MapDefinition map, MapLevelNodeDefinition node)
        {
            CurrentMap = map;
            SelectedNode = node;
        }

        public static void ClearSelection()
        {
            SelectedNode = null;
        }
        #endregion
    }
}
