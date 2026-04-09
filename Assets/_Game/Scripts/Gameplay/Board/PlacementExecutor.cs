using System.Collections.Generic;

namespace BreakItAll.Gameplay
{
    public sealed class PlacementExecutor
    {
        private readonly PlacementValidator _placementValidator;

        public PlacementExecutor(PlacementValidator placementValidator)
        {
            _placementValidator = placementValidator;
        }

        public PlacementExecutionResult Execute(BoardState boardState, ShapeData shapeData, CellCoord anchor)
        {
            PlacementCheckResult check = _placementValidator.Validate(boardState, shapeData, anchor);

            if (!check.IsValid)
            {
                return new PlacementExecutionResult(false, new List<CellCoord>());
            }

            List<CellCoord> placedCells = new List<CellCoord>();

            foreach (CellCoord localCell in shapeData.Cells)
            {
                CellCoord targetCell = anchor.Offset(localCell);
                boardState.SetOccupied(targetCell, true);
                placedCells.Add(targetCell);
            }

            return new PlacementExecutionResult(true, placedCells);
        }
    }
}