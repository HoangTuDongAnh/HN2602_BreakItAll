using _Game.Scripts.Logic;
using _Game.Scripts.Logic.Resolve;
using _Game.Scripts.Modes;

namespace _Game.Scripts.Modes.Objectives
{
    /// <summary>
    /// Puzzle objective implementation. It uses target pattern cells as the fill mask;
    /// provided-block and rotation rules are enforced by BlockSpawner.
    /// </summary>
    public sealed class PuzzleObjective : IGameObjective
    {
        private readonly bool _requireUseAllProvidedBlocks;
        private readonly bool _failWhenTimeEnds;
        private ModeRuntimeContext _context;
        private int _currentFilled;
        private int _targetCount;
        private int _usedProvidedBlocks;
        private int _totalProvidedBlocks;
        private float _remainingTime;
        private float _totalTime;

        public ObjectiveProgress Progress => new ObjectiveProgress(BuildDisplayText(), GetProgressValue(), GetTargetValue(), IsCompleted, IsFailed);
        public bool IsCompleted => _targetCount > 0
                                   && _currentFilled >= _targetCount
                                   && (!_requireUseAllProvidedBlocks || _usedProvidedBlocks >= _totalProvidedBlocks);
        public bool IsFailed => _failWhenTimeEnds && !IsCompleted && _totalTime > 0f && _remainingTime <= 0f;

        public PuzzleObjective(bool requireUseAllProvidedBlocks, bool failWhenTimeEnds)
        {
            _requireUseAllProvidedBlocks = requireUseAllProvidedBlocks;
            _failWhenTimeEnds = failWhenTimeEnds;
        }

        public void Initialize(ModeRuntimeContext context)
        {
            _context = context;
            _remainingTime = 0f;
            _totalTime = 0f;
            RefreshProgress();
        }

        public void NotifyBoardResolved(BoardResolveResult result)
        {
            RefreshProgress();
        }

        public void NotifyScoreChanged(int currentScore) { }
        public void NotifyTimerUpdated(float remainingTimeSeconds, float totalTimeSeconds)
        {
            _remainingTime = remainingTimeSeconds;
            _totalTime = totalTimeSeconds;
        }

        private void RefreshProgress()
        {
            _currentFilled = 0;
            _targetCount = 0;
            _usedProvidedBlocks = 0;
            _totalProvidedBlocks = 0;

            GridManager grid = _context != null ? _context.GridManager : GridManager.Instance;
            if (grid == null) return;

            bool hasTargetMask = grid.HasTargetPatternCells();

            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    GridCell cell = grid.GetCellData(x, y);
                    if (cell == null) continue;
                    if (hasTargetMask && !cell.IsTargetPatternCell) continue;

                    _targetCount++;
                    if (cell.IsOccupied)
                        _currentFilled++;
                }
            }

            BlockSpawner spawner = _context != null ? _context.BlockSpawner : BlockSpawner.Instance;
            if (spawner != null && spawner.IsFixedFillQueueActive)
            {
                _totalProvidedBlocks = spawner.TotalFixedFillBlocks;
                _usedProvidedBlocks = spawner.UsedFixedFillBlocks;
            }
        }

        private int GetProgressValue()
        {
            return _requireUseAllProvidedBlocks
                ? _currentFilled + _usedProvidedBlocks
                : _currentFilled;
        }

        private int GetTargetValue()
        {
            return _requireUseAllProvidedBlocks
                ? _targetCount + _totalProvidedBlocks
                : _targetCount;
        }

        private string BuildDisplayText()
        {
            if (!_requireUseAllProvidedBlocks || _totalProvidedBlocks <= 0)
                return $"{_currentFilled}/{_targetCount}";

            return $"{_currentFilled}/{_targetCount} cells | {_usedProvidedBlocks}/{_totalProvidedBlocks} blocks";
        }
    }
}
