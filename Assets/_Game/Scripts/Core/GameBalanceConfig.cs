using UnityEngine;
using _Game.Scripts.Core.Services;

namespace _Game.Scripts.Core
{
    public class GameBalanceConfig : MonoBehaviour
    {
        [Header("Score")]
        [SerializeField] private int _baseLineScore = 100;
        [SerializeField] private int _comboBonusPerStreak = 50;
        [SerializeField] private int _placementScorePerCell = 0;

        [Header("Spawn")]
        [SerializeField] private float _spawnDelay = 0.5f;
        [SerializeField] private int _spawnBatchSize = 3;

        public int BaseLineScore => _baseLineScore;
        public int ComboBonusPerStreak => _comboBonusPerStreak;
        public int PlacementScorePerCell => _placementScorePerCell;

        public float SpawnDelay => _spawnDelay;
        public int SpawnBatchSize => _spawnBatchSize;

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