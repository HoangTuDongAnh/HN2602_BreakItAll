using System.Collections;
using UnityEngine;
using _Game.Scripts.Core.Services;

namespace _Game.Scripts.Core
{
    public class AudioManager : MonoBehaviour, IAudioService
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
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnComboUpdated -= HandleComboUpdated;
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
        private void Start()
        {
            ApplyVolumes();
            ApplyMusicState();

            GameEvents.OnMoveCompleted += HandleMoveCompleted;
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnComboUpdated += HandleComboUpdated;
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

        #region IAudioService
        public void PlayButtonClick() => PlaySfx(_clickClip);
        public void PlayPlaceBlock() => PlaySfx(_placeBlockClip);
        public void PlayLineClear() => PlaySfx(_clearLineClip);
        public void PlayGameOver() => PlaySfx(_gameOverClip);

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

        private void HandleGameOver()
        {
            PlayGameOver();
        }

        private void HandleComboUpdated(int combo)
        {
            if (combo > 1)
                PlaySfx(_comboClip);
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

        private void PlaySfx(AudioClip clip)
        {
            if (!_sfxEnabled) return;
            if (_sfxSource == null) return;
            if (clip == null) return;

            _sfxSource.volume = _sfxVolume;
            _sfxSource.PlayOneShot(clip);
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
            if (GameServices.Save != null)
            {
                _musicEnabled = GameServices.Save.GetBool("music_enabled", true);
                _sfxEnabled = GameServices.Save.GetBool("sfx_enabled", true);
                _musicVolume = GameServices.Save.GetFloat("music_volume", 1f);
                _sfxVolume = GameServices.Save.GetFloat("sfx_volume", 1f);
                _currentBgmIndex = GameServices.Save.GetInt("bgm_index", 0);
            }
            else
            {
                _musicEnabled = PlayerPrefs.GetInt("music_enabled", 1) == 1;
                _sfxEnabled = PlayerPrefs.GetInt("sfx_enabled", 1) == 1;
                _musicVolume = PlayerPrefs.GetFloat("music_volume", 1f);
                _sfxVolume = PlayerPrefs.GetFloat("sfx_volume", 1f);
                _currentBgmIndex = PlayerPrefs.GetInt("bgm_index", 0);
            }
        }

        private void PersistSettings()
        {
            if (GameServices.Save != null)
            {
                GameServices.Save.SetBool("music_enabled", _musicEnabled);
                GameServices.Save.SetBool("sfx_enabled", _sfxEnabled);
                GameServices.Save.SetFloat("music_volume", _musicVolume);
                GameServices.Save.SetFloat("sfx_volume", _sfxVolume);
                GameServices.Save.SetInt("bgm_index", _currentBgmIndex);
                GameServices.Save.Save();
            }
            else
            {
                PlayerPrefs.SetInt("music_enabled", _musicEnabled ? 1 : 0);
                PlayerPrefs.SetInt("sfx_enabled", _sfxEnabled ? 1 : 0);
                PlayerPrefs.SetFloat("music_volume", _musicVolume);
                PlayerPrefs.SetFloat("sfx_volume", _sfxVolume);
                PlayerPrefs.SetInt("bgm_index", _currentBgmIndex);
                PlayerPrefs.Save();
            }
        }
        #endregion
    }
}