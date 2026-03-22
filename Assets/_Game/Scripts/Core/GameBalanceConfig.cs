using UnityEngine;
using _Game.Scripts.Core.Services;

namespace _Game.Scripts.Core
{
    public class GameBalanceConfig : MonoBehaviour, IGameBalanceService
    {
        [Header("Score")]
        [SerializeField] private int _baseLineScore = 100;
        [SerializeField] private int _comboBonusPerStreak = 50;
        [SerializeField] private int _placementScorePerCell = 0;

        [Header("Spawn")]
        [SerializeField] private float _spawnDelay = 0.5f;
        [SerializeField] private int _spawnBatchSize = 3;
        [SerializeField] private float _boomChance = 0f;
        [SerializeField] private float _toolChance = 0f;

        public int BaseLineScore => _baseLineScore;
        public int ComboBonusPerStreak => _comboBonusPerStreak;
        public int PlacementScorePerCell => _placementScorePerCell;

        public float SpawnDelay => _spawnDelay;
        public int SpawnBatchSize => _spawnBatchSize;
        public float BoomChance => _boomChance;
        public float ToolChance => _toolChance;

        private void Awake()
        {
            GameServices.RegisterBalance(this);
        }

        private void OnDestroy()
        {
            if (GameServices.Balance == this)
                GameServices.RegisterBalance(null);
        }
    }
}