using _Game.Scripts.Logic.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.Scripts.View.UI.Tools
{
    /// <summary>
    /// Button UI de chon Gameplay tool, hien so luong inventory va overlay khi het tool.
    /// Nut add one chi la mock de test flow, sau nay co the noi reward/ad/shop.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class GameplayToolButton : MonoBehaviour
    {
        #region Inspector
        [SerializeField] private GameplayToolType _toolType = GameplayToolType.PlaceSingleCell;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private GameObject _emptyOverlay;
        [SerializeField] private Button _addOneButton;
        [SerializeField] private GameObject _selectedHighlight;
        #endregion

        #region Runtime
        private Button _button;
        private int _lastCount;
        private bool _isToolAvailable = true;
        private string _unavailableReason = string.Empty;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button != null)
                _button.onClick.AddListener(HandleClicked);

            if (_addOneButton != null)
                _addOneButton.onClick.AddListener(HandleAddClicked);

            ToolController.OnToolInventoryChanged += HandleInventoryChanged;
            ToolController.OnActiveToolChanged += HandleActiveToolChanged;
            ToolController.OnToolAvailabilityChanged += HandleAvailabilityChanged;

            RefreshImmediate();
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClicked);

            if (_addOneButton != null)
                _addOneButton.onClick.RemoveListener(HandleAddClicked);

            ToolController.OnToolInventoryChanged -= HandleInventoryChanged;
            ToolController.OnActiveToolChanged -= HandleActiveToolChanged;
            ToolController.OnToolAvailabilityChanged -= HandleAvailabilityChanged;
        }
        #endregion

        #region Handlers
        private void HandleClicked()
        {
            if (ToolController.Instance == null) return;
            ToolController.Instance.SelectTool(_toolType);
        }

        private void HandleAddClicked()
        {
            if (ToolController.Instance == null) return;
            ToolController.Instance.RequestBuyTool(_toolType);
        }

        private void HandleInventoryChanged(GameplayToolType toolType, int count)
        {
            if (toolType != _toolType) return;
            ApplyCount(count);
        }

        private void HandleActiveToolChanged(GameplayToolType activeTool)
        {
            if (_selectedHighlight != null)
                _selectedHighlight.SetActive(activeTool == _toolType);
        }

        private void HandleAvailabilityChanged(GameplayToolType toolType, bool isAvailable, string reason)
        {
            if (toolType != _toolType) return;

            _isToolAvailable = isAvailable;
            _unavailableReason = reason ?? string.Empty;
            ApplyAvailability();
        }
        #endregion

        #region Helpers
        private void RefreshImmediate()
        {
            if (ToolController.Instance != null)
            {
                ApplyCount(ToolController.Instance.GetToolCount(_toolType));
                _isToolAvailable = ToolController.Instance.IsToolAllowed(_toolType);
                _unavailableReason = ToolController.Instance.GetToolUnavailableReason(_toolType);
                ApplyAvailability();
            }

            if (_selectedHighlight != null && ToolController.Instance != null)
                _selectedHighlight.SetActive(ToolController.Instance.ActiveTool == _toolType);
        }

        private void ApplyAvailability()
        {
            if (_button != null)
                _button.interactable = _isToolAvailable;

            bool isEmpty = _lastCount <= 0;
            if (_emptyOverlay != null)
                _emptyOverlay.SetActive(isEmpty || !_isToolAvailable);

            if (_addOneButton != null)
                _addOneButton.gameObject.SetActive(isEmpty && _isToolAvailable);

            if (_selectedHighlight != null && !_isToolAvailable)
                _selectedHighlight.SetActive(false);
        }

        private void ApplyCount(int count)
        {
            _lastCount = count;

            if (_countText != null)
                _countText.text = count.ToString();

            bool isEmpty = count <= 0;
            if (_emptyOverlay != null)
                _emptyOverlay.SetActive(isEmpty || !_isToolAvailable);

            if (_addOneButton != null)
                _addOneButton.gameObject.SetActive(isEmpty && _isToolAvailable);

            ApplyAvailability();
        }
        #endregion
    }
}
