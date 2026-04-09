using System;
using System.Collections.Generic;
using UnityEngine;

namespace BreakItAll.Data
{
    [Serializable]
    public sealed class ShapeMatrixData
    {
        [Min(1)] public int width = 5;
        [Min(1)] public int height = 5;

        [SerializeField] private List<bool> cells = new List<bool>();

        public IReadOnlyList<bool> Cells => cells;

        public void EnsureValidSize()
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);

            int requiredCount = width * height;

            if (cells == null)
            {
                cells = new List<bool>(requiredCount);
            }

            if (cells.Count < requiredCount)
            {
                while (cells.Count < requiredCount)
                {
                    cells.Add(false);
                }
            }
            else if (cells.Count > requiredCount)
            {
                cells.RemoveRange(requiredCount, cells.Count - requiredCount);
            }
        }

        public bool GetCell(int x, int y)
        {
            EnsureValidSize();

            if (!IsInside(x, y))
            {
                return false;
            }

            return cells[ToIndex(x, y)];
        }

        public void SetCell(int x, int y, bool value)
        {
            EnsureValidSize();

            if (!IsInside(x, y))
            {
                return;
            }

            cells[ToIndex(x, y)] = value;
        }

        public void ToggleCell(int x, int y)
        {
            EnsureValidSize();

            if (!IsInside(x, y))
            {
                return;
            }

            int index = ToIndex(x, y);
            cells[index] = !cells[index];
        }

        public void Clear()
        {
            EnsureValidSize();

            for (int i = 0; i < cells.Count; i++)
            {
                cells[i] = false;
            }
        }

        public bool HasAnyFilledCell()
        {
            EnsureValidSize();

            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i])
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsInside(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        private int ToIndex(int x, int y)
        {
            return y * width + x;
        }
    }
}