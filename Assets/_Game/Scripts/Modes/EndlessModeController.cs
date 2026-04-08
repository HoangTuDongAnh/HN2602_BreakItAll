using System;
using BreakItAll.Gameplay;

namespace BreakItAll.Modes
{
    /// <summary>
    /// Điều phối flow Endless trên gameplay core.
    /// </summary>
    public sealed class EndlessModeController : IModeController
    {
        public bool IsRunning { get; private set; }
        public EndlessSessionContext SessionContext { get; }

        public EndlessModeController(EndlessSessionContext sessionContext)
        {
            SessionContext = sessionContext ?? throw new ArgumentNullException(nameof(sessionContext));
        }

        public void StartMode()
        {
            SessionContext.Reset();
            IsRunning = true;

            if (!SessionContext.BoardController.HasAnyMove())
            {
                SessionContext.MarkGameOver();
                IsRunning = false;
            }
        }

        public void StopMode()
        {
            IsRunning = false;
        }

        public EndlessTurnResult TryPlaceFromQueue(int queueIndex, CellCoord anchor)
        {
            if (!IsRunning)
            {
                return EndlessTurnResult.Failed("Endless mode is not running.", SessionContext.CurrentScore, SessionContext.IsGameOver);
            }

            if (SessionContext.IsGameOver)
            {
                IsRunning = false;
                return EndlessTurnResult.Failed("Game is already over.", SessionContext.CurrentScore, true);
            }

            BoardPlaceAndResolveResult boardResult = SessionContext.BoardController.PlaceFromQueueAndResolve(queueIndex, anchor);

            if (!boardResult.Success)
            {
                return EndlessTurnResult.Failed(boardResult.FailureReason, SessionContext.CurrentScore, SessionContext.IsGameOver);
            }

            int gainedScore = SessionContext.ScoreSystem.ApplyMove(boardResult.ClearResult);
            SessionContext.SetLastGainedScore(gainedScore);

            bool hasAnyMove = SessionContext.BoardController.HasAnyMove();
            if (!hasAnyMove)
            {
                SessionContext.MarkGameOver();
                IsRunning = false;
            }

            return new EndlessTurnResult(
                true,
                string.Empty,
                boardResult.PlacementResult,
                boardResult.ClearResult,
                gainedScore,
                SessionContext.CurrentScore,
                SessionContext.IsGameOver);
        }
    }
}