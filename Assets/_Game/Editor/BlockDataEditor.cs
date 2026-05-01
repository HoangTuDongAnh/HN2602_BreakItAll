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
            EnsureShapeData();
        }

        public override void OnInspectorGUI()
        {
            EnsureShapeData();

            BlockShapeEditorGUI.Draw(_targetBlock, serializedObject);

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Asset"))
                {
                    EditorUtility.SetDirty(_targetBlock);
                    AssetDatabase.SaveAssets();
                }

                if (GUILayout.Button("Open Shape Browser"))
                    BlockShapeBrowserWindow.Open(_targetBlock);
            }
        }

        private void EnsureShapeData()
        {
            if (_targetBlock == null) return;

            int clampedColumns = Mathf.Clamp(_targetBlock.columns, 1, BlockData.MaxShapeSize);
            int clampedRows = Mathf.Clamp(_targetBlock.rows, 1, BlockData.MaxShapeSize);
            if (clampedColumns != _targetBlock.columns || clampedRows != _targetBlock.rows)
            {
                Undo.RecordObject(_targetBlock, "Clamp Shape Size");
                _targetBlock.columns = clampedColumns;
                _targetBlock.rows = clampedRows;
                EditorUtility.SetDirty(_targetBlock);
            }

            if (_targetBlock.boardData == null || _targetBlock.boardData.Count == 0)
            {
                Undo.RecordObject(_targetBlock, "Initialize Shape Data");
                _targetBlock.ClearData();
                EditorUtility.SetDirty(_targetBlock);
            }
            else
            {
                _targetBlock.EnsureDataSize();
            }
        }
    }
}
