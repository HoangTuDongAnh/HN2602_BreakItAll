using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Logic.Placement
{
    /// <summary>
    /// Service thuần logic cho validate/preview placement.
    /// Không phụ thuộc GridManager, Mode hoặc UI.
    /// </summary>
    public class BlockPlacementService
    {
        #region Preview API
        public PlacementPreview BuildPreview(GridCoord anchor, IReadOnlyList<Vector2Int> shapeOffsets, BoardState board)
        {
            PlacementResult result = Evaluate(anchor, shapeOffsets, board);

            return new PlacementPreview
            {
                Anchor = result.Anchor,
                IsValid = result.IsValid,
                FailureReason = result.FailureReason,
                IsNearBoard = IsNearBoard(result, board),
                OccupiedCoords = new List<GridCoord>(result.OccupiedCoords),
                ValidCoords = new List<GridCoord>(result.ValidCoords),
                InvalidCoords = new List<GridCoord>(result.InvalidCoords),
                BlockedCoords = new List<GridCoord>(result.BlockedCoords),
                OutOfBoundsCoords = new List<GridCoord>(result.OutOfBoundsCoords)
            };
        }
        #endregion

        #region Validate API
        public PlacementResult Evaluate(GridCoord anchor, IReadOnlyList<Vector2Int> shapeOffsets, BoardState board)
        {
            PlacementResult result = new PlacementResult
            {
                Anchor = anchor,
                IsValid = true
            };

            if (shapeOffsets == null || shapeOffsets.Count == 0 || board == null)
            {
                result.IsValid = false;
                result.FailureReason = "InvalidInput";
                return result;
            }

            HashSet<GridCoord> unique = new HashSet<GridCoord>();

            foreach (Vector2Int offset in shapeOffsets)
            {
                GridCoord coord = anchor + offset;
                result.OccupiedCoords.Add(coord);

                if (!unique.Add(coord))
                {
                    result.IsValid = false;
                    result.FailureReason = "DuplicateCoord";
                    result.InvalidCoords.Add(coord);
                    continue;
                }

                if (!board.IsInside(coord))
                {
                    result.IsValid = false;
                    result.FailureReason = string.IsNullOrEmpty(result.FailureReason) ? "OutOfBounds" : result.FailureReason;
                    result.OutOfBoundsCoords.Add(coord);
                    result.InvalidCoords.Add(coord);
                    continue;
                }

                if (board.IsOccupied(coord))
                {
                    result.IsValid = false;
                    result.FailureReason = string.IsNullOrEmpty(result.FailureReason) ? "Occupied" : result.FailureReason;
                    result.BlockedCoords.Add(coord);
                    result.InvalidCoords.Add(coord);
                    continue;
                }

                result.ValidCoords.Add(coord);
            }

            return result;
        }

        public bool CanPlaceAnywhere(IReadOnlyList<Vector2Int> shapeOffsets, BoardState board)
        {
            if (board == null || shapeOffsets == null || shapeOffsets.Count == 0)
                return false;

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (Evaluate(new GridCoord(x, y), shapeOffsets, board).IsValid)
                        return true;
                }
            }

            return false;
        }
        #endregion

        #region Helpers
        private bool IsNearBoard(PlacementResult result, BoardState board)
        {
            if (result == null || board == null) return false;

            foreach (GridCoord coord in result.OccupiedCoords)
            {
                if (coord.X >= -1 && coord.X <= board.Width && coord.Y >= -1 && coord.Y <= board.Height)
                    return true;
            }

            return false;
        }
        #endregion
    }
}
