using System;
using System.Collections.Generic;
using _Game.Scripts.Data;
using UnityEngine;

namespace _Game.Scripts.Core
{
    public static class GameEvents
    {
        #region Gameplay Events
        public static Func<List<CellData>, Vector2Int, bool> RequestPlaceBlock;

        // total lines, effect center
        public static Action<int, Vector3> OnMoveCompleted;

        public static Action OnGameStarted;
        public static Action OnGamePaused;
        public static Action OnGameResumed;
        public static Action OnGameOver;
        #endregion

        #region UI & Score Events
        public static Action<int, int> OnScoreChanged;
        public static Action<int> OnComboUpdated;
        #endregion

        #region Visual Effects Events
        // text, position, color, scale
        public static Action<string, Vector3, Color, float> OnShowFloatingText;
        #endregion
    }
}