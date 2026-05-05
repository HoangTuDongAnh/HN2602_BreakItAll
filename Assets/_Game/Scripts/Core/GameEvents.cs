using System;
using System.Collections.Generic;
using _Game.Scripts.Data;
using _Game.Scripts.Logic.Resolve;
using _Game.Scripts.Modes.Levels;
using _Game.Scripts.Modes.Objectives;
using UnityEngine;

namespace _Game.Scripts.Core
{
    public static class GameEvents
    {
        #region Gameplay Events
        public static event Func<List<CellData>, Vector2Int, bool> RequestPlaceBlock;
        public static event Action<int, Vector3> OnMoveCompleted;
        public static event Action<BoardResolveResult> OnBoardResolved;
        public static event Action OnGameStarted;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        public static event Action OnGameOver;
        #endregion

        #region Arcade Flow Events
        public static event Action<LevelDefinition> OnArcadeLevelStarted;
        public static event Action<LevelDefinition> OnArcadeLevelCompleted;
        public static event Action<LevelDefinition> OnArcadeLevelFailed;
        #endregion

        #region UI & Score Events
        public static event Action<int, int> OnScoreChanged;
        public static event Action<int> OnComboUpdated;
        public static event Action<float, float> OnTimerUpdated;
        #endregion

        #region Objective Events
        public static event Action<ObjectiveProgress> OnObjectiveProgressChanged;
        public static event Action<ObjectiveProgress> OnObjectiveCompleted;
        public static event Action<ObjectiveProgress> OnObjectiveFailed;
        public static event Action OnPuzzleQueueChanged;
        public static event Action OnPuzzleBlockSetSwitched;
        public static event Action<bool> OnShapeOverlayToggled;
        #endregion

        #region Visual Effects Events
        public static event Action<string, Vector3, Color, float> OnShowFloatingText;
        #endregion

        #region Raise API
        public static bool RaiseRequestPlaceBlock(List<CellData> cells, Vector2Int anchor)
        {
            return RequestPlaceBlock?.Invoke(cells, anchor) ?? false;
        }

        public static void RaiseMoveCompleted(int totalLines, Vector3 effectCenter) => OnMoveCompleted?.Invoke(totalLines, effectCenter);
        public static void RaiseBoardResolved(BoardResolveResult result) => OnBoardResolved?.Invoke(result);
        public static void RaiseGameStarted() => OnGameStarted?.Invoke();
        public static void RaiseGamePaused() => OnGamePaused?.Invoke();
        public static void RaiseGameResumed() => OnGameResumed?.Invoke();
        public static void RaiseGameOver() => OnGameOver?.Invoke();

        public static void RaiseArcadeLevelStarted(LevelDefinition level) => OnArcadeLevelStarted?.Invoke(level);
        public static void RaiseArcadeLevelCompleted(LevelDefinition level) => OnArcadeLevelCompleted?.Invoke(level);
        public static void RaiseArcadeLevelFailed(LevelDefinition level) => OnArcadeLevelFailed?.Invoke(level);

        public static void RaiseScoreChanged(int currentScore, int bestScore) => OnScoreChanged?.Invoke(currentScore, bestScore);
        public static void RaiseComboUpdated(int combo) => OnComboUpdated?.Invoke(combo);
        public static void RaiseTimerUpdated(float remainingSeconds, float totalSeconds) => OnTimerUpdated?.Invoke(remainingSeconds, totalSeconds);

        public static void RaiseObjectiveProgressChanged(ObjectiveProgress progress) => OnObjectiveProgressChanged?.Invoke(progress);
        public static void RaiseObjectiveCompleted(ObjectiveProgress progress) => OnObjectiveCompleted?.Invoke(progress);
        public static void RaiseObjectiveFailed(ObjectiveProgress progress) => OnObjectiveFailed?.Invoke(progress);
        public static void RaisePuzzleBlockSetSwitched() => OnPuzzleBlockSetSwitched?.Invoke();
        public static void RaiseShapeOverlayToggled(bool visible) => OnShapeOverlayToggled?.Invoke(visible);
        public static void RaisePuzzleQueueChanged()
        {
            OnPuzzleQueueChanged?.Invoke();
        }

        public static void RaiseShowFloatingText(string content, Vector3 position, Color color, float scale)
        {
            OnShowFloatingText?.Invoke(content, position, color, scale);
        }
        #endregion
    }
}
