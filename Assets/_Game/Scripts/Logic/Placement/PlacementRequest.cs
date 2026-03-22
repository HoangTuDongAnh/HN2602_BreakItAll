using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Logic.Placement
{
    public class PlacementRequest
    {
        public GridCoord Anchor;
        public IReadOnlyList<Vector2Int> ShapeOffsets;
    }
}