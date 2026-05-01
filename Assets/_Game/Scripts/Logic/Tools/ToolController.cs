using System;
using _Game.Scripts.Core;
using _Game.Scripts.Core.Services;
using _Game.Scripts.Modes;
using UnityEngine;

namespace _Game.Scripts.Logic.Tools
{
    /// <summary>
    /// Dieu phoi tool ho tro gameplay dung chung Endless/Arcade.
    /// Single/Bomb se tam thay queue bang mot tool block 1 o o slot giua de nguoi choi drag nhu block thuong.
    /// Remove se giu queue hien tai va bat hieu ung nhac nho tren cac spawn block.
    /// </summary>
    public class ToolController : MonoBehaviour
    {
        #region Events
        public static event Action<GameplayToolType> OnActiveToolChanged;
        public static event Action<GameplayToolType, int> OnToolInventoryChanged;
        public static event Action<GameplayToolType, string> OnToolPopupRequested;
        public static event Action OnToolPopupHidden;
        #endregion

        #region Singleton
        public static ToolController Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        #endregion

        #region Inspector
        [Header("Rules")]
        [SerializeField] private bool _allowToolsInArcade = true;
        [SerializeField] private bool _consumeToolOnSuccess = true;

        [Header("Initial Inventory")]
        [SerializeField] private int _singleCellCount = 3;
        [SerializeField] private int _removeSpawnBlockCount = 3;
        [SerializeField] private int _bombSquareCount = 3;

        [Header("Tool Block Visual")]
        [SerializeField] private Color _singleCellColor = Color.white;
        [SerializeField] private Color _bombBlockColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color _bombPreviewColor = new Color(1f, 0.65f, 0.1f, 0.65f);
        #endregion

        #region Runtime
        private const string SingleCellCountKey = "tool_single_cell_count";
        private const string RemoveSpawnBlockCountKey = "tool_remove_spawn_block_count";
        private const string BombSquareCountKey = "tool_bomb_square_count";

        public GameplayToolType ActiveTool { get; private set; } = GameplayToolType.None;
        public bool HasActiveTool => ActiveTool != GameplayToolType.None;
        public Color SingleCellColor => _singleCellColor;
        public Color BombBlockColor => _bombBlockColor;
        public Color BombPreviewColor => _bombPreviewColor;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            LoadInventory();
            BroadcastInventory();
            BroadcastActiveTool();
        }

        private void Update()
        {
            if (!HasActiveTool) return;

            if (!CanUseToolNow())
            {
                CancelActiveTool();
                return;
            }

            // Right click / Back tuong duong cancel nhanh khi test tren editor.
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                CancelActiveTool();
        }
        #endregion

        #region Public Button API
        public void SelectTool(GameplayToolType toolType)
        {
            if (toolType == GameplayToolType.None)
            {
                CancelActiveTool();
                return;
            }

            if (!CanUseToolNow()) return;

            // Neu bam lai tool dang chon thi coi nhu huy.
            if (ActiveTool == toolType)
            {
                CancelActiveTool();
                return;
            }

            if (GetToolCount(toolType) <= 0)
            {
                OnToolPopupRequested?.Invoke(toolType, "No tools left");
                return;
            }

            CancelActiveTool(true);

            bool started = false;
            switch (toolType)
            {
                case GameplayToolType.PlaceSingleCell:
                    started = BlockSpawner.Instance != null && BlockSpawner.Instance.BeginToolBlock(toolType, _singleCellColor, _bombPreviewColor);
                    break;
                case GameplayToolType.BombSquare:
                    started = BlockSpawner.Instance != null && BlockSpawner.Instance.BeginToolBlock(GameplayToolType.BombSquare, _bombBlockColor, _bombPreviewColor);
                    break;
                case GameplayToolType.RemoveSpawnBlock:
                    started = BlockSpawner.Instance != null && BlockSpawner.Instance.BeginRemoveToolFeedback();
                    break;
            }

            if (!started) return;

            ActiveTool = toolType;
            BroadcastActiveTool();
            ShowSelectionPopup(toolType);
        }

        public void SelectSingleCellTool() => SelectTool(GameplayToolType.PlaceSingleCell);
        public void SelectRemoveSpawnBlockTool() => SelectTool(GameplayToolType.RemoveSpawnBlock);
        public void SelectBombSquareTool() => SelectTool(GameplayToolType.BombSquare);
        public void SelectBombTool() => SelectTool(GameplayToolType.BombSquare);

        public void CancelActiveTool() => CancelActiveTool(true);

        public void CancelActiveTool(bool restoreQueue)
        {
            if (BlockSpawner.Instance != null)
            {
                if (restoreQueue)
                    BlockSpawner.Instance.CancelActiveToolAndRestoreQueue();

                BlockSpawner.Instance.EndRemoveToolFeedback();
            }

            if (GridManager.Instance != null)
                GridManager.Instance.ClearToolPreview();

            ActiveTool = GameplayToolType.None;
            BroadcastActiveTool();
            HideSelectionPopup();
        }

        public void AddOneTool(GameplayToolType toolType)
        {
            switch (toolType)
            {
                case GameplayToolType.PlaceSingleCell:
                    _singleCellCount++;
                    break;
                case GameplayToolType.RemoveSpawnBlock:
                    _removeSpawnBlockCount++;
                    break;
                case GameplayToolType.BombSquare:
                    _bombSquareCount++;
                    break;
            }

            BroadcastInventory();
            SaveInventory();
        }

        public int GetToolCount(GameplayToolType toolType)
        {
            switch (toolType)
            {
                case GameplayToolType.PlaceSingleCell:
                    return _singleCellCount;
                case GameplayToolType.RemoveSpawnBlock:
                    return _removeSpawnBlockCount;
                case GameplayToolType.BombSquare:
                    return _bombSquareCount;
                default:
                    return 0;
            }
        }

        public bool TryConsumeActiveTool()
        {
            if (!HasActiveTool) return false;

            GameplayToolType tool = ActiveTool;
            if (_consumeToolOnSuccess)
            {
                switch (tool)
                {
                    case GameplayToolType.PlaceSingleCell:
                        _singleCellCount = Mathf.Max(0, _singleCellCount - 1);
                        break;
                    case GameplayToolType.RemoveSpawnBlock:
                        _removeSpawnBlockCount = Mathf.Max(0, _removeSpawnBlockCount - 1);
                        break;
                    case GameplayToolType.BombSquare:
                        _bombSquareCount = Mathf.Max(0, _bombSquareCount - 1);
                        break;
                }
            }

            BroadcastInventory();
            SaveInventory();
            return true;
        }

        public void NotifyToolActionCompleted()
        {
            TryConsumeActiveTool();
            if (BlockSpawner.Instance != null)
            {
                BlockSpawner.Instance.RestoreQueueAfterToolUse();
                BlockSpawner.Instance.EndRemoveToolFeedback();
            }

            if (GridManager.Instance != null)
                GridManager.Instance.ClearToolPreview();

            ActiveTool = GameplayToolType.None;
            BroadcastActiveTool();
            HideSelectionPopup();
        }
        #endregion

        #region Remove Tool Runtime
        public bool TryRemoveSpawnBlock(BlockController block)
        {
            if (ActiveTool != GameplayToolType.RemoveSpawnBlock || block == null || BlockSpawner.Instance == null)
                return false;

            if (!BlockSpawner.Instance.RemoveSpawnBlock(block, true))
                return false;

            NotifyToolActionCompleted();
            return true;
        }
        #endregion

        #region Helpers
        private bool CanUseToolNow()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null) return true;
            if (!manager.IsInputAllowed()) return false;
            if (!_allowToolsInArcade && manager.CurrentModeType != GameModeType.Endless)
                return false;
            return true;
        }

        private void ShowSelectionPopup(GameplayToolType toolType)
        {
            string message;
            switch (toolType)
            {
                case GameplayToolType.PlaceSingleCell:
                    message = "Single block selected";
                    break;
                case GameplayToolType.RemoveSpawnBlock:
                    message = "Tap a spawn block to remove";
                    break;
                case GameplayToolType.BombSquare:
                    message = "Bomb selected";
                    break;
                default:
                    message = string.Empty;
                    break;
            }

            OnToolPopupRequested?.Invoke(toolType, message);
        }

        private void HideSelectionPopup()
        {
            OnToolPopupHidden?.Invoke();
        }

        private void BroadcastInventory()
        {
            OnToolInventoryChanged?.Invoke(GameplayToolType.PlaceSingleCell, _singleCellCount);
            OnToolInventoryChanged?.Invoke(GameplayToolType.RemoveSpawnBlock, _removeSpawnBlockCount);
            OnToolInventoryChanged?.Invoke(GameplayToolType.BombSquare, _bombSquareCount);
        }

        private void BroadcastActiveTool()
        {
            OnActiveToolChanged?.Invoke(ActiveTool);
        }

        private void LoadInventory()
        {
            IGameSaveService save = GameServices.Save;
            if (save == null) return;

            _singleCellCount = Mathf.Max(0, save.GetInt(SingleCellCountKey, _singleCellCount));
            _removeSpawnBlockCount = Mathf.Max(0, save.GetInt(RemoveSpawnBlockCountKey, _removeSpawnBlockCount));
            _bombSquareCount = Mathf.Max(0, save.GetInt(BombSquareCountKey, _bombSquareCount));
        }

        private void SaveInventory()
        {
            IGameSaveService save = GameServices.Save;
            if (save == null) return;

            save.SetInt(SingleCellCountKey, _singleCellCount);
            save.SetInt(RemoveSpawnBlockCountKey, _removeSpawnBlockCount);
            save.SetInt(BombSquareCountKey, _bombSquareCount);
            save.Save();
        }
        #endregion
    }
}
