using System.Collections.Generic;

namespace BreakItAll.Gameplay
{
    /// <summary>
    /// Kiểm tra xem còn ít nhất một nước đi hợp lệ
    /// với bất kỳ shape nào trong queue hiện tại hay không.
    /// </summary>
    public sealed class MoveAvailabilityChecker
    {
        private readonly PlacementValidator _placementValidator;

        public MoveAvailabilityChecker(PlacementValidator placementValidator)
        {
            _placementValidator = placementValidator;
        }

        public bool HasAnyMove(BoardState boardState, IReadOnlyList<ShapeData> shapes)
        {
            if (boardState == null || shapes == null || shapes.Count == 0)
            {
                return false;
            }

            foreach (ShapeData shape in shapes)
            {
                if (shape == null)
                {
                    continue;
                }

                if (CanPlaceShapeAnywhere(boardState, shape))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanPlaceShapeAnywhere(BoardState boardState, ShapeData shape)
        {
            if (boardState == null || shape == null)
            {
                return false;
            }

            for (int x = 0; x < boardState.Width; x++)
            {
                for (int y = 0; y < boardState.Height; y++)
                {
                    CellCoord anchor = new CellCoord(x, y);
                    PlacementCheckResult result = _placementValidator.Validate(boardState, shape, anchor);

                    if (result.IsValid)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}