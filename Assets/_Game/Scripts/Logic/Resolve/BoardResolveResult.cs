using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Logic.Resolve
{
    public class BoardResolveResult
    {
        public int TotalLines;
        public List<Vector2Int> ClearedCells = new();
        public Vector3 EffectCenter = Vector3.zero;

        public bool HasAnyClear => TotalLines > 0;
    }
}