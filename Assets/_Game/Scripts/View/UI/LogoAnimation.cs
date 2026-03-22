using UnityEngine;

namespace _Game.Scripts.View.UI
{
    public class LogoAnimation : MonoBehaviour
    {
        [Header("Zoom Settings (Phóng to/nhỏ)")]
        [Tooltip("Tốc độ Zoom (Nhịp thở)")]
        [SerializeField] private float _zoomSpeed = 2.0f;
        
        [Tooltip("Độ lớn khi Zoom (1.0 = giữ nguyên, 1.1 = to hơn 10%)")]
        [SerializeField] private float _minScale = 0.95f;
        [SerializeField] private float _maxScale = 1.05f;

        [Header("Shake Settings (Lắc lư)")]
        [Tooltip("Tốc độ lắc (Nên để khác tốc độ Zoom để tạo sự lệch pha)")]
        [SerializeField] private float _shakeSpeed = 1.3f; // Để lẻ lẻ một chút cho khó trùng nhịp
        
        [Tooltip("Góc quay tối đa (Độ)")]
        [SerializeField] private float _shakeAngle = 3.0f; // Lắc nhẹ 3 độ thôi

        private RectTransform _rectTransform;
        private Vector3 _initialScale;
        
        // Biến đếm thời gian riêng biệt để tạo sự ngẫu nhiên
        private float _timeZoom;
        private float _timeShake;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _initialScale = transform.localScale;
            
            // Tạo sự lệch pha ngẫu nhiên ngay từ đầu
            _timeZoom = Random.Range(0f, 100f);
            _timeShake = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (_rectTransform == null) return;

            // 1. Xử lý Zoom (Dùng Sin để tạo nhịp thở đều đặn)
            // Mathf.Sin trả về giá trị từ -1 đến 1
            // Ta map nó vào khoảng [0, 1] để Lerp
            _timeZoom += Time.deltaTime * _zoomSpeed;
            float zoomSine = (Mathf.Sin(_timeZoom) + 1f) / 2f; 
            float currentScale = Mathf.Lerp(_minScale, _maxScale, zoomSine);
            
            _rectTransform.localScale = _initialScale * currentScale;

            // 2. Xử lý Lắc lư (Rotation)
            // Dùng Cos cho khác Sin một chút, và dùng thời gian riêng (_timeShake)
            _timeShake += Time.deltaTime * _shakeSpeed;
            float rotationZ = Mathf.Cos(_timeShake) * _shakeAngle;

            _rectTransform.localRotation = Quaternion.Euler(0, 0, rotationZ);
        }
    }
}