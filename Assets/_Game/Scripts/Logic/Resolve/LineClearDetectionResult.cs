using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Logic.Resolve
{
    public class LineClearDetectionResult
    {
        public List<int> RowsToClear { get; } = new();
        public List<int> ColsToClear { get; } = new();
        public HashSet<Vector2Int> CellsToClear { get; } = new();

        public int TotalLines => RowsToClear.Count + ColsToClear.Count;
        public bool HasAny => TotalLines > 0;
    }
}