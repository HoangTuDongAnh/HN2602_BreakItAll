using System;
using _Game.Scripts.Logic.Tools;
using UnityEngine;

namespace _Game.Scripts.Modes.Levels
{
    /// <summary>
    /// Per-level gameplay tool rule.
    /// Default policy:
    /// - Endless is controlled by ToolController global rules.
    /// - Arcade non-Puzzle levels allow tools unless custom rules disable them.
    /// - Puzzle levels disable all tools by default to keep the puzzle solution fair and deterministic.
    /// </summary>
    [Serializable]
    public class ToolRuleDefinition
    {
        [Tooltip("When false, the project default is used: Puzzle disables all tools; other Arcade level types allow tools.")]
        [SerializeField] private bool _useCustomRules;

        [Tooltip("Master toggle for tools in this level. If false, no gameplay tools can be selected.")]
        [SerializeField] private bool _allowTools = true;

        [Tooltip("Allows the Single Cell placement tool.")]
        [SerializeField] private bool _allowSingleCell = true;

        [Tooltip("Allows the Remove Spawn Block tool.")]
        [SerializeField] private bool _allowRemoveSpawnBlock = true;

        [Tooltip("Allows the Bomb Square tool.")]
        [SerializeField] private bool _allowBombSquare = true;

        public bool UseCustomRules => _useCustomRules;
        public bool AllowTools => _allowTools;
        public bool AllowSingleCell => _allowSingleCell;
        public bool AllowRemoveSpawnBlock => _allowRemoveSpawnBlock;
        public bool AllowBombSquare => _allowBombSquare;

        public bool IsToolAllowed(ArcadeLevelType levelType, GameplayToolType toolType)
        {
            if (toolType == GameplayToolType.None)
                return true;

            if (!_useCustomRules)
                return levelType != ArcadeLevelType.Puzzle;

            if (!_allowTools)
                return false;

            switch (toolType)
            {
                case GameplayToolType.PlaceSingleCell:
                    return _allowSingleCell;
                case GameplayToolType.RemoveSpawnBlock:
                    return _allowRemoveSpawnBlock;
                case GameplayToolType.BombSquare:
                    return _allowBombSquare;
                default:
                    return false;
            }
        }

        public string GetDisabledReason(ArcadeLevelType levelType, GameplayToolType toolType)
        {
            if (IsToolAllowed(levelType, toolType))
                return string.Empty;

            if (!_useCustomRules && levelType == ArcadeLevelType.Puzzle)
                return "Tools are disabled in Puzzle levels";

            if (_useCustomRules && !_allowTools)
                return "Tools are disabled in this level";

            return "This tool is disabled in this level";
        }
    }
}
