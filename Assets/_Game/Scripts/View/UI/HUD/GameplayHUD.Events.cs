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
        #region Event Handlers
        private void HandleArcadeLevelStarted(LevelDefinition level)
        {
            _activeArcadeLevel = level;
            SetModeRoots(isArcade: true);
            RefreshArcadeLabels(level);
            RefreshCurrentArcadeProgress();
            UpdatePuzzleNavigation();
        }

        private void HandleObjectiveProgressChanged(ObjectiveProgress progress)
        {
            bool isArcade = IsArcadeSession();
            SetModeRoots(isArcade);
            if (!isArcade) return;

            EnsureActiveArcadeLevel();
            RefreshArcadeProgress(progress);
            UpdatePuzzleNavigation();
        }

        private void RefreshForCurrentSession()
        {
            NormalizeRuntimeVisibility();
            bool isArcade = IsArcadeSession();
            SetModeRoots(isArcade);

            if (!isArcade)
            {
                _activeArcadeLevel = null;
                ClearArcadeText();
                SetTimerVisible(false);
                SetArcadeTypeRoots(null);
                SetPuzzleNavigationVisible(false);
                ApplyShapeOverlayVisible(true);
                return;
            }

            _activeArcadeLevel = ResolveActiveArcadeLevel();
            RefreshArcadeLabels(_activeArcadeLevel);
            RefreshCurrentArcadeProgress();
        }

        private void HideTimerIfNotNeeded()
        {
            if (!UsesTimerArcadeLevel())
                SetTimerVisible(false);
        }
        #endregion

        #region Public Timer API
        public void UpdateTimer(float remainingSeconds, float totalSeconds)
        {
            if (!UsesTimerArcadeLevel()) return;

            TextMeshProUGUI timerText = GetActiveTimerText();
            if (timerText == null) return;

            SetTimerVisible(true);
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds));
            int minutes = seconds / 60;
            int remainder = seconds % 60;
            timerText.text = $"{minutes:00}:{remainder:00}";
        }
        #endregion
    }
}
