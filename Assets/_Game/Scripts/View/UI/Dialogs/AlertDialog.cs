using System;
using UnityEngine;
using _Game.Scripts.View.UI;

namespace _Game.Scripts.View.UI.Dialogs
{
    public class AlertDialog : MonoBehaviour
    {
        private Action _onYesAction;
        private Vector3 _initialScale; // Lưu trữ scale gốc (ví dụ: 3)

        private void Awake()
        {
            // Lưu lại kích thước bạn đã chỉnh trong Inspector
            _initialScale = transform.localScale;
        }

        private void OnEnable()
        {
            // Reset về kích thước gốc (tránh trường hợp animation đóng làm nó nhỏ đi)
            transform.localScale = _initialScale;
        }

        public void Setup(Action onYes)
        {
            _onYesAction = onYes;
        }

        public void OnYesClicked()
        {
            _onYesAction?.Invoke();
            Close();
        }

        public void OnNoClicked()
        {
            Close();
        }

        private void Close()
        {
            _onYesAction = null;
            gameObject.SetActive(false);

            if (UIManager.Instance != null) 
            {
                UIManager.Instance.OnAlertDialogClosed();
            }
        }
    }
}