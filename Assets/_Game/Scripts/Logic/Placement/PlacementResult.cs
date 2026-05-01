using System.Collections.Generic;

namespace _Game.Scripts.Logic.Placement
{
    /// <summary>
    /// Kết quả validate placement. Luôn giữ đủ danh sách cell để caller có thể debug/preview.
    /// </summary>
    public class PlacementResult
    {
        #region Data
        public GridCoord Anchor;
        public List<GridCoord> OccupiedCoords = new List<GridCoord>();
        public List<GridCoord> ValidCoords = new List<GridCoord>();
        public List<GridCoord> InvalidCoords = new List<GridCoord>();
        public List<GridCoord> BlockedCoords = new List<GridCoord>();
        public List<GridCoord> OutOfBoundsCoords = new List<GridCoord>();
        public bool IsValid = true;
        public string FailureReason;
        #endregion
    }
}
