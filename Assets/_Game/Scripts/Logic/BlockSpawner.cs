using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Core;
using UnityEngine;
using _Game.Scripts.Data;

namespace _Game.Scripts.Logic
{
    public class BlockSpawner : MonoBehaviour
    {
        #region Settings & References
        [Header("References")]
        [Tooltip("Prefab gốc của khối gạch (Container)")]
        [SerializeField] private GameObject _blockPrefab; 
        
        [Tooltip("Prefab ô vuông nhỏ (Visual Cell)")]
        [SerializeField] private GameObject _cellPrefab; 
        
        [Tooltip("Danh sách 3 vị trí sinh khối trên UI")]
        [SerializeField] private Transform[] _spawnSlots; 

        [Header("Strategy Configuration")]
        [Tooltip("ScriptableObject chứa thuật toán sinh khối thông minh")]
        [SerializeField] private SmartSpawnStrategy _spawnStrategy; 
        
        [Header("Visual Config")]
        [Tooltip("Bảng màu ngẫu nhiên cho các khối")]
        [SerializeField] private List<Color> _blockPalette; 
        #endregion
        
        #region Singleton & State
        public static BlockSpawner Instance { get; private set; }

        // Danh sách các khối đang hiện hữu trên tay người chơi
        private List<BlockController> _activeBlocks = new List<BlockController>();
        public bool IsGameOver { get; private set; } = false;

        // Cờ đánh dấu đang trong quá trình sinh khối (tránh gọi trùng lặp)
        private bool _isSpawning = false;
        #endregion

        #region Unity Lifecycle
        private void Awake() 
        { 
            if (Instance == null) Instance = this; 
        }

        // Đăng ký sự kiện: Chỉ xử lý tiếp theo sau khi bàn cờ đã hoàn tất việc ăn hàng
        private void OnEnable() => GameEvents.OnMoveCompleted += HandleMoveCompleted;
        private void OnDisable() => GameEvents.OnMoveCompleted -= HandleMoveCompleted;
        #endregion

        #region Public API
        // Reset game về trạng thái ban đầu
        public void ResetSpawner()
        {
            IsGameOver = false;
            _isSpawning = false;
            
            // Xóa sạch các khối cũ
            foreach (var block in _activeBlocks)
            {
                if (block != null) Destroy(block.gameObject);
            }
            _activeBlocks.Clear();

            SpawnBatch();
        }

        // Kiểm tra an toàn: Gọi từ GameManager khi Resume game
        public void CheckAndSpawnIfNeeded()
        {
            if (IsGameOver) return;
            if (_isSpawning) return;

            // Loại bỏ các reference null nếu có
            _activeBlocks.RemoveAll(b => b == null);

            if (_activeBlocks.Count == 0)
            {
                SpawnBatch();
            }
        }
        #endregion

        #region Spawning Logic
        // Sinh một đợt khối mới (thường là 3 khối)
        public void SpawnBatch()
        {
            _isSpawning = true;
            _activeBlocks.Clear();
            
            if (_spawnStrategy == null) 
            {
                _isSpawning = false;
                Debug.LogError("Missing Spawn Strategy!"); 
                return; 
            }

            // 1. Lấy danh sách khối cần sinh từ thuật toán
            float currentFillRate = GridManager.Instance.GetFillRate();
            List<SpawnRequest> batchRequests = _spawnStrategy.GetNextBatch(_spawnSlots.Length, currentFillRate);

            // 2. Cơ chế cứu hộ: Nếu toàn khối khó, thay khối cuối bằng khối dễ
            if (!IsSafeBatch(batchRequests))
            {
                BlockData rescueBlock = _spawnStrategy.GetGuaranteedBlock();
                if(rescueBlock != null)
                {
                    batchRequests[batchRequests.Count - 1] = new SpawnRequest { ShapeData = rescueBlock, RotationIndex = 0 };
                }
            }

            // 3. Thực hiện sinh khối tại các slot
            for (int i = 0; i < _spawnSlots.Length; i++)
            {
                if (i >= batchRequests.Count) break;
                SpawnSingleBlock(i, batchRequests[i]);
            }
            
            _isSpawning = false;
            
            // Kiểm tra ngay lập tức xem vừa sinh ra đã chết chưa
            CheckGameOverCondition(); 
        }

        private bool IsSafeBatch(List<SpawnRequest> requests)
        {
            foreach (var req in requests)
            {
                if (req.ShapeData != null && req.ShapeData.tier == BlockTier.Tier1_Easy) return true;
            }
            return false;
        }
        
        private void SpawnSingleBlock(int slotIndex, SpawnRequest request)
        {
            if (request.ShapeData == null) return;

            // A. Tạo dữ liệu logic (Grid, Mechanic)
            var result = BlockFactory.CreateBlockInstance(request.ShapeData, request.RotationIndex, 0f, 0f);
            
            // B. Instantiate Prefab
            Transform slotTrans = _spawnSlots[slotIndex];
            GameObject blockObj = Instantiate(_blockPrefab, slotTrans.position, Quaternion.identity, slotTrans);
            BlockController ctrl = blockObj.GetComponent<BlockController>();
            
            // C. Chọn màu ngẫu nhiên
            Color randomColor = Color.white;
            if (_blockPalette != null && _blockPalette.Count > 0)
            {
                randomColor = _blockPalette[Random.Range(0, _blockPalette.Count)];
            }

            // D. Khởi tạo Controller
            ctrl.Initialize(result.cells, result.width, _cellPrefab, randomColor);
            ctrl.OnPlaced += OnBlockPlaced;

            _activeBlocks.Add(ctrl);
        }
        #endregion

        #region Game Flow Logic
        
        // Callback khi một khối được đặt thành công
        private void OnBlockPlaced(BlockController placedBlock)
        {
            // Chỉ xóa khỏi danh sách quản lý.
            // KHÔNG gọi spawn hay check game over ở đây để tránh xung đột với GridManager.
            _activeBlocks.Remove(placedBlock);
        }

        // Sự kiện nhận từ GridManager SAU KHI đã xử lý xong bàn cờ (ăn hàng, xóa hiệu ứng...)
        private void HandleMoveCompleted(int linesCleared, Vector3 pos)
        {
            if (IsGameOver) return;

            // Nếu hết gạch -> Sinh tiếp
            if (_activeBlocks.Count == 0) 
            {
                _isSpawning = true;
                StartCoroutine(SpawnNextBatchRoutine());
            }
            // Nếu còn gạch -> Kiểm tra xem gạch còn lại có đặt được không
            else 
            {
                CheckGameOverCondition();
            }
        }

        private IEnumerator SpawnNextBatchRoutine()
        {
            yield return new WaitForSeconds(0.5f); // Delay nhỏ tạo cảm giác mượt mà
            SpawnBatch();
        }

        private void CheckGameOverCondition()
        {
            if (_activeBlocks.Count == 0) return;

            bool canMakeMove = false;
            // Duyệt qua tất cả các khối còn lại trên tay
            foreach (var block in _activeBlocks)
            {
                // Hỏi GridManager xem khối này có đặt được vào đâu không
                if (GridManager.Instance.CanPlaceBlockAnywhere(block.GetShapeOffsets()))
                {
                    canMakeMove = true;
                    break;
                }
            }

            if (!canMakeMove) TriggerGameOver();
        }

        private void TriggerGameOver()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            GameManager.Instance.TriggerGameOver();
        }
        #endregion
    }
}