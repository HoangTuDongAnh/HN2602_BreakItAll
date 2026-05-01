using System.Collections.Generic;
using _Game.Scripts.Data;
using UnityEditor;
using UnityEngine;

namespace _Game.Editor
{
    internal static class BlockShapeEditorGUI
    {
        private const string CellTexturePath = "Assets/_Game/Materials/png/Grid.png";
        private const float CellSize = 42f;
        private const float PreviewCellSize = 28f;
        private const float CellGap = 3f;

        private static readonly Color EmptyCellColor = new Color(0.22f, 0.22f, 0.22f, 1f);
        private static readonly Color EmptyCellAltColor = new Color(0.26f, 0.26f, 0.26f, 1f);
        private static readonly Color BorderColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        private static readonly Color AnchorBorderColor = new Color(1f, 0.74f, 0.18f, 1f);

        private static Texture2D _cellTexture;
        private static Color _previewColor = new Color(0.38f, 0.62f, 1f, 1f);

        public static void Draw(BlockData block, SerializedObject serializedObject)
        {
            if (block == null || serializedObject == null) return;

            block.EnsureDataSize();
            serializedObject.UpdateIfRequiredOrScript();

            DrawIdentity(serializedObject);
            DrawShape(block);
            DrawSpawn(serializedObject);
            DrawValidation(block);

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawIdentity(SerializedObject serializedObject)
        {
            DrawHeader("Identity");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_id"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_displayName"));
        }

        private static void DrawShape(BlockData block)
        {
            DrawHeader("Shape");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawSizeControls(block);
                EditorGUILayout.Space(4f);
                DrawToolbar(block);
                EditorGUILayout.Space(6f);
                DrawGrid(block);
                EditorGUILayout.Space(8f);
                DrawPreview(block);
            }
        }

        private static void DrawSpawn(SerializedObject serializedObject)
        {
            DrawHeader("Spawn");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tier"));

                SerializedProperty weightProperty = serializedObject.FindProperty("spawnWeight");
                weightProperty.floatValue = EditorGUILayout.Slider("Spawn Weight", weightProperty.floatValue, 0f, 5f);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("allowRotation"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enabledInEndless"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enabledInArcade"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tags"), true);
            }
        }

        private static void DrawSizeControls(BlockData block)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                int newColumns = EditorGUILayout.IntSlider("Columns", block.columns, 1, BlockData.MaxShapeSize);
                int newRows = EditorGUILayout.IntSlider("Rows", block.rows, 1, BlockData.MaxShapeSize);

                if (newColumns != block.columns || newRows != block.rows)
                    ResizeMatrix(block, newColumns, newRows, "Resize Shape");
            }

            EditorGUILayout.LabelField("Shape cells are Normal only. Arcade items live on the board grid.", EditorStyles.miniLabel);
        }

        private static void DrawToolbar(BlockData block)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear", EditorStyles.miniButtonLeft))
                    Clear(block);

                if (GUILayout.Button("Fill", EditorStyles.miniButtonMid))
                    Fill(block);

                if (GUILayout.Button("Trim", EditorStyles.miniButtonMid))
                    Trim(block);

                if (GUILayout.Button("5x5", EditorStyles.miniButtonRight))
                    ResizeMatrix(block, BlockData.MaxShapeSize, BlockData.MaxShapeSize, "Normalize Shape Size");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rotate Left", EditorStyles.miniButtonLeft))
                    Rotate(block, clockwise: false);

                if (GUILayout.Button("Rotate Right", EditorStyles.miniButtonMid))
                    Rotate(block, clockwise: true);

                if (GUILayout.Button("Flip X", EditorStyles.miniButtonMid))
                    Flip(block, horizontal: true);

                if (GUILayout.Button("Flip Y", EditorStyles.miniButtonRight))
                    Flip(block, horizontal: false);
            }
        }

        private static void DrawGrid(BlockData block)
        {
            Vector2Int anchor = ResolveAnchor(block);

            using (new EditorGUILayout.VerticalScope())
            {
                for (int y = block.rows - 1; y >= 0; y--)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();

                        for (int x = 0; x < block.columns; x++)
                        {
                            DrawGridCell(block, x, y, anchor);

                            if (x < block.columns - 1)
                                GUILayout.Space(CellGap);
                        }

                        GUILayout.FlexibleSpace();
                    }

                    if (y > 0)
                        GUILayout.Space(CellGap);
                }
            }
        }

        private static void DrawGridCell(BlockData block, int x, int y, Vector2Int anchor)
        {
            Rect rect = GUILayoutUtility.GetRect(CellSize, CellSize, GUILayout.Width(CellSize), GUILayout.Height(CellSize));
            bool occupied = IsOccupied(block, x, y);

            Color emptyColor = ((x + y) & 1) == 0 ? EmptyCellColor : EmptyCellAltColor;
            EditorGUI.DrawRect(rect, emptyColor);

            if (occupied)
                DrawTintedCell(rect, _previewColor);

            DrawBorder(rect, BorderColor, 1f);

            if (occupied && anchor.x == x && anchor.y == y)
                DrawBorder(rect, AnchorBorderColor, 2f);

            HandleCellInput(block, x, y, rect, occupied);
        }

        private static void DrawPreview(BlockData block)
        {
            DrawHeader("Preview", compact: true);
            _previewColor = EditorGUILayout.ColorField("Tint", _previewColor);

            if (!TryGetBounds(block, out int minX, out int minY, out int maxX, out int maxY))
            {
                EditorGUILayout.HelpBox("Shape is empty.", MessageType.Info);
                return;
            }

            float width = (maxX - minX + 1) * PreviewCellSize;
            float height = (maxY - minY + 1) * PreviewCellSize;
            Rect previewRect = GUILayoutUtility.GetRect(width, height + 8f, GUILayout.ExpandWidth(false));
            previewRect.height = height;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (!IsOccupied(block, x, y)) continue;

                    Rect cellRect = new Rect(
                        previewRect.x + (x - minX) * PreviewCellSize,
                        previewRect.y + (maxY - y) * PreviewCellSize,
                        PreviewCellSize,
                        PreviewCellSize
                    );

                    DrawTintedCell(cellRect, _previewColor);
                }
            }
        }

        private static void DrawValidation(BlockData block)
        {
            DrawHeader("Validation");

            int occupiedCount = CountOccupied(block);
            if (occupiedCount == 0)
                EditorGUILayout.HelpBox("This shape has no occupied cells.", MessageType.Warning);

            if (block.spawnWeight <= 0f && (block.enabledInArcade || block.enabledInEndless))
                EditorGUILayout.HelpBox("Spawn Weight is 0 while the shape is enabled for spawning.", MessageType.Info);

            if (HasDuplicateId(block, out string duplicatePath))
                EditorGUILayout.HelpBox($"Duplicate shape id found: {duplicatePath}", MessageType.Warning);

            EditorGUILayout.LabelField($"Occupied Cells: {occupiedCount}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Matrix: {block.columns}x{block.rows} / max {BlockData.MaxShapeSize}x{BlockData.MaxShapeSize}", EditorStyles.miniLabel);
        }

        private static void HandleCellInput(BlockData block, int x, int y, Rect rect, bool occupied)
        {
            Event current = Event.current;
            if (current.type != EventType.MouseDown || !rect.Contains(current.mousePosition))
                return;

            if (current.button != 0 && current.button != 1)
                return;

            bool nextState = current.button == 0 ? !occupied : false;
            SetOccupied(block, x, y, nextState, "Edit Shape Cell");
            current.Use();
        }

        private static void SetOccupied(BlockData block, int x, int y, bool occupied, string undoName)
        {
            Undo.RecordObject(block, undoName);
            CellData cell = block.GetCell(x, y);
            if (cell == null) return;

            cell.isOccupied = occupied;
            cell.blockCellType = BlockCellType.Normal;
            MarkDirty(block);
        }

        private static void ResizeMatrix(BlockData block, int columns, int rows, string undoName)
        {
            columns = Mathf.Clamp(columns, 1, BlockData.MaxShapeSize);
            rows = Mathf.Clamp(rows, 1, BlockData.MaxShapeSize);

            Undo.RecordObject(block, undoName);

            int oldColumns = Mathf.Max(1, block.columns);
            int oldRows = Mathf.Max(1, block.rows);
            List<CellData> oldData = new List<CellData>(block.boardData);
            List<CellData> newData = CreateEmptyData(columns, rows);

            int copyColumns = Mathf.Min(oldColumns, columns);
            int copyRows = Mathf.Min(oldRows, rows);

            for (int y = 0; y < copyRows; y++)
            {
                for (int x = 0; x < copyColumns; x++)
                {
                    int oldIndex = y * oldColumns + x;
                    int newIndex = y * columns + x;
                    if (oldIndex < 0 || oldIndex >= oldData.Count || newIndex < 0 || newIndex >= newData.Count)
                        continue;

                    newData[newIndex].isOccupied = oldData[oldIndex] != null && oldData[oldIndex].isOccupied;
                    newData[newIndex].blockCellType = BlockCellType.Normal;
                }
            }

            block.columns = columns;
            block.rows = rows;
            block.boardData = newData;
            MarkDirty(block);
        }

        private static void Clear(BlockData block)
        {
            Undo.RecordObject(block, "Clear Shape");
            block.EnsureDataSize();

            for (int i = 0; i < block.boardData.Count; i++)
            {
                block.boardData[i].isOccupied = false;
                block.boardData[i].blockCellType = BlockCellType.Normal;
            }

            MarkDirty(block);
        }

        private static void Fill(BlockData block)
        {
            Undo.RecordObject(block, "Fill Shape");
            block.EnsureDataSize();

            for (int i = 0; i < block.boardData.Count; i++)
            {
                block.boardData[i].isOccupied = true;
                block.boardData[i].blockCellType = BlockCellType.Normal;
            }

            MarkDirty(block);
        }

        private static void Trim(BlockData block)
        {
            if (!TryGetBounds(block, out int minX, out int minY, out int maxX, out int maxY))
                return;

            int newColumns = Mathf.Clamp(maxX - minX + 1, 1, BlockData.MaxShapeSize);
            int newRows = Mathf.Clamp(maxY - minY + 1, 1, BlockData.MaxShapeSize);
            bool[,] matrix = new bool[newColumns, newRows];

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (IsOccupied(block, x, y))
                        matrix[x - minX, y - minY] = true;
                }
            }

            ApplyMatrix(block, matrix, newColumns, newRows, "Trim Shape");
        }

        private static void Rotate(BlockData block, bool clockwise)
        {
            int newColumns = block.rows;
            int newRows = block.columns;
            bool[,] matrix = new bool[newColumns, newRows];

            for (int y = 0; y < block.rows; y++)
            {
                for (int x = 0; x < block.columns; x++)
                {
                    if (!IsOccupied(block, x, y)) continue;

                    if (clockwise)
                        matrix[block.rows - 1 - y, x] = true;
                    else
                        matrix[y, block.columns - 1 - x] = true;
                }
            }

            ApplyMatrix(block, matrix, newColumns, newRows, clockwise ? "Rotate Shape Right" : "Rotate Shape Left");
        }

        private static void Flip(BlockData block, bool horizontal)
        {
            bool[,] matrix = new bool[block.columns, block.rows];

            for (int y = 0; y < block.rows; y++)
            {
                for (int x = 0; x < block.columns; x++)
                {
                    if (!IsOccupied(block, x, y)) continue;

                    int newX = horizontal ? block.columns - 1 - x : x;
                    int newY = horizontal ? y : block.rows - 1 - y;
                    matrix[newX, newY] = true;
                }
            }

            ApplyMatrix(block, matrix, block.columns, block.rows, horizontal ? "Flip Shape X" : "Flip Shape Y");
        }

        private static void ApplyMatrix(BlockData block, bool[,] matrix, int columns, int rows, string undoName)
        {
            columns = Mathf.Clamp(columns, 1, BlockData.MaxShapeSize);
            rows = Mathf.Clamp(rows, 1, BlockData.MaxShapeSize);

            Undo.RecordObject(block, undoName);
            block.columns = columns;
            block.rows = rows;
            block.boardData = CreateEmptyData(columns, rows);

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int index = y * columns + x;
                    block.boardData[index].isOccupied = matrix[x, y];
                    block.boardData[index].blockCellType = BlockCellType.Normal;
                }
            }

            MarkDirty(block);
        }

        private static List<CellData> CreateEmptyData(int columns, int rows)
        {
            List<CellData> data = new List<CellData>(columns * rows);
            for (int i = 0; i < columns * rows; i++)
                data.Add(new CellData { isOccupied = false, blockCellType = BlockCellType.Normal });

            return data;
        }

        private static bool IsOccupied(BlockData block, int x, int y)
        {
            CellData cell = block.GetCell(x, y);
            return cell != null && cell.isOccupied;
        }

        private static int CountOccupied(BlockData block)
        {
            block.EnsureDataSize();
            int count = 0;
            for (int i = 0; i < block.boardData.Count; i++)
            {
                if (block.boardData[i] != null && block.boardData[i].isOccupied)
                    count++;
            }

            return count;
        }

        private static Vector2Int ResolveAnchor(BlockData block)
        {
            bool hasAnchor = false;
            Vector2Int anchor = Vector2Int.zero;

            for (int y = 0; y < block.rows; y++)
            {
                for (int x = 0; x < block.columns; x++)
                {
                    if (!IsOccupied(block, x, y)) continue;

                    if (!hasAnchor || y < anchor.y || (y == anchor.y && x < anchor.x))
                    {
                        anchor = new Vector2Int(x, y);
                        hasAnchor = true;
                    }
                }
            }

            return anchor;
        }

        private static bool TryGetBounds(BlockData block, out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = int.MaxValue;
            minY = int.MaxValue;
            maxX = int.MinValue;
            maxY = int.MinValue;

            for (int y = 0; y < block.rows; y++)
            {
                for (int x = 0; x < block.columns; x++)
                {
                    if (!IsOccupied(block, x, y)) continue;

                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            return minX != int.MaxValue;
        }

        private static bool HasDuplicateId(BlockData block, out string duplicatePath)
        {
            duplicatePath = null;
            if (block == null || string.IsNullOrWhiteSpace(block.Id))
                return false;

            string[] guids = AssetDatabase.FindAssets("t:BlockData");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                BlockData other = AssetDatabase.LoadAssetAtPath<BlockData>(path);
                if (other == null || other == block) continue;

                if (!string.Equals(other.Id, block.Id, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                duplicatePath = path;
                return true;
            }

            return false;
        }

        private static void DrawTintedCell(Rect rect, Color tint)
        {
            Texture2D texture = GetCellTexture();
            Color oldColor = GUI.color;
            GUI.color = tint;

            if (texture != null)
                GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            else
                EditorGUI.DrawRect(rect, tint);

            GUI.color = oldColor;
        }

        private static Texture2D GetCellTexture()
        {
            if (_cellTexture == null)
                _cellTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(CellTexturePath);

            return _cellTexture;
        }

        private static void DrawHeader(string title, bool compact = false)
        {
            EditorGUILayout.Space(compact ? 4f : 8f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private static void DrawBorder(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static void MarkDirty(BlockData block)
        {
            block.EnsureDataSize();
            EditorUtility.SetDirty(block);
        }
    }
}
