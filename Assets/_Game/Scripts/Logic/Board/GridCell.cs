using _Game.Scripts.Data;

namespace _Game.Scripts.Logic
{
    /// <summary>
    /// Dữ liệu logic của một ô trên board.
    /// Block chỉ chiếm ô; item/objective marker của Arcade nằm trên board cell.
    /// </summary>
    [System.Serializable]
    public class GridCell
    {
        #region Properties
        public int x { get; private set; }
        public int y { get; private set; }

        public bool IsOccupied { get; private set; }
        public BlockCellType Type { get; private set; }

        public BoardItemType ItemType { get; private set; }
        public string ItemId { get; private set; }
        public string MarkerTag { get; private set; }
        public bool IsTargetPatternCell { get; private set; }
        #endregion

        #region Constructor
        public GridCell(int x, int y)
        {
            this.x = x;
            this.y = y;
            ClearAll();
        }
        #endregion

        #region Occupancy API
        public void SetData(BlockCellType type)
        {
            IsOccupied = true;
            Type = type == BlockCellType.Empty ? BlockCellType.Normal : type;
        }

        public void Clear()
        {
            IsOccupied = false;
            Type = BlockCellType.Empty;
        }
        #endregion

        #region Arcade Board Marker API
        public void SetBoardItem(BoardItemType itemType, string itemId = null, string markerTag = null)
        {
            ItemType = itemType;
            ItemId = itemId;
            MarkerTag = markerTag;
        }

        public void ClearBoardItem()
        {
            ItemType = BoardItemType.None;
            ItemId = null;
            MarkerTag = null;
        }

        public void SetTargetPatternCell(bool isTarget)
        {
            IsTargetPatternCell = isTarget;
        }

        public void ClearAll()
        {
            Clear();
            ClearBoardItem();
            IsTargetPatternCell = false;
        }
        #endregion
    }
}
