using _Game.Scripts.Data;
using _Game.Scripts.Logic.Resolve;
using _Game.Scripts.Modes;
using UnityEngine;

namespace _Game.Scripts.Modes.Objectives
{
    /// <summary>
    /// Collectable level: collect target board items before time runs out.
    /// This type does not use score.
    /// </summary>
    public sealed class CollectItemsObjective : IGameObjective
    {
        #region Fields
        private readonly string _targetItemId;
        private readonly int _targetAmount;
        private readonly bool _failWhenTimeEnds;
        private int _currentAmount;
        private float _remainingTime;
        private float _totalTime;
        #endregion

        #region State
        public ObjectiveProgress Progress => new ObjectiveProgress($"{_currentAmount}/{_targetAmount}", _currentAmount, _targetAmount, IsCompleted, IsFailed);
        public bool IsCompleted => _targetAmount > 0 && _currentAmount >= _targetAmount;
        public bool IsFailed => _failWhenTimeEnds && !IsCompleted && _totalTime > 0f && _remainingTime <= 0f;
        #endregion

        #region Constructor
        public CollectItemsObjective(string targetItemId, int targetAmount, bool failWhenTimeEnds)
        {
            _targetItemId = string.IsNullOrWhiteSpace(targetItemId) ? "gem" : targetItemId.Trim();
            _targetAmount = Mathf.Max(1, targetAmount);
            _failWhenTimeEnds = failWhenTimeEnds;
        }
        #endregion

        #region Lifecycle
        public void Initialize(ModeRuntimeContext context)
        {
            _currentAmount = 0;
            _remainingTime = 0f;
            _totalTime = 0f;
        }

        public void NotifyBoardResolved(BoardResolveResult result)
        {
            if (result == null || !result.HasCollectedItems || IsCompleted || IsFailed) return;

            for (int i = 0; i < result.CollectedItems.Count; i++)
            {
                ResolvedBoardItem item = result.CollectedItems[i];
                if (item.ItemType != BoardItemType.Gem && !string.Equals(item.ItemId, _targetItemId, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                string normalizedId = string.IsNullOrWhiteSpace(item.ItemId) ? "gem" : item.ItemId.Trim();
                if (!string.Equals(normalizedId, _targetItemId, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                _currentAmount = Mathf.Min(_targetAmount, _currentAmount + 1);
            }
        }

        public void NotifyScoreChanged(int currentScore) { }
        public void NotifyTimerUpdated(float remainingTimeSeconds, float totalTimeSeconds)
        {
            _remainingTime = remainingTimeSeconds;
            _totalTime = totalTimeSeconds;
        }
        #endregion
    }
}
