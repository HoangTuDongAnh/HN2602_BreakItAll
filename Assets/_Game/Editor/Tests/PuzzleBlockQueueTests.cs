using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using _Game.Scripts.Data;
using _Game.Scripts.Logic;
using _Game.Scripts.Modes.Levels;

namespace _Game.Editor.Tests
{
    public class PuzzleBlockQueueTests
    {
        private readonly List<Object> _objectsToDestroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _objectsToDestroy.Count; i++)
            {
                if (_objectsToDestroy[i] != null)
                    Object.DestroyImmediate(_objectsToDestroy[i]);
            }

            _objectsToDestroy.Clear();
        }

        [Test]
        public void TryInitialize_BuildsProvidedShapeQueueAndSetCounts()
        {
            PuzzleBlockQueue queue = new PuzzleBlockQueue();
            SmartSpawnStrategy strategy = CreateStrategy("A", "B", "C", "D");
            LevelDefinition level = CreatePuzzleLevel("A", "B", "C", "D");

            bool initialized = queue.TryInitialize(level, strategy, visibleSetSize: 2);

            Assert.IsTrue(initialized);
            Assert.IsTrue(queue.IsActive);
            Assert.AreEqual(4, queue.TotalBlocks);
            Assert.AreEqual(4, queue.RemainingBlocks);
            Assert.AreEqual(0, queue.UsedBlocks);
            Assert.AreEqual(1, queue.CurrentSetNumber);
            Assert.AreEqual(2, queue.TotalSets);
            Assert.IsTrue(queue.CanSwitchSet);
        }

        [Test]
        public void TryGetSpawnRequest_ReturnsRequestForUnusedEntry()
        {
            PuzzleBlockQueue queue = CreateInitializedQueue(2, "A", "B", "C");

            bool success = queue.TryGetSpawnRequest(1, out SpawnRequest request);

            Assert.IsTrue(success);
            Assert.IsNotNull(request.ShapeData);
            Assert.AreEqual("B", request.ShapeData.Id);
            Assert.AreEqual(0, request.RotationIndex);
        }

        [Test]
        public void TryMarkBlockUsed_UpdatesRemainingAndHidesUsedEntryFromSpawnRequests()
        {
            PuzzleBlockQueue queue = CreateInitializedQueue(2, "A", "B", "C");
            BlockController block = CreateBlockController("VisibleBlockA");
            queue.RegisterVisibleBlock(block, 0);

            bool used = queue.TryMarkBlockUsed(block, out int entryIndex);
            bool canSpawnUsedEntry = queue.TryGetSpawnRequest(0, out SpawnRequest ignored);

            Assert.IsTrue(used);
            Assert.AreEqual(0, entryIndex);
            Assert.AreEqual(1, queue.UsedBlocks);
            Assert.AreEqual(2, queue.RemainingBlocks);
            Assert.IsFalse(canSpawnUsedEntry);
        }

        [Test]
        public void TryMarkEntryUnused_ReturnsPlacedBlockBackToQueue()
        {
            PuzzleBlockQueue queue = CreateInitializedQueue(2, "A", "B", "C");
            BlockController block = CreateBlockController("VisibleBlockA");
            queue.RegisterVisibleBlock(block, 0);
            Assert.IsTrue(queue.TryMarkBlockUsed(block, out int entryIndex));

            bool returned = queue.TryMarkEntryUnused(entryIndex);
            bool canSpawnAgain = queue.TryGetSpawnRequest(entryIndex, out SpawnRequest request);

            Assert.IsTrue(returned);
            Assert.IsTrue(canSpawnAgain);
            Assert.AreEqual("A", request.ShapeData.Id);
            Assert.AreEqual(0, queue.UsedBlocks);
            Assert.AreEqual(3, queue.RemainingBlocks);
        }

        [Test]
        public void TryMoveSet_SkipsSetsThatHaveNoUnusedBlocks()
        {
            PuzzleBlockQueue queue = CreateInitializedQueue(2, "A", "B", "C", "D", "E");

            BlockController blockC = CreateBlockController("VisibleBlockC");
            BlockController blockD = CreateBlockController("VisibleBlockD");
            queue.RegisterVisibleBlock(blockC, 2);
            queue.RegisterVisibleBlock(blockD, 3);
            Assert.IsTrue(queue.TryMarkBlockUsed(blockC, out _));
            Assert.IsTrue(queue.TryMarkBlockUsed(blockD, out _));

            bool moved = queue.TryMoveSet(1);

            Assert.IsTrue(moved);
            Assert.AreEqual(3, queue.CurrentSetNumber, "Set 2 has no unused blocks, so Switch should jump to Set 3.");
            queue.GetCurrentSetRange(out int startIndex, out int endIndex);
            Assert.AreEqual(4, startIndex);
            Assert.AreEqual(5, endIndex);
        }

        private PuzzleBlockQueue CreateInitializedQueue(int visibleSetSize, params string[] shapeIds)
        {
            PuzzleBlockQueue queue = new PuzzleBlockQueue();
            SmartSpawnStrategy strategy = CreateStrategy(shapeIds);
            LevelDefinition level = CreatePuzzleLevel(shapeIds);
            Assert.IsTrue(queue.TryInitialize(level, strategy, visibleSetSize));
            return queue;
        }

        private SmartSpawnStrategy CreateStrategy(params string[] shapeIds)
        {
            SmartSpawnStrategy strategy = ScriptableObject.CreateInstance<SmartSpawnStrategy>();
            _objectsToDestroy.Add(strategy);

            List<BlockData> tier1Blocks = new List<BlockData>();
            for (int i = 0; i < shapeIds.Length; i++)
                tier1Blocks.Add(CreateBlockData(shapeIds[i]));

            SetPrivateField(strategy, "_tier1Blocks", tier1Blocks);
            return strategy;
        }

        private BlockData CreateBlockData(string id)
        {
            BlockData block = ScriptableObject.CreateInstance<BlockData>();
            block.name = id;
            block.columns = 1;
            block.rows = 1;
            block.boardData = new List<CellData>
            {
                new CellData { isOccupied = true, blockCellType = BlockCellType.Normal }
            };
            SetPrivateField(block, "_id", id);
            _objectsToDestroy.Add(block);
            return block;
        }

        private LevelDefinition CreatePuzzleLevel(params string[] shapeIds)
        {
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.name = "TestPuzzleLevel";
            _objectsToDestroy.Add(level);

            ObjectiveDefinition objective = new ObjectiveDefinition
            {
                providedShapeIds = new List<string>(shapeIds),
                allowRotation = true,
                requireUseAllProvidedShapes = true
            };

            SetPrivateField(level, "_levelType", ArcadeLevelType.Puzzle);
            SetPrivateField(level, "_objectiveDefinition", objective);
            return level;
        }

        private BlockController CreateBlockController(string name)
        {
            GameObject go = new GameObject(name);
            _objectsToDestroy.Add(go);
            return go.AddComponent<BlockController>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing private field: " + fieldName + " on " + target.GetType().Name);
            field.SetValue(target, value);
        }
    }
}
