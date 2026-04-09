using System;
using System.Collections.Generic;
using BreakItAll.Gameplay;
using UnityEngine;

namespace BreakItAll.Data
{
    [CreateAssetMenu(
        fileName = "ShapeDefinition",
        menuName = "Break It All/Data/Shape Definition")]
    public sealed class ShapeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [Header("Shape Matrix")]
        public ShapeMatrixData matrix = new ShapeMatrixData();

        [Header("Spawn Metadata")]
        public int difficultyTier;
        public List<string> tags = new List<string>();
        public int spawnWeight = 1;
        public int unlockLevel = 0;

        [Header("Availability")]
        public bool enabledInEndless = true;
        public bool enabledInArcade = true;

        private void OnValidate()
        {
            if (matrix == null)
            {
                matrix = new ShapeMatrixData();
            }

            matrix.EnsureValidSize();
        }

        public string GetResolvedId()
        {
            return string.IsNullOrWhiteSpace(id) ? name : id;
        }

        public ShapeData ToRuntimeShape()
        {
            if (matrix == null)
            {
                throw new InvalidOperationException($"ShapeDefinition '{name}' has no matrix.");
            }

            matrix.EnsureValidSize();

            List<CellCoord> rawCells = new List<CellCoord>();

            for (int y = 0; y < matrix.height; y++)
            {
                for (int x = 0; x < matrix.width; x++)
                {
                    if (matrix.GetCell(x, y))
                    {
                        rawCells.Add(new CellCoord(x, y));
                    }
                }
            }

            if (rawCells.Count == 0)
            {
                throw new InvalidOperationException($"ShapeDefinition '{name}' has no filled cells.");
            }

            NormalizeToOrigin(rawCells);

            string runtimeId = GetResolvedId();
            return new ShapeData(runtimeId, rawCells);
        }

        private void NormalizeToOrigin(List<CellCoord> cells)
        {
            int minX = int.MaxValue;
            int minY = int.MaxValue;

            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].X < minX)
                {
                    minX = cells[i].X;
                }

                if (cells[i].Y < minY)
                {
                    minY = cells[i].Y;
                }
            }

            for (int i = 0; i < cells.Count; i++)
            {
                CellCoord cell = cells[i];
                cells[i] = new CellCoord(cell.X - minX, cell.Y - minY);
            }
        }
    }
}