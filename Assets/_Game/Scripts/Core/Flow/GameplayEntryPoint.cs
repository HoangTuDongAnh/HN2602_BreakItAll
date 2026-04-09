using BreakItAll.Data;
using BreakItAll.Gameplay;
using BreakItAll.Infrastructure.Save;
using BreakItAll.Modes;
using BreakItAll.UI;
using UnityEngine;

namespace BreakItAll.Core
{
    /// <summary>
    /// Scene entry point cho Gameplay scene dùng chung.
    /// Ở M2.4 dùng data asset + spawn profile + save service để khởi tạo Endless baseline.
    /// </summary>
    public sealed class GameplayEntryPoint : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private GameplayHUDController gameplayHUDController;
        [SerializeField] private BoardVisualController boardVisualController;
        [SerializeField] private QueuePanelController queuePanelController;

        [Header("Data")]
        [SerializeField] private GameDataCatalog gameDataCatalog;
        [SerializeField] private string endlessModeId = "endless";

        private EndlessModeController _endlessModeController;
        private EndlessGameplayPresenter _presenter;
        private ISaveService _saveService;

        private void Start()
        {
            if (gameDataCatalog == null)
            {
                Debug.LogError("[GameplayEntryPoint] GameDataCatalog is missing.");
                return;
            }

            DataRepository dataRepository = new DataRepository(gameDataCatalog);

            ModeDefinition modeDefinition = dataRepository.GetMode(endlessModeId);
            RuntimeGameConfig runtimeConfig = BuildRuntimeConfig(dataRepository, modeDefinition);

            GameSettingsDefinition settings = dataRepository.GetGameSettings();
            int startingCoins = settings != null ? Mathf.Max(0, settings.startingCoins) : 0;

            _saveService = new SaveService();
            _saveService.LoadOrCreateProfile(startingCoins, endlessModeId);
            _saveService.SetLastSelectedMode(endlessModeId);

            string spawnProfileId = modeDefinition != null ? modeDefinition.defaultSpawnProfileId : string.Empty;
            ShapeSpawnPool shapeSpawnPool = RuntimeShapeFactory.BuildSpawnPool(
                dataRepository,
                GameModeType.Endless,
                spawnProfileId);

            BoardController boardController = new BoardController(
                runtimeConfig.BoardWidth,
                runtimeConfig.BoardHeight,
                runtimeConfig.QueueSize,
                shapeSpawnPool);

            ScoreSystem scoreSystem = new ScoreSystem(
                runtimeConfig.ScorePerLine,
                runtimeConfig.ComboBonusPerStep,
                runtimeConfig.ComboResetAfterMoves);

            EndlessSessionContext sessionContext = new EndlessSessionContext(boardController, scoreSystem);
            _endlessModeController = new EndlessModeController(sessionContext);
            _endlessModeController.StartMode();

            _presenter = new EndlessGameplayPresenter(
                _endlessModeController,
                gameplayHUDController,
                boardVisualController,
                queuePanelController,
                _saveService);

            _presenter.Initialize();
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }

        private RuntimeGameConfig BuildRuntimeConfig(IDataRepository dataRepository, ModeDefinition modeDefinition)
        {
            GameSettingsDefinition settings = dataRepository.GetGameSettings();
            if (settings == null)
            {
                Debug.LogError("[GameplayEntryPoint] GameSettingsDefinition is missing in catalog.");
                return new RuntimeGameConfig(8, 8, 3, 100, 25, 1);
            }

            SpawnProfileDefinition spawnProfile = null;

            if (modeDefinition != null && !string.IsNullOrWhiteSpace(modeDefinition.defaultSpawnProfileId))
            {
                spawnProfile = dataRepository.GetSpawnProfile(modeDefinition.defaultSpawnProfileId);
            }

            int resolvedQueueSize = settings.queueSize;
            if (spawnProfile != null && spawnProfile.queueSizeOverride > 0)
            {
                resolvedQueueSize = spawnProfile.queueSizeOverride;
            }

            return new RuntimeGameConfig(
                settings.boardWidth,
                settings.boardHeight,
                resolvedQueueSize,
                settings.scorePerLine,
                settings.comboBonusPerStep,
                settings.comboResetAfterMoves);
        }
    }
}
