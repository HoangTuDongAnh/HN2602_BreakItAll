using System;
using System.Collections.Generic;

namespace BreakItAll.Gameplay
{
    /// <summary>
    /// Facade gameplay cho thao tác board.
    /// Bao gồm board core + queue + move availability.
    /// Không chứa mode-specific logic.
    /// </summary>
    public sealed class BoardController
    {
        private readonly PlacementValidator _placementValidator;
        private readonly PlacementExecutor _placementExecutor;
        private readonly ClearResolver _clearResolver;
        private readonly MoveAvailabilityChecker _moveAvailabilityChecker;
        private readonly BlockQueueController _blockQueueController;

        public BoardState BoardState { get; }
        public IReadOnlyList<ShapeData> CurrentQueue => _blockQueueController.CurrentQueue;
        public int QueueCount => _blockQueueController.CurrentQueue.Count;

        public BoardController(int width, int height, int queueSize, ShapeSpawnPool shapeSpawnPool)
        {
            if (shapeSpawnPool == null)
            {
                throw new ArgumentNullException(nameof(shapeSpawnPool));
            }

            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (queueSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(queueSize));
            }

            BoardState = new BoardState(width, height);

            _placementValidator = new PlacementValidator();
            _placementExecutor = new PlacementExecutor(_placementValidator);
            _clearResolver = new ClearResolver();
            _moveAvailabilityChecker = new MoveAvailabilityChecker(_placementValidator);

            SpawnSystem spawnSystem = new SpawnSystem(shapeSpawnPool);
            _blockQueueController = new BlockQueueController(spawnSystem, queueSize);
        }

        public void Initialize()
        {
            ResetBoard();
            _blockQueueController.Initialize();
        }

        public PlacementCheckResult CanPlace(ShapeData shapeData, CellCoord anchor)
        {
            return _placementValidator.Validate(BoardState, shapeData, anchor);
        }

        public ShapeData GetQueuedShape(int queueIndex)
        {
            return _blockQueueController.PeekAt(queueIndex);
        }

        public bool HasAnyMove()
        {
            return _moveAvailabilityChecker.HasAnyMove(BoardState, CurrentQueue);
        }

        public BoardPlaceAndResolveResult PlaceFromQueueAndResolve(int queueIndex, CellCoord anchor)
        {
            ShapeData shape = _blockQueueController.PeekAt(queueIndex);

            PlacementCheckResult check = _placementValidator.Validate(BoardState, shape, anchor);
            if (!check.IsValid)
            {
                return BoardPlaceAndResolveResult.Failed(check.Reason);
            }

            _blockQueueController.ConsumeAt(queueIndex);

            PlacementExecutionResult placementResult = _placementExecutor.Execute(BoardState, shape, anchor);
            ClearResolutionResult clearResult = _clearResolver.Resolve(BoardState);

            _blockQueueController.RefillIfEmpty();

            return new BoardPlaceAndResolveResult(
                true,
                placementResult,
                clearResult,
                string.Empty);
        }

        public void ResetBoard()
        {
            BoardState.ClearAll();
        }
    }
}