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
            ToolController.Instance.AddOneTool(_toolType);
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
        #endregion

        #region Helpers
        private void RefreshImmediate()
        {
            if (ToolController.Instance != null)
                ApplyCount(ToolController.Instance.GetToolCount(_toolType));

            if (_selectedHighlight != null && ToolController.Instance != null)
                _selectedHighlight.SetActive(ToolController.Instance.ActiveTool == _toolType);
        }

        private void ApplyCount(int count)
        {
            if (_countText != null)
                _countText.text = count.ToString();

            bool isEmpty = count <= 0;
            if (_emptyOverlay != null)
                _emptyOverlay.SetActive(isEmpty);

            if (_addOneButton != null)
                _addOneButton.gameObject.SetActive(isEmpty);
        }
        #endregion
    }
}
