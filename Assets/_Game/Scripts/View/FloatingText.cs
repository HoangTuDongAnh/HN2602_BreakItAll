using UnityEngine;
using TMPro;

namespace _Game.Scripts.View
{
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _textMesh;
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _fadeSpeed = 2f;
        [SerializeField] private float _lifeTime = 1.0f;
        [SerializeField] private float _punchScale = 1.18f;

        private Color _startColor;
        private float _timer;
        private Vector3 _baseScale;

        public void Init(string content, Color color, float sizeScale = 1f)
        {
            if (_textMesh == null) return;

            _textMesh.text = content;
            _textMesh.color = color;
            _startColor = color;
            _timer = 0f;

            _baseScale = Vector3.one * sizeScale;
            transform.localScale = _baseScale * _punchScale;
        }

        private void Update()
        {
            transform.position += Vector3.up * _moveSpeed * Time.deltaTime;

            _timer += Time.deltaTime;
            float progress = Mathf.Clamp01(_timer / Mathf.Max(0.01f, _lifeTime));
            float alpha = Mathf.Lerp(1f, 0f, progress);
            float scaleT = Mathf.Clamp01(progress * 4f);
            transform.localScale = Vector3.Lerp(_baseScale * _punchScale, _baseScale, scaleT);

            if (_textMesh != null)
                _textMesh.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);

            if (_timer >= _lifeTime) Destroy(gameObject);
        }
    }
}
