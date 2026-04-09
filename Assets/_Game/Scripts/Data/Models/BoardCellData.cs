using System;

namespace BreakItAll.Data
{
    [Serializable]
    public sealed class BoardCellData
    {
        public int x;
        public int y;
        public bool occupiedAtStart;
        public string itemId;
        public bool targetPatternFilled;
        public string markerTag;
    }
}