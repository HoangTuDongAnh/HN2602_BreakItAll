using _Game.Scripts.Core;
using _Game.Scripts.Logic;

namespace _Game.Scripts.Core.Services
{
    /// <summary>
    /// Registry mỏng cho các service thật sự cần truy cập chéo layer.
    /// Giữ lại các service dùng chung ổn định; các hệ gameplay như placement dùng trực tiếp class runtime.
    /// </summary>
    public static class GameServices
    {
        #region Services
        public static IGameSaveService Save { get; private set; }
        public static GameBalanceConfig Balance { get; private set; }
        #endregion

        #region Compatibility Properties
        public static GameManager GameState => GameManager.Instance;
        public static GameManager Session => GameManager.Instance;
        public static ScoreManager Score => ScoreManager.Instance;
        public static AudioManager Audio => AudioManager.Instance;
        public static GridManager BoardQuery => GridManager.Instance;
        #endregion

        #region Register
        public static void RegisterSave(IGameSaveService service) => Save = service;
        public static void RegisterBalance(GameBalanceConfig service) => Balance = service;

        // Compatibility no-op để các script cũ/Prefab không vỡ khi migrate.
        // Sau khi scene ổn định có thể xóa dần các API no-op này.
        public static void RegisterGameState(GameManager service) { }
        public static void RegisterSession(GameManager service) { }
        public static void RegisterScore(ScoreManager service) { }
        public static void RegisterAudio(AudioManager service) { }
        public static void RegisterBoardQuery(GridManager service) { }
        public static void RegisterMode(object service) { }
        #endregion
    }
}
