using _Game.Scripts.Logic.Resolve;
using _Game.Scripts.Modes;
using UnityEngine;

namespace _Game.Scripts.Modes.Objectives
{
    /// <summary>
    /// Score level: reach the target score before time runs out.
    /// This is the only Arcade level type that uses ScoreManager scoring.
    /// </summary>
    public sealed class ScoreObjective : IGameObjective
    {
        #region Fields
        private readonly int _targetScore;
        private readonly bool _failWhenTimeEnds;
        private float _remainingTime;
        private float _totalTime;
        private int _currentScore;
        #endregion

        #region State
        public ObjectiveProgress Progress => new ObjectiveProgress($"{_currentScore}/{_targetScore}", _currentScore, _targetScore, IsCompleted, IsFailed);
        public bool IsCompleted => _targetScore > 0 && _currentScore >= _targetScore;
        public bool IsFailed => _failWhenTimeEnds && !IsCompleted && _totalTime > 0f && _remainingTime <= 0f;
        #endregion

        #region Constructor
        public ScoreObjective(int targetScore, bool failWhenTimeEnds = true)
        {
            _targetScore = Mathf.Max(1, targetScore);
            _failWhenTimeEnds = failWhenTimeEnds;
        }
        #endregion

        #region Lifecycle
        public void Initialize(ModeRuntimeContext context)
        {
            _currentScore = context != null && context.ScoreManager != null ? context.ScoreManager.CurrentScore : 0;
            _remainingTime = 0f;
            _totalTime = 0f;
        }

        public void NotifyBoardResolved(BoardResolveResult result) { }

        public void NotifyScoreChanged(int currentScore)
        {
            _currentScore = Mathf.Max(0, currentScore);
        }

        public void NotifyTimerUpdated(float remainingTimeSeconds, float totalTimeSeconds)
        {
            _remainingTime = remainingTimeSeconds;
            _totalTime = totalTimeSeconds;
        }
        #endregion
    }
}
