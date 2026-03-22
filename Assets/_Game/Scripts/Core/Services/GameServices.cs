namespace _Game.Scripts.Core.Services
{
    public static class GameServices
    {
        public static IGameStateService GameState { get; private set; }
        public static IScoreService Score { get; private set; }
        public static IAudioService Audio { get; private set; }
        public static IBoardQueryService BoardQuery { get; private set; }
        public static IBlockPlacementService Placement { get; private set; }
        public static IGameSaveService Save { get; private set; }
        public static IGameBalanceService Balance { get; private set; }
        public static IGameSessionService Session { get; private set; }

        public static void RegisterGameState(IGameStateService service) => GameState = service;
        public static void RegisterScore(IScoreService service) => Score = service;
        public static void RegisterAudio(IAudioService service) => Audio = service;
        public static void RegisterBoardQuery(IBoardQueryService service) => BoardQuery = service;
        public static void RegisterPlacement(IBlockPlacementService service) => Placement = service;
        public static void RegisterSave(IGameSaveService service) => Save = service;
        public static void RegisterBalance(IGameBalanceService service) => Balance = service;
        public static void RegisterSession(IGameSessionService service) => Session = service;

        public static void ClearAll()
        {
            GameState = null;
            Score = null;
            Audio = null;
            BoardQuery = null;
            Placement = null;
            Save = null;
            Balance = null;
            Session = null;
        }
    }
}