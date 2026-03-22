using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Core.Services;

namespace _Game.Scripts.Logic.Placement
{
    public class BlockPlacementService : IBlockPlacementService
    {
        public PlacementPreview BuildPreview(GridCoord anchor, IReadOnlyList<Vector2Int> shapeOffsets, BoardState board)
        {
            var result = Evaluate(anchor, shapeOffsets, board);

            return new PlacementPreview
            {
                Anchor = result.Anchor,
                OccupiedCoords = result.OccupiedCoords,
                IsValid = result.IsValid
            };
        }

        public PlacementResult Evaluate(GridCoord anchor, IReadOnlyList<Vector2Int> shapeOffsets, BoardState board)
        {
            var result = new PlacementResult
            {
                Anchor = anchor,
                IsValid = true
            };

            var unique = new HashSet<GridCoord>();

            foreach (var offset in shapeOffsets)
            {
                var coord = anchor + offset;

                if (!board.IsInside(coord))
                {
                    result.IsValid = false;
                    result.FailureReason = "OutOfBounds";
                    return result;
                }

                if (board.IsOccupied(coord))
                {
                    result.IsValid = false;
                    result.FailureReason = "Occupied";
                    return result;
                }

                if (!unique.Add(coord))
                {
                    result.IsValid = false;
                    result.FailureReason = "DuplicateCoord";
                    return result;
                }

                result.OccupiedCoords.Add(coord);
            }

            return result;
        }

        public bool CanPlaceAnywhere(IReadOnlyList<Vector2Int> shapeOffsets, BoardState board)
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var anchor = new GridCoord(x, y);
                    if (Evaluate(anchor, shapeOffsets, board).IsValid)
                        return true;
                }
            }

            return false;
        }
    }
}