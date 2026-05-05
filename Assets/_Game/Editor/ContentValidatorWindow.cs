using System;
using System.Collections.Generic;
using _Game.Scripts.Data;
using _Game.Scripts.Logic;
using _Game.Scripts.Modes.Levels;
using _Game.Scripts.Modes.Map;
using _Game.Scripts.Logic.Tools;
using UnityEditor;
using UnityEngine;

namespace _Game.Editor
{
    public class ContentValidatorWindow : EditorWindow
    {
        private enum IssueSeverity
        {
            Info,
            Warning,
            Error
        }

        private sealed class Issue
        {
            public IssueSeverity Severity;
            public UnityEngine.Object Target;
            public string Category;
            public string Message;
        }

        private readonly List<Issue> _issues = new List<Issue>();
        private Vector2 _scroll;
        private bool _showInfo = true;
        private bool _showWarnings = true;
        private bool _showErrors = true;

        [MenuItem("Tools/_Game/Validate Content")]
        public static void OpenGameMenu()
        {
            Open();
        }

        [MenuItem("Tools/Block Blast/Validate Content")]
        public static void OpenBlockBlastMenu()
        {
            Open();
        }

        public static void Open()
        {
            ContentValidatorWindow window = GetWindow<ContentValidatorWindow>("Content Validator");
            window.minSize = new Vector2(860f, 520f);
            window.Show();
            window.ValidateContent();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSummary();
            DrawIssues();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Validate", GUILayout.Width(100f)))
                        ValidateContent();

                    if (GUILayout.Button("Fix Board Sizes", GUILayout.Width(128f)))
                    {
                        FixBoardSizes();
                        ValidateContent();
                    }

                    GUILayout.FlexibleSpace();
                    _showErrors = GUILayout.Toggle(_showErrors, "Errors", EditorStyles.toolbarButton, GUILayout.Width(74f));
                    _showWarnings = GUILayout.Toggle(_showWarnings, "Warnings", EditorStyles.toolbarButton, GUILayout.Width(86f));
                    _showInfo = GUILayout.Toggle(_showInfo, "Info", EditorStyles.toolbarButton, GUILayout.Width(62f));
                }

                EditorGUILayout.HelpBox("Phase 3 validator checks BlockData, SpawnStrategy, LevelDefinition, MapDefinition, fixed 9x9 board size, and objective-specific data.", MessageType.Info);
            }
        }

        private void DrawSummary()
        {
            int errors = 0;
            int warnings = 0;
            int info = 0;

            for (int i = 0; i < _issues.Count; i++)
            {
                switch (_issues[i].Severity)
                {
                    case IssueSeverity.Error:
                        errors++;
                        break;
                    case IssueSeverity.Warning:
                        warnings++;
                        break;
                    default:
                        info++;
                        break;
                }
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Errors: {errors}", GUILayout.Width(100f));
                EditorGUILayout.LabelField($"Warnings: {warnings}", GUILayout.Width(120f));
                EditorGUILayout.LabelField($"Info: {info}", GUILayout.Width(90f));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Total: {_issues.Count}", GUILayout.Width(110f));
            }
        }

        private void DrawIssues()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < _issues.Count; i++)
            {
                Issue issue = _issues[i];
                if (!ShouldShow(issue)) continue;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUIStyle severityStyle = new GUIStyle(EditorStyles.boldLabel);
                        severityStyle.normal.textColor = GetSeverityColor(issue.Severity);
                        EditorGUILayout.LabelField(issue.Severity.ToString(), severityStyle, GUILayout.Width(72f));
                        EditorGUILayout.LabelField(issue.Category, EditorStyles.boldLabel, GUILayout.Width(150f));
                        EditorGUILayout.ObjectField(issue.Target, typeof(UnityEngine.Object), false);

                        using (new EditorGUI.DisabledScope(issue.Target == null))
                        {
                            if (GUILayout.Button("Select", GUILayout.Width(62f)))
                            {
                                Selection.activeObject = issue.Target;
                                EditorGUIUtility.PingObject(issue.Target);
                            }
                        }
                    }

                    EditorGUILayout.LabelField(issue.Message, EditorStyles.wordWrappedLabel);
                }
            }

            if (_issues.Count == 0)
                EditorGUILayout.HelpBox("No issues found. Content looks good.", MessageType.Info);

            EditorGUILayout.EndScrollView();
        }

        private bool ShouldShow(Issue issue)
        {
            switch (issue.Severity)
            {
                case IssueSeverity.Error:
                    return _showErrors;
                case IssueSeverity.Warning:
                    return _showWarnings;
                default:
                    return _showInfo;
            }
        }

        private static Color GetSeverityColor(IssueSeverity severity)
        {
            switch (severity)
            {
                case IssueSeverity.Error:
                    return new Color(1f, 0.25f, 0.2f);
                case IssueSeverity.Warning:
                    return new Color(1f, 0.72f, 0.15f);
                default:
                    return new Color(0.5f, 0.75f, 1f);
            }
        }

        private void ValidateContent()
        {
            _issues.Clear();

            BlockData[] blocks = LoadAssets<BlockData>();
            SmartSpawnStrategy[] strategies = LoadAssets<SmartSpawnStrategy>();
            LevelDefinition[] levels = LoadAssets<LevelDefinition>();
            MapDefinition[] maps = LoadAssets<MapDefinition>();

            Dictionary<string, BlockData> blockById = ValidateBlocks(blocks);
            HashSet<string> strategyShapeIds = ValidateSpawnStrategies(strategies);
            ValidateLevels(levels, blockById, strategyShapeIds);
            ValidateMaps(maps);
        }

        private Dictionary<string, BlockData> ValidateBlocks(BlockData[] blocks)
        {
            Dictionary<string, BlockData> blockById = new Dictionary<string, BlockData>(StringComparer.OrdinalIgnoreCase);

            if (blocks.Length == 0)
                Add(IssueSeverity.Error, null, "BlockData", "No BlockData assets were found.");

            for (int i = 0; i < blocks.Length; i++)
            {
                BlockData block = blocks[i];
                if (block == null) continue;

                string id = Normalize(block.Id);
                if (string.IsNullOrEmpty(id))
                    Add(IssueSeverity.Error, block, "BlockData", "Block id is empty.");
                else if (blockById.TryGetValue(id, out BlockData duplicate))
                    Add(IssueSeverity.Error, block, "BlockData", $"Duplicate block id '{id}'. Existing asset: {duplicate.name}.");
                else
                    blockById[id] = block;

                if (block.columns < 1 || block.columns > BlockData.MaxShapeSize || block.rows < 1 || block.rows > BlockData.MaxShapeSize)
                    Add(IssueSeverity.Error, block, "BlockData", $"Invalid shape size {block.columns}x{block.rows}. Expected 1..{BlockData.MaxShapeSize}.");

                int expected = Mathf.Max(0, block.columns * block.rows);
                if (block.boardData == null)
                {
                    Add(IssueSeverity.Error, block, "BlockData", "boardData is null.");
                }
                else
                {
                    if (block.boardData.Count != expected)
                        Add(IssueSeverity.Warning, block, "BlockData", $"boardData count is {block.boardData.Count}, expected {expected}. OnValidate can normalize this.");

                    bool hasOccupied = false;
                    for (int c = 0; c < block.boardData.Count; c++)
                    {
                        CellData cell = block.boardData[c];
                        if (cell != null && cell.isOccupied)
                        {
                            hasOccupied = true;
                            break;
                        }
                    }

                    if (!hasOccupied)
                        Add(IssueSeverity.Error, block, "BlockData", "Shape has no occupied cells.");
                }

                if ((block.enabledInEndless || block.enabledInArcade) && block.spawnWeight <= 0f)
                    Add(IssueSeverity.Warning, block, "BlockData", "Block is enabled for spawning but spawnWeight is 0.");
            }

            return blockById;
        }

        private HashSet<string> ValidateSpawnStrategies(SmartSpawnStrategy[] strategies)
        {
            HashSet<string> allStrategyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (strategies.Length == 0)
            {
                Add(IssueSeverity.Warning, null, "SpawnStrategy", "No SmartSpawnStrategy assets found. Puzzle provided shape validation will only use BlockData.");
                return allStrategyIds;
            }

            for (int i = 0; i < strategies.Length; i++)
            {
                SmartSpawnStrategy strategy = strategies[i];
                if (strategy == null) continue;

                SerializedObject so = new SerializedObject(strategy);
                HashSet<string> idsInStrategy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int entryCount = 0;

                ValidateStrategyList(strategy, so.FindProperty("_tier1Blocks"), idsInStrategy, allStrategyIds, ref entryCount);
                ValidateStrategyList(strategy, so.FindProperty("_tier2Blocks"), idsInStrategy, allStrategyIds, ref entryCount);
                ValidateStrategyList(strategy, so.FindProperty("_tier3Blocks"), idsInStrategy, allStrategyIds, ref entryCount);

                if (entryCount == 0)
                    Add(IssueSeverity.Error, strategy, "SpawnStrategy", "Strategy has no block entries.");
            }

            return allStrategyIds;
        }

        private void ValidateStrategyList(SmartSpawnStrategy strategy, SerializedProperty list, HashSet<string> idsInStrategy, HashSet<string> allStrategyIds, ref int entryCount)
        {
            if (list == null || !list.isArray) return;

            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                BlockData block = element.objectReferenceValue as BlockData;
                if (block == null)
                {
                    Add(IssueSeverity.Warning, strategy, "SpawnStrategy", $"Null block entry at {list.displayName}[{i}].");
                    continue;
                }

                entryCount++;
                string id = Normalize(block.Id);
                if (string.IsNullOrEmpty(id)) continue;

                if (!idsInStrategy.Add(id))
                    Add(IssueSeverity.Warning, strategy, "SpawnStrategy", $"Block id '{id}' appears more than once in this strategy.");

                allStrategyIds.Add(id);
            }
        }

        private void ValidateLevels(LevelDefinition[] levels, Dictionary<string, BlockData> blockById, HashSet<string> strategyShapeIds)
        {
            Dictionary<string, LevelDefinition> levelById = new Dictionary<string, LevelDefinition>(StringComparer.OrdinalIgnoreCase);

            if (levels.Length == 0)
                Add(IssueSeverity.Warning, null, "LevelDefinition", "No LevelDefinition assets found.");

            for (int i = 0; i < levels.Length; i++)
            {
                LevelDefinition level = levels[i];
                if (level == null) continue;

                string id = Normalize(level.LevelId);
                if (string.IsNullOrEmpty(id))
                    Add(IssueSeverity.Error, level, "LevelDefinition", "Level id is empty.");
                else if (levelById.TryGetValue(id, out LevelDefinition duplicate))
                    Add(IssueSeverity.Error, level, "LevelDefinition", $"Duplicate level id '{id}'. Existing asset: {duplicate.name}.");
                else
                    levelById[id] = level;

                ValidateSerializedBoardSize(level);
                ValidateBoardCells(level);
                ValidateObjective(level, blockById, strategyShapeIds);
                ValidateSpawnProfile(level, blockById, strategyShapeIds);
                ValidateToolRules(level);
            }
        }

        private void ValidateSerializedBoardSize(LevelDefinition level)
        {
            SerializedObject so = new SerializedObject(level);
            int width = so.FindProperty("_boardWidth")?.intValue ?? LevelDefinition.FixedBoardWidth;
            int height = so.FindProperty("_boardHeight")?.intValue ?? LevelDefinition.FixedBoardHeight;
            if (width != LevelDefinition.FixedBoardWidth || height != LevelDefinition.FixedBoardHeight)
                Add(IssueSeverity.Warning, level, "Board", $"Serialized board size is {width}x{height}. Runtime is fixed at {LevelDefinition.FixedBoardWidth}x{LevelDefinition.FixedBoardHeight}. Use Fix Board Sizes.");
        }

        private void ValidateBoardCells(LevelDefinition level)
        {
            IReadOnlyList<BoardCellData> cells = level.BoardCells;
            if (cells == null) return;

            HashSet<Vector2Int> coords = new HashSet<Vector2Int>();
            for (int i = 0; i < cells.Count; i++)
            {
                BoardCellData cell = cells[i];
                if (cell == null)
                {
                    Add(IssueSeverity.Error, level, "Board", $"Board cell at index {i} is null.");
                    continue;
                }

                Vector2Int coord = new Vector2Int(cell.x, cell.y);
                if (coord.x < 0 || coord.x >= LevelDefinition.FixedBoardWidth || coord.y < 0 || coord.y >= LevelDefinition.FixedBoardHeight)
                    Add(IssueSeverity.Error, level, "Board", $"Board cell ({coord.x},{coord.y}) is outside fixed 9x9 board.");

                if (!coords.Add(coord))
                    Add(IssueSeverity.Error, level, "Board", $"Duplicate board cell coordinate ({coord.x},{coord.y}).");
            }
        }

        private void ValidateObjective(LevelDefinition level, Dictionary<string, BlockData> blockById, HashSet<string> strategyShapeIds)
        {
            ObjectiveDefinition objective = level.Objective;
            if (objective == null)
            {
                Add(IssueSeverity.Error, level, "Objective", "ObjectiveDefinition is null.");
                return;
            }

            switch (level.LevelType)
            {
                case ArcadeLevelType.Score:
                    if (objective.targetScore <= 0)
                        Add(IssueSeverity.Error, level, "Objective", "Score level targetScore must be greater than 0.");
                    ValidateTimer(level);
                    break;

                case ArcadeLevelType.Collectable:
                    if (string.IsNullOrWhiteSpace(objective.targetItemId))
                        Add(IssueSeverity.Error, level, "Objective", "Collectable level targetItemId is empty.");
                    if (objective.targetGemCount <= 0)
                        Add(IssueSeverity.Error, level, "Objective", "Collectable level targetGemCount must be greater than 0.");
                    if (!HasBoardItem(level, BoardItemType.Gem))
                        Add(IssueSeverity.Warning, level, "Objective", "Collectable level has no Gem item on the board.");
                    ValidateTimer(level);
                    break;

                case ArcadeLevelType.Shape:
                    if (!HasTargetPattern(level))
                        Add(IssueSeverity.Error, level, "Objective", "Shape level has no target pattern cells.");
                    break;

                case ArcadeLevelType.Puzzle:
                    if (!HasTargetPattern(level))
                        Add(IssueSeverity.Error, level, "Objective", "Puzzle level has no target pattern cells.");
                    ValidatePuzzleShapes(level, objective, blockById, strategyShapeIds);
                    ValidateTimer(level);
                    break;
            }
        }

        private void ValidateToolRules(LevelDefinition level)
        {
            ToolRuleDefinition rules = level.ToolRule;
            if (rules == null)
            {
                Add(IssueSeverity.Warning, level, "Tool Rules", "ToolRuleDefinition is null. LevelDefinition.OnValidate will recreate it.");
                return;
            }

            bool singleAllowed = rules.IsToolAllowed(level.LevelType, GameplayToolType.PlaceSingleCell);
            bool removeAllowed = rules.IsToolAllowed(level.LevelType, GameplayToolType.RemoveSpawnBlock);
            bool bombAllowed = rules.IsToolAllowed(level.LevelType, GameplayToolType.BombSquare);

            if (level.LevelType == ArcadeLevelType.Puzzle && !rules.UseCustomRules)
            {
                Add(IssueSeverity.Info, level, "Tool Rules", "Puzzle level uses default policy: all gameplay tools are disabled.");
                return;
            }

            if (level.LevelType == ArcadeLevelType.Puzzle && (singleAllowed || removeAllowed || bombAllowed))
                Add(IssueSeverity.Warning, level, "Tool Rules", "Puzzle level allows at least one tool. This can break provided-block puzzle fairness unless intentional.");

            if (rules.UseCustomRules && rules.AllowTools && !singleAllowed && !removeAllowed && !bombAllowed)
                Add(IssueSeverity.Warning, level, "Tool Rules", "Custom rules enable the master Allow Tools toggle but disable every individual tool.");
        }

        private void ValidatePuzzleShapes(LevelDefinition level, ObjectiveDefinition objective, Dictionary<string, BlockData> blockById, HashSet<string> strategyShapeIds)
        {
            if (objective.providedShapeIds == null || objective.providedShapeIds.Count == 0)
            {
                Add(IssueSeverity.Error, level, "Puzzle", "Puzzle level has no providedShapeIds.");
                return;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < objective.providedShapeIds.Count; i++)
            {
                string shapeId = Normalize(objective.providedShapeIds[i]);
                if (string.IsNullOrEmpty(shapeId))
                {
                    Add(IssueSeverity.Error, level, "Puzzle", $"providedShapeIds[{i}] is empty.");
                    continue;
                }

                if (!ids.Add(shapeId))
                    Add(IssueSeverity.Warning, level, "Puzzle", $"Duplicate provided shape id '{shapeId}'.");

                if (!blockById.ContainsKey(shapeId))
                    Add(IssueSeverity.Error, level, "Puzzle", $"Provided shape id '{shapeId}' does not exist in BlockData assets.");
                else if (strategyShapeIds.Count > 0 && !strategyShapeIds.Contains(shapeId))
                    Add(IssueSeverity.Warning, level, "Puzzle", $"Provided shape id '{shapeId}' exists but is not present in any SmartSpawnStrategy tier list.");
            }
        }

        private void ValidateTimer(LevelDefinition level)
        {
            TimerRuleDefinition timer = level.TimerRule;
            if (timer == null)
            {
                Add(IssueSeverity.Error, level, "Timer", "TimerRule is null for a timed level.");
                return;
            }

            if (timer.totalTimeSeconds <= 0f)
                Add(IssueSeverity.Error, level, "Timer", "totalTimeSeconds must be greater than 0.");
        }

        private void ValidateSpawnProfile(LevelDefinition level, Dictionary<string, BlockData> blockById, HashSet<string> strategyShapeIds)
        {
            SpawnProfileDefinition profile = level.SpawnProfileOverride;
            if (profile == null)
            {
                Add(IssueSeverity.Warning, level, "SpawnProfile", "SpawnProfileOverride is null.");
                return;
            }

            if (profile.queueSizeOverride <= 0)
                Add(IssueSeverity.Error, level, "SpawnProfile", "queueSizeOverride must be greater than 0.");

            ValidateShapeIdList(level, "allowedShapeIds", profile.allowedShapeIds, blockById, strategyShapeIds);
            ValidateShapeIdList(level, "blockedShapeIds", profile.blockedShapeIds, blockById, strategyShapeIds);
        }

        private void ValidateShapeIdList(LevelDefinition level, string label, List<string> ids, Dictionary<string, BlockData> blockById, HashSet<string> strategyShapeIds)
        {
            if (ids == null) return;

            for (int i = 0; i < ids.Count; i++)
            {
                string shapeId = Normalize(ids[i]);
                if (string.IsNullOrEmpty(shapeId))
                {
                    Add(IssueSeverity.Warning, level, "SpawnProfile", $"{label}[{i}] is empty.");
                    continue;
                }

                if (!blockById.ContainsKey(shapeId))
                    Add(IssueSeverity.Error, level, "SpawnProfile", $"{label} references unknown shape id '{shapeId}'.");
                else if (strategyShapeIds.Count > 0 && !strategyShapeIds.Contains(shapeId))
                    Add(IssueSeverity.Warning, level, "SpawnProfile", $"{label} references '{shapeId}', but it is not present in any SmartSpawnStrategy tier list.");
            }
        }

        private void ValidateMaps(MapDefinition[] maps)
        {
            if (maps.Length == 0)
            {
                Add(IssueSeverity.Warning, null, "Map", "No MapDefinition assets found.");
                return;
            }

            for (int i = 0; i < maps.Length; i++)
            {
                MapDefinition map = maps[i];
                if (map == null) continue;

                if (map.Worlds == null || map.Worlds.Count == 0)
                    Add(IssueSeverity.Warning, map, "Map", "Map has no worlds.");

                ValidateMapWorlds(map);
                ValidateMapNodes(map);
            }
        }

        private void ValidateMapWorlds(MapDefinition map)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IReadOnlyList<MapWorldDefinition> worlds = map.Worlds;
            if (worlds == null) return;

            for (int i = 0; i < worlds.Count; i++)
            {
                MapWorldDefinition world = worlds[i];
                if (world == null)
                {
                    Add(IssueSeverity.Error, map, "Map", $"World at index {i} is null.");
                    continue;
                }

                string id = Normalize(world.worldId);
                if (string.IsNullOrEmpty(id))
                    Add(IssueSeverity.Error, map, "Map", $"World at index {i} has empty worldId.");
                else if (!ids.Add(id))
                    Add(IssueSeverity.Error, map, "Map", $"Duplicate worldId '{id}'.");
            }
        }

        private void ValidateMapNodes(MapDefinition map)
        {
            IReadOnlyList<MapLevelNodeDefinition> nodes = map.Nodes;
            if (nodes == null || nodes.Count == 0)
            {
                Add(IssueSeverity.Warning, map, "Map", "Map has no level nodes.");
                return;
            }

            HashSet<string> nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<LevelDefinition> levels = new HashSet<LevelDefinition>();

            for (int i = 0; i < nodes.Count; i++)
            {
                MapLevelNodeDefinition node = nodes[i];
                if (node == null)
                {
                    Add(IssueSeverity.Error, map, "Map", $"Node at index {i} is null.");
                    continue;
                }

                string nodeId = Normalize(node.NodeId);
                if (string.IsNullOrEmpty(nodeId))
                    Add(IssueSeverity.Error, map, "Map", $"Node at index {i} has empty node id.");
                else if (!nodeIds.Add(nodeId))
                    Add(IssueSeverity.Error, map, "Map", $"Duplicate node id '{nodeId}'.");

                if (node.Level == null)
                    Add(IssueSeverity.Error, map, "Map", $"Node '{nodeId}' has no Level reference.");
                else if (!levels.Add(node.Level))
                    Add(IssueSeverity.Warning, map, "Map", $"Level '{node.Level.LevelId}' is referenced by more than one map node.");
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                MapLevelNodeDefinition node = nodes[i];
                if (node == null || node.RequiredCompletedNodeIds == null) continue;

                for (int r = 0; r < node.RequiredCompletedNodeIds.Count; r++)
                {
                    string requiredId = Normalize(node.RequiredCompletedNodeIds[r]);
                    if (string.IsNullOrEmpty(requiredId))
                        Add(IssueSeverity.Warning, map, "Map", $"Node '{node.NodeId}' has an empty required node id.");
                    else if (!nodeIds.Contains(requiredId))
                        Add(IssueSeverity.Error, map, "Map", $"Node '{node.NodeId}' requires missing node id '{requiredId}'.");
                }
            }
        }

        private static bool HasBoardItem(LevelDefinition level, BoardItemType type)
        {
            IReadOnlyList<BoardCellData> cells = level.BoardCells;
            if (cells == null) return false;

            for (int i = 0; i < cells.Count; i++)
            {
                BoardCellData cell = cells[i];
                if (cell != null && cell.itemType == type)
                    return true;
            }

            return false;
        }

        private static bool HasTargetPattern(LevelDefinition level)
        {
            IReadOnlyList<BoardCellData> cells = level.BoardCells;
            if (cells == null) return false;

            for (int i = 0; i < cells.Count; i++)
            {
                BoardCellData cell = cells[i];
                if (cell == null) continue;
                if (cell.itemType == BoardItemType.TargetPattern || cell.targetPatternFilled)
                    return true;
            }

            return false;
        }

        private void FixBoardSizes()
        {
            LevelDefinition[] levels = LoadAssets<LevelDefinition>();
            for (int i = 0; i < levels.Length; i++)
            {
                LevelDefinition level = levels[i];
                if (level == null) continue;

                SerializedObject so = new SerializedObject(level);
                bool changed = false;

                SerializedProperty width = so.FindProperty("_boardWidth");
                SerializedProperty height = so.FindProperty("_boardHeight");
                SerializedProperty cells = so.FindProperty("_boardCells");

                if (width != null && width.intValue != LevelDefinition.FixedBoardWidth)
                {
                    width.intValue = LevelDefinition.FixedBoardWidth;
                    changed = true;
                }

                if (height != null && height.intValue != LevelDefinition.FixedBoardHeight)
                {
                    height.intValue = LevelDefinition.FixedBoardHeight;
                    changed = true;
                }

                if (cells != null && cells.isArray)
                {
                    for (int c = cells.arraySize - 1; c >= 0; c--)
                    {
                        SerializedProperty cell = cells.GetArrayElementAtIndex(c);
                        int x = cell.FindPropertyRelative("x").intValue;
                        int y = cell.FindPropertyRelative("y").intValue;
                        if (x < 0 || x >= LevelDefinition.FixedBoardWidth || y < 0 || y >= LevelDefinition.FixedBoardHeight)
                        {
                            cells.DeleteArrayElementAtIndex(c);
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(level);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static T[] LoadAssets<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            List<T> assets = new List<T>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    assets.Add(asset);
            }

            return assets.ToArray();
        }

        private void Add(IssueSeverity severity, UnityEngine.Object target, string category, string message)
        {
            _issues.Add(new Issue
            {
                Severity = severity,
                Target = target,
                Category = category,
                Message = message
            });
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
