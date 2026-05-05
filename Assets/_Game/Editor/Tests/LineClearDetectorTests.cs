using NUnit.Framework;
using UnityEngine;
using _Game.Scripts.Data;
using _Game.Scripts.Logic;
using _Game.Scripts.Logic.Resolve;

namespace _Game.Editor.Tests
{
    public class LineClearDetectorTests
    {
        private LineClearDetector _detector;

        [SetUp]
        public void SetUp()
        {
            _detector = new LineClearDetector();
        }

        [Test]
        public void Detect_ReturnsFullRow()
        {
            GridCell[,] grid = CreateGrid(4, 4);
            FillRow(grid, 4, 1);

            LineClearDetectionResult result = _detector.Detect(grid, 4, 4);

            CollectionAssert.AreEquivalent(new[] { 1 }, result.RowsToClear);
            Assert.AreEqual(0, result.ColsToClear.Count);
            Assert.AreEqual(4, result.CellsToClear.Count);
            Assert.IsTrue(result.CellsToClear.Contains(new Vector2Int(0, 1)));
            Assert.IsTrue(result.CellsToClear.Contains(new Vector2Int(3, 1)));
        }

        [Test]
        public void Detect_ReturnsFullColumn()
        {
            GridCell[,] grid = CreateGrid(4, 4);
            FillColumn(grid, 4, 2);

            LineClearDetectionResult result = _detector.Detect(grid, 4, 4);

            Assert.AreEqual(0, result.RowsToClear.Count);
            CollectionAssert.AreEquivalent(new[] { 2 }, result.ColsToClear);
            Assert.AreEqual(4, result.CellsToClear.Count);
            Assert.IsTrue(result.CellsToClear.Contains(new Vector2Int(2, 0)));
            Assert.IsTrue(result.CellsToClear.Contains(new Vector2Int(2, 3)));
        }

        [Test]
        public void Detect_RowAndColumn_DeduplicatesIntersectionCell()
        {
            GridCell[,] grid = CreateGrid(3, 3);
            FillRow(grid, 3, 1);
            FillColumn(grid, 3, 2);

            LineClearDetectionResult result = _detector.Detect(grid, 3, 3);

            CollectionAssert.AreEquivalent(new[] { 1 }, result.RowsToClear);
            CollectionAssert.AreEquivalent(new[] { 2 }, result.ColsToClear);
            Assert.AreEqual(5, result.CellsToClear.Count, "3 row cells + 3 column cells - 1 shared intersection.");
            Assert.IsTrue(result.CellsToClear.Contains(new Vector2Int(2, 1)));
        }

        [Test]
        public void Detect_ReturnsEmptyResult_WhenNoLineIsFull()
        {
            GridCell[,] grid = CreateGrid(3, 3);
            grid[0, 0].SetData(BlockCellType.Normal);
            grid[1, 1].SetData(BlockCellType.Normal);
            grid[2, 2].SetData(BlockCellType.Normal);

            LineClearDetectionResult result = _detector.Detect(grid, 3, 3);

            Assert.IsFalse(result.HasAny);
            Assert.AreEqual(0, result.RowsToClear.Count);
            Assert.AreEqual(0, result.ColsToClear.Count);
            Assert.AreEqual(0, result.CellsToClear.Count);
        }

        private static GridCell[,] CreateGrid(int width, int height)
        {
            GridCell[,] grid = new GridCell[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                    grid[x, y] = new GridCell(x, y);
            }

            return grid;
        }

        private static void FillRow(GridCell[,] grid, int width, int row)
        {
            for (int x = 0; x < width; x++)
                grid[x, row].SetData(BlockCellType.Normal);
        }

        private static void FillColumn(GridCell[,] grid, int height, int column)
        {
            for (int y = 0; y < height; y++)
                grid[column, y].SetData(BlockCellType.Normal);
        }
    }
}
