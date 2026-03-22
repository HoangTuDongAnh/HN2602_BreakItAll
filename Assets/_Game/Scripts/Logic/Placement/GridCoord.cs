using System;
using UnityEngine;

namespace _Game.Scripts.Logic.Placement
{
    [Serializable]
    public struct GridCoord : IEquatable<GridCoord>
    {
        public int X;
        public int Y;

        public GridCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector2Int ToVector2Int() => new Vector2Int(X, Y);

        public static GridCoord FromVector2Int(Vector2Int value) => new GridCoord(value.x, value.y);

        public bool Equals(GridCoord other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridCoord other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);

        public static GridCoord operator +(GridCoord a, Vector2Int b) => new GridCoord(a.X + b.x, a.Y + b.y);
        public static GridCoord operator +(GridCoord a, GridCoord b) => new GridCoord(a.X + b.X, a.Y + b.Y);
    }
}