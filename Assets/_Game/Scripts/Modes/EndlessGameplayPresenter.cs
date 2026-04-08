using BreakItAll.Gameplay;
using BreakItAll.UI;

namespace BreakItAll.Modes
{
    /// <summary>
    /// Nối Endless session/runtime state sang HUD + visual.
    /// UI chỉ present, không giữ game rules.
    /// </summary>
    public sealed class EndlessGameplayPresenter
    {
        private readonly EndlessModeController _modeController;
        private readonly GameplayHUDController _hudController;
        private readonly BoardVisualController _boardVisualController;
        private readonly QueuePanelController _queuePanelController;

        public EndlessGameplayPresenter(
            EndlessModeController modeController,
            GameplayHUDController hudController,
            BoardVisualController boardVisualController,
            QueuePanelController queuePanelController)
        {
            _modeController = modeController;
            _hudController = hudController;
            _boardVisualController = boardVisualController;
            _queuePanelController = queuePanelController;
        }

        public void Initialize()
        {
            _hudController.BindPlaceRequest(HandlePlaceRequest);
            _boardVisualController.Bind(_modeController.SessionContext.BoardController.BoardState);
            RefreshAll();
        }

        private void HandlePlaceRequest(int queueIndex, int anchorX, int anchorY)
        {
            EndlessTurnResult result = _modeController.TryPlaceFromQueue(queueIndex, new CellCoord(anchorX, anchorY));

            if (!result.Success)
            {
                _hudController.SetStatus($"Place failed: {result.FailureReason}");
                RefreshAll();
                return;
            }

            if (result.IsGameOver)
            {
                _hudController.SetStatus("Game Over");
            }
            else
            {
                int totalLines = result.ClearResult.ClearedRowCount + result.ClearResult.ClearedColumnCount;
                _hudController.SetStatus($"Placed. +{result.GainedScore} score. Cleared Lines: {totalLines}");
            }

            RefreshAll();
        }

        public void RefreshAll()
        {
            EndlessSessionContext session = _modeController.SessionContext;

            _hudController.SetScore(session.CurrentScore);
            _hudController.SetCombo(session.CurrentComboStep);

            if (session.IsGameOver)
            {
                _hudController.SetStatus("Game Over");
            }

            _boardVisualController.Refresh();
            _queuePanelController.Refresh(session.BoardController.CurrentQueue);
        }
    }
}