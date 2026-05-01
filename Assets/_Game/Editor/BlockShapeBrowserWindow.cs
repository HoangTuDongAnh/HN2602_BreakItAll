using System.Collections.Generic;
using System.IO;
using _Game.Scripts.Data;
using UnityEditor;
using UnityEngine;

namespace _Game.Editor
{
    public class BlockShapeBrowserWindow : EditorWindow
    {
        private const string DefaultRootPath = "Assets/_Game/Data/BlockShapes";

        private readonly List<ShapeAssetInfo> _shapeAssets = new List<ShapeAssetInfo>();
        private readonly Dictionary<string, bool> _folderFoldouts = new Dictionary<string, bool>();

        private enum ShapeListLabelMode
        {
            FileName,
            Id,
            DisplayName
        }

        private DefaultAsset _rootFolder;
        private string _rootPath = DefaultRootPath;
        private string _searchText = string.Empty;
        private int _tierFilter;
        private ShapeListLabelMode _labelMode = ShapeListLabelMode.FileName;
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;
        private BlockData _selectedShape;

        private static readonly string[] TierOptions =
        {
            "All Tiers",
            "Tier 1 - Easy",
            "Tier 2 - Medium",
            "Tier 3 - Hard"
        };

        private static readonly string[] LabelModeOptions =
        {
            "File",
            "Id",
            "Display"
        };

        [MenuItem("Tools/Block Blast/Shape Browser")]
        public static void Open()
        {
            BlockShapeBrowserWindow window = GetWindow<BlockShapeBrowserWindow>("Shape Browser");
            window.minSize = new Vector2(860f, 520f);
            window.Show();
        }

        public static void Open(BlockData selectedShape)
        {
            Open();
            BlockShapeBrowserWindow window = GetWindow<BlockShapeBrowserWindow>("Shape Browser");
            window.SetSelectedShape(selectedShape);
        }

        private void OnEnable()
        {
            if (_rootFolder == null)
                _rootFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(_rootPath);

            RefreshAssets();
        }

        private void OnGUI()
        {
            DrawToolbar();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawAssetList();
                DrawSelectedEditor();
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

                    if (GUILayout.Button("Browse", GUILayout.Width(80f)))
                        BrowseForFolder();

                    if (GUILayout.Button("Refresh", GUILayout.Width(80f)))
                        RefreshAssets();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _searchText = EditorGUILayout.TextField("Search", _searchText);
                    _tierFilter = EditorGUILayout.Popup(_tierFilter, TierOptions, GUILayout.Width(150f));

                    if (GUILayout.Button("Create Shape", GUILayout.Width(110f)))
                        CreateShape();
                }
            }
        }

        private void DrawAssetList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(310f)))
            {
                EditorGUILayout.LabelField($"Shapes ({GetFilteredCount()})", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Show", GUILayout.Width(36f));
                    _labelMode = (ShapeListLabelMode)GUILayout.Toolbar((int)_labelMode, LabelModeOptions);
                }

                _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, EditorStyles.helpBox);

                string currentFolder = null;
                for (int i = 0; i < _shapeAssets.Count; i++)
                {
                    ShapeAssetInfo info = _shapeAssets[i];
                    if (info.Asset == null || !MatchesFilters(info.Asset))
                        continue;

                    if (currentFolder != info.Folder)
                    {
                        currentFolder = info.Folder;
                        DrawFolderHeader(currentFolder);
                    }

                    if (!IsFolderOpen(currentFolder))
                        continue;

                    DrawShapeRow(info);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSelectedEditor()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (_selectedShape == null)
                {
                    EditorGUILayout.HelpBox("Select a BlockData asset from the left panel.", MessageType.Info);
                    return;
                }

                DrawSelectedToolbar();

                _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
                SerializedObject serializedShape = new SerializedObject(_selectedShape);
                BlockShapeEditorGUI.Draw(_selectedShape, serializedShape);
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSelectedToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField(_selectedShape.name, EditorStyles.boldLabel);

                if (GUILayout.Button("Select", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    Selection.activeObject = _selectedShape;

                if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                    EditorGUIUtility.PingObject(_selectedShape);

                if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                    DuplicateSelected();

                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    EditorUtility.SetDirty(_selectedShape);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        private void DrawFolderHeader(string folder)
        {
            if (!_folderFoldouts.ContainsKey(folder))
                _folderFoldouts[folder] = true;

            using (new EditorGUILayout.HorizontalScope())
            {
                _folderFoldouts[folder] = EditorGUILayout.Foldout(_folderFoldouts[folder], folder, true);
            }
        }

        private void DrawShapeRow(ShapeAssetInfo info)
        {
            bool selected = info.Asset == _selectedShape;
            Color oldColor = GUI.backgroundColor;
            if (selected)
                GUI.backgroundColor = new Color(0.42f, 0.62f, 1f, 1f);

            string label = $"{GetShapeLabel(info)}  ({CountOccupied(info.Asset)} cells)";
            GUIContent content = new GUIContent(label, GetShapeTooltip(info));
            if (GUILayout.Button(content, EditorStyles.miniButton))
                SetSelectedShape(info.Asset);

            GUI.backgroundColor = oldColor;
        }

        private string GetShapeLabel(ShapeAssetInfo info)
        {
            if (info == null || info.Asset == null)
                return "(Missing Shape)";

            switch (_labelMode)
            {
                case ShapeListLabelMode.Id:
                    return info.Asset.Id;
                case ShapeListLabelMode.DisplayName:
                    return info.Asset.DisplayName;
                default:
                    return Path.GetFileNameWithoutExtension(info.Path);
            }
        }

        private static string GetShapeTooltip(ShapeAssetInfo info)
        {
            if (info == null || info.Asset == null)
                return string.Empty;

            return $"File: {Path.GetFileNameWithoutExtension(info.Path)}\nId: {info.Asset.Id}\nDisplay: {info.Asset.DisplayName}";
        }

        private bool IsFolderOpen(string folder)
        {
            return _folderFoldouts.TryGetValue(folder, out bool open) && open;
        }

        private void BrowseForFolder()
        {
            string absoluteRoot = Path.GetFullPath(Application.dataPath + "/..").Replace("\\", "/");
            string selected = EditorUtility.OpenFolderPanel("Select Shape Root Folder", Application.dataPath, string.Empty);
            if (string.IsNullOrEmpty(selected))
                return;

            selected = selected.Replace("\\", "/");
            if (!selected.StartsWith(absoluteRoot))
            {
                EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder inside this Unity project.", "OK");
                return;
            }

            string assetPath = selected.Replace(absoluteRoot + "/", string.Empty);
            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                EditorUtility.DisplayDialog("Invalid Folder", "Selected path is not a valid Unity asset folder.", "OK");
                return;
            }

            _rootPath = assetPath;
            _rootFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(_rootPath);
            RefreshAssets();
        }

        private void RefreshAssets()
        {
            _shapeAssets.Clear();

            if (string.IsNullOrEmpty(_rootPath) || !AssetDatabase.IsValidFolder(_rootPath))
                _rootPath = DefaultRootPath;

            if (!AssetDatabase.IsValidFolder(_rootPath))
                return;

            string[] guids = AssetDatabase.FindAssets("t:BlockData", new[] { _rootPath });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                BlockData asset = AssetDatabase.LoadAssetAtPath<BlockData>(path);
                if (asset == null) continue;

                _shapeAssets.Add(new ShapeAssetInfo
                {
                    Asset = asset,
                    Path = path,
                    Folder = Path.GetDirectoryName(path)?.Replace("\\", "/") ?? _rootPath
                });
            }

            _shapeAssets.Sort((a, b) => string.Compare(a.Path, b.Path, System.StringComparison.OrdinalIgnoreCase));

            if (_selectedShape == null && _shapeAssets.Count > 0)
                SetSelectedShape(_shapeAssets[0].Asset);
        }

        private void SetSelectedShape(BlockData shape)
        {
            _selectedShape = shape;
            _rightScroll = Vector2.zero;

            if (_selectedShape != null)
            {
                string path = AssetDatabase.GetAssetPath(_selectedShape);
                string folder = Path.GetDirectoryName(path)?.Replace("\\", "/");
                if (!string.IsNullOrEmpty(folder))
                    _folderFoldouts[folder] = true;
            }

            Repaint();
        }

        private void CreateShape()
        {
            string folder = GetCreationFolder();
            if (!AssetDatabase.IsValidFolder(folder))
                folder = _rootPath;

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/NewBlockShape.asset");
            BlockData asset = CreateInstance<BlockData>();
            asset.columns = BlockData.MaxShapeSize;
            asset.rows = BlockData.MaxShapeSize;
            asset.ClearData();

            AssetDatabase.CreateAsset(asset, path);
            ApplyDefaultIdentity(asset, path);
            AssetDatabase.SaveAssets();
            RefreshAssets();
            SetSelectedShape(asset);
            Selection.activeObject = asset;
        }

        private void DuplicateSelected()
        {
            if (_selectedShape == null) return;

            string sourcePath = AssetDatabase.GetAssetPath(_selectedShape);
            if (string.IsNullOrEmpty(sourcePath)) return;

            string folder = Path.GetDirectoryName(sourcePath)?.Replace("\\", "/") ?? _rootPath;
            string targetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{_selectedShape.name}_Copy.asset");

            if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
            {
                EditorUtility.DisplayDialog("Duplicate Failed", "Unity could not duplicate this shape asset.", "OK");
                return;
            }

            AssetDatabase.SaveAssets();
            BlockData duplicated = AssetDatabase.LoadAssetAtPath<BlockData>(targetPath);
            ApplyDefaultIdentity(duplicated, targetPath);
            AssetDatabase.SaveAssets();
            RefreshAssets();
            SetSelectedShape(duplicated);
            Selection.activeObject = _selectedShape;
        }

        private static void ApplyDefaultIdentity(BlockData asset, string assetPath)
        {
            if (asset == null) return;

            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            string labelSource = RemoveShapePrefix(fileName);

            SerializedObject serializedAsset = new SerializedObject(asset);
            SerializedProperty idProperty = serializedAsset.FindProperty("_id");
            SerializedProperty displayNameProperty = serializedAsset.FindProperty("_displayName");

            if (idProperty != null)
                idProperty.stringValue = ToSnakeCase(labelSource);

            if (displayNameProperty != null)
                displayNameProperty.stringValue = ToDisplayName(labelSource);

            serializedAsset.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
        }

        private static string RemoveShapePrefix(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.StartsWith("Shape_", System.StringComparison.OrdinalIgnoreCase)
                ? value.Substring("Shape_".Length)
                : value;
        }

        private static string ToSnakeCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "shape";

            System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
            bool previousSeparator = false;

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                    previousSeparator = false;
                    continue;
                }

                if (previousSeparator || builder.Length == 0)
                    continue;

                builder.Append('_');
                previousSeparator = true;
            }

            string result = builder.ToString().Trim('_');
            return string.IsNullOrEmpty(result) ? "shape" : result;
        }

        private static string ToDisplayName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Shape";

            string[] words = value.Replace('-', '_').Split(new[] { '_' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                return "Shape";

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length <= 1)
                {
                    words[i] = words[i].ToUpperInvariant();
                    continue;
                }

                words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
            }

            return string.Join(" ", words);
        }

        private string GetCreationFolder()
        {
            if (_selectedShape == null)
                return _rootPath;

            string path = AssetDatabase.GetAssetPath(_selectedShape);
            return Path.GetDirectoryName(path)?.Replace("\\", "/") ?? _rootPath;
        }

        private bool MatchesFilters(BlockData asset)
        {
            if (asset == null) return false;

            if (_tierFilter > 0 && (int)asset.tier != _tierFilter - 1)
                return false;

            if (string.IsNullOrWhiteSpace(_searchText))
                return true;

            string needle = _searchText.Trim().ToLowerInvariant();
            if (asset.name.ToLowerInvariant().Contains(needle)) return true;
            if (asset.Id.ToLowerInvariant().Contains(needle)) return true;
            if (asset.DisplayName.ToLowerInvariant().Contains(needle)) return true;

            if (asset.tags != null)
            {
                for (int i = 0; i < asset.tags.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(asset.tags[i]) && asset.tags[i].ToLowerInvariant().Contains(needle))
                        return true;
                }
            }

            return false;
        }

        private int GetFilteredCount()
        {
            int count = 0;
            for (int i = 0; i < _shapeAssets.Count; i++)
            {
                if (_shapeAssets[i].Asset != null && MatchesFilters(_shapeAssets[i].Asset))
                    count++;
            }

            return count;
        }

        private int CountOccupied(BlockData asset)
        {
            if (asset == null) return 0;

            asset.EnsureDataSize();
            int count = 0;
            for (int i = 0; i < asset.boardData.Count; i++)
            {
                if (asset.boardData[i] != null && asset.boardData[i].isOccupied)
                    count++;
            }

            return count;
        }

        private sealed class ShapeAssetInfo
        {
            public BlockData Asset;
            public string Path;
            public string Folder;
        }
    }
}
