using _Game.Scripts.Core;
using _Game.Scripts.Core.Arcade;
using _Game.Scripts.Logic;
using _Game.Scripts.Modes;
using _Game.Scripts.Modes.Levels;
using _Game.Scripts.Modes.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.Scripts.View.UI
{
    public partial class GameplayHUD
    {
        #region Logic Helpers
        private bool IsArcadeSession()
        {
            return GameManager.Instance != null && GameManager.Instance.CurrentModeType == GameModeType.Arcade;
        }

        private bool ShouldShowEndlessRoot()
        {
            return !IsArcadeSession();
        }

        private bool IsScoreArcadeLevel()
        {
            return _activeArcadeLevel != null && _activeArcadeLevel.LevelType == ArcadeLevelType.Score;
        }

        private bool IsShapeArcadeLevel()
        {
            return _activeArcadeLevel != null && _activeArcadeLevel.LevelType == ArcadeLevelType.Shape;
        }

        private bool IsCollectableArcadeLevel()
        {
            return _activeArcadeLevel != null && _activeArcadeLevel.LevelType == ArcadeLevelType.Collectable;
        }

        private bool IsPuzzleArcadeLevel()
        {
            return IsArcadeSession()
                   && _activeArcadeLevel != null
                   && _activeArcadeLevel.LevelType == ArcadeLevelType.Puzzle;
        }

        private bool UsesTimerArcadeLevel()
        {
            return _activeArcadeLevel != null && _activeArcadeLevel.UsesTimer;
        }

        private TextMeshProUGUI GetActiveTimerText()
        {
            if (IsScoreArcadeLevel()) return _scoreTimerText;
            if (IsCollectableArcadeLevel()) return _collectableTimerText;
            if (IsPuzzleArcadeLevel()) return _puzzleTimerText;
            return null;
        }

        private void EnsureActiveArcadeLevel()
        {
            if (_activeArcadeLevel == null)
                _activeArcadeLevel = ResolveActiveArcadeLevel();
        }

        private LevelDefinition ResolveActiveArcadeLevel()
        {
            GameManager manager = GameManager.Instance;
            if (manager != null && manager.ActiveArcadeLevel != null)
                return manager.ActiveArcadeLevel;

            return ArcadeSession.SelectedLevel;
        }

        private static string BuildProgressText(ObjectiveProgress progress)
        {
            if (!string.IsNullOrEmpty(progress.DisplayText))
                return progress.DisplayText;

            return progress.TargetValue > 0 ? $"{progress.CurrentValue}/{progress.TargetValue}" : string.Empty;
        }
        #endregion
    }
}
