using _Game.Scripts.Data;
using _Game.Scripts.Logic;
using UnityEngine;

namespace _Game.Scripts.View
{
    public class GridCellView : MonoBehaviour
    {
        #region Components
        [Header("References")]
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private SpriteRenderer _itemRenderer;
        [SerializeField] private TextMesh _debugText;
        #endregion

        #region Visual Config
        [Header("Colors")]
        [SerializeField] private Color _emptyColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _validPreviewColor = new Color(1f, 1f, 1f, 0.55f);
        [SerializeField] private Color _invalidPreviewColor = new Color(1f, 0.25f, 0.25f, 0.65f);
        [SerializeField] private Color _timeBonusColor = new Color(0.2f, 0.75f, 1f, 1f);
        [SerializeField] private Color _gemColor = new Color(0.25f, 1f, 0.55f, 1f);
        [SerializeField] private Color _targetPatternColor = new Color(1f, 0.85f, 0.25f, 1f);

        [Header("Item Marker")]
        [SerializeField] private float _itemMarkerScale = 0.38f;
        [SerializeField] private int _itemSortingOrderOffset = 8;

        [Header("Clear Preview / Animation")]
        [SerializeField] private Color _clearPreviewTint = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private float _clearPreviewPulseScale = 1.08f;
        [SerializeField] private Color _clearFlashColor = Color.white;
        #endregion

        #region Runtime
        private GridCell _linkedData;
        private Color _themeColor = Color.white;
        private Vector3 _baseScale = Vector3.one;
        private static Sprite _circleSprite;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _baseScale = transform.localScale;
            CacheItemRenderer();
            ConfigureItemRenderer();
        }
        #endregion

        #region Public API
        public void Init(GridCell data, Color themeColor)
        {
            _linkedData = data;
            _themeColor = themeColor;
            CacheItemRenderer();
            ConfigureItemRenderer();
            if (data != null) gameObject.name = $"Cell [{data.x},{data.y}]";
            UpdateVisual();
        }

        public void UpdateVisual()
        {
            if (_linkedData == null || _renderer == null) return;
            transform.localScale = _baseScale;

            if (_linkedData.IsOccupied)
                _renderer.color = ResolveBlockColor(_linkedData.Type);
            else if (_linkedData.IsTargetPatternCell)
                _renderer.color = _targetPatternColor;
            else
                _renderer.color = _emptyColor;

            RefreshItemMarker();
        }

        public void ShowShadowState() => ShowPreviewState(true);

        public void ShowPreviewState(bool isValid)
        {
            if (_renderer == null) return;
            transform.localScale = _baseScale;
            _renderer.color = isValid ? _validPreviewColor : _invalidPreviewColor;
            SetItemMarkerVisible(false);
        }

        public void ShowCustomPreview(Color previewColor)
        {
            if (_renderer == null) return;
            transform.localScale = _baseScale;
            _renderer.color = previewColor;
            SetItemMarkerVisible(false);
        }

        public void ShowClearPreview(Color blockColor)
        {
            if (_renderer == null) return;
            Color target = Color.Lerp(blockColor, _clearPreviewTint, 0.22f);
            target.a = Mathf.Max(target.a, 0.82f);
            _renderer.color = target;
            transform.localScale = _baseScale * _clearPreviewPulseScale;
            RefreshItemMarker();
        }

        public System.Collections.IEnumerator PlayClearAnimation(float duration)
        {
            if (_renderer == null) yield break;
            duration = Mathf.Max(0.05f, duration);
            Color startColor = _renderer.color;
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                _renderer.color = Color.Lerp(startColor, _clearFlashColor, pulse);
                transform.localScale = Vector3.LerpUnclamped(startScale, _baseScale * 1.18f, pulse);
                RefreshItemMarker();
                yield return null;
            }

            transform.localScale = _baseScale;
        }
        #endregion

        #region Helpers
        private void CacheItemRenderer()
        {
            if (_itemRenderer != null) return;

            Transform marker = transform.Find("Item_Marker");
            if (marker != null)
                _itemRenderer = marker.GetComponent<SpriteRenderer>();
        }

        private void ConfigureItemRenderer()
        {
            if (_itemRenderer == null) return;

            if (_circleSprite == null)
                _circleSprite = CreateCircleSprite();

            _itemRenderer.sprite = _circleSprite;
            _itemRenderer.transform.localPosition = new Vector3(0f, 0f, -0.08f);
            _itemRenderer.transform.localScale = Vector3.one * Mathf.Max(0.05f, _itemMarkerScale);

            int blockLayerId = SortingLayer.NameToID("Block");
            if (blockLayerId != 0)
                _itemRenderer.sortingLayerID = blockLayerId;

            if (_renderer != null)
                _itemRenderer.sortingOrder = _renderer.sortingOrder + _itemSortingOrderOffset;

            _itemRenderer.enabled = false;
        }

        private void RefreshItemMarker()
        {
            if (_itemRenderer == null || _linkedData == null)
                return;

            switch (_linkedData.ItemType)
            {
                case BoardItemType.Gem:
                    _itemRenderer.color = _gemColor;
                    SetItemMarkerVisible(true);
                    break;
                case BoardItemType.TimeBonus:
                    _itemRenderer.color = _timeBonusColor;
                    SetItemMarkerVisible(true);
                    break;
                default:
                    SetItemMarkerVisible(false);
                    break;
            }
        }

        private void SetItemMarkerVisible(bool visible)
        {
            if (_itemRenderer != null)
                _itemRenderer.enabled = visible;
        }

        private Color ResolveBlockColor(BlockCellType type)
        {
            switch (type)
            {
                case BlockCellType.TimeBonus: return _timeBonusColor;
                case BlockCellType.Gem: return _gemColor;
                case BlockCellType.Normal:
                default: return _themeColor;
            }
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 32;
            const float radius = size * 0.46f;
            const float center = (size - 1) * 0.5f;

            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                name = "Runtime_ItemMarker_Circle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color clear = new Color(1f, 1f, 1f, 0f);
            Color white = Color.white;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(radius - distance + 0.8f);
                    texture.SetPixel(x, y, alpha > 0f ? new Color(white.r, white.g, white.b, alpha) : clear);
                }
            }

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
        #endregion
    }
}
