using _Game.Scripts.Logic.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.Scripts.View.UI.Tools
{
    /// <summary>
    /// Popup dung chung cho Tool Bar.
    /// - Khi chon tool: hien thong bao tool dang active va cho phep cancel.
    /// - Khi mua tool: hien thong tin gia/cheat, Buy Tool de xac nhan, Cancel de huy mua.
    /// </summary>
    public class GameplayToolPopup : MonoBehaviour
    {
        #region Inspector
        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _buyButton;
        #endregion

        #region Runtime
        private GameplayToolType _pendingPurchaseTool = GameplayToolType.None;
        private bool _isPurchaseMode;
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

            AutoBindReferences();
        }

        private void OnEnable()
        {
            ToolController.OnToolPopupRequested += ShowToolMessage;
            ToolController.OnToolPurchasePopupRequested += ShowPurchase;
            ToolController.OnToolPopupHidden += Hide;

            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(HandleCancelClicked);

            if (_buyButton != null)
                _buyButton.onClick.AddListener(HandleBuyClicked);

            Hide();
        }

        private void OnDisable()
        {
            ToolController.OnToolPopupRequested -= ShowToolMessage;
            ToolController.OnToolPurchasePopupRequested -= ShowPurchase;
            ToolController.OnToolPopupHidden -= Hide;

            if (_cancelButton != null)
                _cancelButton.onClick.RemoveListener(HandleCancelClicked);

            if (_buyButton != null)
                _buyButton.onClick.RemoveListener(HandleBuyClicked);
        }
        #endregion

        #region Public API
        public void Hide()
        {
            _isPurchaseMode = false;
            _pendingPurchaseTool = GameplayToolType.None;

            SetBuyButtonVisible(false, false);
            SetVisible(false);
        }
        #endregion

        #region Handlers
        private void ShowToolMessage(GameplayToolType toolType, string message)
        {
            _isPurchaseMode = false;
            _pendingPurchaseTool = GameplayToolType.None;

            SetVisible(true);
            SetBuyButtonVisible(false, false);

            if (_messageText != null)
                _messageText.text = message;
        }

        private void ShowPurchase(GameplayToolType toolType, string message, int price, bool isCheat, bool canBuy)
        {
            _isPurchaseMode = true;
            _pendingPurchaseTool = toolType;

            SetVisible(true);
            SetBuyButtonVisible(true, canBuy);

            if (_messageText != null)
                _messageText.text = message;
        }

        private void HandleCancelClicked()
        {
            if (_isPurchaseMode)
            {
                if (ToolController.Instance != null)
                    ToolController.Instance.CancelToolPurchase();
                else
                    Hide();

                return;
            }

            if (ToolController.Instance != null)
                ToolController.Instance.CancelActiveTool();
            else
                Hide();
        }

        private void HandleBuyClicked()
        {
            if (!_isPurchaseMode || _pendingPurchaseTool == GameplayToolType.None)
                return;

            if (ToolController.Instance != null)
                ToolController.Instance.TryBuyTool(_pendingPurchaseTool);
            else
                Hide();
        }
        #endregion

        #region Helpers
        private void AutoBindReferences()
        {
            if (_messageText == null)
                _messageText = FindText("Text_Tool") ?? GetComponentInChildren<TextMeshProUGUI>(true);

            if (_cancelButton == null)
                _cancelButton = FindButton("Btn_CancelTool") ?? FindButton("Button_CancelTool") ?? FindButton("Button_Cancel");

            if (_buyButton == null)
                _buyButton = FindButton("Btn_BuyTool") ?? FindButton("Button_BuyTool") ?? FindButton("Button_Buy");
        }

        private Button FindButton(string childName)
        {
            Transform child = FindChild(transform, childName);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private TextMeshProUGUI FindText(string childName)
        {
            Transform child = FindChild(transform, childName);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        private Transform FindChild(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName)) return null;

            if (root.name == childName) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                Transform found = FindChild(child, childName);
                if (found != null) return found;
            }

            return null;
        }

        private void SetBuyButtonVisible(bool visible, bool interactable)
        {
            if (_buyButton == null) return;

            _buyButton.gameObject.SetActive(visible);
            _buyButton.interactable = interactable;
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
