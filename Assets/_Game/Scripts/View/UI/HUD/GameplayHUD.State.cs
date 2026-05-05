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
        #region UI State
        private void RefreshArcadeLabels(LevelDefinition level)
        {
            bool hasLevel = level != null;

            if (_levelText)
                _levelText.text = hasLevel ? level.OrderIndex.ToString() : string.Empty;

            if (_levelLabelRoot != null)
                _levelLabelRoot.SetActive(true);

            if (_legacyArcadeLabelText != null)
                _legacyArcadeLabelText.gameObject.SetActive(false);

            SetArcadeTypeRoots(level);
            SetTimerVisible(level != null && level.UsesTimer);

            if (level != null && level.LevelType == ArcadeLevelType.Shape)
            {
                _shapeOverlayVisible = false;
                ApplyShapeOverlayVisible(false);
            }
            else
            {
                _shapeOverlayVisible = true;
                ApplyShapeOverlayVisible(true);
            }

            UpdateShapeOverlayButton();
            UpdatePuzzleNavigation();
        }

        private void RefreshCurrentArcadeProgress()
        {
            GameManager manager = GameManager.Instance;
            RefreshArcadeProgress(manager != null ? manager.CurrentObjectiveProgress : ObjectiveProgress.Empty);
        }

        private void RefreshArcadeProgress(ObjectiveProgress progress)
        {
            ClearProgressText();

            if (_activeArcadeLevel == null)
                return;

            string raw = BuildProgressText(progress);
            if (string.IsNullOrEmpty(raw))
                return;

            switch (_activeArcadeLevel.LevelType)
            {
                case ArcadeLevelType.Score:
                    if (_scoreProgressText != null)
                        _scoreProgressText.text = raw;
                    break;
                case ArcadeLevelType.Collectable:
                    if (_collectableProgressText != null)
                        _collectableProgressText.text = $"Gem {raw}";
                    break;
                case ArcadeLevelType.Shape:
                    if (_shapeProgressText != null)
                        _shapeProgressText.text = raw;
                    break;
                case ArcadeLevelType.Puzzle:
                    // Puzzle UI intentionally does not show objective progress text.
                    // It only shows timer, remaining blocks and the Switch button.
                    break;
            }
        }

        private void ClearArcadeText()
        {
            if (_levelText) _levelText.text = string.Empty;
            if (_legacyArcadeLabelText) _legacyArcadeLabelText.text = string.Empty;
            ClearProgressText();
        }

        private void ClearProgressText()
        {
            if (_scoreProgressText) _scoreProgressText.text = string.Empty;
            if (_collectableProgressText) _collectableProgressText.text = string.Empty;
            if (_shapeProgressText) _shapeProgressText.text = string.Empty;
            if (_puzzleProgressText) _puzzleProgressText.text = string.Empty;
        }

        private void SetModeRoots(bool isArcade)
        {
            if (_endlessRoot)
                _endlessRoot.SetActive(!isArcade);

            if (_arcadeRoot)
                _arcadeRoot.SetActive(isArcade);
        }

        private void SetArcadeTypeRoots(LevelDefinition level)
        {
            ArcadeLevelType? type = level != null ? level.LevelType : null;

            SetObjectActive(_scoreLabelRoot, type == ArcadeLevelType.Score);
            SetObjectActive(_collectableLabelRoot, type == ArcadeLevelType.Collectable);
            SetObjectActive(_shapeLabelRoot, type == ArcadeLevelType.Shape);
            SetObjectActive(_puzzleLabelRoot, type == ArcadeLevelType.Puzzle);
        }

        private void SetTimerVisible(bool visible)
        {
            if (_scoreTimerText)
                _scoreTimerText.gameObject.SetActive(visible && IsScoreArcadeLevel());

            if (_collectableTimerText)
                _collectableTimerText.gameObject.SetActive(visible && IsCollectableArcadeLevel());

            if (_puzzleTimerText)
                _puzzleTimerText.gameObject.SetActive(visible && IsPuzzleArcadeLevel());
        }

        private void UpdatePuzzleNavigation()
        {
            bool visible = IsPuzzleArcadeLevel();
            SetPuzzleNavigationVisible(visible);
            if (!visible) return;

            BlockSpawner spawner = BlockSpawner.Instance;
            int remaining = spawner != null ? spawner.RemainingPuzzleBlocks : 0;

            if (_puzzleRemainingText != null)
                _puzzleRemainingText.text = $"Block Remaining: {remaining}";

            if (_puzzleSwitchButtonText != null)
                _puzzleSwitchButtonText.text = "Switch";

            if (_puzzleSwitchButton != null)
                _puzzleSwitchButton.interactable = spawner != null && spawner.CanSwitchPuzzleBlockSet;
        }

        private void SetPuzzleNavigationVisible(bool visible)
        {
            if (_puzzleSwitchButton != null)
                _puzzleSwitchButton.gameObject.SetActive(visible);

            if (_puzzleRemainingText != null)
                _puzzleRemainingText.gameObject.SetActive(visible);

            if (_puzzlePreviousButton != null && _puzzlePreviousButton != _puzzleSwitchButton)
                _puzzlePreviousButton.gameObject.SetActive(false);

            if (_puzzleNextButton != null && _puzzleNextButton != _puzzleSwitchButton)
                _puzzleNextButton.gameObject.SetActive(false);

            if (_puzzlePageText != null && _puzzlePageText != _puzzleRemainingText)
                _puzzlePageText.gameObject.SetActive(false);

            if (_puzzleProgressText != null)
                _puzzleProgressText.gameObject.SetActive(false);
        }

        private void NormalizeRuntimeVisibility()
        {
            bool isArcade = IsArcadeSession();
            SetModeRoots(isArcade);

            if (!isArcade)
            {
                _activeArcadeLevel = null;
                SetObjectActive(_levelLabelRoot, false);
                SetArcadeTypeRoots(null);
                SetTimerVisible(false);
                SetPuzzleNavigationVisible(false);
                UpdateShapeOverlayButton();
                return;
            }

            EnsureActiveArcadeLevel();
            SetObjectActive(_levelLabelRoot, _activeArcadeLevel != null);
            SetArcadeTypeRoots(_activeArcadeLevel);
            SetTimerVisible(_activeArcadeLevel != null && _activeArcadeLevel.UsesTimer);
            UpdateShapeOverlayButton();
            UpdatePuzzleNavigation();
        }

        private void ApplyShapeOverlayVisible(bool visible)
        {
            GridManager.Instance?.SetTargetPatternOverlayVisible(visible);
        }

        private void UpdateShapeOverlayButton()
        {
            if (_shapeOverlayButton != null)
                _shapeOverlayButton.gameObject.SetActive(IsShapeArcadeLevel());

            if (_shapeOverlayButtonText != null)
                _shapeOverlayButtonText.text = _shapeOverlayVisible ? "Overlay On" : "Overlay Off";
        }

        private static void SetObjectActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }
        #endregion
    }
}
