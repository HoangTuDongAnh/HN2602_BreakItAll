using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Game.Scripts.Data
{
    /// <summary>
    /// Dữ liệu khuôn mẫu của một block shape.
    /// ScriptableObject này chỉ mô tả hình dạng và metadata spawn.
    /// Không chứa mechanic kiểu Boom/Tool/Ice; mechanic Arcade nên nằm ở board/level data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBlockShape", menuName = "Game/Block Shape Template")]
    public class BlockData : ScriptableObject
    {
        public const int MaxShapeSize = 5;

        #region Identity & Spawn Metadata
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;

        [Header("Spawn Metadata")]
        [Tooltip("Độ khó của khối này, dùng cho SmartSpawnStrategy.")]
        public BlockTier tier = BlockTier.Tier1_Easy;

        [Min(0f)]
        [Tooltip("Trọng số spawn cơ bản. 0 nghĩa là không spawn tự nhiên, trừ khi SpawnProfile override sau này.")]
        public float spawnWeight = 1f;

        [Tooltip("Tag mềm để SpawnProfile/Arcade lọc hoặc tăng giảm trọng số, ví dụ: small, line, square, rescue.")]
        public List<string> tags = new List<string>();

        [Tooltip("Cho phép shape này xuất hiện trong Endless.")]
        public bool enabledInEndless = true;

        [Tooltip("Cho phép shape này xuất hiện trong Arcade.")]
        public bool enabledInArcade = true;
        #endregion

        #region Shape Dimensions
        [Header("Shape Dimensions")]
        [Tooltip("Số cột của ma trận khối.")]
        public int columns = 5;

        [Tooltip("Số hàng của ma trận khối.")]
        public int rows = 5;

        [Tooltip("Cho phép xoay khối khi sinh ra. V1 có thể tắt nếu muốn đúng thiết kế 'blocks do not rotate'.")]
        public bool allowRotation = true;
        #endregion

        #region Raw Shape Data
        [FormerlySerializedAs("boardData")]
        [HideInInspector]
        public List<CellData> boardData = new List<CellData>();
        #endregion

        #region Properties
        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        #endregion

        #region Helper Methods
        public void ClearData()
        {
            boardData.Clear();
            int total = Mathf.Max(0, columns * rows);
            for (int i = 0; i < total; i++)
                boardData.Add(new CellData());
        }

        public void EnsureDataSize()
        {
            int expectedCount = Mathf.Max(0, columns * rows);

            while (boardData.Count < expectedCount)
                boardData.Add(new CellData());

            if (boardData.Count > expectedCount)
                boardData.RemoveRange(expectedCount, boardData.Count - expectedCount);

            NormalizeCellTypes();
        }

        public CellData GetCell(int x, int y)
        {
            if (x < 0 || x >= columns || y < 0 || y >= rows)
                return null;

            EnsureDataSize();
            int index = y * columns + x;
            return index >= 0 && index < boardData.Count ? boardData[index] : null;
        }

        public List<Vector2Int> GetOccupiedCoordinates()
        {
            EnsureDataSize();

            List<Vector2Int> occupied = new List<Vector2Int>();
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    CellData cell = GetCell(x, y);
                    if (cell != null && cell.isOccupied)
                        occupied.Add(new Vector2Int(x, y));
                }
            }

            return occupied;
        }
        #endregion

        #region Validation
        private void OnValidate()
        {
            columns = Mathf.Clamp(columns, 1, MaxShapeSize);
            rows = Mathf.Clamp(rows, 1, MaxShapeSize);
            spawnWeight = Mathf.Max(0f, spawnWeight);
            EnsureDataSize();
        }
        #endregion

        #region Internal Sanitize
        private void NormalizeCellTypes()
        {
            if (boardData == null) return;

            for (int i = 0; i < boardData.Count; i++)
            {
                CellData cell = boardData[i];
                if (cell == null)
                {
                    boardData[i] = new CellData();
                    continue;
                }

                cell.blockCellType = BlockCellType.Normal;
            }
        }
        #endregion
    }

    [System.Serializable]
    public class CellData
    {
        #region Data
        [Tooltip("Ô này có gạch hay là ô rỗng?")]
        public bool isOccupied;

        [FormerlySerializedAs("cellType")]
        [Tooltip("Loại cell nằm trong block. V1 nên giữ Normal; TimeBonus/Gem chỉ là điểm mở rộng sau.")]
        public BlockCellType blockCellType = BlockCellType.Normal;
        #endregion

        #region Public API
        public CellData Clone()
        {
            return new CellData
            {
                isOccupied = isOccupied,
                blockCellType = blockCellType
            };
        }
        #endregion
    }
}
