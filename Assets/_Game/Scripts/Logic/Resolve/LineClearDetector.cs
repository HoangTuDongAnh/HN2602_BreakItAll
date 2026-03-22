using UnityEngine;
using _Game.Scripts.Data;

namespace _Game.Scripts.Logic.Resolve
{
    public class LineClearDetector
    {
        public LineClearDetectionResult Detect(GridCell[,] gridData, int width, int height)
        {
            LineClearDetectionResult result = new LineClearDetectionResult();

            for (int y = 0; y < height; y++)
            {
                bool isFull = true;

                for (int x = 0; x < width; x++)
                {
                    if (!gridData[x, y].IsOccupied)
                    {
                        isFull = false;
                        break;
                    }
                }

                if (isFull)
                    result.RowsToClear.Add(y);
            }

            for (int x = 0; x < width; x++)
            {
                bool isFull = true;

                for (int y = 0; y < height; y++)
                {
                    if (!gridData[x, y].IsOccupied)
                    {
                        isFull = false;
                        break;
                    }
                }

                if (isFull)
                    result.ColsToClear.Add(x);
            }

            foreach (int row in result.RowsToClear)
            {
                for (int x = 0; x < width; x++)
                    result.CellsToClear.Add(new Vector2Int(x, row));
            }

            foreach (int col in result.ColsToClear)
            {
                for (int y = 0; y < height; y++)
                    result.CellsToClear.Add(new Vector2Int(col, y));
            }

            return result;
        }
    }
}