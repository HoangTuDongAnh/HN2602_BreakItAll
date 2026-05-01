using System;
using _Game.Scripts.Logic;

namespace _Game.Scripts.Logic.Placement
{
    public class BoardState
    {
        private readonly GridCell[,] _cells;
        private readonly Func<GridCell, bool> _blocksPlacement;

        public int Width { get; }
        public int Height { get; }

        public BoardState(GridCell[,] cells, int width, int height, Func<GridCell, bool> blocksPlacement = null)
        {
            _cells = cells;
            _blocksPlacement = blocksPlacement;
            Width = width;
            Height = height;
        }

        public bool IsInside(GridCoord coord)
        {
            return coord.X >= 0 && coord.X < Width && coord.Y >= 0 && coord.Y < Height;
        }

        public bool IsOccupied(GridCoord coord)
        {
            GridCell cell = _cells[coord.X, coord.Y];
            return cell.IsOccupied || (_blocksPlacement != null && _blocksPlacement(cell));
        }
    }
}
