using UnityEngine;
using _Game.Scripts.View;

namespace _Game.Scripts.Core
{
    public class VFXManager : MonoBehaviour
    {
        #region Singleton
        public static VFXManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        #endregion

        #region Configuration
        [Header("Floating Text")]
        [SerializeField] private FloatingText _floatingTextPrefab;
        #endregion

        #region Event Listening
        private void OnEnable() => GameEvents.OnShowFloatingText += ShowFloatingText;
        private void OnDisable() => GameEvents.OnShowFloatingText -= ShowFloatingText;
        #endregion

        #region Effects Logic
        // Sinh ra chữ bay tại vị trí chỉ định
        private void ShowFloatingText(string content, Vector3 pos, Color color, float scale)
        {
            if (_floatingTextPrefab == null) return;

            // Chỉnh Z = -5 để chữ nổi lên trên các block và background
            Vector3 spawnPos = new Vector3(pos.x, pos.y, -5f); 
            
            FloatingText textObj = Instantiate(_floatingTextPrefab, spawnPos, Quaternion.identity);
            textObj.Init(content, color, scale);
        }
        #endregion
    }
}
