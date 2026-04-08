using BreakItAll.Gameplay;
using BreakItAll.Modes;
using BreakItAll.UI;
using UnityEngine;

namespace BreakItAll.Core
{
    /// <summary>
    /// Scene entry point cho Gameplay scene dùng chung.
    /// Ở M1.5 chỉ khởi động Endless baseline.
    /// </summary>
    public sealed class GameplayEntryPoint : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private GameplayHUDController gameplayHUDController;
        [SerializeField] private BoardVisualController boardVisualController;
        [SerializeField] private QueuePanelController queuePanelController;

        private EndlessModeController _endlessModeController;
        private EndlessGameplayPresenter _presenter;

        private void Start()
        {
            ShapeSpawnPool pool = DefaultShapeLibrary.CreateDefaultPool();

            BoardController boardController = new BoardController(
                GameConstants.DefaultBoardWidth,
                GameConstants.DefaultBoardHeight,
                pool);

            ScoreSystem scoreSystem = new ScoreSystem();
            EndlessSessionContext sessionContext = new EndlessSessionContext(boardController, scoreSystem);
            _endlessModeController = new EndlessModeController(sessionContext);

            _endlessModeController.StartMode();

            _presenter = new EndlessGameplayPresenter(
                _endlessModeController,
                gameplayHUDController,
                boardVisualController,
                queuePanelController);

            _presenter.Initialize();
        }
    }
}