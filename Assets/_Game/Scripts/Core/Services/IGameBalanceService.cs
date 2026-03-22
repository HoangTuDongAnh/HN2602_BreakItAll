namespace _Game.Scripts.Core.Services
{
    public interface IGameBalanceService
    {
        int BaseLineScore { get; }
        int ComboBonusPerStreak { get; }
        int PlacementScorePerCell { get; }

        float SpawnDelay { get; }
        int SpawnBatchSize { get; }
        float BoomChance { get; }
        float ToolChance { get; }
    }
}