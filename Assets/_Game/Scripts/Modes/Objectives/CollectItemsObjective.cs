using _Game.Scripts.Data;
using _Game.Scripts.Logic.Resolve;
using _Game.Scripts.Modes;
using UnityEngine;

namespace _Game.Scripts.Modes.Objectives
{
    /// <summary>
    /// Collect level: gem/item nam tren board. Khi clear line chua item thi item duoc tinh la da collect.
    /// Collect khong dung diem va khong co timer.
    /// </summary>
    public sealed class CollectItemsObjective : IGameObjective
    {
        #region Fields
        private readonly string _targetItemId;
        private readonly int _targetAmount;
        private int _currentAmount;
        #endregion

        #region State
        public ObjectiveProgress Progress => new ObjectiveProgress($"{_currentAmount}/{_targetAmount}", _currentAmount, _targetAmount, IsCompleted, IsFailed);
        public bool IsCompleted => _targetAmount > 0 && _currentAmount >= _targetAmount;
        public bool IsFailed { get; private set; }
        #endregion

        #region Constructor
        public CollectItemsObjective(string targetItemId, int targetAmount)
        {
            _targetItemId = string.IsNullOrWhiteSpace(targetItemId) ? "gem" : targetItemId.Trim();
            _targetAmount = Mathf.Max(1, targetAmount);
        }
        #endregion

        #region Lifecycle
        public void Initialize(ModeRuntimeContext context)
        {
            _currentAmount = 0;
            IsFailed = false;
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
        public void NotifyTimerUpdated(float remainingTimeSeconds, float totalTimeSeconds) { }
        #endregion
    }
}
