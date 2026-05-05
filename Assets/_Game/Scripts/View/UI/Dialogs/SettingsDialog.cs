using UnityEngine;
using UnityEngine.UI;
using _Game.Scripts.Core;
using _Game.Scripts.View.UI;
using _Game.Scripts.Data;

namespace _Game.Scripts.View.UI.Dialogs
{
    public class SettingsDialog : MonoBehaviour
    {
        [Header("Audio Controls")]
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Toggle _soundToggle;
        [SerializeField] private Slider _soundSlider;

        [Header("Navigation References")]
        [SerializeField] private GameObject _btnHomeObj; // Nút Home (để ẩn/hiện)
        [SerializeField] private RectTransform _rtApplyBtn; // [NEW] RectTransform nút Apply
        [SerializeField] private RectTransform _rtCloseBtn; // [NEW] RectTransform nút Close

        [Header("Layout Config (X Position)")]
        [Tooltip("Tọa độ X của nút Apply khi ở màn hình HOME")]
        [SerializeField] private float _applyX_Home;
        [Tooltip("Tọa độ X của nút Apply khi đang CHƠI (Mặc định)")]
        [SerializeField] private float _applyX_Gameplay;

        [Space(10)]
        [Tooltip("Tọa độ X của nút Close khi ở màn hình HOME")]
        [SerializeField] private float _closeX_Home;
        [Tooltip("Tọa độ X của nút Close khi đang CHƠI (Mặc định)")]
        [SerializeField] private float _closeX_Gameplay;

        // Lưu trữ giá trị gốc để hoàn tác
        private float _origMusicVol, _origSfxVol;
        private bool _origMusicOn, _origSfxOn;

        private void Awake()
        {
            if (_musicSlider) _musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            if (_soundSlider) _soundSlider.onValueChanged.AddListener(OnSoundSliderChanged);
            if (_musicToggle) _musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
            if (_soundToggle) _soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
        }

        private void OnEnable()
        {
            // --- 1. Xử lý Layout (Ẩn hiện Home & Di chuyển nút) ---
            bool isAtHome = GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Home;

            // A. Ẩn/Hiện nút Home
            if (_btnHomeObj != null)
            {
                _btnHomeObj.SetActive(!isAtHome);
            }

            // B. Di chuyển nút Apply
            if (_rtApplyBtn != null)
            {
                Vector2 pos = _rtApplyBtn.anchoredPosition;
                pos.x = isAtHome ? _applyX_Home : _applyX_Gameplay;
                _rtApplyBtn.anchoredPosition = pos;
            }

            // C. Di chuyển nút Close
            if (_rtCloseBtn != null)
            {
                Vector2 pos = _rtCloseBtn.anchoredPosition;
                pos.x = isAtHome ? _closeX_Home : _closeX_Gameplay;
                _rtCloseBtn.anchoredPosition = pos;
            }

            // --- 2. Xử lý Audio ---
            if (AudioManager.Instance == null) return;

            _origMusicVol = AudioManager.Instance.GetMusicVolume();
            _origSfxVol = AudioManager.Instance.GetSFXVolume();
            _origMusicOn = AudioManager.Instance.IsMusicOn();
            _origSfxOn = AudioManager.Instance.IsSFXOn();

            InitializeUI();
        }

        private void InitializeUI()
        {
            if (_musicSlider) _musicSlider.SetValueWithoutNotify(_origMusicVol);
            if (_soundSlider) _soundSlider.SetValueWithoutNotify(_origSfxVol);
            if (_musicToggle) _musicToggle.SetIsOnWithoutNotify(_origMusicOn);
            if (_soundToggle) _soundToggle.SetIsOnWithoutNotify(_origSfxOn);
        }

        #region Realtime Events
        private void OnMusicSliderChanged(float value) => AudioManager.Instance?.SetMusicVolume(value);
        private void OnSoundSliderChanged(float value) => AudioManager.Instance?.SetSfxVolume(value);
        private void OnMusicToggleChanged(bool isOn) => AudioManager.Instance?.ToggleMusic(isOn);
        private void OnSoundToggleChanged(bool isOn) => AudioManager.Instance?.ToggleSfx(isOn);
        #endregion

        #region Actions

        public void OnApplyClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SaveSettings();
                AudioManager.Instance.PlayClickSound();
                
                _origMusicVol = AudioManager.Instance.GetMusicVolume();
                _origSfxVol = AudioManager.Instance.GetSFXVolume();
                _origMusicOn = AudioManager.Instance.IsMusicOn();
                _origSfxOn = AudioManager.Instance.IsSFXOn();
                
                CloseDialog();
            }
        }

        public void OnCloseClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(_origMusicVol);
                AudioManager.Instance.SetSfxVolume(_origSfxVol);
                AudioManager.Instance.ToggleMusic(_origMusicOn);
                AudioManager.Instance.ToggleSfx(_origSfxOn);
            }
            CloseDialog();
        }

        public void OnHomeClicked()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Home)
            {
                OnCloseClicked();
                return;
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowAlertDialog(() => 
                {
                    OnCloseClicked(); 
                    GameManager.Instance?.EndGame();
                });
            }
        }

        private void CloseDialog()
        {
            if (UIManager.Instance != null) UIManager.Instance.OnDialogCloseClicked();
        }

        #endregion
    }
}
