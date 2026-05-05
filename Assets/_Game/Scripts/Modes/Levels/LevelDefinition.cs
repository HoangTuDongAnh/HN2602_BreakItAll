using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Modes.Levels
{
    [CreateAssetMenu(fileName = "Level_Arcade_001", menuName = "_Game/Arcade/Level Definition")]
    public class LevelDefinition : ScriptableObject
    {
        public const int FixedBoardWidth = 9;
        public const int FixedBoardHeight = 9;

        #region Identity
        [Header("Identity")]
        [SerializeField] private string _levelId = "level_001";
        [SerializeField] private string _worldId = "world_01";
        [SerializeField] private int _orderIndex = 1;
        [SerializeField] private string _displayName = "Level 1";
        #endregion

        #region Board
        [Header("Board")]
        [HideInInspector, SerializeField] private int _boardWidth = FixedBoardWidth;
        [HideInInspector, SerializeField] private int _boardHeight = FixedBoardHeight;
        [SerializeField] private List<BoardCellData> _boardCells = new List<BoardCellData>();
        #endregion

        #region Objective
        [Header("Objective")]
        [SerializeField] private ArcadeLevelType _levelType = ArcadeLevelType.Collectable;
        [SerializeField] private ObjectiveDefinition _objectiveDefinition = new ObjectiveDefinition();

        [Tooltip("Used by Score, Collectable, and Puzzle levels. Shape does not use a timer.")]
        [SerializeField] private TimerRuleDefinition _timerRule = new TimerRuleDefinition();

        [SerializeField] private SpawnProfileDefinition _spawnProfileOverride = new SpawnProfileDefinition();

        [Header("Tool Rules")]
        [SerializeField] private ToolRuleDefinition _toolRule = new ToolRuleDefinition();
        #endregion

        #region Rewards
        [Header("Rewards")]
        [SerializeField] private int _rewardCoins = 10;
        #endregion

        #region Properties
        public string LevelId => string.IsNullOrWhiteSpace(_levelId) ? name : _levelId;
        public string WorldId => _worldId;
        public int OrderIndex => _orderIndex;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? LevelId : _displayName;
        public string NumberedDisplayName => _orderIndex > 0 ? $"Level {_orderIndex}" : DisplayName;
        public int BoardWidth => _boardWidth;
        public int BoardHeight => _boardHeight;
        public IReadOnlyList<BoardCellData> BoardCells => _boardCells;
        public ArcadeLevelType LevelType => _levelType;
        public ObjectiveDefinition Objective => _objectiveDefinition;
        public bool UsesScore => _levelType == ArcadeLevelType.Score;
        public bool UsesTimer => _levelType == ArcadeLevelType.Score
                                 || _levelType == ArcadeLevelType.Collectable
                                 || _levelType == ArcadeLevelType.Puzzle;
        public TimerRuleDefinition TimerRule => UsesTimer ? _timerRule : null;
        public SpawnProfileDefinition SpawnProfileOverride => _spawnProfileOverride;
        public ToolRuleDefinition ToolRule => _toolRule ?? (_toolRule = new ToolRuleDefinition());
        public int RewardCoins => Mathf.Max(0, _rewardCoins);
        #endregion

        #region Validation
        private void OnValidate()
        {
            _boardWidth = FixedBoardWidth;
            _boardHeight = FixedBoardHeight;

            if (_boardCells == null)
                _boardCells = new List<BoardCellData>();

            if (_toolRule == null)
                _toolRule = new ToolRuleDefinition();

            if (_objectiveDefinition == null)
                _objectiveDefinition = new ObjectiveDefinition();

            if (_timerRule == null)
                _timerRule = new TimerRuleDefinition();

            if (_spawnProfileOverride == null)
                _spawnProfileOverride = new SpawnProfileDefinition();

            if (_rewardCoins < 0)
                _rewardCoins = 0;
        }
        #endregion
    }
}
