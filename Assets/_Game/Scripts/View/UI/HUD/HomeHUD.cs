using UnityEngine;
using _Game.Scripts.Core;

namespace _Game.Scripts.View.UI
{
    /// <summary>
    /// Home screen button bridge.
    /// Giữ HomeHUD đơn giản: chỉ nhận click từ UI và chuyển cho GameManager/UIManager.
    /// </summary>
    public class HomeHUD : MonoBehaviour
    {
        #region Main Buttons
        public void OnPlayClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartEndless();
        }

        public void OnArcadeClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OpenArcadeMap();
            else if (UIManager.Instance != null)
                UIManager.Instance.ShowMap();
        }
        #endregion

        #region Dialog Buttons
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
        #endregion

        #region Quit
        public void OnQuitClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowAlertDialog(() => 
                { 
                    Application.Quit(); 
                    
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                });
            }
        }
        #endregion
    }
}
