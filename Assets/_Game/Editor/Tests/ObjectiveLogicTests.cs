using NUnit.Framework;
using UnityEngine;
using _Game.Scripts.Data;
using _Game.Scripts.Logic.Resolve;
using _Game.Scripts.Modes.Objectives;

namespace _Game.Editor.Tests
{
    public class ObjectiveLogicTests
    {
        [Test]
        public void ScoreObjective_Completes_WhenTargetScoreIsReached()
        {
            ScoreObjective objective = new ScoreObjective(100, failWhenTimeEnds: true);
            objective.Initialize(null);

            objective.NotifyScoreChanged(100);

            Assert.IsTrue(objective.IsCompleted);
            Assert.IsFalse(objective.IsFailed);
            Assert.AreEqual(100, objective.Progress.CurrentValue);
            Assert.AreEqual(100, objective.Progress.TargetValue);
        }

        [Test]
        public void ScoreObjective_Fails_WhenTimerEndsBeforeTargetScore()
        {
            ScoreObjective objective = new ScoreObjective(100, failWhenTimeEnds: true);
            objective.Initialize(null);

            objective.NotifyScoreChanged(40);
            objective.NotifyTimerUpdated(0f, 60f);

            Assert.IsFalse(objective.IsCompleted);
            Assert.IsTrue(objective.IsFailed);
        }

        [Test]
        public void CollectItemsObjective_CollectsOnlyMatchingTargetItem()
        {
            CollectItemsObjective objective = new CollectItemsObjective("gem", 2, failWhenTimeEnds: true);
            objective.Initialize(null);

            BoardResolveResult result = new BoardResolveResult();
            result.CollectedItems.Add(new ResolvedBoardItem
            {
                Coord = new Vector2Int(0, 0),
                ItemType = BoardItemType.Gem,
                ItemId = "gem"
            });
            result.CollectedItems.Add(new ResolvedBoardItem
            {
                Coord = new Vector2Int(1, 0),
                ItemType = BoardItemType.TimeBonus,
                ItemId = "time_bonus"
            });

            objective.NotifyBoardResolved(result);

            Assert.IsFalse(objective.IsCompleted);
            Assert.AreEqual(1, objective.Progress.CurrentValue);
            Assert.AreEqual(2, objective.Progress.TargetValue);
        }

        [Test]
        public void CollectItemsObjective_Completes_WhenEnoughMatchingItemsAreCollected()
        {
            CollectItemsObjective objective = new CollectItemsObjective("gem", 2, failWhenTimeEnds: true);
            objective.Initialize(null);

            BoardResolveResult first = new BoardResolveResult();
            first.CollectedItems.Add(new ResolvedBoardItem { ItemType = BoardItemType.Gem, ItemId = "gem" });
            BoardResolveResult second = new BoardResolveResult();
            second.CollectedItems.Add(new ResolvedBoardItem { ItemType = BoardItemType.Gem, ItemId = "gem" });

            objective.NotifyBoardResolved(first);
            objective.NotifyBoardResolved(second);

            Assert.IsTrue(objective.IsCompleted);
            Assert.IsFalse(objective.IsFailed);
            Assert.AreEqual(2, objective.Progress.CurrentValue);
        }

        [Test]
        public void CollectItemsObjective_Fails_WhenTimerEndsBeforeTargetAmount()
        {
            CollectItemsObjective objective = new CollectItemsObjective("gem", 2, failWhenTimeEnds: true);
            objective.Initialize(null);

            objective.NotifyTimerUpdated(0f, 60f);

            Assert.IsFalse(objective.IsCompleted);
            Assert.IsTrue(objective.IsFailed);
        }
    }
}
