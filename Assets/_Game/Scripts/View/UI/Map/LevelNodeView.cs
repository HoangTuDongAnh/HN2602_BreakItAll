using System.Collections;
using _Game.Scripts.Core.Arcade;
using _Game.Scripts.Modes.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.Scripts.View.UI.Map
{
    /// <summary>
    /// Circular Arcade map node. States are Locked, Unlocked/current, and Passed.
    /// </summary>
    public class LevelNodeView : MonoBehaviour
    {
        #region Inspector - References
        [Header("References")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _nodeBackground;
        [SerializeField] private Image _glowImage;
        [SerializeField] private GameObject _lockedOverlay;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private CanvasGroup _canvasGroup;
        #endregion

        #region Inspector - Colors
        [Header("Colors")]
        [SerializeField] private Color _lockedColor = new Color(0.44f, 0.5f, 0.62f, 1f);
        [SerializeField] private Color _unlockedColor = new Color(0.2f, 0.82f, 0.28f, 1f);
        [SerializeField] private Color _passedColor = new Color(1f, 0.78f, 0.16f, 1f);
        [SerializeField] private Color _ringColor = Color.white;
        [SerializeField] private Color _currentGlowColor = new Color(0.25f, 1f, 0.36f, 0.45f);
        [SerializeField] private Color _textTopColor = Color.white;
        [SerializeField] private Color _textBottomColor = new Color(0.8f, 0.95f, 1f, 1f);
        #endregion

        #region Inspector - Animation
        [Header("Animation")]
        [SerializeField] private bool _playPopAnimation = true;
        [SerializeField] private float _lockedAlpha = 0.72f;
        [SerializeField] private float _popDuration = 0.22f;
        [SerializeField] private float _popOvershootScale = 1.12f;
        #endregion

        #region Runtime
        private MapScreenController _owner;
        private MapLevelNodeDefinition _node;
        private bool _isUnlocked;
        private bool _isPassed;
        private int _displayNumber;
        private Coroutine _animationRoutine;
        private Image _ringImage;
        private Image _fillImage;
        private static Sprite _circleSprite;
        #endregion

        #region Public API
        public void Setup(MapScreenController owner, MapLevelNodeDefinition node, bool isUnlocked, bool isPassed, int displayNumber)
        {
            CacheReferences();
            EnsureGeneratedVisuals();

            _owner = owner;
            _node = node;
            _isUnlocked = isUnlocked;
            _isPassed = isPassed;
            _displayNumber = Mathf.Max(1, displayNumber);

            SetupButton();
            RefreshVisuals();
            RestartAnimation();
        }

        public void Setup(MapScreenController owner, MapLevelNodeDefinition node, bool isUnlocked, int stars, int displayNumber)
        {
            bool passed = node != null && ArcadeProgressService.IsLevelPassed(node.Level);
            Setup(owner, node, isUnlocked, passed, displayNumber);
        }
        #endregion

        #region Setup
        private void CacheReferences()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            if (_nodeBackground == null) _nodeBackground = GetComponent<Image>();
        }

        private void EnsureGeneratedVisuals()
        {
            Sprite circle = GetCircleSprite();

            if (_nodeBackground != null)
            {
                _nodeBackground.sprite = circle;
                _nodeBackground.type = Image.Type.Simple;
                _nodeBackground.preserveAspect = true;
            }

            if (_ringImage == null)
                _ringImage = CreateChildImage("Ring", 0.08f, 0.92f);

            if (_fillImage == null)
                _fillImage = CreateChildImage("Fill", 0.18f, 0.82f);

            if (_lockedOverlay == null)
            {
                Image overlay = CreateChildImage("Locked_Overlay", 0.18f, 0.82f);
                _lockedOverlay = overlay.gameObject;
            }

            if (_levelText != null)
            {
                RectTransform textRect = _levelText.transform as RectTransform;
                if (textRect != null)
                {
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                    textRect.anchoredPosition = Vector2.zero;
                    textRect.localScale = Vector3.one;
                }

                _levelText.transform.SetAsLastSibling();
            }
        }

        private Image CreateChildImage(string name, float min, float max)
        {
            Transform existing = transform.Find(name);
            GameObject child = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(transform, false);

            RectTransform rect = child.transform as RectTransform;
            rect.anchorMin = new Vector2(min, min);
            rect.anchorMax = new Vector2(max, max);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            Image image = child.GetComponent<Image>();
            image.sprite = GetCircleSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private void SetupButton()
        {
            if (_button == null) return;

            _button.onClick.RemoveListener(HandleClicked);
            _button.onClick.AddListener(HandleClicked);
            _button.interactable = _isUnlocked;
        }
        #endregion

        #region Visuals
        private void RefreshVisuals()
        {
            if (_levelText != null)
            {
                _levelText.text = _displayNumber.ToString();
                _levelText.alignment = TextAlignmentOptions.Center;
                _levelText.enableAutoSizing = true;
                _levelText.fontSizeMin = 24f;
                _levelText.fontSizeMax = 56f;
                ApplyTMPPolish(_levelText, 0.14f);
            }

            Color fillColor = !_isUnlocked ? _lockedColor : (_isPassed ? _passedColor : _unlockedColor);
            Color glowColor = !_isUnlocked
                ? new Color(0f, 0f, 0f, 0f)
                : (_isPassed ? new Color(_passedColor.r, _passedColor.g, _passedColor.b, 0.28f) : _currentGlowColor);

            if (_nodeBackground != null)
                _nodeBackground.color = glowColor;

            if (_ringImage != null)
                _ringImage.color = _isUnlocked ? _ringColor : new Color(0.78f, 0.82f, 0.9f, 0.78f);

            if (_fillImage != null)
                _fillImage.color = fillColor;

            if (_glowImage != null)
            {
                _glowImage.gameObject.SetActive(_isUnlocked);
                _glowImage.sprite = GetCircleSprite();
                _glowImage.color = glowColor;
                _glowImage.raycastTarget = false;
            }

            if (_lockedOverlay != null)
            {
                _lockedOverlay.SetActive(!_isUnlocked);
                Image overlay = _lockedOverlay.GetComponent<Image>();
                if (overlay != null)
                    overlay.color = new Color(0.08f, 0.12f, 0.18f, 0.22f);
            }

            if (_canvasGroup != null)
                _canvasGroup.alpha = _isUnlocked ? 1f : _lockedAlpha;
        }

        private void ApplyTMPPolish(TMP_Text text, float outlineWidth)
        {
            if (text == null) return;

            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(_textTopColor, _textTopColor, _textBottomColor, _textBottomColor);

            if (text.fontMaterial == null) return;

            Material material = text.fontMaterial;
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
            material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.78f));
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.35f);
            material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.08f);
            material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.35f));
        }
        #endregion

        #region Animation
        private void RestartAnimation()
        {
            if (!_playPopAnimation) return;

            if (_animationRoutine != null)
                StopCoroutine(_animationRoutine);

            _animationRoutine = StartCoroutine(PlayNodeAnimation());
        }

        private IEnumerator PlayNodeAnimation()
        {
            RectTransform rect = transform as RectTransform;
            if (rect == null) yield break;

            float startScale = _isUnlocked ? 0.78f : 0.92f;
            float overshoot = _isUnlocked ? _popOvershootScale : 1f;
            rect.localScale = Vector3.one * startScale;

            float time = 0f;
            while (time < _popDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / _popDuration);
                float scale = t < 0.55f
                    ? Mathf.Lerp(startScale, overshoot, t / 0.55f)
                    : Mathf.Lerp(overshoot, 1f, (t - 0.55f) / 0.45f);
                rect.localScale = Vector3.one * scale;
                yield return null;
            }

            rect.localScale = Vector3.one;
            _animationRoutine = null;
        }
        #endregion

        #region Events
        private void HandleClicked()
        {
            if (!_isUnlocked || _owner == null || _node == null) return;
            _owner.PlayNode(_node);
        }
        #endregion

        #region Sprite
        private static Sprite GetCircleSprite()
        {
            if (_circleSprite != null)
                return _circleSprite;

            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear
            };

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.48f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius + 1.5f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            _circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _circleSprite.hideFlags = HideFlags.HideAndDontSave;
            return _circleSprite;
        }
        #endregion
    }
}
