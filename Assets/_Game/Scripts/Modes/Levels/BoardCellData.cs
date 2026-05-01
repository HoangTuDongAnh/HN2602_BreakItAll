using System;
using _Game.Scripts.Data;
using UnityEngine;

namespace _Game.Scripts.Modes.Levels
{
    /// <summary>
    /// Dữ liệu setup một ô board cho Arcade level.
    /// Board giữ item/marker, block chỉ giữ shape.
    /// </summary>
    [Serializable]
    public class BoardCellData
    {
        #region Fields
        [Min(0)] public int x;
        [Min(0)] public int y;

        [Header("Start Occupancy")]
        public bool occupiedAtStart;
        public BlockCellType occupiedCellType = BlockCellType.Normal;
        public Color occupiedColor = Color.gray;

        [Header("Arcade Markers")]
        public BoardItemType itemType = BoardItemType.None;
        public string itemId;
        public string markerTag;
        public bool targetPatternFilled;
        #endregion
    }
}
