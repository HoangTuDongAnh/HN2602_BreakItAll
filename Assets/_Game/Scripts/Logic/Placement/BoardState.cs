using _Game.Scripts.Logic;

namespace _Game.Scripts.Logic.Placement
{
    public class BoardState
    {
        private readonly GridCell[,] _cells;
        public int Width { get; }
        public int Height { get; }

        public BoardState(GridCell[,] cells, int width, int height)
        {
            _cells = cells;
            Width = width;
            Height = height;
        }

        public bool IsInside(GridCoord coord)
        {
            return coord.X >= 0 && coord.X < Width && coord.Y >= 0 && coord.Y < Height;
        }

        public bool IsOccupied(GridCoord coord)
        {
            return _cells[coord.X, coord.Y].IsOccupied;
        }
    }
}