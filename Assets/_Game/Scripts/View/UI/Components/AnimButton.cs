using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using _Game.Scripts.Core; // Để gọi Audio Click

namespace _Game.Scripts.View.UI.Components
{
    [RequireComponent(typeof(RectTransform))] // Bắt buộc phải có RectTransform
    public class AnimButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Settings")]
        [Tooltip("Tỷ lệ thu nhỏ khi bấm (0.9 = 90%)")]
        [SerializeField] private float _pressedScale = 0.9f;
        
        [Tooltip("Tốc độ hiệu ứng")]
        [SerializeField] private float _speed = 15f;
        
        [Tooltip("Có phát tiếng click không?")]
        [SerializeField] private bool _playClickSound = true;

        [Header("Events")]
        // Sự kiện này sẽ hiện trong Inspector giống như Button thường
        public UnityEvent OnClick;

        private RectTransform _rectTransform;
        private Vector3 _originalScale;
        private Vector3 _targetScale;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _originalScale = _rectTransform.localScale;
            _targetScale = _originalScale;
        }

        private void OnEnable()
        {
            // Reset trạng thái mỗi khi bật lại nút
            _rectTransform.localScale = _originalScale;
            _targetScale = _originalScale;
        }

        private void Update()
        {
            // Lerp để tạo hiệu ứng mượt mà thay vì giật cục
            _rectTransform.localScale = Vector3.Lerp(_rectTransform.localScale, _targetScale, Time.unscaledDeltaTime * _speed);
        }

        // 1. Khi ngón tay chạm vào
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Interactable()) return;

            _targetScale = _originalScale * _pressedScale; // Thu nhỏ lại
        }

        // 2. Khi ngón tay thả ra
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!Interactable()) return;

            _targetScale = _originalScale; // Trả về kích thước gốc

            // QUAN TRỌNG: Kiểm tra xem lúc thả tay, ngón tay có còn nằm trên nút không?
            // Nếu người dùng bấm, xong kéo tay ra chỗ khác rồi thả -> Coi như hủy.
            if (RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, eventData.position, eventData.pressEventCamera))
            {
                // Phát tiếng click
                if (_playClickSound && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayClickSound();
                }

                // Gọi sự kiện Click
                OnClick?.Invoke();
            }
        }

        // Kiểm tra xem nút có bị disable không (nếu dùng kèm Button component của Unity)
        private bool Interactable()
        {
            var btn = GetComponent<Button>();
            if (btn != null && !btn.interactable) return false;
            
            var group = GetComponent<CanvasGroup>();
            if (group != null && !group.interactable) return false;

            return true;
        }
    }
}
