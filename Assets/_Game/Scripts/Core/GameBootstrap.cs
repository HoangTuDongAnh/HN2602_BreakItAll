using UnityEngine;
using _Game.Scripts.Core.Services;

namespace _Game.Scripts.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        private static bool _initialized;

        private void Awake()
        {
            if (_initialized) return;

            GameServices.RegisterSave(new PlayerPrefsSaveService());

            _initialized = true;
        }
    }
}