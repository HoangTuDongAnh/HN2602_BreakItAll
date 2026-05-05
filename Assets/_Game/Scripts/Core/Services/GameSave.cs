namespace _Game.Scripts.Core.Services
{
    /// <summary>
    /// Centralized access point for persistent save data.
    /// Use this instead of calling PlayerPrefs directly from gameplay systems.
    /// If GameBootstrap has not registered a save service yet, a PlayerPrefs-backed fallback is used.
    /// </summary>
    public static class GameSave
    {
        private static readonly IGameSaveService FallbackSave = new PlayerPrefsSaveService();

        public static IGameSaveService Current
        {
            get
            {
                if (GameServices.Save != null)
                    return GameServices.Save;

                GameServices.RegisterSave(FallbackSave);
                return FallbackSave;
            }
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            return Current.GetInt(key, defaultValue);
        }

        public static void SetInt(string key, int value)
        {
            Current.SetInt(key, value);
        }

        public static float GetFloat(string key, float defaultValue = 0f)
        {
            return Current.GetFloat(key, defaultValue);
        }

        public static void SetFloat(string key, float value)
        {
            Current.SetFloat(key, value);
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
            return Current.GetBool(key, defaultValue);
        }

        public static void SetBool(string key, bool value)
        {
            Current.SetBool(key, value);
        }

        public static void Save()
        {
            Current.Save();
        }
    }
}
