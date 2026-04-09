using System.Collections.Generic;

namespace BreakItAll.Gameplay
{
    public sealed class ClearResolver
    {
        public ClearResolutionResult Resolve(BoardState boardState)
        {
            if (boardState == null)
            {
                return ClearResolutionResult.Empty();
            }

            List<int> fullRows = FindFullRows(boardState);
            List<int> fullColumns = FindFullColumns(boardState);

            if (fullRows.Count == 0 && fullColumns.Count == 0)
            {
                return ClearResolutionResult.Empty();
            }

            HashSet<CellCoord> cellsToClear = new HashSet<CellCoord>();

            foreach (int row in fullRows)
            {
                for (int x = 0; x < boardState.Width; x++)
                {
                    cellsToClear.Add(new CellCoord(x, row));
                }
            }

            foreach (int column in fullColumns)
            {
                for (int y = 0; y < boardState.Height; y++)
                {
                    cellsToClear.Add(new CellCoord(column, y));
                }
            }

            List<CellCoord> clearedCells = new List<CellCoord>(cellsToClear);

            foreach (CellCoord cell in clearedCells)
            {
                boardState.SetOccupied(cell, false);
            }

            return new ClearResolutionResult(
                fullRows.Count,
                fullColumns.Count,
                clearedCells.Count,
                fullRows,
                fullColumns,
                clearedCells);
        }

        private List<int> FindFullRows(BoardState boardState)
        {
            List<int> rows = new List<int>();

            for (int y = 0; y < boardState.Height; y++)
            {
                bool isFull = true;

                for (int x = 0; x < boardState.Width; x++)
                {
                    if (!boardState.IsOccupied(new CellCoord(x, y)))
                    {
                        isFull = false;
                        break;
                    }
                }

                if (isFull)
                {
                    rows.Add(y);
                }
            }

            return rows;
        }

        private List<int> FindFullColumns(BoardState boardState)
        {
            List<int> columns = new List<int>();

            for (int x = 0; x < boardState.Width; x++)
            {
                bool isFull = true;

                for (int y = 0; y < boardState.Height; y++)
                {
                    if (!boardState.IsOccupied(new CellCoord(x, y)))
                    {
                        isFull = false;
                        break;
                    }
                }

                if (isFull)
                {
                    columns.Add(x);
                }
            }

            return columns;
        }
    }
}