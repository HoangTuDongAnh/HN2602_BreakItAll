using _Game.Scripts.Logic;
using _Game.Scripts.Logic.Resolve;
using _Game.Scripts.Modes;
using UnityEngine;

namespace _Game.Scripts.Modes.Objectives
{
    /// <summary>
    /// Shape level: pass khi tat ca target pattern cells tren board da duoc fill.
    /// Shape khong dung diem va khong co timer.
    /// </summary>
    public sealed class ShapeObjective : IGameObjective
    {
        #region Fields
        private ModeRuntimeContext _context;
        private readonly bool _requireExactShape;
        private int _currentFilled;
        private int _targetCount;
        private int _outsideOccupied;
        #endregion

        #region State
        public ObjectiveProgress Progress => new ObjectiveProgress(BuildDisplayText(), _currentFilled, _targetCount, IsCompleted, IsFailed);
        public bool IsCompleted => _targetCount > 0 && _currentFilled >= _targetCount && (!_requireExactShape || _outsideOccupied == 0);
        public bool IsFailed { get; private set; }
        #endregion

        #region Constructor
        public ShapeObjective(bool requireExactShape)
        {
            _requireExactShape = requireExactShape;
        }
        #endregion

        #region Lifecycle
        public void Initialize(ModeRuntimeContext context)
        {
            _context = context;
            IsFailed = false;
            RefreshPatternProgress();
        }

        public void NotifyBoardResolved(BoardResolveResult result)
        {
            RefreshPatternProgress();
        }

        public void NotifyScoreChanged(int currentScore) { }
        public void NotifyTimerUpdated(float remainingTimeSeconds, float totalTimeSeconds) { }
        #endregion

        #region Helpers
        private void RefreshPatternProgress()
        {
            _currentFilled = 0;
            _targetCount = 0;
            _outsideOccupied = 0;

            GridManager grid = _context != null ? _context.GridManager : GridManager.Instance;
            if (grid == null) return;

            int width = grid.Width;
            int height = grid.Height;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GridCell cell = grid.GetCellData(x, y);
                    if (cell == null) continue;
                    if (!cell.IsTargetPatternCell)
                    {
                        if (cell.IsOccupied)
                            _outsideOccupied++;
                        continue;
                    }

                    _targetCount++;
                    if (cell.IsOccupied)
                        _currentFilled++;
                }
            }
        }

        private string BuildDisplayText()
        {
            if (!_requireExactShape || _outsideOccupied <= 0)
                return $"{_currentFilled}/{_targetCount}";

            return $"{_currentFilled}/{_targetCount} +{_outsideOccupied}";
        }
        #endregion
    }
}
