using _Game.Scripts.Core.Services;
using UnityEngine;

namespace _Game.Scripts.Logic.Tools
{
    /// <summary>
    /// Owns tool inventory counts and persistence.
    /// ToolController should focus on tool behavior; this service focuses on counts.
    /// </summary>
    public sealed class ToolInventoryService
    {
        private int _singleCellCount;
        private int _removeSpawnBlockCount;
        private int _bombSquareCount;

        public int GetCount(GameplayToolType toolType)
        {
            switch (toolType)
            {
                case GameplayToolType.PlaceSingleCell:
                    return _singleCellCount;
                case GameplayToolType.RemoveSpawnBlock:
                    return _removeSpawnBlockCount;
                case GameplayToolType.BombSquare:
                    return _bombSquareCount;
                default:
                    return 0;
            }
        }

        public void SetCount(GameplayToolType toolType, int count)
        {
            count = Mathf.Max(0, count);

            switch (toolType)
            {
                case GameplayToolType.PlaceSingleCell:
                    _singleCellCount = count;
                    break;
                case GameplayToolType.RemoveSpawnBlock:
                    _removeSpawnBlockCount = count;
                    break;
                case GameplayToolType.BombSquare:
                    _bombSquareCount = count;
                    break;
            }
        }

        public void Add(GameplayToolType toolType, int amount)
        {
            if (toolType == GameplayToolType.None || amount == 0) return;

            SetCount(toolType, GetCount(toolType) + amount);
        }

        public bool TryConsume(GameplayToolType toolType, int amount = 1)
        {
            if (toolType == GameplayToolType.None) return false;
            amount = Mathf.Max(1, amount);

            int current = GetCount(toolType);
            if (current < amount) return false;

            SetCount(toolType, current - amount);
            return true;
        }

        public void Load(int defaultSingleCellCount, int defaultRemoveSpawnBlockCount, int defaultBombSquareCount)
        {
            _singleCellCount = Mathf.Max(0, GameSave.GetInt(SaveKeys.ToolSingleCellCount, defaultSingleCellCount));
            _removeSpawnBlockCount = Mathf.Max(0, GameSave.GetInt(SaveKeys.ToolRemoveSpawnBlockCount, defaultRemoveSpawnBlockCount));
            _bombSquareCount = Mathf.Max(0, GameSave.GetInt(SaveKeys.ToolBombSquareCount, defaultBombSquareCount));
        }

        public void Save()
        {
            GameSave.SetInt(SaveKeys.ToolSingleCellCount, _singleCellCount);
            GameSave.SetInt(SaveKeys.ToolRemoveSpawnBlockCount, _removeSpawnBlockCount);
            GameSave.SetInt(SaveKeys.ToolBombSquareCount, _bombSquareCount);
            GameSave.Save();
        }
    }
}
