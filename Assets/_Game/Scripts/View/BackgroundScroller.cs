using UnityEngine;

namespace _Game.Scripts.View
{
    public class BackgroundScroller : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Tốc độ di chuyển")]
        [SerializeField] private float _scrollSpeed = 0.5f;

        [Tooltip("Hướng di chuyển (X, Y). Ví dụ: (1, 0) là sang phải, (1, -1) là chéo xuống")]
        [SerializeField] private Vector2 _direction = new Vector2(1, 0);

        private Renderer _renderer;
        private Vector2 _currentOffset;

        private void Start()
        {
            _renderer = GetComponent<Renderer>();
        }

        private void Update()
        {
            // Tính toán độ dịch chuyển
            // Time.deltaTime giúp chuyển động mượt mà theo thời gian thực
            float x = _direction.x * _scrollSpeed * Time.deltaTime;
            float y = _direction.y * _scrollSpeed * Time.deltaTime;

            // Cộng dồn vào offset hiện tại
            _currentOffset.x += x;
            _currentOffset.y += y;

            // Áp dụng offset vào Material của Quad
            if (_renderer != null)
            {
                _renderer.material.mainTextureOffset = _currentOffset;
            }
        }
    }
}