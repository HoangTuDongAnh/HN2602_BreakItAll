using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using _Game.Scripts.Data;
using _Game.Scripts.Logic;
using _Game.Scripts.Logic.Placement;

namespace _Game.Editor.Tests
{
    public class BlockPlacementServiceTests
    {
        private BlockPlacementService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new BlockPlacementService();
        }

        [Test]
        public void Evaluate_ReturnsValid_WhenShapeIsInsideEmptyBoard()
        {
            BoardState board = CreateBoard(4, 4);
            List<Vector2Int> shape = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(0, 1)
            };

            PlacementResult result = _service.Evaluate(new GridCoord(1, 1), shape, board);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(3, result.OccupiedCoords.Count);
            Assert.AreEqual(3, result.ValidCoords.Count);
            Assert.AreEqual(0, result.InvalidCoords.Count);
        }

        [Test]
        public void Evaluate_ReturnsOutOfBounds_WhenAnyCellIsOutsideBoard()
        {
            BoardState board = CreateBoard(3, 3);
            List<Vector2Int> shape = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0)
            };

            PlacementResult result = _service.Evaluate(new GridCoord(2, 0), shape, board);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("OutOfBounds", result.FailureReason);
            Assert.AreEqual(1, result.OutOfBoundsCoords.Count);
            Assert.AreEqual(new GridCoord(3, 0), result.OutOfBoundsCoords[0]);
        }

        [Test]
        public void Evaluate_ReturnsOccupied_WhenAnyCellOverlapsExistingBlock()
        {
            GridCell[,] cells = CreateCells(3, 3);
            cells[1, 1].SetData(BlockCellType.Normal);
            BoardState board = new BoardState(cells, 3, 3);
            List<Vector2Int> shape = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0)
            };

            PlacementResult result = _service.Evaluate(new GridCoord(1, 1), shape, board);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Occupied", result.FailureReason);
            Assert.AreEqual(1, result.BlockedCoords.Count);
            Assert.AreEqual(new GridCoord(1, 1), result.BlockedCoords[0]);
        }

        [Test]
        public void Evaluate_ReturnsDuplicateCoord_WhenShapeOffsetsContainDuplicates()
        {
            BoardState board = CreateBoard(3, 3);
            List<Vector2Int> shape = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(0, 0)
            };

            PlacementResult result = _service.Evaluate(new GridCoord(1, 1), shape, board);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("DuplicateCoord", result.FailureReason);
            Assert.AreEqual(1, result.InvalidCoords.Count);
        }

        [Test]
        public void CanPlaceAnywhere_ReturnsFalse_WhenBoardIsFull()
        {
            GridCell[,] cells = CreateCells(2, 2);
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                    cells[x, y].SetData(BlockCellType.Normal);
            }

            BoardState board = new BoardState(cells, 2, 2);
            List<Vector2Int> shape = new List<Vector2Int> { new Vector2Int(0, 0) };

            Assert.IsFalse(_service.CanPlaceAnywhere(shape, board));
        }

        private static BoardState CreateBoard(int width, int height)
        {
            return new BoardState(CreateCells(width, height), width, height);
        }

        private static GridCell[,] CreateCells(int width, int height)
        {
            GridCell[,] cells = new GridCell[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                    cells[x, y] = new GridCell(x, y);
            }

            return cells;
        }
    }
}
