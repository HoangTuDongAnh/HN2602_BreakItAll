using System;

namespace BreakItAll.Gameplay
{
    [Serializable]
    public readonly struct CellCoord : IEquatable<CellCoord>
    {
        public int X { get; }
        public int Y { get; }

        public CellCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public CellCoord Offset(CellCoord other)
        {
            return new CellCoord(X + other.X, Y + other.Y);
        }

        public bool Equals(CellCoord other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is CellCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
        {
            return $"({X},{Y})";
        }
    }
}