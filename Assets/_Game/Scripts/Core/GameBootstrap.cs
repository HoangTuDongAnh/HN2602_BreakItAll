using UnityEngine;

namespace BreakItAll.Core
{
    /// <summary>
    /// Entry point cấp app.
    /// Ở M1.1 chỉ đóng vai trò mốc kiến trúc và nơi setup tối thiểu.
    /// Chưa chứa gameplay logic.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private bool dontDestroyOnLoad = true;

        private void Awake()
        {
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            Debug.Log("[GameBootstrap] Initialized.");
        }
    }
}