using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Data;

namespace _Game.Scripts.Logic
{
    // Struct lưu yêu cầu sinh khối (Hình dáng + Góc xoay)
    [System.Serializable]
    public struct SpawnRequest
    {
        public BlockData ShapeData;
        public int RotationIndex; // 0, 1, 2, 3
    }

    [CreateAssetMenu(fileName = "SmartSpawnStrategy", menuName = "Game/Spawn Strategy")]
    public class SmartSpawnStrategy : ScriptableObject
    {
        #region Database
        [Header("Block Database")]
        [Tooltip("Danh sách các khối Tier 1 (Dễ/Nhỏ)")]
        [SerializeField] private List<BlockData> _tier1Blocks;
        
        [Tooltip("Danh sách các khối Tier 2 (Trung bình)")]
        [SerializeField] private List<BlockData> _tier2Blocks;
        
        [Tooltip("Danh sách các khối Tier 3 (Khó/Lớn)")]
        [SerializeField] private List<BlockData> _tier3Blocks;
        #endregion

        #region Configuration
        [Header("Difficulty Config")]
        [Tooltip("Ngưỡng 'Hoảng loạn': Nếu bàn cờ đầy hơn mức này (0.0-1.0), tăng tỷ lệ khối Dễ")]
        [Range(0,1)] [SerializeField] private float _panicThreshold = 0.7f; 
        
        [Tooltip("Ngưỡng 'An toàn': Nếu bàn cờ trống hơn mức này, tăng tỷ lệ khối Khó")]
        [Range(0,1)] [SerializeField] private float _safeThreshold = 0.3f;
        #endregion

        #region Public Methods
        /// <summary>
        /// Trả về danh sách các khối cần sinh ra cho đợt tiếp theo.
        /// </summary>
        public List<SpawnRequest> GetNextBatch(int count, float currentFillRate)
        {
            List<SpawnRequest> result = new List<SpawnRequest>();
            
            // Dictionary để tránh trùng lặp góc xoay quá nhiều cho cùng 1 hình
            Dictionary<BlockData, List<int>> batchHistory = new Dictionary<BlockData, List<int>>();

            for (int i = 0; i < count; i++)
            {
                // 1. Quyết định Tier và Chọn Hình
                BlockTier tier = DecideTier(currentFillRate);
                BlockData shape = GetRandomBlockOfTier(tier);

                // 2. Chọn góc xoay thông minh (Tránh trùng góc)
                int chosenRotation = 0;

                if (shape.allowRotation)
                {
                    if (!batchHistory.ContainsKey(shape))
                    {
                        // Hình mới -> Random góc
                        chosenRotation = Random.Range(0, 4);
                        batchHistory[shape] = new List<int> { chosenRotation };
                    }
                    else
                    {
                        // Hình cũ -> Tìm góc chưa dùng
                        List<int> usedRots = batchHistory[shape];
                        List<int> availableRots = new List<int>();
                        
                        for (int r = 0; r < 4; r++)
                        {
                            if (!usedRots.Contains(r)) availableRots.Add(r);
                        }

                        if (availableRots.Count > 0)
                        {
                            chosenRotation = availableRots[Random.Range(0, availableRots.Count)];
                            usedRots.Add(chosenRotation);
                        }
                        else
                        {
                            chosenRotation = Random.Range(0, 4); // Hết góc -> Random lại
                        }
                    }
                }

                result.Add(new SpawnRequest { ShapeData = shape, RotationIndex = chosenRotation });
            }
            
            return result;
        }

        /// <summary>
        /// Trả về một khối "Cứu sinh" (Thường là 1x1) để đảm bảo người chơi không bị kẹt chết tức tưởi.
        /// </summary>
        public BlockData GetGuaranteedBlock()
        {
            return (_tier1Blocks.Count > 0) ? _tier1Blocks[0] : null;
        }
        #endregion

        #region Internal Logic
        private BlockTier DecideTier(float fillRate)
        {
            float roll = Random.value;
            
            // Panic Mode (>70% đầy): Ưu tiên Dễ để người chơi dọn dẹp
            if (fillRate > _panicThreshold)
            {
                if (roll < 0.6f) return BlockTier.Tier1_Easy;
                if (roll < 0.9f) return BlockTier.Tier2_Medium;
                return BlockTier.Tier3_Hard;
            }
            // Safe Mode (<30% đầy): Ưu tiên Khó để thử thách
            else if (fillRate < _safeThreshold)
            {
                if (roll < 0.1f) return BlockTier.Tier1_Easy;
                if (roll < 0.5f) return BlockTier.Tier2_Medium;
                return BlockTier.Tier3_Hard;
            }
            // Normal Mode
            else
            {
                if (roll < 0.3f) return BlockTier.Tier1_Easy;
                if (roll < 0.8f) return BlockTier.Tier2_Medium;
                return BlockTier.Tier3_Hard;
            }
        }

        private BlockData GetRandomBlockOfTier(BlockTier tier)
        {
            List<BlockData> sourceList = null;
            switch (tier)
            {
                case BlockTier.Tier1_Easy: sourceList = _tier1Blocks; break;
                case BlockTier.Tier2_Medium: sourceList = _tier2Blocks; break;
                case BlockTier.Tier3_Hard: sourceList = _tier3Blocks; break;
            }

            // Fallback an toàn (nếu list rỗng thì lấy Tier 2)
            if (sourceList == null || sourceList.Count == 0) sourceList = _tier2Blocks;
            if (sourceList == null || sourceList.Count == 0) return null;

            return sourceList[Random.Range(0, sourceList.Count)];
        }
        #endregion
    }
}