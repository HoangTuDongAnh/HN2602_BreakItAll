using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Data;

namespace _Game.Scripts.Logic.Resolve
{
    /// <summary>
    /// Dữ liệu sau mỗi lần đặt block và resolve board.
    /// Result này là hook chính để Score/Arcade Objective/VFX đọc mà không phụ thuộc GridManager.
    /// </summary>
    public class BoardResolveResult
    {
        #region Placement
        public List<Vector2Int> PlacedCells = new List<Vector2Int>();
        #endregion

        #region Clear Result
        public int TotalLines;
        public List<int> ClearedRows = new List<int>();
        public List<int> ClearedColumns = new List<int>();
        public List<Vector2Int> ClearedCells = new List<Vector2Int>();
        public Vector3 EffectCenter = Vector3.zero;
        #endregion

        #region Arcade Hooks
        public List<ResolvedBoardItem> CollectedItems = new List<ResolvedBoardItem>();
        public List<Vector2Int> ClearedTargetCells = new List<Vector2Int>();
        #endregion

        #region Properties
        public bool HasAnyClear => TotalLines > 0;
        public bool HasCollectedItems => CollectedItems != null && CollectedItems.Count > 0;
        #endregion
    }

    [System.Serializable]
    public struct ResolvedBoardItem
    {
        public Vector2Int Coord;
        public BoardItemType ItemType;
        public string ItemId;
        public string MarkerTag;
    }
}
