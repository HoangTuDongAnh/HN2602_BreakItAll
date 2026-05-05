using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.Scripts.View.UI.Transitions
{
    /// <summary>
    /// Transition đơn giản dạng loading overlay toàn màn hình.
    /// Dùng khi đổi Screen_Home / Screen_Gameplay / Screen_Map.
    /// Root object phải active; script sẽ tự ẩn/hiện bằng CanvasGroup.
    /// </summary>
    public class UILoadingTransitionController : MonoBehaviour
    {
        #region References
        [Header("References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _loadingVisual;
        [SerializeField] private TextMeshProUGUI _loadingText;
        #endregion

        #region Timing
        [Header("Timing")]
        [SerializeField] private float _fadeInDuration = 0.18f;
        [SerializeField] private float _minimumVisibleDuration = 0.9f;
        [SerializeField] private float _fadeOutDuration = 0.18f;
        [SerializeField] private bool _useUnscaledTime = true;
        #endregion

        #region Loading Text
        [Header("Loading Text")]
        [SerializeField] private string _loadingLabel = "Loading";
        [SerializeField] private float _dotInterval = 0.18f;
        #endregion

        #region Visual Shake
        [Header("Visual Shake")]
        [SerializeField] private float _shakeAngle = 6f;
        [SerializeField] private float _shakeSpeed = 16f;
        [SerializeField] private float _bobAmount = 8f;
        [SerializeField] private float _bobSpeed = 8f;
        #endregion

        #region Runtime
        private Coroutine _routine;
        private Coroutine _textRoutine;
        private Coroutine _visualRoutine;
        private Vector2 _visualStartAnchoredPosition;
        private Quaternion _visualStartRotation;

        public bool IsPlaying { get; private set; }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (_loadingVisual != null)
            {
                _visualStartAnchoredPosition = _loadingVisual.anchoredPosition;
                _visualStartRotation = _loadingVisual.localRotation;
            }

            HideImmediate();
        }

        private void OnDisable()
        {
            StopLoopRoutines();
            IsPlaying = false;
        }
        #endregion

        #region Public API
        public void Play(Action onMidPoint)
        {
            Play(onMidPoint, null);
        }

        public void Play(Action onMidPoint, Action onComplete)
        {
            // The overlay is allowed to be inactive in the scene/prefab for design convenience.
            // StartCoroutine cannot run on an inactive GameObject, so make sure the overlay
            // is active before starting the transition routine.
            EnsureActiveForCoroutine();

            if (!isActiveAndEnabled)
            {
                // If a disabled parent or disabled component still prevents coroutine execution,
                // do the screen switch immediately instead of throwing a coroutine error.
                onMidPoint?.Invoke();
                onComplete?.Invoke();
                return;
            }

            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(PlayRoutine(onMidPoint, onComplete));
        }

        private void EnsureActiveForCoroutine()
        {
            // Activate parents too; activeSelf=true on this object is not enough if a parent is inactive.
            Transform current = transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                    current.gameObject.SetActive(true);

                current = current.parent;
            }

            if (!enabled)
                enabled = true;

            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }

        public void HideImmediate()
        {
            if (_canvasGroup == null) return;

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            StopLoopRoutines();
            ResetVisualTransform();
            IsPlaying = false;
        }
        #endregion

        #region Main Routine
        private IEnumerator PlayRoutine(Action onMidPoint, Action onComplete)
        {
            IsPlaying = true;
            gameObject.SetActive(true);

            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            StartLoopRoutines();

            yield return FadeRoutine(0f, 1f, _fadeInDuration);

            onMidPoint?.Invoke();

            float elapsed = 0f;
            float minDuration = Mathf.Max(0f, _minimumVisibleDuration);
            while (elapsed < minDuration)
            {
                elapsed += GetDeltaTime();
                yield return null;
            }

            yield return FadeRoutine(1f, 0f, _fadeOutDuration);

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            StopLoopRoutines();
            ResetVisualTransform();

            IsPlaying = false;
            _routine = null;

            onComplete?.Invoke();
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            if (_canvasGroup == null) yield break;

            duration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Mathf.Clamp01(elapsed / duration);
                t = Mathf.SmoothStep(0f, 1f, t);
                _canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            _canvasGroup.alpha = to;
        }
        #endregion

        #region Loop Routines
        private void StartLoopRoutines()
        {
            StopLoopRoutines();

            if (_loadingText != null)
                _textRoutine = StartCoroutine(LoadingTextRoutine());

            if (_loadingVisual != null)
                _visualRoutine = StartCoroutine(VisualShakeRoutine());
        }

        private void StopLoopRoutines()
        {
            if (_textRoutine != null)
            {
                StopCoroutine(_textRoutine);
                _textRoutine = null;
            }

            if (_visualRoutine != null)
            {
                StopCoroutine(_visualRoutine);
                _visualRoutine = null;
            }
        }

        private IEnumerator LoadingTextRoutine()
        {
            int dotCount = 1;

            while (true)
            {
                _loadingText.text = _loadingLabel + new string('.', dotCount);
                dotCount++;
                if (dotCount > 3) dotCount = 1;

                float elapsed = 0f;
                float wait = Mathf.Max(0.03f, _dotInterval);
                while (elapsed < wait)
                {
                    elapsed += GetDeltaTime();
                    yield return null;
                }
            }
        }

        private IEnumerator VisualShakeRoutine()
        {
            while (true)
            {
                float time = GetTime();
                float angle = Mathf.Sin(time * _shakeSpeed) * _shakeAngle;
                float bob = Mathf.Sin(time * _bobSpeed) * _bobAmount;

                _loadingVisual.localRotation = _visualStartRotation * Quaternion.Euler(0f, 0f, angle);
                _loadingVisual.anchoredPosition = _visualStartAnchoredPosition + new Vector2(0f, bob);

                yield return null;
            }
        }

        private void ResetVisualTransform()
        {
            if (_loadingVisual == null) return;

            _loadingVisual.anchoredPosition = _visualStartAnchoredPosition;
            _loadingVisual.localRotation = _visualStartRotation;
        }
        #endregion

        #region Time Helpers
        private float GetDeltaTime()
        {
            return _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        private float GetTime()
        {
            return _useUnscaledTime ? Time.unscaledTime : Time.time;
        }
        #endregion
    }
}
