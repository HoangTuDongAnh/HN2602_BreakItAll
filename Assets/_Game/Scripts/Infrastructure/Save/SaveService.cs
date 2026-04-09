using System;
using UnityEngine;

namespace BreakItAll.Infrastructure.Save
{
    public sealed class SaveService : ISaveService
    {
        private const string ProfileSaveKey = "break_it_all.player_profile";

        public PlayerProfile CurrentProfile { get; private set; }
        public bool HasLoadedProfile { get; private set; }

        public event Action<PlayerProfile> ProfileChanged;

        public void LoadOrCreateProfile(int startingCoins, string defaultSelectedMode)
        {
            if (PlayerPrefs.HasKey(ProfileSaveKey))
            {
                string json = PlayerPrefs.GetString(ProfileSaveKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    try
                    {
                        CurrentProfile = JsonUtility.FromJson<PlayerProfile>(json);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[SaveService] Failed to parse player profile. Creating new profile. Reason: {ex.Message}");
                    }
                }
            }

            if (CurrentProfile == null)
            {
                CurrentProfile = CreateDefaultProfile(startingCoins, defaultSelectedMode);
                Save();
            }
            else
            {
                PatchMissingDefaults(CurrentProfile, startingCoins, defaultSelectedMode);
                Save();
            }

            HasLoadedProfile = true;
            NotifyChanged();
        }

        public void Save()
        {
            if (CurrentProfile == null)
            {
                Debug.LogWarning("[SaveService] Save called without a loaded profile.");
                return;
            }

            string json = JsonUtility.ToJson(CurrentProfile);
            PlayerPrefs.SetString(ProfileSaveKey, json);
            PlayerPrefs.Save();
        }

        public void SetLastSelectedMode(string modeId)
        {
            if (CurrentProfile == null)
            {
                return;
            }

            CurrentProfile.lastSelectedMode = string.IsNullOrWhiteSpace(modeId) ? CurrentProfile.lastSelectedMode : modeId;
            Save();
            NotifyChanged();
        }

        public bool TryUpdateBestEndlessScore(int score)
        {
            if (CurrentProfile == null)
            {
                return false;
            }

            if (score <= CurrentProfile.bestEndlessScore)
            {
                return false;
            }

            CurrentProfile.bestEndlessScore = score;
            Save();
            NotifyChanged();
            return true;
        }

        public void AddCoins(int amount)
        {
            if (CurrentProfile == null || amount == 0)
            {
                return;
            }

            CurrentProfile.coins = Mathf.Max(0, CurrentProfile.coins + amount);
            Save();
            NotifyChanged();
        }

        private PlayerProfile CreateDefaultProfile(int startingCoins, string defaultSelectedMode)
        {
            return new PlayerProfile
            {
                version = 1,
                coins = Mathf.Max(0, startingCoins),
                bestEndlessScore = 0,
                currentWorldUnlocked = 1,
                lastSelectedMode = defaultSelectedMode
            };
        }

        private void PatchMissingDefaults(PlayerProfile profile, int startingCoins, string defaultSelectedMode)
        {
            if (profile.version <= 0)
            {
                profile.version = 1;
            }

            if (profile.currentWorldUnlocked <= 0)
            {
                profile.currentWorldUnlocked = 1;
            }

            if (profile.completedLevels == null)
            {
                profile.completedLevels = new System.Collections.Generic.List<string>();
            }

            if (profile.levelStars == null)
            {
                profile.levelStars = new System.Collections.Generic.List<LevelStarRecord>();
            }

            if (profile.settingsFlags == null)
            {
                profile.settingsFlags = new System.Collections.Generic.List<string>();
            }

            if (profile.continueUsageMetrics == null)
            {
                profile.continueUsageMetrics = new ContinueUsageMetrics();
            }

            if (profile.coins < 0)
            {
                profile.coins = Mathf.Max(0, startingCoins);
            }

            if (string.IsNullOrWhiteSpace(profile.lastSelectedMode))
            {
                profile.lastSelectedMode = defaultSelectedMode;
            }
        }

        private void NotifyChanged()
        {
            ProfileChanged?.Invoke(CurrentProfile);
        }
    }
}
