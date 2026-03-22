using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Logic.Placement;

namespace _Game.Scripts.Core.Services
{
    public interface IBlockPlacementService
    {
        PlacementPreview BuildPreview(GridCoord anchor, IReadOnlyList<Vector2Int> shapeOffsets, BoardState board);
        PlacementResult Evaluate(GridCoord anchor, IReadOnlyList<Vector2Int> shapeOffsets, BoardState board);
        bool CanPlaceAnywhere(IReadOnlyList<Vector2Int> shapeOffsets, BoardState board);
    }
}