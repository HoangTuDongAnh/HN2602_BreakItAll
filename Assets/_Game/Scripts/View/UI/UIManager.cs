using UnityEngine;
using _Game.Scripts.Core;
using _Game.Scripts.Data;
using _Game.Scripts.View.UI.Dialogs; 

namespace _Game.Scripts.View.UI
{
    public class UIManager : MonoBehaviour
    {
        #region Singleton
        public static UIManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        #endregion

        #region View References

        [Header("--- MAIN SCREENS ---")]
        [SerializeField] private HomeHUD _homeHUD; 
        [SerializeField] private GameObject _panelContainer; // Nền đen chắn input

        [Header("--- DIALOGS ---")]
        [SerializeField] private GameplayHUD _gameplayHUD;
        [SerializeField] private SettingsDialog _settingsDialog;
        [SerializeField] private GameOverDialog _gameOverDialog;
        [SerializeField] private MusicChangeDialog _musicChangeDialog;
        [SerializeField] private AlertDialog _alertDialog;
        
        // [UPDATE] Đổi từ GameObject sang GuideDialog để quản lý tốt hơn
        [SerializeField] private GuideDialog _dialogGuide; 

        #endregion

        private void Start()
        {
            ShowHome();
        }
        
        private void OnEnable() => GameEvents.OnScoreChanged += UpdateScoreUI;
        private void OnDisable() => GameEvents.OnScoreChanged -= UpdateScoreUI;

        #region Navigation

        public void ShowHome()
        {
            if (_homeHUD) _homeHUD.gameObject.SetActive(true);
            if (_gameplayHUD) _gameplayHUD.gameObject.SetActive(false);
            CloseAllDialogs();
        }

        public void ShowGameplay()
        {
            if (_homeHUD) _homeHUD.gameObject.SetActive(false);
            if (_gameplayHUD) _gameplayHUD.gameObject.SetActive(true);
            CloseAllDialogs();
        }

        public void ShowSettings()
        {
            EnablePanelContainer();
            if (_settingsDialog) _settingsDialog.gameObject.SetActive(true);
            
            // Tắt các dialog khác để tránh chồng chéo
            if (_dialogGuide) _dialogGuide.gameObject.SetActive(false);
            if (_musicChangeDialog) _musicChangeDialog.gameObject.SetActive(false);
        }

        public void ShowGuide()
        {
            EnablePanelContainer();
            if (_dialogGuide) _dialogGuide.gameObject.SetActive(true);
            
            // Tắt Setting nếu đang mở
            if (_settingsDialog) _settingsDialog.gameObject.SetActive(false);
        }

        public void ShowMusicChangeDialog()
        {
            EnablePanelContainer();
            if (_musicChangeDialog) _musicChangeDialog.gameObject.SetActive(true);
            if (_settingsDialog) _settingsDialog.gameObject.SetActive(false);
        }
        
        public void ShowGameOver(int currentScore, int highScore)
        {
            EnablePanelContainer();
            if (_gameOverDialog)
            {
                _gameOverDialog.gameObject.SetActive(true);
                _gameOverDialog.Setup(currentScore, highScore);
            }
            CloseAllDialogs(exceptGameOver: true);
        }

        public void CloseAllDialogs(bool exceptGameOver = false)
        {
            if (_settingsDialog) _settingsDialog.gameObject.SetActive(false);
            if (_dialogGuide) _dialogGuide.gameObject.SetActive(false);
            if (_musicChangeDialog) _musicChangeDialog.gameObject.SetActive(false);
            
            if (_alertDialog) _alertDialog.gameObject.SetActive(false);
            
            if (!exceptGameOver && _gameOverDialog) _gameOverDialog.gameObject.SetActive(false);
            if (!exceptGameOver && _panelContainer) _panelContainer.SetActive(false);
        }

        private void EnablePanelContainer()
        {
            if (_panelContainer)
            {
                _panelContainer.SetActive(true);
                _panelContainer.transform.SetAsLastSibling(); 
            }
        }

        #endregion

        #region Alert Logic

        public void ShowAlertDialog(System.Action onConfirmAction)
        {
            EnablePanelContainer(); 
            
            if (_alertDialog != null)
            {
                _alertDialog.gameObject.SetActive(true);
                _alertDialog.Setup(onConfirmAction);
                _alertDialog.transform.SetAsLastSibling(); 
            }
            else
            {
                Debug.LogError("UIManager: Chưa gán Alert Dialog trong Inspector!");
            }
        }

        public void OnAlertDialogClosed()
        {
            // Kiểm tra xem còn Dialog nào khác đang mở không
            bool isAnyOtherDialogActive = 
                (_settingsDialog && _settingsDialog.gameObject.activeSelf) ||
                (_dialogGuide && _dialogGuide.gameObject.activeSelf) ||
                (_musicChangeDialog && _musicChangeDialog.gameObject.activeSelf) ||
                (_gameOverDialog && _gameOverDialog.gameObject.activeSelf);

            // Nếu không còn ai dùng nền đen -> Tắt đi
            if (!isAnyOtherDialogActive)
            {
                if (_panelContainer) _panelContainer.SetActive(false);
            }
        }

        #endregion

        #region Shared Logic
        public void OnDialogCloseClicked()
        {
            CloseAllDialogs();
            if (GameManager.Instance.CurrentState == GameState.Paused)
            {
                GameManager.Instance.ResumeGame();
            }
        }
        
        private void UpdateScoreUI(int currentScore, int highScore)
        {
            if (_gameplayHUD) _gameplayHUD.UpdateScore(currentScore, highScore);
        }
        
        public void OnPauseSettingBtnClicked() { GameManager.Instance.PauseGame(); ShowSettings(); }
        public void OnGuideCloseClicked() => OnDialogCloseClicked();
        #endregion
    }
}