using System;
using System.Collections.Generic;
using _Game.Scripts.Data;
using _Game.Scripts.Logic;
using _Game.Scripts.Modes.Levels;
using _Game.Scripts.Modes.Map;
using UnityEditor;
using UnityEngine;

namespace _Game.Editor
{
    public class ArcadeDesignerWindow : EditorWindow
    {
        private enum DesignerSection
        {
            Overview,
            Levels,
            ShapeBlocks,
            SpawnStrategy,
            Workflow
        }

        private DesignerSection _section = DesignerSection.Overview;
        private Vector2 _scroll;
        private UnityEngine.Object _selectedSpawnStrategy;
        private UnityEditor.Editor _spawnStrategyEditor;

        [MenuItem("Tools/Block Blast/Arcade Designer")]
        public static void Open()
        {
            ArcadeDesignerWindow window = GetWindow<ArcadeDesignerWindow>("Arcade Designer");
            window.minSize = new Vector2(980f, 560f);
            window.Show();
        }

        private void OnDisable()
        {
            if (_spawnStrategyEditor != null)
                DestroyImmediate(_spawnStrategyEditor);
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSidebar();
                DrawMainPanel();
            }
        }

        private void DrawSidebar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(190f), GUILayout.ExpandHeight(true)))
            {
                EditorGUILayout.LabelField("Arcade Designer", EditorStyles.boldLabel);
                EditorGUILayout.Space(4f);

                DrawSectionButton(DesignerSection.Overview, "Overview");
                DrawSectionButton(DesignerSection.Levels, "Levels");
                DrawSectionButton(DesignerSection.ShapeBlocks, "Shape Blocks");
                DrawSectionButton(DesignerSection.SpawnStrategy, "Spawn Strategy");
                DrawSectionButton(DesignerSection.Workflow, "Workflow");

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Open Level Browser"))
                    ArcadeLevelBrowserWindow.Open();

                if (GUILayout.Button("Open Shape Browser"))
                    BlockShapeBrowserWindow.Open();
            }
        }

        private void DrawSectionButton(DesignerSection section, string label)
        {
            bool selected = _section == section;
            Color oldColor = GUI.backgroundColor;
            if (selected)
                GUI.backgroundColor = new Color(0.36f, 0.58f, 1f, 1f);

            if (GUILayout.Button(label, GUILayout.Height(28f)))
            {
                _section = section;
                _scroll = Vector2.zero;
            }

            GUI.backgroundColor = oldColor;
        }

        private void DrawMainPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll);

                switch (_section)
                {
                    case DesignerSection.Overview:
                        DrawOverview();
                        break;
                    case DesignerSection.Levels:
                        DrawLevels();
                        break;
                    case DesignerSection.ShapeBlocks:
                        DrawShapeBlocks();
                        break;
                    case DesignerSection.SpawnStrategy:
                        DrawSpawnStrategy();
                        break;
                    case DesignerSection.Workflow:
                        DrawWorkflow();
                        break;
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawOverview()
        {
            List<LevelDefinition> levels = LoadAssets<LevelDefinition>();
            List<BlockData> shapes = LoadAssets<BlockData>();
            List<SmartSpawnStrategy> strategies = LoadAssets<SmartSpawnStrategy>();
            List<MapDefinition> maps = LoadAssets<MapDefinition>();

            DrawHeader("Project Snapshot");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Levels: {levels.Count}");
                EditorGUILayout.LabelField($"Shape Blocks: {shapes.Count}");
                EditorGUILayout.LabelField($"Spawn Strategies: {strategies.Count}");
                EditorGUILayout.LabelField($"Maps: {maps.Count}");
            }

            DrawHeader("Level Types");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (ArcadeLevelType type in System.Enum.GetValues(typeof(ArcadeLevelType)))
                    EditorGUILayout.LabelField($"{type}: {CountLevelsOfType(levels, type)}");
            }

            DrawHeader("Design Checks");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                int duplicateLevelIds = CountDuplicateLevelIds(levels);
                int duplicateOrderIndexes = CountDuplicateOrderIndexes(levels);
                int mapNodesWithoutLevels = CountMapNodesWithoutLevels(maps);
                int duplicateMapNodeIds = CountDuplicateMapNodeIds(maps);
                int brokenMapRequirements = CountBrokenMapRequirements(maps);
                int missingSpawnProfileId = CountMissingSpawnProfileIds(levels);
                int unknownSpawnProfileShapes = CountSpawnProfilesWithUnknownShapeIds(levels, shapes);
                int timedLevelsWithoutAutoStart = CountTimedLevelsWithoutAutoStart(levels);
                int collectableUnderTarget = CountCollectableLevelsUnderTargetAmount(levels);
                int puzzleWithoutBlocks = CountPuzzleLevelsWithoutBlocks(levels);
                int puzzleWithUnknownShapes = CountPuzzleLevelsWithUnknownProvidedShapes(levels, shapes);
                int puzzleWithDisabledShapes = CountPuzzleLevelsWithDisabledArcadeShapes(levels, shapes);
                int puzzleWithoutMask = CountPuzzleLevelsWithoutMask(levels);
                int shapeWithoutMask = CountShapeLevelsWithoutMask(levels);

                DrawCheckLine(duplicateLevelIds == 0, $"{duplicateLevelIds} duplicate level ids");
                DrawCheckLine(duplicateOrderIndexes == 0, $"{duplicateOrderIndexes} duplicate level order indexes within the same world");
                DrawCheckLine(mapNodesWithoutLevels == 0, $"{mapNodesWithoutLevels} map nodes without level references");
                DrawCheckLine(duplicateMapNodeIds == 0, $"{duplicateMapNodeIds} duplicate map node ids");
                DrawCheckLine(brokenMapRequirements == 0, $"{brokenMapRequirements} map node requirements point to missing node ids");
                DrawCheckLine(missingSpawnProfileId == 0, $"{missingSpawnProfileId} levels missing spawn profile id");
                DrawCheckLine(unknownSpawnProfileShapes == 0, $"{unknownSpawnProfileShapes} levels reference missing shape ids in spawn profiles");
                DrawCheckLine(timedLevelsWithoutAutoStart == 0, $"{timedLevelsWithoutAutoStart} timed levels have timer auto-start disabled");
                DrawCheckLine(collectableUnderTarget == 0, $"{collectableUnderTarget} Collectable levels have fewer board items than target count");
                DrawCheckLine(puzzleWithoutBlocks == 0, $"{puzzleWithoutBlocks} Puzzle levels without provided blocks");
                DrawCheckLine(puzzleWithUnknownShapes == 0, $"{puzzleWithUnknownShapes} Puzzle levels reference missing provided shape ids");
                DrawCheckLine(puzzleWithDisabledShapes == 0, $"{puzzleWithDisabledShapes} Puzzle levels use shapes disabled for Arcade");
                DrawCheckLine(puzzleWithoutMask == 0, $"{puzzleWithoutMask} Puzzle levels without target mask cells");
                DrawCheckLine(shapeWithoutMask == 0, $"{shapeWithoutMask} Shape levels without target mask cells");
            }
        }

        private void DrawLevels()
        {
            DrawHeader("Levels");
            EditorGUILayout.HelpBox("Use this section for level order, map nodes, objective data, board masks, and per-level spawn profiles.", MessageType.Info);

            if (GUILayout.Button("Open Arcade Level Browser", GUILayout.Height(32f)))
                ArcadeLevelBrowserWindow.Open();

            DrawLevelSummary();
        }

        private void DrawShapeBlocks()
        {
            DrawHeader("Shape Blocks");
            EditorGUILayout.HelpBox("Use this section to create block shapes, spawn metadata, tiers, tags, and rotation flags.", MessageType.Info);

            if (GUILayout.Button("Open Shape Browser", GUILayout.Height(32f)))
                BlockShapeBrowserWindow.Open();

            List<BlockData> shapes = LoadAssets<BlockData>();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Total Shapes: {shapes.Count}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Arcade Enabled: {CountArcadeEnabledShapes(shapes)}");
            }
        }

        private void DrawSpawnStrategy()
        {
            DrawHeader("Spawn Strategy");
            EditorGUILayout.HelpBox("Global SmartSpawnStrategy assets define the shape database. Each LevelDefinition owns a Level Spawn Profile override in the Level Browser.", MessageType.Info);

            List<SmartSpawnStrategy> strategies = LoadAssets<SmartSpawnStrategy>();
            if (strategies.Count == 0)
            {
                EditorGUILayout.HelpBox("No SmartSpawnStrategy assets found.", MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(300f)))
                {
                    EditorGUILayout.LabelField("Strategy Assets", EditorStyles.boldLabel);
                    for (int i = 0; i < strategies.Count; i++)
                    {
                        SmartSpawnStrategy strategy = strategies[i];
                        bool selected = strategy == _selectedSpawnStrategy;
                        Color oldColor = GUI.backgroundColor;
                        if (selected)
                            GUI.backgroundColor = new Color(0.36f, 0.58f, 1f, 1f);

                        if (GUILayout.Button(strategy.name, EditorStyles.miniButton))
                            SelectSpawnStrategy(strategy);

                        GUI.backgroundColor = oldColor;
                    }
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (_selectedSpawnStrategy == null)
                        SelectSpawnStrategy(strategies[0]);

                    if (_selectedSpawnStrategy == null)
                        return;

                    EditorGUILayout.ObjectField("Selected", _selectedSpawnStrategy, typeof(SmartSpawnStrategy), false);
                    EnsureSpawnStrategyEditor();
                    _spawnStrategyEditor?.OnInspectorGUI();
                }
            }
        }

        private void DrawWorkflow()
        {
            DrawHeader("Arcade Level Workflow");
            DrawWorkflowStep("1. Shape Blocks", "Create reusable BlockData shapes, set tier, tags, rotation, and arcade availability.");
            DrawWorkflowStep("2. Spawn Strategy", "Assign shape assets to SmartSpawnStrategy tiers so runtime spawn can resolve ids and filters.");
            DrawWorkflowStep("3. Level Definition", "Pick Score, Collectable, Shape, or Puzzle; paint board markers; configure objective and timer rules for timed types.");
            DrawWorkflowStep("4. Level Spawn Profile", "Set per-level queue size, allowed/blocked shape ids, tag filters, and difficulty bias.");
            DrawWorkflowStep("5. Map Nodes", "Append or rebuild map nodes, sync node ids, then playtest from the Arcade map.");
        }

        private void DrawLevelSummary()
        {
            List<LevelDefinition> levels = LoadAssets<LevelDefinition>();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Total Levels: {levels.Count}", EditorStyles.boldLabel);
                foreach (ArcadeLevelType type in System.Enum.GetValues(typeof(ArcadeLevelType)))
                    EditorGUILayout.LabelField($"{type}: {CountLevelsOfType(levels, type)}");
            }
        }

        private void SelectSpawnStrategy(SmartSpawnStrategy strategy)
        {
            if (_selectedSpawnStrategy == strategy) return;

            _selectedSpawnStrategy = strategy;
            if (_spawnStrategyEditor != null)
            {
                DestroyImmediate(_spawnStrategyEditor);
                _spawnStrategyEditor = null;
            }
        }

        private void EnsureSpawnStrategyEditor()
        {
            if (_selectedSpawnStrategy == null) return;
            if (_spawnStrategyEditor != null && _spawnStrategyEditor.target == _selectedSpawnStrategy) return;

            if (_spawnStrategyEditor != null)
                DestroyImmediate(_spawnStrategyEditor);

            _spawnStrategyEditor = UnityEditor.Editor.CreateEditor(_selectedSpawnStrategy);
        }

        private static void DrawWorkflowStep(string title, string body)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(body, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private static void DrawCheckLine(bool pass, string text)
        {
            MessageType type = pass ? MessageType.None : MessageType.Warning;
            if (pass)
                EditorGUILayout.LabelField($"OK - {text}", EditorStyles.miniLabel);
            else
                EditorGUILayout.HelpBox(text, type);
        }

        private static void DrawHeader(string label)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        private static List<T> LoadAssets<T>() where T : UnityEngine.Object
        {
            List<T> assets = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    assets.Add(asset);
            }

            assets.Sort((a, b) => string.Compare(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b), System.StringComparison.OrdinalIgnoreCase));
            return assets;
        }

        private static int CountLevelsOfType(List<LevelDefinition> levels, ArcadeLevelType type)
        {
            int count = 0;
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i] != null && levels[i].LevelType == type)
                    count++;
            }

            return count;
        }

        private static int CountDuplicateLevelIds(List<LevelDefinition> levels)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int duplicateCount = 0;

            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];
                if (level == null) continue;

                string levelId = NormalizeId(level.LevelId);
                if (string.IsNullOrEmpty(levelId)) continue;

                if (!seen.Add(levelId))
                    duplicateCount++;
            }

            return duplicateCount;
        }

        private static int CountDuplicateOrderIndexes(List<LevelDefinition> levels)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int duplicateCount = 0;

            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];
                if (level == null) continue;

                string worldId = NormalizeId(level.WorldId);
                string key = $"{worldId}:{level.OrderIndex}";
                if (!seen.Add(key))
                    duplicateCount++;
            }

            return duplicateCount;
        }

        private static int CountMapNodesWithoutLevels(List<MapDefinition> maps)
        {
            int count = 0;

            for (int i = 0; i < maps.Count; i++)
            {
                MapDefinition map = maps[i];
                if (map == null || map.Nodes == null) continue;

                for (int j = 0; j < map.Nodes.Count; j++)
                {
                    MapLevelNodeDefinition node = map.Nodes[j];
                    if (node == null || node.Level == null)
                        count++;
                }
            }

            return count;
        }

        private static int CountDuplicateMapNodeIds(List<MapDefinition> maps)
        {
            int duplicateCount = 0;

            for (int i = 0; i < maps.Count; i++)
            {
                MapDefinition map = maps[i];
                if (map == null || map.Nodes == null) continue;

                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int j = 0; j < map.Nodes.Count; j++)
                {
                    MapLevelNodeDefinition node = map.Nodes[j];
                    string nodeId = node != null ? NormalizeId(node.NodeId) : string.Empty;
                    if (string.IsNullOrEmpty(nodeId)) continue;

                    if (!seen.Add(nodeId))
                        duplicateCount++;
                }
            }

            return duplicateCount;
        }

        private static int CountBrokenMapRequirements(List<MapDefinition> maps)
        {
            int brokenCount = 0;

            for (int i = 0; i < maps.Count; i++)
            {
                MapDefinition map = maps[i];
                if (map == null || map.Nodes == null) continue;

                HashSet<string> nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int j = 0; j < map.Nodes.Count; j++)
                {
                    MapLevelNodeDefinition node = map.Nodes[j];
                    string nodeId = node != null ? NormalizeId(node.NodeId) : string.Empty;
                    if (!string.IsNullOrEmpty(nodeId))
                        nodeIds.Add(nodeId);
                }

                for (int j = 0; j < map.Nodes.Count; j++)
                {
                    MapLevelNodeDefinition node = map.Nodes[j];
                    if (node == null || node.RequiredCompletedNodeIds == null) continue;

                    for (int k = 0; k < node.RequiredCompletedNodeIds.Count; k++)
                    {
                        string requiredId = NormalizeId(node.RequiredCompletedNodeIds[k]);
                        if (!string.IsNullOrEmpty(requiredId) && !nodeIds.Contains(requiredId))
                            brokenCount++;
                    }
                }
            }

            return brokenCount;
        }

        private static int CountMissingSpawnProfileIds(List<LevelDefinition> levels)
        {
            int count = 0;
            for (int i = 0; i < levels.Count; i++)
            {
                SpawnProfileDefinition profile = levels[i] != null ? levels[i].SpawnProfileOverride : null;
                if (profile == null || string.IsNullOrWhiteSpace(profile.profileId))
                    count++;
            }

            return count;
        }

        private static int CountPuzzleLevelsWithoutBlocks(List<LevelDefinition> levels)
        {
            int count = 0;
            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];
                if (level == null || level.LevelType != ArcadeLevelType.Puzzle) continue;

                ObjectiveDefinition objective = level.Objective;
                if (objective == null || objective.providedShapeIds == null || objective.providedShapeIds.Count == 0)
                    count++;
            }

            return count;
        }

        private static int CountPuzzleLevelsWithUnknownProvidedShapes(List<LevelDefinition> levels, List<BlockData> shapes)
        {
            HashSet<string> shapeIds = BuildShapeIdSet(shapes);
            int count = 0;

            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];
                if (level == null || level.LevelType != ArcadeLevelType.Puzzle) continue;

                ObjectiveDefinition objective = level.Objective;
                if (objective == null || HasUnknownShapeId(objective.providedShapeIds, shapeIds))
                    count++;
            }

            return count;
        }

        private static int CountPuzzleLevelsWithDisabledArcadeShapes(List<LevelDefinition> levels, List<BlockData> shapes)
        {
            Dictionary<string, BlockData> shapeById = BuildShapeLookup(shapes);
            int count = 0;

            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];
                if (level == null || level.LevelType != ArcadeLevelType.Puzzle) continue;

                ObjectiveDefinition objective = level.Objective;
                if (objective == null || objective.providedShapeIds == null) continue;

                bool hasDisabledShape = false;
                for (int j = 0; j < objective.providedShapeIds.Count; j++)
                {
                    string shapeId = NormalizeId(objective.providedShapeIds[j]);
                    if (string.IsNullOrEmpty(shapeId)) continue;

                    if (shapeById.TryGetValue(shapeId, out BlockData shape) && shape != null && !shape.enabledInArcade)
                    {
                        hasDisabledShape = true;
                        break;
                    }
                }

                if (hasDisabledShape)
                    count++;
            }

            return count;
        }

        private static int CountSpawnProfilesWithUnknownShapeIds(List<LevelDefinition> levels, List<BlockData> shapes)
        {
            HashSet<string> shapeIds = BuildShapeIdSet(shapes);
            int count = 0;

            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];
                SpawnProfileDefinition profile = level != null ? level.SpawnProfileOverride : null;
                if (profile == null) continue;

                if (HasUnknownShapeId(profile.allowedShapeIds, shapeIds) || HasUnknownShapeId(profile.blockedShapeIds, shapeIds))
                    count++;
            }

            return count;
        }

        private static int CountTimedLevelsWithoutAutoStart(List<LevelDefinition> levels)
        {
            int count = 0;
            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];
                if (level == null || !level.UsesTimer) continue;

                TimerRuleDefinition timer = level.TimerRule;
                if (timer == null || !timer.startTimerOnSessionStart)
                    count++;
            }

            return count;
        }

        private static int CountCollectableLevelsUnderTargetAmount(List<LevelDefinition> levels)
        {
            int count = 0;
            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];
                if (level == null || level.LevelType != ArcadeLevelType.Collectable) continue;

                int targetAmount = level.Objective != null ? Mathf.Max(1, level.Objective.targetGemCount) : 1;
                if (CountCollectableTargetItems(level) < targetAmount)
                    count++;
            }

            return count;
        }

        private static int CountPuzzleLevelsWithoutMask(List<LevelDefinition> levels)
        {
            int count = 0;
            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];
                if (level == null || level.LevelType != ArcadeLevelType.Puzzle) continue;
                if (!HasTargetMask(level))
                    count++;
            }

            return count;
        }

        private static int CountShapeLevelsWithoutMask(List<LevelDefinition> levels)
        {
            int count = 0;
            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];
                if (level == null || level.LevelType != ArcadeLevelType.Shape) continue;
                if (!HasTargetMask(level))
                    count++;
            }

            return count;
        }

        private static bool HasTargetMask(LevelDefinition level)
        {
            if (level == null || level.BoardCells == null) return false;
            for (int i = 0; i < level.BoardCells.Count; i++)
            {
                BoardCellData cell = level.BoardCells[i];
                if (cell != null && cell.targetPatternFilled)
                    return true;
            }

            return false;
        }

        private static int CountCollectableTargetItems(LevelDefinition level)
        {
            if (level == null || level.BoardCells == null) return 0;

            string targetItemId = level.Objective != null && !string.IsNullOrWhiteSpace(level.Objective.targetItemId)
                ? level.Objective.targetItemId.Trim()
                : "gem";

            int count = 0;
            for (int i = 0; i < level.BoardCells.Count; i++)
            {
                BoardCellData cell = level.BoardCells[i];
                if (cell == null) continue;
                if (cell.itemType == BoardItemType.Gem)
                {
                    count++;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(cell.itemId)
                    && string.Equals(cell.itemId.Trim(), targetItemId, System.StringComparison.OrdinalIgnoreCase))
                    count++;
            }

            return count;
        }

        private static HashSet<string> BuildShapeIdSet(List<BlockData> shapes)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (shapes == null) return ids;

            for (int i = 0; i < shapes.Count; i++)
            {
                BlockData shape = shapes[i];
                string shapeId = shape != null ? NormalizeId(shape.Id) : string.Empty;
                if (!string.IsNullOrEmpty(shapeId))
                    ids.Add(shapeId);
            }

            return ids;
        }

        private static Dictionary<string, BlockData> BuildShapeLookup(List<BlockData> shapes)
        {
            Dictionary<string, BlockData> lookup = new Dictionary<string, BlockData>(StringComparer.OrdinalIgnoreCase);
            if (shapes == null) return lookup;

            for (int i = 0; i < shapes.Count; i++)
            {
                BlockData shape = shapes[i];
                string shapeId = shape != null ? NormalizeId(shape.Id) : string.Empty;
                if (!string.IsNullOrEmpty(shapeId) && !lookup.ContainsKey(shapeId))
                    lookup.Add(shapeId, shape);
            }

            return lookup;
        }

        private static bool HasUnknownShapeId(List<string> ids, HashSet<string> knownShapeIds)
        {
            if (ids == null) return false;
            knownShapeIds ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < ids.Count; i++)
            {
                string shapeId = NormalizeId(ids[i]);
                if (!string.IsNullOrEmpty(shapeId) && !knownShapeIds.Contains(shapeId))
                    return true;
            }

            return false;
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static int CountArcadeEnabledShapes(List<BlockData> shapes)
        {
            int count = 0;
            for (int i = 0; i < shapes.Count; i++)
            {
                if (shapes[i] != null && shapes[i].enabledInArcade)
                    count++;
            }

            return count;
        }
    }
}
