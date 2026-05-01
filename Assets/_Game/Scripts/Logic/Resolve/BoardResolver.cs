using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _Game.Scripts.Data;
using _Game.Scripts.View;

namespace _Game.Scripts.Logic.Resolve
{
    /// <summary>
    /// Resolve hàng/cột sau khi đặt block.
    /// Không tự cộng điểm, không xử lý UI mode; chỉ trả về BoardResolveResult đầy đủ.
    /// </summary>
    public class BoardResolver
    {
        #region Fields
        private readonly LineClearDetector _lineClearDetector;
        private readonly float _clearStepDelay;
        private readonly float _clearAnimationDuration;
        #endregion

        #region Constructor
        public BoardResolver(LineClearDetector lineClearDetector, float clearStepDelay, float clearAnimationDuration = 0.16f)
        {
            _lineClearDetector = lineClearDetector;
            _clearStepDelay = clearStepDelay;
            _clearAnimationDuration = Mathf.Max(0.05f, clearAnimationDuration);
        }
        #endregion

        #region Resolve API
        public IEnumerator Resolve(
            GridCell[,] gridData,
            GridCellView[,] gridViews,
            int width,
            int height,
            Func<int, int, Vector3> getWorldPosition,
            IReadOnlyList<Vector2Int> placedCells,
            Action<BoardResolveResult> onResolved)
        {
            LineClearDetectionResult detection = _lineClearDetector.Detect(gridData, width, height);

            BoardResolveResult result = new BoardResolveResult
            {
                TotalLines = detection.TotalLines,
                ClearedRows = detection.RowsToClear.ToList(),
                ClearedColumns = detection.ColsToClear.ToList(),
                PlacedCells = placedCells != null ? new List<Vector2Int>(placedCells) : new List<Vector2Int>()
            };

            if (!detection.HasAny)
            {
                onResolved?.Invoke(result);
                yield break;
            }

            result.ClearedCells = detection.CellsToClear
                .OrderBy(c => c.x)
                .ThenBy(c => c.y)
                .ToList();

            CaptureArcadeMarkers(gridData, result);
            result.EffectCenter = CalculateEffectCenter(result.ClearedCells, getWorldPosition);

            foreach (Vector2Int cell in result.ClearedCells)
            {
                GridCellView view = gridViews[cell.x, cell.y];
                if (view != null)
                    view.StartCoroutine(view.PlayClearAnimation(_clearAnimationDuration));
            }

            yield return new WaitForSeconds(_clearAnimationDuration);

            foreach (Vector2Int cell in result.ClearedCells)
            {
                GridCell data = gridData[cell.x, cell.y];
                data.Clear();
                data.ClearBoardItem();

                gridViews[cell.x, cell.y].UpdateVisual();

                if (_clearStepDelay > 0f)
                    yield return new WaitForSeconds(_clearStepDelay);
            }

            onResolved?.Invoke(result);
        }
        #endregion

        #region Helpers
        private void CaptureArcadeMarkers(GridCell[,] gridData, BoardResolveResult result)
        {
            foreach (Vector2Int cellCoord in result.ClearedCells)
            {
                GridCell cell = gridData[cellCoord.x, cellCoord.y];
                if (cell == null) continue;

                if (cell.ItemType != BoardItemType.None)
                {
                    result.CollectedItems.Add(new ResolvedBoardItem
                    {
                        Coord = cellCoord,
                        ItemType = cell.ItemType,
                        ItemId = cell.ItemId,
                        MarkerTag = cell.MarkerTag
                    });
                }

                if (cell.IsTargetPatternCell)
                    result.ClearedTargetCells.Add(cellCoord);
            }
        }

        private Vector3 CalculateEffectCenter(List<Vector2Int> clearedCells, Func<int, int, Vector3> getWorldPosition)
        {
            if (clearedCells == null || clearedCells.Count == 0 || getWorldPosition == null)
                return Vector3.zero;

            Vector3 center = Vector3.zero;
            foreach (Vector2Int cell in clearedCells)
                center += getWorldPosition(cell.x, cell.y);

            return center / clearedCells.Count;
        }
        #endregion
    }
}
