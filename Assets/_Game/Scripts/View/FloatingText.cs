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

        private Color _startColor;
        private float _timer;

        public void Init(string content, Color color, float sizeScale = 1f)
        {
            _textMesh.text = content;
            _textMesh.color = color;
            _startColor = color;
            _timer = 0;
            
            transform.localScale = Vector3.one * sizeScale;
        }

        private void Update()
        {
            // 1. Bay lên
            transform.position += Vector3.up * _moveSpeed * Time.deltaTime;

            // 2. Mờ dần (Fade Alpha)
            _timer += Time.deltaTime;
            float progress = _timer / _lifeTime;
            float alpha = Mathf.Lerp(1f, 0f, progress);
            
            _textMesh.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);

            // 3. Tự hủy
            if (_timer >= _lifeTime) Destroy(gameObject);
        }
    }
}