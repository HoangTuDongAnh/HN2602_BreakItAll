using System;
using System.Text;

namespace BreakItAll.Gameplay
{
    /// <summary>
    /// Chỉ lưu trạng thái occupied cơ bản cho M1.5.
    /// Chưa chứa item/cell marker/objective data.
    /// </summary>
    public sealed class BoardState
    {
        private readonly bool[,] _occupied;

        public int Width { get; }
        public int Height { get; }

        public event Action StateChanged;

        public BoardState(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            Width = width;
            Height = height;
            _occupied = new bool[width, height];
        }

        public bool IsInside(CellCoord coord)
        {
            return coord.X >= 0 && coord.X < Width && coord.Y >= 0 && coord.Y < Height;
        }

        public bool IsOccupied(CellCoord coord)
        {
            if (!IsInside(coord))
            {
                throw new ArgumentOutOfRangeException(nameof(coord), $"Cell {coord} is outside board.");
            }

            return _occupied[coord.X, coord.Y];
        }

        public void SetOccupied(CellCoord coord, bool occupied)
        {
            if (!IsInside(coord))
            {
                throw new ArgumentOutOfRangeException(nameof(coord), $"Cell {coord} is outside board.");
            }

            _occupied[coord.X, coord.Y] = occupied;
            StateChanged?.Invoke();
        }

        public void ClearAll()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _occupied[x, y] = false;
                }
            }

            StateChanged?.Invoke();
        }

        public BoardState Clone()
        {
            BoardState clone = new BoardState(Width, Height);

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    clone._occupied[x, y] = _occupied[x, y];
                }
            }

            return clone;
        }

        public string Dump()
        {
            StringBuilder sb = new StringBuilder();

            for (int y = Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < Width; x++)
                {
                    sb.Append(_occupied[x, y] ? "1" : "0");
                }

                if (y > 0)
                {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }
}