using _Game.Scripts.Data;
using UnityEditor;
using UnityEngine;

namespace _Game.Editor
{
    [CustomEditor(typeof(BlockData))]
    public class BlockDataEditor : UnityEditor.Editor
    {
        private BlockData _targetBlock;

        private void OnEnable()
        {
            _targetBlock = (BlockData)target;
            if (_targetBlock.boardData == null || _targetBlock.boardData.Count == 0)
            {
                _targetBlock.ClearData();
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("SHAPE TEMPLATE EDITOR", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Left Click to Toggle Block ON/OFF.", MessageType.Info);

            EditorGUILayout.Space(5);

            #region Grid Drawing
            // Draw 5x5 Grid
            for (int y = _targetBlock.rows - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                for (int x = 0; x < _targetBlock.columns; x++)
                {
                    DrawCellButton(x, y);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            #endregion

            #region Utilities
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Reset Shape", GUILayout.Height(30)))
            {
                _targetBlock.ClearData();
                EditorUtility.SetDirty(_targetBlock);
            }
            #endregion

            if (GUI.changed) EditorUtility.SetDirty(_targetBlock);
        }

        private void DrawCellButton(int x, int y)
        {
            CellData cell = _targetBlock.GetCell(x, y);
            if (cell == null) return;

            // Simple Logic: Gray = Empty, Blue = Occupied
            Color btnColor = cell.isOccupied ? new Color(0.4f, 0.6f, 1f) : Color.gray;
            GUI.backgroundColor = btnColor;

            if (GUILayout.Button("", GUILayout.Width(40), GUILayout.Height(40)))
            {
                // Toggle Logic
                cell.isOccupied = !cell.isOccupied;
                // Always reset properties to default when editing shape
                cell.cellType = CellType.Normal;
                cell.toolType = ToolType.None;
            }

            GUI.backgroundColor = Color.white;
        }
    }
}