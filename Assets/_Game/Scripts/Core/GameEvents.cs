using System;
using System.Collections.Generic;
using _Game.Scripts.Data;
using UnityEngine;

namespace _Game.Scripts.Core
{
    public static class GameEvents
    {
        #region Gameplay Events
        // Yêu cầu đặt khối gạch (Input -> GridManager)
        public static Func<List<CellData>, Vector2Int, bool> RequestPlaceBlock;
        
        // Hoàn thành một nước đi (sau khi đã xóa hàng xong)
        // Param 1: Số hàng đã ăn
        // Param 2: Vị trí trung tâm vụ nổ (để hiện effect)
        public static Action<int, Vector3> OnMoveCompleted; 
        #endregion

        #region UI & Score Events
        // Điểm số thay đổi -> Cập nhật UI
        public static Action<int, int> OnScoreChanged; // (CurrentScore, HighScore)
        
        // Combo thay đổi -> Rung camera
        public static Action<int> OnComboUpdated; // (ComboStreak)
        #endregion

        #region Visual Effects Events
        // Yêu cầu hiển thị chữ bay (Floating Text)
        // Params: Nội dung, Vị trí, Màu sắc, Độ to
        public static Action<string, Vector3, Color, float> OnShowFloatingText; 
        #endregion
    }
}