using System;

namespace BreakItAll.Infrastructure.Save
{
    public interface ISaveService
    {
        PlayerProfile CurrentProfile { get; }
        bool HasLoadedProfile { get; }

        void LoadOrCreateProfile(int startingCoins, string defaultSelectedMode);
        void Save();

        void SetLastSelectedMode(string modeId);
        bool TryUpdateBestEndlessScore(int score);
        void AddCoins(int amount);

        event Action<PlayerProfile> ProfileChanged;
    }
}