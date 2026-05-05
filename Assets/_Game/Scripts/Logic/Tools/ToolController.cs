using System;
using _Game.Scripts.Core;
using _Game.Scripts.Core.Arcade;
using _Game.Scripts.Modes;
using _Game.Scripts.Modes.Levels;
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
        public static event Action<GameplayToolType, bool, string> OnToolAvailabilityChanged;
        public static event Action<GameplayToolType, string> OnToolPopupRequested;
        public static event Action<GameplayToolType, string, int, bool, bool> OnToolPurchasePopupRequested;
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

        [Header("Tool Shop")]
        [SerializeField] private bool _cheatToolPurchase;
        [Min(0)] [SerializeField] private int _singleCellPrice = 30;
        [Min(0)] [SerializeField] private int _removeSpawnBlockPrice = 45;
        [Min(0)] [SerializeField] private int _bombSquarePrice = 60;

        [Header("Tool Block Visual")]
        [SerializeField] private Color _singleCellColor = Color.white;
        [SerializeField] private Color _bombBlockColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color _bombPreviewColor = new Color(1f, 0.65f, 0.1f, 0.65f);
        #endregion

        #region Runtime
        private readonly ToolInventoryService _inventory = new ToolInventoryService();

        public GameplayToolType ActiveTool { get; private set; } = GameplayToolType.None;
        public bool HasActiveTool => ActiveTool != GameplayToolType.None;
        public Color SingleCellColor => _singleCellColor;
        public Color BombBlockColor => _bombBlockColor;
        public Color BombPreviewColor => _bombPreviewColor;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            GameEvents.OnArcadeLevelStarted += HandleArcadeLevelStarted;
            GameEvents.OnGameStarted += HandleGameStarted;
        }

        private void OnDisable()
        {
            GameEvents.OnArcadeLevelStarted -= HandleArcadeLevelStarted;
            GameEvents.OnGameStarted -= HandleGameStarted;
        }

        private void Start()
        {
            LoadInventory();
            BroadcastInventory();
            BroadcastActiveTool();
            BroadcastAvailability();
        }

        private void Update()
        {
            if (!HasActiveTool) return;

            if (!CanUseToolNow(ActiveTool))
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

            if (!CanUseToolNow(toolType))
            {
                OnToolPopupRequested?.Invoke(toolType, GetToolDisabledReason(toolType));
                return;
            }

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
            AddTool(toolType, 1);
        }

        public void RequestBuyTool(GameplayToolType toolType)
        {
            if (toolType == GameplayToolType.None) return;

            int price = GetToolPrice(toolType);
            bool isCheat = _cheatToolPurchase;
            bool canBuy = isCheat || ArcadeProgressService.GetCoins() >= price;
            string message = BuildPurchaseMessage(toolType, price, isCheat, canBuy);

            OnToolPurchasePopupRequested?.Invoke(toolType, message, price, isCheat, canBuy);
        }

        public bool TryBuyTool(GameplayToolType toolType)
        {
            if (toolType == GameplayToolType.None) return false;

            int price = GetToolPrice(toolType);
            if (!_cheatToolPurchase)
            {
                int coins = ArcadeProgressService.GetCoins();
                if (coins < price)
                {
                    RequestBuyTool(toolType);
                    return false;
                }

                ArcadeProgressService.SetCoins(coins - price);
            }

            AddTool(toolType, 1);
            HideSelectionPopup();
            return true;
        }

        public void CancelToolPurchase()
        {
            HideSelectionPopup();
        }

        public int GetToolCount(GameplayToolType toolType)
        {
            return _inventory.GetCount(toolType);
        }

        public bool TryConsumeActiveTool()
        {
            if (!HasActiveTool) return false;

            if (_consumeToolOnSuccess && !_inventory.TryConsume(ActiveTool))
                return false;

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
        public bool IsToolAllowed(GameplayToolType toolType)
        {
            return CanUseToolNow(toolType);
        }

        public string GetToolUnavailableReason(GameplayToolType toolType)
        {
            return GetToolDisabledReason(toolType);
        }

        private bool CanUseToolNow(GameplayToolType toolType = GameplayToolType.None)
        {
            GameManager manager = GameManager.Instance;
            if (manager == null) return true;
            if (!manager.IsInputAllowed()) return false;

            if (manager.CurrentModeType == GameModeType.Endless)
                return true;

            if (!_allowToolsInArcade)
                return false;

            LevelDefinition activeLevel = manager.ActiveArcadeLevel;
            if (activeLevel == null)
                return true;

            GameplayToolType checkedTool = toolType == GameplayToolType.None ? ActiveTool : toolType;
            if (checkedTool == GameplayToolType.None)
                return activeLevel.ToolRule != null && activeLevel.ToolRule.IsToolAllowed(activeLevel.LevelType, GameplayToolType.PlaceSingleCell)
                       || activeLevel.ToolRule != null && activeLevel.ToolRule.IsToolAllowed(activeLevel.LevelType, GameplayToolType.RemoveSpawnBlock)
                       || activeLevel.ToolRule != null && activeLevel.ToolRule.IsToolAllowed(activeLevel.LevelType, GameplayToolType.BombSquare);

            return activeLevel.ToolRule == null || activeLevel.ToolRule.IsToolAllowed(activeLevel.LevelType, checkedTool);
        }

        private string GetToolDisabledReason(GameplayToolType toolType)
        {
            GameManager manager = GameManager.Instance;
            if (manager == null) return "Tool unavailable";
            if (!manager.IsInputAllowed()) return "Cannot use tools right now";
            if (manager.CurrentModeType != GameModeType.Endless && !_allowToolsInArcade)
                return "Tools are disabled in Arcade";

            LevelDefinition activeLevel = manager.ActiveArcadeLevel;
            if (activeLevel == null || activeLevel.ToolRule == null)
                return "Tool unavailable";

            string reason = activeLevel.ToolRule.GetDisabledReason(activeLevel.LevelType, toolType);
            return string.IsNullOrEmpty(reason) ? "Tool unavailable" : reason;
        }

        private void HandleArcadeLevelStarted(LevelDefinition level)
        {
            if (HasActiveTool && (level == null || level.ToolRule == null || !level.ToolRule.IsToolAllowed(level.LevelType, ActiveTool)))
                CancelActiveTool();

            BroadcastAvailability();
        }

        private void HandleGameStarted()
        {
            BroadcastAvailability();
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

        private void AddTool(GameplayToolType toolType, int amount)
        {
            _inventory.Add(toolType, amount);
            BroadcastInventory();
            SaveInventory();
        }

        public int GetToolPrice(GameplayToolType toolType)
        {
            switch (toolType)
            {
                case GameplayToolType.PlaceSingleCell:
                    return Mathf.Max(0, _singleCellPrice);
                case GameplayToolType.RemoveSpawnBlock:
                    return Mathf.Max(0, _removeSpawnBlockPrice);
                case GameplayToolType.BombSquare:
                    return Mathf.Max(0, _bombSquarePrice);
                default:
                    return 0;
            }
        }

        private string BuildPurchaseMessage(GameplayToolType toolType, int price, bool isCheat, bool canBuy)
        {
            string toolName = GetToolDisplayName(toolType);
            if (isCheat)
                return $"Cheat enabled\nGet 1 {toolName} for free?";

            if (!canBuy)
                return $"Not enough coins\n{toolName} costs {price} coins";

            return $"Buy 1 {toolName} for {price} coins?";
        }

        private string GetToolDisplayName(GameplayToolType toolType)
        {
            switch (toolType)
            {
                case GameplayToolType.PlaceSingleCell:
                    return "Single";
                case GameplayToolType.RemoveSpawnBlock:
                    return "Remove";
                case GameplayToolType.BombSquare:
                    return "Bomb";
                default:
                    return "Tool";
            }
        }

        private void BroadcastInventory()
        {
            OnToolInventoryChanged?.Invoke(GameplayToolType.PlaceSingleCell, _inventory.GetCount(GameplayToolType.PlaceSingleCell));
            OnToolInventoryChanged?.Invoke(GameplayToolType.RemoveSpawnBlock, _inventory.GetCount(GameplayToolType.RemoveSpawnBlock));
            OnToolInventoryChanged?.Invoke(GameplayToolType.BombSquare, _inventory.GetCount(GameplayToolType.BombSquare));
        }

        private void BroadcastActiveTool()
        {
            OnActiveToolChanged?.Invoke(ActiveTool);
        }

        private void BroadcastAvailability()
        {
            BroadcastToolAvailability(GameplayToolType.PlaceSingleCell);
            BroadcastToolAvailability(GameplayToolType.RemoveSpawnBlock);
            BroadcastToolAvailability(GameplayToolType.BombSquare);
        }

        private void BroadcastToolAvailability(GameplayToolType toolType)
        {
            bool allowed = CanUseToolNow(toolType);
            string reason = allowed ? string.Empty : GetToolDisabledReason(toolType);
            OnToolAvailabilityChanged?.Invoke(toolType, allowed, reason);
        }

        private void LoadInventory()
        {
            _inventory.Load(_singleCellCount, _removeSpawnBlockCount, _bombSquareCount);
        }

        private void SaveInventory()
        {
            _inventory.Save();
        }
        #endregion
    }
}
