using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.Scripts.View.UI.Map
{
    /// <summary>
    /// Vẽ path UI giữa 2 node level.
    /// Root RectTransform luôn stretch full parent để mọi tọa độ trùng với node container.
    /// Điều này tránh lỗi line bị rơi xuống dưới / lệch khỏi node trong ScrollRect.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MapPathLineView : MonoBehaviour
    {
        #region Inspector
        [SerializeField] private Image _lineImage;
        #endregion

        #region Cached
        private RectTransform _rectTransform;
        private readonly List<RectTransform> _segments = new List<RectTransform>();
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            CacheReferences();
            StretchToParent();
        }
        #endregion

        #region Public API
        public void Setup(Vector2 start, Vector2 end, Color color, float thickness)
        {
            CacheReferences();
            StretchToParent();
            ClearSegments();

            if (_lineImage != null)
                _lineImage.enabled = false;

            RectTransform segment = GetOrCreateSegment(0, color);
            SetupStraightSegment(segment, start, end, color, Mathf.Max(1f, thickness));
        }

        public void SetupCurved(
            Vector2 start,
            Vector2 end,
            Color color,
            float thickness,
            float curveStrength,
            int segmentCount,
            float nodeRadiusOffset,
            float curveDirection = 1f)
        {
            CacheReferences();
            StretchToParent();
            ClearSegments();

            float safeThickness = Mathf.Max(1f, thickness);
            segmentCount = Mathf.Clamp(segmentCount, 6, 96);
            nodeRadiusOffset = Mathf.Max(0f, nodeRadiusOffset);

            Vector2 direction = end - start;
            float distance = direction.magnitude;
            if (distance <= 0.01f)
                return;

            Vector2 normalized = direction / distance;
            float trim = Mathf.Min(nodeRadiusOffset, distance * 0.42f);
            Vector2 trimmedStart = start + normalized * trim;
            Vector2 trimmedEnd = end - normalized * trim;

            Vector2 control = CalculateControlPoint(trimmedStart, trimmedEnd, curveStrength, curveDirection);
            Vector2 previous = trimmedStart;

            if (_lineImage != null)
                _lineImage.enabled = false;

            for (int i = 1; i <= segmentCount; i++)
            {
                float t = i / (float)segmentCount;
                Vector2 current = EvaluateQuadraticBezier(trimmedStart, control, trimmedEnd, t);
                RectTransform segment = GetOrCreateSegment(i - 1, color);
                SetupStraightSegment(segment, previous, current, color, safeThickness, safeThickness * 0.8f);
                previous = current;
            }
        }
        #endregion

        #region Segment Setup
        private RectTransform GetOrCreateSegment(int index, Color color)
        {
            while (_segments.Count <= index)
            {
                GameObject segmentObject = new GameObject($"Curve_Segment_{_segments.Count:00}", typeof(RectTransform), typeof(Image));
                segmentObject.transform.SetParent(transform, false);

                Image image = segmentObject.GetComponent<Image>();
                image.raycastTarget = false;
                image.color = color;

                _segments.Add(segmentObject.transform as RectTransform);
            }

            RectTransform segment = _segments[index];
            segment.gameObject.SetActive(true);
            return segment;
        }

        private void SetupStraightSegment(RectTransform rect, Vector2 start, Vector2 end, Color color, float thickness, float lengthPadding = 0f)
        {
            if (rect == null) return;

            Vector2 delta = end - start;
            float distance = delta.magnitude;
            if (distance <= 0.01f)
            {
                rect.gameObject.SetActive(false);
                return;
            }

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (start + end) * 0.5f;
            rect.sizeDelta = new Vector2(distance + Mathf.Max(0f, lengthPadding), thickness);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);

            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.enabled = true;
                image.color = color;
                image.raycastTarget = false;
            }
        }
        #endregion

        #region Curve Math
        private Vector2 CalculateControlPoint(Vector2 start, Vector2 end, float curveStrength, float curveDirection)
        {
            Vector2 middle = (start + end) * 0.5f;
            Vector2 delta = end - start;
            Vector2 perpendicular = new Vector2(-delta.y, delta.x).normalized;

            if (Mathf.Abs(curveDirection) < 0.01f)
                curveDirection = 1f;

            float bend = delta.magnitude * Mathf.Clamp(curveStrength, -0.85f, 0.85f) * Mathf.Sign(curveDirection);
            return middle + perpendicular * bend;
        }

        private Vector2 EvaluateQuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }
        #endregion

        #region Helpers
        private void CacheReferences()
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;

            if (_lineImage == null)
                _lineImage = GetComponent<Image>();
        }

        private void StretchToParent()
        {
            if (_rectTransform == null) return;

            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.one;
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
            _rectTransform.localScale = Vector3.one;
            _rectTransform.localRotation = Quaternion.identity;
            _rectTransform.anchoredPosition3D = Vector3.zero;
        }

        private void ClearSegments()
        {
            for (int i = 0; i < _segments.Count; i++)
            {
                if (_segments[i] != null)
                    Destroy(_segments[i].gameObject);
            }

            _segments.Clear();
        }
        #endregion
    }
}
