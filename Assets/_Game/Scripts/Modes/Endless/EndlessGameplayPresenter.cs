using BreakItAll.Gameplay;
using BreakItAll.Infrastructure.Save;
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
        private readonly ISaveService _saveService;

        public EndlessGameplayPresenter(
            EndlessModeController modeController,
            GameplayHUDController hudController,
            BoardVisualController boardVisualController,
            QueuePanelController queuePanelController,
            ISaveService saveService)
        {
            _modeController = modeController;
            _hudController = hudController;
            _boardVisualController = boardVisualController;
            _queuePanelController = queuePanelController;
            _saveService = saveService;
        }

        public void Initialize()
        {
            _hudController.BindPlaceRequest(HandlePlaceRequest);
            _hudController.BindRestartRequest(HandleRestartRequest);
            _boardVisualController.Bind(_modeController.SessionContext.BoardController.BoardState);

            if (_saveService != null)
            {
                _saveService.ProfileChanged += HandleProfileChanged;
            }

            RefreshAll();
        }

        public void Dispose()
        {
            if (_saveService != null)
            {
                _saveService.ProfileChanged -= HandleProfileChanged;
            }
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
                bool isNewBest = _saveService != null && _saveService.TryUpdateBestEndlessScore(result.TotalScore);
                _hudController.SetStatus(isNewBest ? "Game Over - New Best!" : "Game Over");
            }
            else
            {
                int totalLines = result.ClearResult.ClearedRowCount + result.ClearResult.ClearedColumnCount;
                _hudController.SetStatus($"Placed. +{result.GainedScore} score. Cleared Lines: {totalLines}");
            }

            RefreshAll();
        }

        private void HandleRestartRequest()
        {
            _modeController.StartMode();
            _hudController.SetStatus("Endless restarted");
            RefreshAll();
        }

        private void HandleProfileChanged(PlayerProfile profile)
        {
            RefreshProfile(profile);
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
            RefreshProfile(_saveService != null ? _saveService.CurrentProfile : null);
        }

        private void RefreshProfile(PlayerProfile profile)
        {
            if (profile == null)
            {
                _hudController.SetBestScore(0);
                _hudController.SetCoins(0);
                return;
            }

            _hudController.SetBestScore(profile.bestEndlessScore);
            _hudController.SetCoins(profile.coins);
        }
    }
}
