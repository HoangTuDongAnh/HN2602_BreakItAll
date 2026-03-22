using UnityEngine;

namespace _Game.Scripts.Core.Services
{
    public class PlayerPrefsSaveService : IGameSaveService
    {
        public int GetInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            int fallback = defaultValue ? 1 : 0;
            return PlayerPrefs.GetInt(key, fallback) == 1;
        }

        public void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}