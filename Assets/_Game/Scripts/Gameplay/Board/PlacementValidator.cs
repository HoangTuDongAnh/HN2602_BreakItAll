namespace BreakItAll.Gameplay
{
    public sealed class PlacementValidator
    {
        public PlacementCheckResult Validate(BoardState boardState, ShapeData shapeData, CellCoord anchor)
        {
            if (boardState == null)
            {
                return PlacementCheckResult.Invalid("BoardState is null.");
            }

            if (shapeData == null)
            {
                return PlacementCheckResult.Invalid("ShapeData is null.");
            }

            foreach (CellCoord localCell in shapeData.Cells)
            {
                CellCoord targetCell = anchor.Offset(localCell);

                if (!boardState.IsInside(targetCell))
                {
                    return PlacementCheckResult.Invalid($"Cell {targetCell} is outside board.");
                }

                if (boardState.IsOccupied(targetCell))
                {
                    return PlacementCheckResult.Invalid($"Cell {targetCell} is already occupied.");
                }
            }

            return PlacementCheckResult.Valid();
        }
    }
}