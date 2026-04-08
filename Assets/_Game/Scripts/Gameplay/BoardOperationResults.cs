using System.Collections.Generic;

namespace BreakItAll.Gameplay
{
    public readonly struct PlacementCheckResult
    {
        public bool IsValid { get; }
        public string Reason { get; }

        public PlacementCheckResult(bool isValid, string reason)
        {
            IsValid = isValid;
            Reason = reason;
        }

        public static PlacementCheckResult Valid()
        {
            return new PlacementCheckResult(true, string.Empty);
        }

        public static PlacementCheckResult Invalid(string reason)
        {
            return new PlacementCheckResult(false, reason);
        }
    }

    public readonly struct PlacementExecutionResult
    {
        public bool Success { get; }
        public IReadOnlyList<CellCoord> PlacedCells { get; }

        public PlacementExecutionResult(bool success, IReadOnlyList<CellCoord> placedCells)
        {
            Success = success;
            PlacedCells = placedCells;
        }
    }

    public readonly struct ClearResolutionResult
    {
        public int ClearedRowCount { get; }
        public int ClearedColumnCount { get; }
        public int TotalClearedCellCount { get; }
        public IReadOnlyList<int> ClearedRows { get; }
        public IReadOnlyList<int> ClearedColumns { get; }
        public IReadOnlyList<CellCoord> ClearedCells { get; }

        public bool HasAnyClear => ClearedRowCount > 0 || ClearedColumnCount > 0;

        public ClearResolutionResult(
            int clearedRowCount,
            int clearedColumnCount,
            int totalClearedCellCount,
            IReadOnlyList<int> clearedRows,
            IReadOnlyList<int> clearedColumns,
            IReadOnlyList<CellCoord> clearedCells)
        {
            ClearedRowCount = clearedRowCount;
            ClearedColumnCount = clearedColumnCount;
            TotalClearedCellCount = totalClearedCellCount;
            ClearedRows = clearedRows;
            ClearedColumns = clearedColumns;
            ClearedCells = clearedCells;
        }

        public static ClearResolutionResult Empty()
        {
            return new ClearResolutionResult(
                0,
                0,
                0,
                new List<int>(),
                new List<int>(),
                new List<CellCoord>());
        }
    }

    public readonly struct BoardPlaceAndResolveResult
    {
        public bool Success { get; }
        public PlacementExecutionResult PlacementResult { get; }
        public ClearResolutionResult ClearResult { get; }
        public string FailureReason { get; }

        public BoardPlaceAndResolveResult(
            bool success,
            PlacementExecutionResult placementResult,
            ClearResolutionResult clearResult,
            string failureReason)
        {
            Success = success;
            PlacementResult = placementResult;
            ClearResult = clearResult;
            FailureReason = failureReason;
        }

        public static BoardPlaceAndResolveResult Failed(string reason)
        {
            return new BoardPlaceAndResolveResult(
                false,
                new PlacementExecutionResult(false, new List<CellCoord>()),
                ClearResolutionResult.Empty(),
                reason);
        }
    }
}