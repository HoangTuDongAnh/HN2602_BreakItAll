using System.Collections.Generic;
using System.IO;
using _Game.Scripts.Data;
using _Game.Scripts.Modes.Levels;
using _Game.Scripts.Modes.Map;
using UnityEditor;
using UnityEngine;

namespace _Game.Editor
{
    public class ArcadeLevelBrowserWindow : EditorWindow
    {
        private const string DefaultRootPath = "Assets/_Game/Data/Arcade";
        private const string DefaultMapPath = "Assets/_Game/Data/Arcade/Map_Arcade.asset";
        private const int RuntimeBoardSize = 9;
        private const float CellSize = 34f;
        private const float CellGap = 3f;

        private readonly List<LevelAssetInfo> _levelAssets = new List<LevelAssetInfo>();
        private readonly Dictionary<string, bool> _folderFoldouts = new Dictionary<string, bool>();

        private enum LevelLabelMode
        {
            FileName,
            Id,
            DisplayName,
            Type
        }

        private enum BoardPaintMode
        {
            Occupied,
            Gem,
            TimeBonus,
            Target,
            Clear
        }

        private DefaultAsset _rootFolder;
        private string _rootPath = DefaultRootPath;
        private string _searchText = string.Empty;
        private LevelLabelMode _labelMode = LevelLabelMode.FileName;
        private BoardPaintMode _paintMode = BoardPaintMode.Target;
        private MapDefinition _mapDefinition;
        private LevelDefinition _selectedLevel;
        private Vector2 _leftScroll;
        private Vector2 _mapScroll;
        private Vector2 _rightScroll;
        private bool _showSpawnProfile;

        private static readonly string[] LabelModeOptions = { "File", "Id", "Display", "Type" };
        private static readonly string[] PaintModeOptions = { "Block", "Gem", "Time", "Target", "Clear" };

        [MenuItem("Tools/Block Blast/Arcade Level Browser")]
        public static void Open()
        {
            ArcadeLevelBrowserWindow window = GetWindow<ArcadeLevelBrowserWindow>("Arcade Levels");
            window.minSize = new Vector2(1160f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            if (_rootFolder == null)
                _rootFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(_rootPath);

            if (_mapDefinition == null)
                _mapDefinition = AssetDatabase.LoadAssetAtPath<MapDefinition>(DefaultMapPath);

            RefreshAssets();
        }

        private void OnGUI()
        {
            DrawToolbar();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawLevelList();
                DrawMapPanel();
                DrawSelectedLevelEditor();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    _rootFolder = (DefaultAsset)EditorGUILayout.ObjectField("Root Folder", _rootFolder, typeof(DefaultAsset), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        string path = AssetDatabase.GetAssetPath(_rootFolder);
                        if (AssetDatabase.IsValidFolder(path))
                            _rootPath = path;

                        RefreshAssets();
                    }

                    if (GUILayout.Button("Browse", GUILayout.Width(78f)))
                        BrowseForFolder();

                    if (GUILayout.Button("Refresh", GUILayout.Width(78f)))
                        RefreshAssets();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _mapDefinition = (MapDefinition)EditorGUILayout.ObjectField("Map", _mapDefinition, typeof(MapDefinition), false);
                    _searchText = EditorGUILayout.TextField("Search", _searchText, GUILayout.MinWidth(180f));

                    if (GUILayout.Button("Create Level", GUILayout.Width(110f)))
                        CreateLevel();
                }
            }
        }

        private void DrawLevelList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(300f)))
            {
                EditorGUILayout.LabelField($"Levels ({GetFilteredCount()})", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Show", GUILayout.Width(36f));
                    _labelMode = (LevelLabelMode)GUILayout.Toolbar((int)_labelMode, LabelModeOptions);
                }

                _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, EditorStyles.helpBox);

                string currentFolder = null;
                for (int i = 0; i < _levelAssets.Count; i++)
                {
                    LevelAssetInfo info = _levelAssets[i];
                    if (info.Asset == null || !MatchesSearch(info.Asset, info.Path))
                        continue;

                    if (currentFolder != info.Folder)
                    {
                        currentFolder = info.Folder;
                        DrawFolderHeader(currentFolder);
                    }

                    if (!IsFolderOpen(currentFolder))
                        continue;

                    DrawLevelRow(info);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawMapPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(330f)))
            {
                EditorGUILayout.LabelField("Map Nodes", EditorStyles.boldLabel);

                if (_mapDefinition == null)
                {
                    EditorGUILayout.HelpBox("Assign a MapDefinition asset.", MessageType.Info);
                    return;
                }

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    bool canAppend = _selectedLevel != null && !MapContainsLevel(_selectedLevel);
                    using (new EditorGUI.DisabledScope(!canAppend))
                    {
                        if (GUILayout.Button("Append Selected", EditorStyles.toolbarButton))
                            AppendSelectedLevelToMap();
                    }

                    if (GUILayout.Button("Rebuild From List", EditorStyles.toolbarButton))
                        RebuildMapFromLevelList();

                    if (GUILayout.Button("Sync IDs", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                        SyncMapNodeIds();
                }

                SerializedObject serializedMap = new SerializedObject(_mapDefinition);
                serializedMap.UpdateIfRequiredOrScript();
                SerializedProperty nodes = serializedMap.FindProperty("_nodes");

                _mapScroll = EditorGUILayout.BeginScrollView(_mapScroll, EditorStyles.helpBox);
                for (int i = 0; i < nodes.arraySize; i++)
                    DrawMapNodeRow(nodes, i);
                EditorGUILayout.EndScrollView();

                if (serializedMap.ApplyModifiedProperties())
                    EditorUtility.SetDirty(_mapDefinition);
            }
        }

        private void DrawSelectedLevelEditor()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (_selectedLevel == null)
                {
                    EditorGUILayout.HelpBox("Select a LevelDefinition asset from the left panel.", MessageType.Info);
                    return;
                }

                DrawSelectedToolbar();

                SerializedObject serializedLevel = new SerializedObject(_selectedLevel);
                serializedLevel.UpdateIfRequiredOrScript();

                _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
                DrawIdentity(serializedLevel);
                DrawObjective(serializedLevel);
                DrawBoard(serializedLevel);
                DrawSpawnAndRewards(serializedLevel);
                EditorGUILayout.EndScrollView();

                if (serializedLevel.ApplyModifiedProperties())
                    EditorUtility.SetDirty(_selectedLevel);
            }
        }

        private void DrawSelectedToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField(_selectedLevel.name, EditorStyles.boldLabel);

                if (GUILayout.Button("Select", EditorStyles.toolbarButton, GUILayout.Width(62f)))
                    Selection.activeObject = _selectedLevel;

                if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(52f)))
                    EditorGUIUtility.PingObject(_selectedLevel);

                if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton, GUILayout.Width(78f)))
                    DuplicateSelectedLevel();

                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(52f)))
                {
                    EditorUtility.SetDirty(_selectedLevel);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        private void DrawIdentity(SerializedObject serializedLevel)
        {
            DrawHeader("Identity");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(serializedLevel.FindProperty("_levelId"));
                EditorGUILayout.PropertyField(serializedLevel.FindProperty("_displayName"));
                EditorGUILayout.PropertyField(serializedLevel.FindProperty("_orderIndex"));
                EditorGUILayout.PropertyField(serializedLevel.FindProperty("_worldId"));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Sync From File", EditorStyles.miniButtonLeft))
                        SyncIdentityFromFile(serializedLevel);

                    if (GUILayout.Button("Set 9x9", EditorStyles.miniButtonRight))
                        SetBoardSize(serializedLevel, RuntimeBoardSize, RuntimeBoardSize);
                }
            }
        }

        private void DrawObjective(SerializedObject serializedLevel)
        {
            DrawHeader("Level Type");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                SerializedProperty typeProperty = serializedLevel.FindProperty("_levelType");
                EditorGUILayout.PropertyField(typeProperty);

                SerializedProperty objective = serializedLevel.FindProperty("_objectiveDefinition");
                SerializedProperty timer = serializedLevel.FindProperty("_timerRule");
                ArcadeLevelType levelType = (ArcadeLevelType)typeProperty.intValue;

                switch (levelType)
                {
                    case ArcadeLevelType.Timed:
                        DrawPropertyIfFound(objective, "targetScore");
                        DrawPropertyIfFound(objective, "bonusTimeSeconds");
                        EditorGUILayout.Space(4f);
                        EditorGUILayout.PropertyField(timer.FindPropertyRelative("totalTimeSeconds"));
                        EditorGUILayout.PropertyField(timer.FindPropertyRelative("failWhenTimeEnds"));
                        EditorGUILayout.PropertyField(timer.FindPropertyRelative("startTimerOnSessionStart"));
                        break;

                    case ArcadeLevelType.Collectable:
                        DrawPropertyIfFound(objective, "targetItemId");
                        DrawPropertyIfFound(objective, "targetGemCount");
                        break;

                    case ArcadeLevelType.Shape:
                        DrawPropertyIfFound(objective, "targetPatternId");
                        DrawPropertyIfFound(objective, "helperToolCount");
                        DrawPropertyIfFound(objective, "requireExactTargetShape");
                        break;

                    case ArcadeLevelType.Fill:
                        DrawPropertyIfFound(objective, "providedShapeIds", includeChildren: true);
                        DrawPropertyIfFound(objective, "allowRotation");
                        DrawPropertyIfFound(objective, "allowMovePlacedBlocks");
                        DrawPropertyIfFound(objective, "requireUseAllProvidedShapes");
                        break;

                    default:
                        EditorGUILayout.HelpBox("This level has an invalid type value. Pick a type above to fix it.", MessageType.Warning);
                        break;
                }
            }
        }

        private void DrawBoard(SerializedObject serializedLevel)
        {
            DrawHeader("Board 9x9");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                SerializedProperty width = serializedLevel.FindProperty("_boardWidth");
                SerializedProperty height = serializedLevel.FindProperty("_boardHeight");
                SerializedProperty cells = serializedLevel.FindProperty("_boardCells");

                using (new EditorGUILayout.HorizontalScope())
                {
                    width.intValue = EditorGUILayout.IntSlider("Width", width.intValue, 1, RuntimeBoardSize);
                    height.intValue = EditorGUILayout.IntSlider("Height", height.intValue, 1, RuntimeBoardSize);
                }

                if (width.intValue != RuntimeBoardSize || height.intValue != RuntimeBoardSize)
                    EditorGUILayout.HelpBox("Runtime GridManager is fixed at 9x9. Use Set 9x9 before testing this level.", MessageType.Warning);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Paint", GUILayout.Width(42f));
                    _paintMode = (BoardPaintMode)GUILayout.Toolbar((int)_paintMode, PaintModeOptions);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Clear Board", EditorStyles.miniButtonLeft))
                        ClearBoardCells(cells);

                    if (GUILayout.Button("Clear Items", EditorStyles.miniButtonMid))
                        ClearItems(cells);

                    if (GUILayout.Button("Heart Target", EditorStyles.miniButtonRight))
                        StampHeartTarget(cells, width.intValue, height.intValue);
                }

                EditorGUILayout.Space(8f);
                DrawBoardGrid(cells, width.intValue, height.intValue);
                DrawBoardSummary(cells);
            }
        }

        private void DrawSpawnAndRewards(SerializedObject serializedLevel)
        {
            DrawHeader("Spawn & Rewards");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showSpawnProfile = EditorGUILayout.Foldout(_showSpawnProfile, "Spawn Profile Override", true);
                if (_showSpawnProfile)
                    EditorGUILayout.PropertyField(serializedLevel.FindProperty("_spawnProfileOverride"), includeChildren: true);

                EditorGUILayout.PropertyField(serializedLevel.FindProperty("_rewardCoins"));
            }
        }

        private void DrawBoardGrid(SerializedProperty cells, int width, int height)
        {
            width = Mathf.Clamp(width, 1, RuntimeBoardSize);
            height = Mathf.Clamp(height, 1, RuntimeBoardSize);

            for (int y = height - 1; y >= 0; y--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    for (int x = 0; x < width; x++)
                    {
                        Rect rect = GUILayoutUtility.GetRect(CellSize, CellSize, GUILayout.Width(CellSize), GUILayout.Height(CellSize));
                        DrawBoardCell(rect, cells, x, y);

                        if (x < width - 1)
                            GUILayout.Space(CellGap);
                    }

                    GUILayout.FlexibleSpace();
                }

                if (y > 0)
                    GUILayout.Space(CellGap);
            }
        }

        private void DrawBoardCell(Rect rect, SerializedProperty cells, int x, int y)
        {
            SerializedProperty cell = FindCell(cells, x, y, out int index);
            CellPreview preview = ReadCellPreview(cell);

            EditorGUI.DrawRect(rect, ((x + y) & 1) == 0 ? new Color(0.18f, 0.21f, 0.25f) : new Color(0.22f, 0.25f, 0.29f));

            if (preview.IsTarget)
                EditorGUI.DrawRect(Inset(rect, 2f), new Color(0.12f, 0.5f, 1f, 0.75f));

            if (preview.IsOccupied)
                EditorGUI.DrawRect(Inset(rect, 5f), new Color(1f, 0.74f, 0.16f, 1f));

            if (preview.ItemType == BoardItemType.Gem)
                DrawCellBadge(rect, "G", new Color(0.14f, 0.9f, 0.35f, 1f));
            else if (preview.ItemType == BoardItemType.TimeBonus)
                DrawCellBadge(rect, "+", new Color(0.1f, 0.86f, 1f, 1f));
            else if (preview.ItemType == BoardItemType.TargetPattern && !preview.IsTarget)
                DrawCellBadge(rect, "T", new Color(0.12f, 0.5f, 1f, 1f));

            Handles.color = new Color(0f, 0f, 0f, 0.55f);
            Handles.DrawSolidRectangleWithOutline(rect, Color.clear, new Color(0f, 0f, 0f, 0.55f));

            HandleCellInput(cells, x, y, rect, index);
        }

        private void HandleCellInput(SerializedProperty cells, int x, int y, Rect rect, int existingIndex)
        {
            Event current = Event.current;
            if (current.type != EventType.MouseDown || !rect.Contains(current.mousePosition))
                return;

            if (current.button == 1 || _paintMode == BoardPaintMode.Clear)
                RemoveCell(cells, existingIndex);
            else if (current.button == 0)
                PaintCell(cells, x, y, _paintMode);
            else
                return;

            GUI.changed = true;
            current.Use();
        }

        private void PaintCell(SerializedProperty cells, int x, int y, BoardPaintMode mode)
        {
            SerializedProperty cell = EnsureCell(cells, x, y);
            ResetCell(cell, x, y);

            switch (mode)
            {
                case BoardPaintMode.Occupied:
                    cell.FindPropertyRelative("occupiedAtStart").boolValue = true;
                    cell.FindPropertyRelative("occupiedCellType").intValue = (int)BlockCellType.Normal;
                    cell.FindPropertyRelative("occupiedColor").colorValue = new Color(1f, 0.74f, 0.16f, 1f);
                    break;

                case BoardPaintMode.Gem:
                    cell.FindPropertyRelative("itemType").intValue = (int)BoardItemType.Gem;
                    cell.FindPropertyRelative("itemId").stringValue = "gem";
                    break;

                case BoardPaintMode.TimeBonus:
                    cell.FindPropertyRelative("itemType").intValue = (int)BoardItemType.TimeBonus;
                    cell.FindPropertyRelative("itemId").stringValue = "time_bonus";
                    break;

                case BoardPaintMode.Target:
                    cell.FindPropertyRelative("itemType").intValue = (int)BoardItemType.TargetPattern;
                    cell.FindPropertyRelative("itemId").stringValue = "target";
                    cell.FindPropertyRelative("targetPatternFilled").boolValue = true;
                    break;
            }
        }

        private static void ResetCell(SerializedProperty cell, int x, int y)
        {
            cell.FindPropertyRelative("x").intValue = x;
            cell.FindPropertyRelative("y").intValue = y;
            cell.FindPropertyRelative("occupiedAtStart").boolValue = false;
            cell.FindPropertyRelative("occupiedCellType").intValue = (int)BlockCellType.Normal;
            cell.FindPropertyRelative("occupiedColor").colorValue = Color.gray;
            cell.FindPropertyRelative("itemType").intValue = (int)BoardItemType.None;
            cell.FindPropertyRelative("itemId").stringValue = string.Empty;
            cell.FindPropertyRelative("markerTag").stringValue = string.Empty;
            cell.FindPropertyRelative("targetPatternFilled").boolValue = false;
        }

        private static SerializedProperty EnsureCell(SerializedProperty cells, int x, int y)
        {
            SerializedProperty existing = FindCell(cells, x, y, out _);
            if (existing != null)
                return existing;

            int index = cells.arraySize;
            cells.InsertArrayElementAtIndex(index);
            SerializedProperty cell = cells.GetArrayElementAtIndex(index);
            ResetCell(cell, x, y);
            return cell;
        }

        private static SerializedProperty FindCell(SerializedProperty cells, int x, int y, out int index)
        {
            for (int i = 0; i < cells.arraySize; i++)
            {
                SerializedProperty cell = cells.GetArrayElementAtIndex(i);
                if (cell.FindPropertyRelative("x").intValue == x && cell.FindPropertyRelative("y").intValue == y)
                {
                    index = i;
                    return cell;
                }
            }

            index = -1;
            return null;
        }

        private static void RemoveCell(SerializedProperty cells, int index)
        {
            if (index >= 0 && index < cells.arraySize)
                cells.DeleteArrayElementAtIndex(index);
        }

        private static CellPreview ReadCellPreview(SerializedProperty cell)
        {
            if (cell == null)
                return default;

            return new CellPreview
            {
                IsOccupied = cell.FindPropertyRelative("occupiedAtStart").boolValue,
                IsTarget = cell.FindPropertyRelative("targetPatternFilled").boolValue,
                ItemType = (BoardItemType)cell.FindPropertyRelative("itemType").intValue
            };
        }

        private static void DrawCellBadge(Rect rect, string text, Color color)
        {
            Rect badge = new Rect(rect.x + rect.width * 0.52f, rect.y + 3f, rect.width * 0.36f, rect.height * 0.36f);
            EditorGUI.DrawRect(badge, color);

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                fontSize = 10
            };
            GUI.Label(badge, text, style);
        }

        private static Rect Inset(Rect rect, float amount)
        {
            return new Rect(rect.x + amount, rect.y + amount, rect.width - amount * 2f, rect.height - amount * 2f);
        }

        private void DrawBoardSummary(SerializedProperty cells)
        {
            int occupied = 0;
            int gems = 0;
            int timeBonus = 0;
            int targets = 0;

            for (int i = 0; i < cells.arraySize; i++)
            {
                CellPreview preview = ReadCellPreview(cells.GetArrayElementAtIndex(i));
                if (preview.IsOccupied) occupied++;
                if (preview.IsTarget) targets++;
                if (preview.ItemType == BoardItemType.Gem) gems++;
                if (preview.ItemType == BoardItemType.TimeBonus) timeBonus++;
            }

            EditorGUILayout.LabelField($"Cells: {cells.arraySize} | Blocks: {occupied} | Gems: {gems} | Time: {timeBonus} | Targets: {targets}", EditorStyles.miniLabel);
        }

        private void ClearBoardCells(SerializedProperty cells)
        {
            cells.ClearArray();
        }

        private void ClearItems(SerializedProperty cells)
        {
            for (int i = cells.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty cell = cells.GetArrayElementAtIndex(i);
                cell.FindPropertyRelative("itemType").intValue = (int)BoardItemType.None;
                cell.FindPropertyRelative("itemId").stringValue = string.Empty;
                cell.FindPropertyRelative("markerTag").stringValue = string.Empty;

                if (!cell.FindPropertyRelative("occupiedAtStart").boolValue && !cell.FindPropertyRelative("targetPatternFilled").boolValue)
                    cells.DeleteArrayElementAtIndex(i);
            }
        }

        private void StampHeartTarget(SerializedProperty cells, int width, int height)
        {
            if (width < RuntimeBoardSize || height < RuntimeBoardSize)
                return;

            ClearBoardCells(cells);
            Vector2Int[] points =
            {
                new Vector2Int(2, 6), new Vector2Int(3, 7), new Vector2Int(4, 6), new Vector2Int(5, 7), new Vector2Int(6, 6),
                new Vector2Int(1, 5), new Vector2Int(2, 5), new Vector2Int(3, 5), new Vector2Int(4, 5), new Vector2Int(5, 5), new Vector2Int(6, 5), new Vector2Int(7, 5),
                new Vector2Int(2, 4), new Vector2Int(3, 4), new Vector2Int(4, 4), new Vector2Int(5, 4), new Vector2Int(6, 4),
                new Vector2Int(3, 3), new Vector2Int(4, 3), new Vector2Int(5, 3),
                new Vector2Int(4, 2)
            };

            for (int i = 0; i < points.Length; i++)
                PaintCell(cells, points[i].x, points[i].y, BoardPaintMode.Target);
        }

        private void DrawMapNodeRow(SerializedProperty nodes, int index)
        {
            SerializedProperty node = nodes.GetArrayElementAtIndex(index);
            SerializedProperty nodeId = node.FindPropertyRelative("_nodeId");
            SerializedProperty level = node.FindPropertyRelative("_level");
            SerializedProperty requirements = node.FindPropertyRelative("_requiredCompletedNodeIds");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{index + 1:00}", GUILayout.Width(28f));
                    EditorGUILayout.PropertyField(nodeId, GUIContent.none);

                    if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(56f)))
                    {
                        LevelDefinition selected = level.objectReferenceValue as LevelDefinition;
                        if (selected != null)
                            SetSelectedLevel(selected);
                    }
                }

                EditorGUILayout.PropertyField(level, GUIContent.none);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(index <= 0))
                    {
                        if (GUILayout.Button("Up", EditorStyles.miniButtonLeft))
                            nodes.MoveArrayElement(index, index - 1);
                    }

                    using (new EditorGUI.DisabledScope(index >= nodes.arraySize - 1))
                    {
                        if (GUILayout.Button("Down", EditorStyles.miniButtonMid))
                            nodes.MoveArrayElement(index, index + 1);
                    }

                    if (GUILayout.Button("No Req", EditorStyles.miniButtonMid))
                        requirements.ClearArray();

                    if (GUILayout.Button("Remove", EditorStyles.miniButtonRight))
                        nodes.DeleteArrayElementAtIndex(index);
                }
            }
        }

        private void AppendSelectedLevelToMap()
        {
            if (_mapDefinition == null || _selectedLevel == null)
                return;

            Undo.RecordObject(_mapDefinition, "Append Arcade Level Node");
            SerializedObject serializedMap = new SerializedObject(_mapDefinition);
            serializedMap.Update();
            SerializedProperty nodes = serializedMap.FindProperty("_nodes");
            AddLevelNode(nodes, _selectedLevel);
            serializedMap.ApplyModifiedProperties();
            EditorUtility.SetDirty(_mapDefinition);
        }

        private void RebuildMapFromLevelList()
        {
            if (_mapDefinition == null)
                return;

            Undo.RecordObject(_mapDefinition, "Rebuild Arcade Map");
            SerializedObject serializedMap = new SerializedObject(_mapDefinition);
            serializedMap.Update();
            SerializedProperty nodes = serializedMap.FindProperty("_nodes");
            nodes.ClearArray();

            for (int i = 0; i < _levelAssets.Count; i++)
            {
                if (_levelAssets[i].Asset != null)
                    AddLevelNode(nodes, _levelAssets[i].Asset);
            }

            serializedMap.ApplyModifiedProperties();
            EditorUtility.SetDirty(_mapDefinition);
        }

        private void SyncMapNodeIds()
        {
            if (_mapDefinition == null)
                return;

            Undo.RecordObject(_mapDefinition, "Sync Arcade Map Node IDs");
            SerializedObject serializedMap = new SerializedObject(_mapDefinition);
            serializedMap.Update();
            SerializedProperty nodes = serializedMap.FindProperty("_nodes");

            for (int i = 0; i < nodes.arraySize; i++)
            {
                SerializedProperty node = nodes.GetArrayElementAtIndex(i);
                SerializedProperty level = node.FindPropertyRelative("_level");
                LevelDefinition levelAsset = level.objectReferenceValue as LevelDefinition;
                if (levelAsset == null) continue;

                node.FindPropertyRelative("_nodeId").stringValue = levelAsset.LevelId;
                node.FindPropertyRelative("_anchoredPosition").vector2Value = Vector2.zero;
            }

            serializedMap.ApplyModifiedProperties();
            EditorUtility.SetDirty(_mapDefinition);
        }

        private static void AddLevelNode(SerializedProperty nodes, LevelDefinition level)
        {
            int index = nodes.arraySize;
            nodes.InsertArrayElementAtIndex(index);

            SerializedProperty node = nodes.GetArrayElementAtIndex(index);
            node.FindPropertyRelative("_nodeId").stringValue = level != null ? level.LevelId : $"level_{index + 1:000}";
            node.FindPropertyRelative("_level").objectReferenceValue = level;
            node.FindPropertyRelative("_anchoredPosition").vector2Value = Vector2.zero;
            node.FindPropertyRelative("_requiredCompletedNodeIds").ClearArray();
        }

        private bool MapContainsLevel(LevelDefinition level)
        {
            if (_mapDefinition == null || level == null || _mapDefinition.Nodes == null)
                return false;

            for (int i = 0; i < _mapDefinition.Nodes.Count; i++)
            {
                MapLevelNodeDefinition node = _mapDefinition.Nodes[i];
                if (node != null && node.Level == level)
                    return true;
            }

            return false;
        }

        private void DrawLevelRow(LevelAssetInfo info)
        {
            bool selected = info.Asset == _selectedLevel;
            Color oldColor = GUI.backgroundColor;
            if (selected)
                GUI.backgroundColor = new Color(0.36f, 0.64f, 1f, 1f);

            GUIContent content = new GUIContent(GetLevelLabel(info), GetLevelTooltip(info));
            if (GUILayout.Button(content, EditorStyles.miniButton))
                SetSelectedLevel(info.Asset);

            GUI.backgroundColor = oldColor;
        }

        private string GetLevelLabel(LevelAssetInfo info)
        {
            if (info == null || info.Asset == null)
                return "(Missing Level)";

            switch (_labelMode)
            {
                case LevelLabelMode.Id:
                    return info.Asset.LevelId;
                case LevelLabelMode.DisplayName:
                    return info.Asset.DisplayName;
                case LevelLabelMode.Type:
                    return $"{info.Asset.LevelType} - {info.Asset.DisplayName}";
                default:
                    return Path.GetFileNameWithoutExtension(info.Path);
            }
        }

        private static string GetLevelTooltip(LevelAssetInfo info)
        {
            if (info == null || info.Asset == null)
                return string.Empty;

            return $"File: {Path.GetFileNameWithoutExtension(info.Path)}\nId: {info.Asset.LevelId}\nType: {info.Asset.LevelType}\nOrder: {info.Asset.OrderIndex}";
        }

        private void SetSelectedLevel(LevelDefinition level)
        {
            _selectedLevel = level;
            Selection.activeObject = level;
            Repaint();
        }

        private void DrawFolderHeader(string folder)
        {
            if (!_folderFoldouts.ContainsKey(folder))
                _folderFoldouts[folder] = true;

            _folderFoldouts[folder] = EditorGUILayout.Foldout(_folderFoldouts[folder], folder, true);
        }

        private bool IsFolderOpen(string folder)
        {
            return _folderFoldouts.TryGetValue(folder, out bool open) && open;
        }

        private void RefreshAssets()
        {
            _levelAssets.Clear();

            if (!AssetDatabase.IsValidFolder(_rootPath))
                return;

            string[] guids = AssetDatabase.FindAssets("t:LevelDefinition", new[] { _rootPath });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                LevelDefinition asset = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (asset == null) continue;

                _levelAssets.Add(new LevelAssetInfo(asset, path, GetRelativeFolder(path)));
            }

            _levelAssets.Sort((a, b) =>
            {
                int order = a.Asset.OrderIndex.CompareTo(b.Asset.OrderIndex);
                return order != 0 ? order : string.CompareOrdinal(a.Path, b.Path);
            });

            if (_selectedLevel == null && _levelAssets.Count > 0)
                _selectedLevel = _levelAssets[0].Asset;
        }

        private bool MatchesSearch(LevelDefinition level, string path)
        {
            if (string.IsNullOrWhiteSpace(_searchText))
                return true;

            string search = _searchText.Trim().ToLowerInvariant();
            return level.LevelId.ToLowerInvariant().Contains(search)
                   || level.DisplayName.ToLowerInvariant().Contains(search)
                   || Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Contains(search)
                   || level.LevelType.ToString().ToLowerInvariant().Contains(search);
        }

        private int GetFilteredCount()
        {
            int count = 0;
            for (int i = 0; i < _levelAssets.Count; i++)
            {
                if (_levelAssets[i].Asset != null && MatchesSearch(_levelAssets[i].Asset, _levelAssets[i].Path))
                    count++;
            }

            return count;
        }

        private string GetRelativeFolder(string assetPath)
        {
            string folder = Path.GetDirectoryName(assetPath)?.Replace("\\", "/") ?? _rootPath;
            if (folder.StartsWith(_rootPath))
                folder = folder.Substring(_rootPath.Length).Trim('/');

            return string.IsNullOrEmpty(folder) ? "(Root)" : folder;
        }

        private void BrowseForFolder()
        {
            string projectRoot = Path.GetFullPath(Application.dataPath + "/..").Replace("\\", "/");
            string selected = EditorUtility.OpenFolderPanel("Select Arcade Level Root Folder", Application.dataPath, string.Empty);
            if (string.IsNullOrEmpty(selected))
                return;

            selected = selected.Replace("\\", "/");
            if (!selected.StartsWith(projectRoot))
            {
                EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder inside this Unity project.", "OK");
                return;
            }

            _rootPath = "Assets" + selected.Substring(Application.dataPath.Replace("\\", "/").Length);
            _rootFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(_rootPath);
            RefreshAssets();
        }

        private void CreateLevel()
        {
            if (!AssetDatabase.IsValidFolder(_rootPath))
                return;

            int nextIndex = GetNextOrderIndex();
            string fileName = $"lvl_{nextIndex:000}.asset";
            string path = AssetDatabase.GenerateUniqueAssetPath($"{_rootPath}/{fileName}");

            LevelDefinition level = CreateInstance<LevelDefinition>();
            AssetDatabase.CreateAsset(level, path);

            SerializedObject serializedLevel = new SerializedObject(level);
            serializedLevel.Update();
            serializedLevel.FindProperty("_levelId").stringValue = $"level_{nextIndex:000}";
            serializedLevel.FindProperty("_worldId").stringValue = "arcade";
            serializedLevel.FindProperty("_orderIndex").intValue = nextIndex;
            serializedLevel.FindProperty("_displayName").stringValue = $"Level {nextIndex}";
            serializedLevel.FindProperty("_boardWidth").intValue = RuntimeBoardSize;
            serializedLevel.FindProperty("_boardHeight").intValue = RuntimeBoardSize;
            serializedLevel.FindProperty("_levelType").intValue = (int)ArcadeLevelType.Timed;
            serializedLevel.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshAssets();
            SetSelectedLevel(level);
        }

        private void DuplicateSelectedLevel()
        {
            if (_selectedLevel == null)
                return;

            string sourcePath = AssetDatabase.GetAssetPath(_selectedLevel);
            string copyPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(Path.GetDirectoryName(sourcePath) ?? _rootPath, $"{_selectedLevel.name}_copy.asset").Replace("\\", "/"));

            if (!AssetDatabase.CopyAsset(sourcePath, copyPath))
                return;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LevelDefinition copy = AssetDatabase.LoadAssetAtPath<LevelDefinition>(copyPath);
            if (copy != null)
            {
                int nextIndex = GetNextOrderIndex();
                SerializedObject serializedCopy = new SerializedObject(copy);
                serializedCopy.Update();
                serializedCopy.FindProperty("_levelId").stringValue = $"level_{nextIndex:000}";
                serializedCopy.FindProperty("_orderIndex").intValue = nextIndex;
                serializedCopy.FindProperty("_displayName").stringValue = $"Level {nextIndex}";
                serializedCopy.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(copy);
            }

            AssetDatabase.SaveAssets();
            RefreshAssets();
            SetSelectedLevel(copy);
        }

        private int GetNextOrderIndex()
        {
            int max = 0;
            for (int i = 0; i < _levelAssets.Count; i++)
            {
                if (_levelAssets[i].Asset != null)
                    max = Mathf.Max(max, _levelAssets[i].Asset.OrderIndex);
            }

            return max + 1;
        }

        private void SyncIdentityFromFile(SerializedObject serializedLevel)
        {
            string path = AssetDatabase.GetAssetPath(_selectedLevel);
            string fileName = Path.GetFileNameWithoutExtension(path);
            int order = ExtractTrailingNumber(fileName);
            if (order <= 0)
                order = GetNextOrderIndex();

            serializedLevel.FindProperty("_levelId").stringValue = $"level_{order:000}";
            serializedLevel.FindProperty("_displayName").stringValue = $"Level {order}";
            serializedLevel.FindProperty("_orderIndex").intValue = order;
        }

        private static int ExtractTrailingNumber(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            int start = value.Length;
            while (start > 0 && char.IsDigit(value[start - 1]))
                start--;

            if (start == value.Length)
                return 0;

            return int.TryParse(value.Substring(start), out int number) ? number : 0;
        }

        private static void SetBoardSize(SerializedObject serializedLevel, int width, int height)
        {
            serializedLevel.FindProperty("_boardWidth").intValue = width;
            serializedLevel.FindProperty("_boardHeight").intValue = height;

            SerializedProperty cells = serializedLevel.FindProperty("_boardCells");
            for (int i = cells.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty cell = cells.GetArrayElementAtIndex(i);
                int x = cell.FindPropertyRelative("x").intValue;
                int y = cell.FindPropertyRelative("y").intValue;
                if (x >= width || y >= height)
                    cells.DeleteArrayElementAtIndex(i);
            }
        }

        private static void DrawPropertyIfFound(SerializedProperty parent, string propertyName, bool includeChildren = false)
        {
            SerializedProperty property = parent != null ? parent.FindPropertyRelative(propertyName) : null;
            if (property != null)
                EditorGUILayout.PropertyField(property, includeChildren);
        }

        private static void DrawHeader(string label)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        private sealed class LevelAssetInfo
        {
            public readonly LevelDefinition Asset;
            public readonly string Path;
            public readonly string Folder;

            public LevelAssetInfo(LevelDefinition asset, string path, string folder)
            {
                Asset = asset;
                Path = path;
                Folder = folder;
            }
        }

        private struct CellPreview
        {
            public bool IsOccupied;
            public bool IsTarget;
            public BoardItemType ItemType;
        }
    }
}
