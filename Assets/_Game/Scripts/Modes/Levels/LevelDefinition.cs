using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Modes.Levels
{
    [CreateAssetMenu(fileName = "Level_Arcade_001", menuName = "_Game/Arcade/Level Definition")]
    public class LevelDefinition : ScriptableObject
    {
        #region Identity
        [Header("Identity")]
        [SerializeField] private string _levelId = "level_001";
        [SerializeField] private string _worldId = "world_01";
        [SerializeField] private int _orderIndex = 1;
        [SerializeField] private string _displayName = "Level 1";
        #endregion

        #region Board
        [Header("Board")]
        [SerializeField] private int _boardWidth = 9;
        [SerializeField] private int _boardHeight = 9;
        [SerializeField] private List<BoardCellData> _boardCells = new List<BoardCellData>();
        #endregion

        #region Objective
        [Header("Objective")]
        [SerializeField] private ArcadeLevelType _levelType = ArcadeLevelType.Collectable;
        [SerializeField] private ObjectiveDefinition _objectiveDefinition = new ObjectiveDefinition();

        [Tooltip("Chỉ dùng khi Level Type = Time. Collect/Shape không có giới hạn thời gian.")]
        [SerializeField] private TimerRuleDefinition _timerRule = new TimerRuleDefinition();

        [SerializeField] private SpawnProfileDefinition _spawnProfileOverride = new SpawnProfileDefinition();
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
        public int BoardWidth => Mathf.Max(1, _boardWidth);
        public int BoardHeight => Mathf.Max(1, _boardHeight);
        public IReadOnlyList<BoardCellData> BoardCells => _boardCells;
        public ArcadeLevelType LevelType => _levelType;
        public ObjectiveDefinition Objective => _objectiveDefinition;
        public TimerRuleDefinition TimerRule => _levelType == ArcadeLevelType.Timed ? _timerRule : null;
        public SpawnProfileDefinition SpawnProfileOverride => _spawnProfileOverride;
        public int RewardCoins => Mathf.Max(0, _rewardCoins);
        #endregion
    }
}
