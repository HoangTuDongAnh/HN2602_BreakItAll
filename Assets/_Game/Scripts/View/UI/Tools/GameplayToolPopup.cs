using _Game.Scripts.Logic.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.Scripts.View.UI.Tools
{
    /// <summary>
    /// Popup nho nam tren Tool_Bar de thong bao tool dang chon va cho phep huy.
    /// Co the gan vao UI tu thiet ke rieng; cac reference de trong se duoc bo qua an toan.
    /// </summary>
    public class GameplayToolPopup : MonoBehaviour
    {
        #region Inspector
        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _cancelButton;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_root == null)
                _root = gameObject;

            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            ToolController.OnToolPopupRequested += Show;
            ToolController.OnToolPopupHidden += Hide;

            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(HandleCancelClicked);

            Hide();
        }

        private void OnDisable()
        {
            ToolController.OnToolPopupRequested -= Show;
            ToolController.OnToolPopupHidden -= Hide;

            if (_cancelButton != null)
                _cancelButton.onClick.RemoveListener(HandleCancelClicked);
        }
        #endregion

        #region Public API
        public void Hide()
        {
            SetVisible(false);
        }
        #endregion

        #region Handlers
        private void Show(GameplayToolType toolType, string message)
        {
            SetVisible(true);

            if (_messageText != null)
                _messageText.text = message;
        }

        private void HandleCancelClicked()
        {
            if (ToolController.Instance != null)
                ToolController.Instance.CancelActiveTool();
            else
                Hide();
        }

        private void SetVisible(bool visible)
        {
            if (_root != null && _root != gameObject)
                _root.SetActive(visible);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }
        }
        #endregion
    }
}
