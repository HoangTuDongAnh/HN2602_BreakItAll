using BreakItAll.Gameplay;

namespace BreakItAll.Modes
{
    public readonly struct EndlessTurnResult
    {
        public bool Success { get; }
        public string FailureReason { get; }
        public PlacementExecutionResult PlacementResult { get; }
        public ClearResolutionResult ClearResult { get; }
        public int GainedScore { get; }
        public int TotalScore { get; }
        public bool IsGameOver { get; }

        public EndlessTurnResult(
            bool success,
            string failureReason,
            PlacementExecutionResult placementResult,
            ClearResolutionResult clearResult,
            int gainedScore,
            int totalScore,
            bool isGameOver)
        {
            Success = success;
            FailureReason = failureReason;
            PlacementResult = placementResult;
            ClearResult = clearResult;
            GainedScore = gainedScore;
            TotalScore = totalScore;
            IsGameOver = isGameOver;
        }

        public static EndlessTurnResult Failed(string reason, int totalScore, bool isGameOver)
        {
            return new EndlessTurnResult(
                false,
                reason,
                new PlacementExecutionResult(false, System.Array.Empty<CellCoord>()),
                ClearResolutionResult.Empty(),
                0,
                totalScore,
                isGameOver);
        }
    }
}