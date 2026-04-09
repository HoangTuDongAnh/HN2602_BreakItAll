#if UNITY_EDITOR
using BreakItAll.Data;
using UnityEditor;
using UnityEngine;

namespace BreakItAll.Tools.Editor
{
    [CustomEditor(typeof(ShapeDefinition))]
    public sealed class ShapeDefinitionEditor : UnityEditor.Editor
    {
        private const float CellSize = 28f;

        public override void OnInspectorGUI()
        {
            ShapeDefinition shapeDefinition = (ShapeDefinition)target;

            if (shapeDefinition.matrix == null)
            {
                shapeDefinition.matrix = new ShapeMatrixData();
            }

            DrawIdentitySection(shapeDefinition);
            EditorGUILayout.Space(8f);

            DrawMatrixSettings(shapeDefinition);
            EditorGUILayout.Space(8f);

            DrawMatrixGrid(shapeDefinition);
            EditorGUILayout.Space(8f);

            DrawSpawnSection(shapeDefinition);
            EditorGUILayout.Space(8f);

            DrawAvailabilitySection(shapeDefinition);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(shapeDefinition);
            }
        }

        private void DrawIdentitySection(ShapeDefinition shapeDefinition)
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            shapeDefinition.id = EditorGUILayout.TextField("Id", shapeDefinition.id);
            shapeDefinition.displayName = EditorGUILayout.TextField("Display Name", shapeDefinition.displayName);
        }

        private void DrawMatrixSettings(ShapeDefinition shapeDefinition)
        {
            EditorGUILayout.LabelField("Shape Matrix", EditorStyles.boldLabel);

            int newWidth = EditorGUILayout.IntField("Width", shapeDefinition.matrix.width);
            int newHeight = EditorGUILayout.IntField("Height", shapeDefinition.matrix.height);

            if (newWidth != shapeDefinition.matrix.width || newHeight != shapeDefinition.matrix.height)
            {
                Undo.RecordObject(shapeDefinition, "Resize Shape Matrix");
                shapeDefinition.matrix.width = Mathf.Max(1, newWidth);
                shapeDefinition.matrix.height = Mathf.Max(1, newHeight);
                shapeDefinition.matrix.EnsureValidSize();
                EditorUtility.SetDirty(shapeDefinition);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear Matrix", GUILayout.Height(24f)))
                {
                    Undo.RecordObject(shapeDefinition, "Clear Shape Matrix");
                    shapeDefinition.matrix.Clear();
                    EditorUtility.SetDirty(shapeDefinition);
                }

                if (GUILayout.Button("Fill All", GUILayout.Height(24f)))
                {
                    Undo.RecordObject(shapeDefinition, "Fill Shape Matrix");
                    shapeDefinition.matrix.EnsureValidSize();

                    for (int y = 0; y < shapeDefinition.matrix.height; y++)
                    {
                        for (int x = 0; x < shapeDefinition.matrix.width; x++)
                        {
                            shapeDefinition.matrix.SetCell(x, y, true);
                        }
                    }

                    EditorUtility.SetDirty(shapeDefinition);
                }
            }
        }

        private void DrawMatrixGrid(ShapeDefinition shapeDefinition)
        {
            shapeDefinition.matrix.EnsureValidSize();

            EditorGUILayout.LabelField("Click cells to toggle", EditorStyles.miniBoldLabel);

            for (int y = shapeDefinition.matrix.height - 1; y >= 0; y--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int x = 0; x < shapeDefinition.matrix.width; x++)
                    {
                        bool filled = shapeDefinition.matrix.GetCell(x, y);

                        Color previousColor = GUI.backgroundColor;
                        GUI.backgroundColor = filled ? new Color(0.35f, 0.85f, 0.35f) : new Color(0.85f, 0.85f, 0.85f);

                        string label = filled ? "■" : "";
                        if (GUILayout.Button(label, GUILayout.Width(CellSize), GUILayout.Height(CellSize)))
                        {
                            Undo.RecordObject(shapeDefinition, "Toggle Shape Cell");
                            shapeDefinition.matrix.ToggleCell(x, y);
                            EditorUtility.SetDirty(shapeDefinition);
                        }

                        GUI.backgroundColor = previousColor;
                    }
                }
            }

            if (!shapeDefinition.matrix.HasAnyFilledCell())
            {
                EditorGUILayout.HelpBox("Shape should have at least one filled cell.", MessageType.Warning);
            }
        }

        private void DrawSpawnSection(ShapeDefinition shapeDefinition)
        {
            EditorGUILayout.LabelField("Spawn Metadata", EditorStyles.boldLabel);

            shapeDefinition.difficultyTier = EditorGUILayout.IntField("Difficulty Tier", shapeDefinition.difficultyTier);
            shapeDefinition.spawnWeight = EditorGUILayout.IntField("Spawn Weight", shapeDefinition.spawnWeight);
            shapeDefinition.unlockLevel = EditorGUILayout.IntField("Unlock Level", shapeDefinition.unlockLevel);

            SerializedProperty tagsProperty = serializedObject.FindProperty("tags");
            serializedObject.Update();
            EditorGUILayout.PropertyField(tagsProperty, true);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAvailabilitySection(ShapeDefinition shapeDefinition)
        {
            EditorGUILayout.LabelField("Availability", EditorStyles.boldLabel);
            shapeDefinition.enabledInEndless = EditorGUILayout.Toggle("Enabled In Endless", shapeDefinition.enabledInEndless);
            shapeDefinition.enabledInArcade = EditorGUILayout.Toggle("Enabled In Arcade", shapeDefinition.enabledInArcade);
        }
    }
}
#endif