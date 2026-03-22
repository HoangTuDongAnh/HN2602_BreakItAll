using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sources")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("Music Library")]
        [SerializeField] private List<AudioClip> _bgmList; 

        [Header("Music Settings")]
        [SerializeField] private float _loopGapDuration = 3.0f; 
        [SerializeField] private bool _isLooping = true;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip _clickClip;
        [SerializeField] private AudioClip _placeBlockClip;
        [SerializeField] private AudioClip _clearLineClip;
        [SerializeField] private AudioClip _comboClip;
        [SerializeField] private AudioClip _gameOverClip;

        // Settings Data
        private int _currentBgmIndex = 0; 
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;
        private bool _isMusicMuted = false;
        private bool _isSfxMuted = false;
        private Coroutine _musicCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            LoadSettings();
        }

        private void Start() => PlayBGM(_currentBgmIndex);
        private void OnEnable() => GameEvents.OnMoveCompleted += HandleMoveCompleted;
        private void OnDisable() => GameEvents.OnMoveCompleted -= HandleMoveCompleted;

        #region SFX Logic (Giữ nguyên)
        private void HandleMoveCompleted(int linesCleared, Vector3 pos)
        {
            if (linesCleared == 0) PlaySFX(_placeBlockClip, 0.9f, 1.1f);
            else if (linesCleared >= 2) PlaySFX(_comboClip);
            else PlaySFX(_clearLineClip);
        }
        public void PlayClickSound() => PlaySFX(_clickClip);
        public void PlayGameOverSound() { StopMusic(); PlaySFX(_gameOverClip); }
        public void PlaySFX(AudioClip clip, float minPitch = 1f, float maxPitch = 1f)
        {
            if (clip == null || _isSfxMuted) return;
            _sfxSource.pitch = Random.Range(minPitch, maxPitch);
            _sfxSource.PlayOneShot(clip);
        }
        #endregion

        #region Music Logic (Giữ nguyên)
        public void PlayBGM(int index)
        {
            if (_bgmList == null || _bgmList.Count == 0) return;
            if (index < 0 || index >= _bgmList.Count) index = 0;
            _currentBgmIndex = index;
            if (_musicCoroutine != null) StopCoroutine(_musicCoroutine);
            _musicCoroutine = StartCoroutine(PlayBGMWithGapRoutine(_bgmList[index]));
        }
        private IEnumerator PlayBGMWithGapRoutine(AudioClip clip)
        {
            _musicSource.loop = false;
            _musicSource.clip = clip;
            while (true)
            {
                _musicSource.Play();
                yield return new WaitForSecondsRealtime(clip.length);
                if (!_isLooping) break;
                yield return new WaitForSecondsRealtime(_loopGapDuration);
            }
        }
        public void StopMusic() { if (_musicCoroutine != null) StopCoroutine(_musicCoroutine); _musicSource.Stop(); }
        public void SaveBGMIndex(int index) { _currentBgmIndex = index; PlayerPrefs.SetInt("BGMIndex", index); PlayerPrefs.Save(); PlayBGM(_currentBgmIndex); }
        public int GetCurrentBGMIndex() => _currentBgmIndex;
        public AudioClip GetBGMClip(int index) => (_bgmList != null && index >= 0 && index < _bgmList.Count) ? _bgmList[index] : null;
        public int GetBGMCount() => _bgmList == null ? 0 : _bgmList.Count;
        #endregion

        #region Settings & Volume [ĐIỀU CHỈNH QUAN TRỌNG]

        // Chỉ thay đổi giá trị Runtime (Nghe thử ngay lập tức) - KHÔNG LƯU
        public void SetMusicVolume(float volume)
        {
            _musicVolume = volume;
            _musicSource.volume = _isMusicMuted ? 0 : _musicVolume;
        }

        public void SetSfxVolume(float volume)
        {
            _sfxVolume = volume;
            _sfxSource.volume = _isSfxMuted ? 0 : _sfxVolume;
        }

        public void ToggleMusic(bool isOn)
        {
            _isMusicMuted = !isOn; 
            _musicSource.mute = _isMusicMuted;
        }

        public void ToggleSfx(bool isOn)
        {
            _isSfxMuted = !isOn;
            _sfxSource.mute = _isSfxMuted;
        }
        
        public void SaveSettings()
        {
            PlayerPrefs.SetFloat("MusicVol", _musicVolume);
            PlayerPrefs.SetFloat("SFXVol", _sfxVolume);
            PlayerPrefs.SetInt("MusicMute", _isMusicMuted ? 1 : 0);
            PlayerPrefs.SetInt("SFXMute", _isSfxMuted ? 1 : 0);
            PlayerPrefs.Save();
            
            Debug.Log("Audio Settings Saved!");
        }

        private void LoadSettings()
        {
            _musicVolume = PlayerPrefs.GetFloat("MusicVol", 0.5f);
            _sfxVolume = PlayerPrefs.GetFloat("SFXVol", 1f);
            _isMusicMuted = PlayerPrefs.GetInt("MusicMute", 0) == 1;
            _isSfxMuted = PlayerPrefs.GetInt("SFXMute", 0) == 1;
            _currentBgmIndex = PlayerPrefs.GetInt("BGMIndex", 0);

            _musicSource.volume = _musicVolume;
            _musicSource.mute = _isMusicMuted;
            _sfxSource.volume = _sfxVolume;
            _sfxSource.mute = _isSfxMuted;
        }

        public float GetMusicVolume() => _musicVolume;
        public float GetSFXVolume() => _sfxVolume;
        public bool IsMusicOn() => !_isMusicMuted;
        public bool IsSFXOn() => !_isSfxMuted;

        #endregion
    }
}