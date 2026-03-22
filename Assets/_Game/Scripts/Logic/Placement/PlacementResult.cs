using System.Collections.Generic;

namespace _Game.Scripts.Logic.Placement
{
    public class PlacementResult
    {
        public GridCoord Anchor;
        public List<GridCoord> OccupiedCoords = new();
        public bool IsValid;
        public string FailureReason;
    }
}