using _Game.Scripts.Modes.Levels;
using UnityEditor;
using UnityEngine;

namespace _Game.Editor
{
    [CustomEditor(typeof(LevelDefinition))]
    public class LevelDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty _levelId;
        private SerializedProperty _worldId;
        private SerializedProperty _orderIndex;
        private SerializedProperty _displayName;
        private SerializedProperty _boardCells;
        private SerializedProperty _levelType;
        private SerializedProperty _objectiveDefinition;
        private SerializedProperty _timerRule;
        private SerializedProperty _spawnProfileOverride;
        private SerializedProperty _toolRule;
        private SerializedProperty _rewardCoins;

        private bool _showBoardCells;
        private bool _showSpawnProfile;

        private void OnEnable()
        {
            _levelId = serializedObject.FindProperty("_levelId");
            _worldId = serializedObject.FindProperty("_worldId");
            _orderIndex = serializedObject.FindProperty("_orderIndex");
            _displayName = serializedObject.FindProperty("_displayName");
            _boardCells = serializedObject.FindProperty("_boardCells");
            _levelType = serializedObject.FindProperty("_levelType");
            _objectiveDefinition = serializedObject.FindProperty("_objectiveDefinition");
            _timerRule = serializedObject.FindProperty("_timerRule");
            _spawnProfileOverride = serializedObject.FindProperty("_spawnProfileOverride");
            _toolRule = serializedObject.FindProperty("_toolRule");
            _rewardCoins = serializedObject.FindProperty("_rewardCoins");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            DrawIdentity();
            DrawBoardSummary();
            DrawObjective();
            DrawSpawnAndRewards();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawIdentity()
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_levelId);
                EditorGUILayout.PropertyField(_displayName);
                EditorGUILayout.PropertyField(_orderIndex);
                EditorGUILayout.PropertyField(_worldId);
            }
        }

        private void DrawBoardSummary()
        {
            EditorGUILayout.LabelField("Board", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Fixed Size", $"{LevelDefinition.FixedBoardWidth}x{LevelDefinition.FixedBoardHeight}");
                EditorGUILayout.HelpBox("Board size is fixed for this project. Use board cells only to paint starting blocks, items, and target patterns.", MessageType.Info);

                _showBoardCells = EditorGUILayout.Foldout(_showBoardCells, $"Board Cells ({_boardCells.arraySize})", true);
                if (_showBoardCells)
                    EditorGUILayout.PropertyField(_boardCells, true);
            }
        }

        private void DrawObjective()
        {
            EditorGUILayout.LabelField("Objective", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_levelType);
                EditorGUILayout.PropertyField(_objectiveDefinition, true);

                ArcadeLevelType type = (ArcadeLevelType)_levelType.intValue;
                if (UsesTimer(type))
                    EditorGUILayout.PropertyField(_timerRule, true);
                else
                    EditorGUILayout.HelpBox("This objective type does not use timer settings.", MessageType.None);
            }
        }

        private void DrawSpawnAndRewards()
        {
            EditorGUILayout.LabelField("Spawn & Rewards", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showSpawnProfile = EditorGUILayout.Foldout(_showSpawnProfile, "Per-Level Spawn Profile Override", true);
                if (_showSpawnProfile)
                    EditorGUILayout.PropertyField(_spawnProfileOverride, true);

                if (_toolRule != null)
                    EditorGUILayout.PropertyField(_toolRule, true);

                EditorGUILayout.PropertyField(_rewardCoins);
            }
        }

        private static bool UsesTimer(ArcadeLevelType type)
        {
            return type == ArcadeLevelType.Score
                   || type == ArcadeLevelType.Collectable
                   || type == ArcadeLevelType.Puzzle;
        }
    }
}
