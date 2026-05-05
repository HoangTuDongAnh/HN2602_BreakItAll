using System;
using UnityEngine;
using UnityEngine.UI;
using _Game.Scripts.Core;
using _Game.Scripts.Core.Arcade;
using _Game.Scripts.Data;
using _Game.Scripts.Modes.Levels;
using _Game.Scripts.Modes.Objectives;
using _Game.Scripts.Modes.Map;
using _Game.Scripts.View.UI.Dialogs;
using _Game.Scripts.View.UI.Map;
using _Game.Scripts.View.UI.Transitions;

namespace _Game.Scripts.View.UI
{
    public class UIManager : MonoBehaviour
    {
        #region Singleton
        public static UIManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            EnsureReferences();
            BindButtonClickSounds();
        }
        #endregion

        #region View References
        [Header("--- MAIN SCREENS ---")]
        [SerializeField] private HomeHUD _homeHUD;
        [SerializeField] private GameplayHUD _gameplayHUD;
        [SerializeField] private MapScreenController _mapScreen;
        [SerializeField] private GameObject _panelContainer;
        [SerializeField] private UILoadingTransitionController _loadingTransition;
        [SerializeField] private bool _useLoadingTransition = true;
        [SerializeField] private bool _transitionOnStart = false;

        [Header("--- DIALOGS ---")]
        [SerializeField] private SettingsDialog _settingsDialog;
        [SerializeField] private GameOverDialog _gameOverDialog;
        [SerializeField] private ArcadeResultDialog _arcadeResultDialog;
        [SerializeField] private LevelPreviewDialog _levelPreviewDialog;
        [SerializeField] private MusicChangeDialog _musicChangeDialog;
        [SerializeField] private AlertDialog _alertDialog;
        [SerializeField] private GuideDialog _dialogGuide;
        #endregion

        #region Unity Lifecycle
        private bool _hasStarted;
        private MainScreen _currentScreen = MainScreen.None;

        private enum MainScreen
        {
            None,
            Home,
            Gameplay,
            Map
        }

        private void Start()
        {
            EnsureReferences();
            BindButtonClickSounds();
            _hasStarted = true;
            ShowHome();
        }
        
        private void OnEnable() => GameEvents.OnScoreChanged += UpdateScoreUI;
        private void OnDisable() => GameEvents.OnScoreChanged -= UpdateScoreUI;
        #endregion

        #region Reference Auto Binding
        [ContextMenu("Auto Setup UI References")]
        private void EnsureReferences()
        {
            if (_homeHUD == null)
                _homeHUD = GetComponentInChildren<HomeHUD>(true);

            if (_gameplayHUD == null)
                _gameplayHUD = GetComponentInChildren<GameplayHUD>(true);

            if (_mapScreen == null)
                _mapScreen = GetComponentInChildren<MapScreenController>(true);

            if (_arcadeResultDialog == null)
                _arcadeResultDialog = FindComponentByName<ArcadeResultDialog>("Dialog_ArcadeResult")
                    ?? GetComponentInChildren<ArcadeResultDialog>(true);

            if (_levelPreviewDialog == null)
                _levelPreviewDialog = FindComponentByName<LevelPreviewDialog>("Dialog_LevelPreview")
                    ?? GetComponentInChildren<LevelPreviewDialog>(true);
        }

        private T FindComponentByName<T>(string objectName) where T : Component
        {
            if (string.IsNullOrEmpty(objectName)) return null;

            T[] components = GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component != null && component.gameObject.name == objectName)
                    return component;
            }

            return null;
        }

        #endregion

        #region Main Screen Navigation
        public void ShowHome()
        {
            ShowScreen(MainScreen.Home);
        }

        public void ShowGameplay()
        {
            ShowScreen(MainScreen.Gameplay);
        }

        public void ShowGameplay(Action onShown)
        {
            ShowScreen(MainScreen.Gameplay, onShown);
        }

        /// <summary>
        /// Tên ngắn để GameManager gọi. Giữ thêm ShowArcadeMap() alias bên dưới để không vỡ reference cũ.
        /// </summary>
        public void ShowMap()
        {
            ShowScreen(MainScreen.Map);
        }

        public void ShowArcadeMap() => ShowMap();

        private void ShowScreen(MainScreen targetScreen)
        {
            ShowScreen(targetScreen, null);
        }

        private void ShowScreen(MainScreen targetScreen, Action onShown)
        {
            if (_currentScreen == targetScreen)
            {
                RefreshScreen(targetScreen);
                BindButtonClickSounds();
                onShown?.Invoke();
                return;
            }

            if (ShouldUseLoadingTransition())
                _loadingTransition.Play(() => ApplyScreen(targetScreen), onShown);
            else
            {
                ApplyScreen(targetScreen);
                onShown?.Invoke();
            }
        }

        private bool ShouldUseLoadingTransition()
        {
            if (!_useLoadingTransition || _loadingTransition == null)
                return false;

            if (_loadingTransition.IsPlaying)
                return false;

            if (_currentScreen == MainScreen.None && !_transitionOnStart)
                return false;

            if (!_hasStarted && !_transitionOnStart)
                return false;

            return true;
        }

        private void ApplyScreen(MainScreen targetScreen)
        {
            switch (targetScreen)
            {
                case MainScreen.Home:
                    SetMainScreens(home: true, gameplay: false, map: false);
                    break;
                case MainScreen.Gameplay:
                    SetMainScreens(home: false, gameplay: true, map: false);
                    break;
                case MainScreen.Map:
                    SetMainScreens(home: false, gameplay: false, map: true);
                    break;
            }

            _currentScreen = targetScreen;
            RefreshScreen(targetScreen);
            CloseAllDialogs();
            BindButtonClickSounds();
        }

        private void RefreshScreen(MainScreen targetScreen)
        {
            if (targetScreen == MainScreen.Map && _mapScreen != null)
                _mapScreen.Rebuild();
        }

        private void SetMainScreens(bool home, bool gameplay, bool map)
        {
            if (_homeHUD) _homeHUD.gameObject.SetActive(home);
            if (_gameplayHUD) _gameplayHUD.gameObject.SetActive(gameplay);
            if (_mapScreen) _mapScreen.gameObject.SetActive(map);
        }
        #endregion

        #region Dialog Navigation
        public void ShowSettings()
        {
            EnablePanelContainer();
            PlayDialogOpenSound();
            if (_settingsDialog) _settingsDialog.gameObject.SetActive(true);
            if (_dialogGuide) _dialogGuide.gameObject.SetActive(false);
            if (_musicChangeDialog) _musicChangeDialog.gameObject.SetActive(false);
            BindButtonClickSounds();
        }

        public void ShowGuide()
        {
            EnablePanelContainer();
            PlayDialogOpenSound();
            if (_dialogGuide) _dialogGuide.gameObject.SetActive(true);
            if (_settingsDialog) _settingsDialog.gameObject.SetActive(false);
            if (_musicChangeDialog) _musicChangeDialog.gameObject.SetActive(false);
            BindButtonClickSounds();
        }

        public void ShowMusicChangeDialog()
        {
            EnablePanelContainer();
            PlayDialogOpenSound();
            if (_musicChangeDialog) _musicChangeDialog.gameObject.SetActive(true);
            if (_settingsDialog) _settingsDialog.gameObject.SetActive(false);
            if (_dialogGuide) _dialogGuide.gameObject.SetActive(false);
            BindButtonClickSounds();
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
            BindButtonClickSounds();
        }

        public void ShowLevelPreview(MapDefinition map, MapLevelNodeDefinition node)
        {
            if (node == null || node.Level == null)
                return;

            if (_levelPreviewDialog == null)
            {
                ArcadeSession.SelectLevel(map, node);
                GameManager.Instance?.StartArcadeLevel(node.Level);
                return;
            }

            EnablePanelContainer();
            PlayDialogOpenSound();
            _levelPreviewDialog.gameObject.SetActive(true);
            _levelPreviewDialog.Setup(map, node);
            CloseAllDialogs(exceptLevelPreview: true);
            BindButtonClickSounds();
        }

        public void ShowArcadeResult(LevelDefinition level, ObjectiveProgress progress, bool success, int rewardCoins)
        {
            if (_arcadeResultDialog == null)
            {
                if (success)
                    ShowArcadeMap();
                else
                    ShowGameOver(
                        ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0,
                        ScoreManager.Instance != null ? ScoreManager.Instance.BestScore : 0);

                return;
            }

            EnablePanelContainer();
            _arcadeResultDialog.gameObject.SetActive(true);
            _arcadeResultDialog.Setup(level, progress, success, rewardCoins);
            CloseAllDialogs(exceptArcadeResult: true);
            BindButtonClickSounds();
        }

        public void CloseAllDialogs(bool exceptGameOver = false, bool exceptArcadeResult = false, bool exceptLevelPreview = false)
        {
            if (_settingsDialog) _settingsDialog.gameObject.SetActive(false);
            if (_dialogGuide) _dialogGuide.gameObject.SetActive(false);
            if (_musicChangeDialog) _musicChangeDialog.gameObject.SetActive(false);
            if (_alertDialog) _alertDialog.gameObject.SetActive(false);
            if (!exceptGameOver && _gameOverDialog) _gameOverDialog.gameObject.SetActive(false);
            if (!exceptArcadeResult && _arcadeResultDialog) _arcadeResultDialog.gameObject.SetActive(false);
            if (!exceptLevelPreview && _levelPreviewDialog) _levelPreviewDialog.gameObject.SetActive(false);
            if (!exceptGameOver && !exceptArcadeResult && !exceptLevelPreview && _panelContainer) _panelContainer.SetActive(false);
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
            PlayDialogOpenSound();
            
            if (_alertDialog != null)
            {
                _alertDialog.gameObject.SetActive(true);
                _alertDialog.Setup(onConfirmAction);
                _alertDialog.transform.SetAsLastSibling(); 
                BindButtonClickSounds();
            }
            else
            {
                Debug.LogError("UIManager: Chưa gán Alert Dialog trong Inspector!");
            }
        }

        public void OnAlertDialogClosed()
        {
            bool isAnyOtherDialogActive = 
                (_settingsDialog && _settingsDialog.gameObject.activeSelf) ||
                (_dialogGuide && _dialogGuide.gameObject.activeSelf) ||
                (_musicChangeDialog && _musicChangeDialog.gameObject.activeSelf) ||
                (_arcadeResultDialog && _arcadeResultDialog.gameObject.activeSelf) ||
                (_levelPreviewDialog && _levelPreviewDialog.gameObject.activeSelf) ||
                (_gameOverDialog && _gameOverDialog.gameObject.activeSelf);

            if (!isAnyOtherDialogActive && _panelContainer)
                _panelContainer.SetActive(false);
        }
        #endregion

        #region Shared Logic
        public void OnDialogCloseClicked()
        {
            PlayDialogCloseSound();
            CloseAllDialogs();
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
                GameManager.Instance.ResumeGame();
        }
        
        private void UpdateScoreUI(int currentScore, int highScore)
        {
            if (_gameplayHUD) _gameplayHUD.UpdateScore(currentScore, highScore);
        }
        
        public void OnPauseSettingBtnClicked()
        {
            if (GameManager.Instance != null) GameManager.Instance.PauseGame();
            ShowSettings();
        }

        public void OnGuideCloseClicked() => OnDialogCloseClicked();

        private void BindButtonClickSounds()
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null) continue;

                button.onClick.RemoveListener(PlayButtonClickSound);
                button.onClick.AddListener(PlayButtonClickSound);
            }
        }

        private void PlayButtonClickSound()
        {
            AudioManager.Instance?.PlayButtonClick();
        }

        private void PlayDialogOpenSound()
        {
            AudioManager.Instance?.PlayDialogOpen();
        }

        private void PlayDialogCloseSound()
        {
            AudioManager.Instance?.PlayDialogClose();
        }
        #endregion
    }
}
