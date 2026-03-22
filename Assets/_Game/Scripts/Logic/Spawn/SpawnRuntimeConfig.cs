using System;
using UnityEngine;

namespace _Game.Scripts.Logic.Spawn
{
    [Serializable]
    public class SpawnRuntimeConfig
    {
        [SerializeField] private float _spawnDelay = 0.5f;
        [SerializeField] private int _batchSize = 3;
        [SerializeField] private float _boomChance = 0f;
        [SerializeField] private float _toolChance = 0f;

        public float SpawnDelay => _spawnDelay;
        public int BatchSize => _batchSize;
        public float BoomChance => _boomChance;
        public float ToolChance => _toolChance;
    }
}