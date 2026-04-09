using System;
using BreakItAll.Gameplay;

namespace BreakItAll.Data
{
    [Serializable]
    public struct GridPositionData
    {
        public int x;
        public int y;

        public GridPositionData(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public CellCoord ToRuntimeCell()
        {
            return new CellCoord(x, y);
        }
    }
}