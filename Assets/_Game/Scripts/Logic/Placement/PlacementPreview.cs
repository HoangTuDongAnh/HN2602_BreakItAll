using System.Collections.Generic;

namespace _Game.Scripts.Logic.Placement
{
    public class PlacementPreview
    {
        public GridCoord Anchor;
        public List<GridCoord> OccupiedCoords = new();
        public bool IsValid;
    }
}