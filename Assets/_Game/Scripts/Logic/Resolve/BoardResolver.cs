using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _Game.Scripts.Data;
using _Game.Scripts.View;

namespace _Game.Scripts.Logic.Resolve
{
    public class BoardResolver
    {
        private readonly LineClearDetector _lineClearDetector;
        private readonly float _clearStepDelay;

        public BoardResolver(LineClearDetector lineClearDetector, float clearStepDelay)
        {
            _lineClearDetector = lineClearDetector;
            _clearStepDelay = clearStepDelay;
        }

        public IEnumerator Resolve(
            GridCell[,] gridData,
            GridCellView[,] gridViews,
            int width,
            int height,
            Func<int, int, Vector3> getWorldPosition,
            Action<BoardResolveResult> onResolved)
        {
            LineClearDetectionResult detection = _lineClearDetector.Detect(gridData, width, height);

            BoardResolveResult result = new BoardResolveResult
            {
                TotalLines = detection.TotalLines
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

            Vector3 centerPos = Vector3.zero;
            foreach (Vector2Int cell in result.ClearedCells)
            {
                centerPos += getWorldPosition(cell.x, cell.y);
            }

            centerPos /= result.ClearedCells.Count;
            result.EffectCenter = centerPos;

            foreach (Vector2Int cell in result.ClearedCells)
            {
                gridData[cell.x, cell.y].Clear();
                gridViews[cell.x, cell.y].UpdateVisual();

                if (_clearStepDelay > 0f)
                    yield return new WaitForSeconds(_clearStepDelay);
            }

            onResolved?.Invoke(result);
        }
    }
}