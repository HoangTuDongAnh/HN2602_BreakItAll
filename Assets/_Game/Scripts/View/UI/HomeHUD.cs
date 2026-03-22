using UnityEngine;
using _Game.Scripts.Core;

namespace _Game.Scripts.View.UI
{
    public class HomeHUD : MonoBehaviour
    {
        public void OnPlayClicked() => GameManager.Instance.StartGame();
        
        public void OnSettingsClicked()
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowSettings();
        }

        public void OnGuideClicked()
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowGuide();
        }
        
        public void OnMusicClicked()
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMusicChangeDialog();
        }

        // Nút Quit gọi Alert
        public void OnQuitClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowAlertDialog(() => 
                { 
                    // Hành động confirm: Thoát game
                    Application.Quit(); 
                    
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false; // Thoát trong Editor để test
#endif
                });
            }
        }
    }
}