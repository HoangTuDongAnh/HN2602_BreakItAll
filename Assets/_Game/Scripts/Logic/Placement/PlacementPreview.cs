using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Logic.Placement
{
    /// <summary>
    /// Dữ liệu preview khi người chơi kéo block trên board.
    /// Được thiết kế để UI/Grid có thể hiển thị cả trạng thái hợp lệ và không hợp lệ.
    /// </summary>
    public class PlacementPreview
    {
        #region Placement Data
        public GridCoord Anchor;
        public bool IsValid;
        public bool IsNearBoard;
        public string FailureReason;
        public Vector3 WorldSnapPosition;
        #endregion

        #region Cell Groups
        public List<GridCoord> OccupiedCoords = new List<GridCoord>();
        public List<GridCoord> ValidCoords = new List<GridCoord>();
        public List<GridCoord> InvalidCoords = new List<GridCoord>();
        public List<GridCoord> BlockedCoords = new List<GridCoord>();
        public List<GridCoord> OutOfBoundsCoords = new List<GridCoord>();
        #endregion
    }
}
