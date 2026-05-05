using System.Collections;
using UnityEngine;
using _Game.Scripts.Core.Services;
using _Game.Scripts.Data;
using _Game.Scripts.Logic.Resolve;
using _Game.Scripts.Modes;
using _Game.Scripts.Modes.Levels;

namespace _Game.Scripts.Core
{
    public class AudioManager : MonoBehaviour
    {
        #region Singleton
        public static AudioManager Instance { get; private set; }

        private Coroutine _loopCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            LoadSettings();
            GameServices.RegisterAudio(this);
        }

        private void OnDestroy()
        {
            if (GameServices.Audio == this)
                GameServices.RegisterAudio(null);

            GameEvents.OnMoveCompleted -= HandleMoveCompleted;
            GameEvents.OnBoardResolved -= HandleBoardResolved;
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnGamePaused -= HandleGamePaused;
            GameEvents.OnGameResumed -= HandleGameResumed;
            GameEvents.OnComboUpdated -= HandleComboUpdated;
            GameEvents.OnArcadeLevelCompleted -= HandleArcadeLevelCompleted;
            GameEvents.OnArcadeLevelFailed -= HandleArcadeLevelFailed;
            GameEvents.OnPuzzleBlockSetSwitched -= HandlePuzzleBlockSetSwitched;
            GameEvents.OnShapeOverlayToggled -= HandleShapeOverlayToggled;
        }
        #endregion

        #region Legacy Serialized Fields
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("BGM Playlist")]
        [SerializeField] private AudioClip[] _bgmList;
        [SerializeField] private float _loopGapDuration = 3f;
        [SerializeField] private bool _isLooping = true;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip _clickClip;
        [SerializeField] private AudioClip _placeBlockClip;
        [SerializeField] private AudioClip _clearLineClip;
        [SerializeField] private AudioClip _comboClip;
        [SerializeField] private AudioClip _gameOverClip;
        [SerializeField] private AudioClip _arcadeCompleteClip;
        [SerializeField] private AudioClip _arcadeFailClip;
        [SerializeField] private AudioClip _collectItemClip;
        [SerializeField] private AudioClip _timeBonusClip;
        [SerializeField] private AudioClip _puzzleSwitchClip;
        [SerializeField] private AudioClip _shapeOverlayClip;
        [SerializeField] private AudioClip _dialogOpenClip;
        [SerializeField] private AudioClip _dialogCloseClip;
        [SerializeField] private AudioClip _pauseClip;
        [SerializeField] private AudioClip _resumeClip;
        [SerializeField] private float _uiClickDebounceSeconds = 0.03f;

        [Header("Saved State")]
        [SerializeField] private bool _musicEnabled = true;
        [SerializeField] private bool _sfxEnabled = true;
        [SerializeField] private float _musicVolume = 1f;
        [SerializeField] private float _sfxVolume = 1f;
        [SerializeField] private int _currentBgmIndex = 0;

        public bool IsMusicEnabled => _musicEnabled;
        public bool IsSfxEnabled => _sfxEnabled;
        #endregion

        #region Lifecycle
        private int _lastUiClickFrame = -1;
        private float _lastUiClickTime = -999f;

        private void Start()
        {
            ApplyVolumes();
            ApplyMusicState();

            GameEvents.OnMoveCompleted += HandleMoveCompleted;
            GameEvents.OnBoardResolved += HandleBoardResolved;
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnGamePaused += HandleGamePaused;
            GameEvents.OnGameResumed += HandleGameResumed;
            GameEvents.OnComboUpdated += HandleComboUpdated;
            GameEvents.OnArcadeLevelCompleted += HandleArcadeLevelCompleted;
            GameEvents.OnArcadeLevelFailed += HandleArcadeLevelFailed;
            GameEvents.OnPuzzleBlockSetSwitched += HandlePuzzleBlockSetSwitched;
            GameEvents.OnShapeOverlayToggled += HandleShapeOverlayToggled;
        }

        private void Update()
        {
            if (!_musicEnabled) return;
            if (!_isLooping) return;
            if (_musicSource == null) return;
            if (_musicSource.clip == null) return;
            if (_musicSource.isPlaying) return;
            if (_loopCoroutine != null) return;

            _loopCoroutine = StartCoroutine(RestartMusicAfterGap());
        }
        #endregion

        #region Audio API
        public void PlayButtonClick() => PlayUiSfx(_clickClip, fallbackToClick: false);
        public void PlayPlaceBlock() => PlaySfx(_placeBlockClip);
        public void PlayLineClear() => PlaySfx(_clearLineClip);
        public void PlayGameOver() => PlaySfx(_gameOverClip);
        public void PlayArcadeComplete() => PlaySfx(ResolveClip(_arcadeCompleteClip, _comboClip, _clearLineClip));
        public void PlayArcadeFail() => PlaySfx(ResolveClip(_arcadeFailClip, _gameOverClip));
        public void PlayCollectItem() => PlaySfx(ResolveClip(_collectItemClip, _comboClip, _clickClip));
        public void PlayTimeBonus() => PlaySfx(ResolveClip(_timeBonusClip, _collectItemClip, _comboClip));
        public void PlayPuzzleSwitch() => PlayUiSfx(_puzzleSwitchClip, fallbackToClick: true);
        public void PlayShapeOverlay() => PlayUiSfx(_shapeOverlayClip, fallbackToClick: true);
        public void PlayDialogOpen() => PlayUiSfx(_dialogOpenClip, fallbackToClick: true);
        public void PlayDialogClose() => PlayUiSfx(_dialogCloseClip, fallbackToClick: true);
        public void PlayPause() => PlayUiSfx(_pauseClip, fallbackToClick: true);
        public void PlayResume() => PlayUiSfx(_resumeClip, fallbackToClick: true);

        public void SetMusicEnabled(bool enabled)
        {
            _musicEnabled = enabled;
            PersistSettings();
            ApplyMusicState();
        }

        public void SetSfxEnabled(bool enabled)
        {
            _sfxEnabled = enabled;
            PersistSettings();
        }
        #endregion

        #region Backward-Compatible API
        public void PlayClickSound() => PlayButtonClick();

        public float GetMusicVolume() => _musicVolume;
        public float GetSFXVolume() => _sfxVolume;
        public bool IsMusicOn() => _musicEnabled;
        public bool IsSFXOn() => _sfxEnabled;

        public void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            ApplyVolumes();
        }

        public void SetSfxVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);
            ApplyVolumes();
        }

        public void ToggleMusic(bool isOn)
        {
            _musicEnabled = isOn;
            ApplyMusicState();
        }

        public void ToggleSfx(bool isOn)
        {
            _sfxEnabled = isOn;
        }

        public void SaveSettings()
        {
            PersistSettings();
        }

        public void SaveBGMIndex(int index)
        {
            _currentBgmIndex = Mathf.Clamp(index, 0, Mathf.Max(0, GetBGMCount() - 1));
            PersistSettings();
        }

        public int GetCurrentBGMIndex() => _currentBgmIndex;

        public int GetBGMCount()
        {
            return _bgmList != null ? _bgmList.Length : 0;
        }

        public AudioClip GetBGMClip(int index)
        {
            if (_bgmList == null || _bgmList.Length == 0) return null;
            if (index < 0 || index >= _bgmList.Length) return null;
            return _bgmList[index];
        }

        public void PlayBGM(int index)
        {
            if (_musicSource == null) return;

            AudioClip clip = GetBGMClip(index);
            if (clip == null) return;

            _currentBgmIndex = index;
            _musicSource.clip = clip;
            _musicSource.volume = _musicVolume;

            if (_musicEnabled)
                _musicSource.Play();

            if (_loopCoroutine != null)
            {
                StopCoroutine(_loopCoroutine);
                _loopCoroutine = null;
            }
        }
        #endregion

        #region Event Handlers
        private void HandleMoveCompleted(int totalLines, Vector3 effectCenter)
        {
            if (totalLines > 0) PlayLineClear();
            else PlayPlaceBlock();
        }

        private void HandleBoardResolved(BoardResolveResult result)
        {
            if (result == null || !result.HasCollectedItems) return;

            bool hasTimeBonus = false;
            bool hasCollectItem = false;

            for (int i = 0; i < result.CollectedItems.Count; i++)
            {
                ResolvedBoardItem item = result.CollectedItems[i];
                if (item.ItemType == BoardItemType.TimeBonus)
                {
                    hasTimeBonus = true;
                    continue;
                }

                if (item.ItemType == BoardItemType.Gem || !string.IsNullOrWhiteSpace(item.ItemId))
                    hasCollectItem = true;
            }

            if (hasTimeBonus)
                PlayTimeBonus();
            else if (hasCollectItem)
                PlayCollectItem();
        }

        private void HandleGameOver()
        {
            if (GameManager.Instance != null
                && GameManager.Instance.CurrentModeType == GameModeType.Arcade
                && GameManager.Instance.ActiveArcadeLevel != null)
                return;

            PlayGameOver();
        }

        private void HandleGamePaused()
        {
            PlayPause();
        }

        private void HandleGameResumed()
        {
            PlayResume();
        }

        private void HandleComboUpdated(int combo)
        {
            if (combo > 1)
                PlaySfx(_comboClip);
        }

        private void HandleArcadeLevelCompleted(LevelDefinition level)
        {
            PlayArcadeComplete();
        }

        private void HandleArcadeLevelFailed(LevelDefinition level)
        {
            PlayArcadeFail();
        }

        private void HandlePuzzleBlockSetSwitched()
        {
            PlayPuzzleSwitch();
        }

        private void HandleShapeOverlayToggled(bool visible)
        {
            PlayShapeOverlay();
        }
        #endregion

        #region Internal
        private IEnumerator RestartMusicAfterGap()
        {
            yield return new WaitForSecondsRealtime(_loopGapDuration);

            if (_musicEnabled && _musicSource != null && _musicSource.clip != null)
                _musicSource.Play();

            _loopCoroutine = null;
        }

        private void PlayUiSfx(AudioClip primaryClip, bool fallbackToClick)
        {
            AudioClip clip = fallbackToClick ? ResolveClip(primaryClip, _clickClip) : primaryClip;
            if (clip == null) return;

            if (!TryBeginUiClickFeedback())
                return;

            PlaySfx(clip);
        }

        private bool TryBeginUiClickFeedback()
        {
            if (Time.frameCount == _lastUiClickFrame)
                return false;

            float now = Time.unscaledTime;
            if (now - _lastUiClickTime < Mathf.Max(0f, _uiClickDebounceSeconds))
                return false;

            _lastUiClickFrame = Time.frameCount;
            _lastUiClickTime = now;
            return true;
        }

        private void PlaySfx(AudioClip clip)
        {
            if (!_sfxEnabled) return;
            if (_sfxSource == null) return;
            if (clip == null) return;

            _sfxSource.volume = _sfxVolume;
            _sfxSource.PlayOneShot(clip);
        }

        private static AudioClip ResolveClip(params AudioClip[] clips)
        {
            if (clips == null) return null;

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    return clips[i];
            }

            return null;
        }

        private void ApplyVolumes()
        {
            if (_musicSource != null)
                _musicSource.volume = _musicVolume;

            if (_sfxSource != null)
                _sfxSource.volume = _sfxVolume;
        }

        private void ApplyMusicState()
        {
            if (_musicSource == null) return;

            if (_musicEnabled)
            {
                AudioClip clip = GetBGMClip(_currentBgmIndex);
                if (clip != null)
                    _musicSource.clip = clip;

                _musicSource.volume = _musicVolume;

                if (_musicSource.clip != null && !_musicSource.isPlaying)
                    _musicSource.Play();
            }
            else
            {
                _musicSource.Stop();
            }

            if (_loopCoroutine != null)
            {
                StopCoroutine(_loopCoroutine);
                _loopCoroutine = null;
            }
        }

        private void LoadSettings()
        {
            _musicEnabled = GameSave.GetBool(SaveKeys.MusicEnabled, true);
            _sfxEnabled = GameSave.GetBool(SaveKeys.SfxEnabled, true);
            _musicVolume = GameSave.GetFloat(SaveKeys.MusicVolume, 1f);
            _sfxVolume = GameSave.GetFloat(SaveKeys.SfxVolume, 1f);
            _currentBgmIndex = GameSave.GetInt(SaveKeys.BgmIndex, 0);
        }

        private void PersistSettings()
        {
            GameSave.SetBool(SaveKeys.MusicEnabled, _musicEnabled);
            GameSave.SetBool(SaveKeys.SfxEnabled, _sfxEnabled);
            GameSave.SetFloat(SaveKeys.MusicVolume, _musicVolume);
            GameSave.SetFloat(SaveKeys.SfxVolume, _sfxVolume);
            GameSave.SetInt(SaveKeys.BgmIndex, _currentBgmIndex);
            GameSave.Save();
        }
        #endregion
    }
}
