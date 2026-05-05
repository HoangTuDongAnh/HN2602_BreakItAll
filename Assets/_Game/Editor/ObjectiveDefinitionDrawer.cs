using System.Collections.Generic;
using _Game.Scripts.Modes.Levels;
using UnityEditor;
using UnityEngine;

namespace _Game.Editor
{
    [CustomPropertyDrawer(typeof(ObjectiveDefinition))]
    public class ObjectiveDefinitionDrawer : PropertyDrawer
    {
        private const float HelpBoxHeight = 38f;
        private const float Spacing = 3f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            ArcadeLevelType levelType = ResolveLevelType(property);
            Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, BuildHeader(label, levelType), true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            row.y += EditorGUIUtility.singleLineHeight + Spacing;

            DrawHelp(row, levelType);
            row.y += HelpBoxHeight + Spacing;

            List<string> fields = GetVisibleFieldNames(levelType);
            for (int i = 0; i < fields.Count; i++)
            {
                SerializedProperty child = property.FindPropertyRelative(fields[i]);
                if (child == null) continue;

                float height = EditorGUI.GetPropertyHeight(child, true);
                Rect fieldRect = new Rect(position.x, row.y, position.width, height);
                EditorGUI.PropertyField(fieldRect, child, true);
                row.y += height + Spacing;
            }

            DrawHiddenSummary(property, levelType, row);
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return height;

            height += Spacing + HelpBoxHeight + Spacing;

            List<string> fields = GetVisibleFieldNames(ResolveLevelType(property));
            for (int i = 0; i < fields.Count; i++)
            {
                SerializedProperty child = property.FindPropertyRelative(fields[i]);
                if (child == null) continue;
                height += EditorGUI.GetPropertyHeight(child, true) + Spacing;
            }

            height += EditorGUIUtility.singleLineHeight + Spacing;
            return height;
        }

        private static GUIContent BuildHeader(GUIContent label, ArcadeLevelType levelType)
        {
            return new GUIContent($"{label.text} ({levelType})", label.tooltip);
        }

        private static ArcadeLevelType ResolveLevelType(SerializedProperty property)
        {
            SerializedProperty typeProperty = property.serializedObject.FindProperty("_levelType");
            if (typeProperty == null)
                return ArcadeLevelType.Collectable;

            int value = typeProperty.intValue;
            if (!System.Enum.IsDefined(typeof(ArcadeLevelType), value))
                return ArcadeLevelType.Collectable;

            return (ArcadeLevelType)value;
        }

        private static List<string> GetVisibleFieldNames(ArcadeLevelType levelType)
        {
            switch (levelType)
            {
                case ArcadeLevelType.Score:
                    return new List<string> { "targetScore", "bonusTimeSeconds" };
                case ArcadeLevelType.Collectable:
                    return new List<string> { "targetItemId", "targetGemCount", "bonusTimeSeconds" };
                case ArcadeLevelType.Shape:
                    return new List<string> { "targetPatternId", "helperToolCount", "requireExactTargetShape" };
                case ArcadeLevelType.Puzzle:
                    return new List<string> { "providedShapeIds", "allowRotation", "requireUseAllProvidedShapes" };
                default:
                    return new List<string>();
            }
        }

        private static void DrawHelp(Rect rect, ArcadeLevelType levelType)
        {
            string message;
            switch (levelType)
            {
                case ArcadeLevelType.Score:
                    message = "Score objective: reach the target score before the timer ends.";
                    break;
                case ArcadeLevelType.Collectable:
                    message = "Collectable objective: clear lines that contain the target board item.";
                    break;
                case ArcadeLevelType.Shape:
                    message = "Shape objective: fill the highlighted target pattern. Shape levels do not use timer.";
                    break;
                case ArcadeLevelType.Puzzle:
                    message = "Puzzle objective: arrange provided blocks to fill the target board area.";
                    break;
                default:
                    message = "Choose a valid level type to show objective settings.";
                    break;
            }

            EditorGUI.HelpBox(rect, message, MessageType.Info);
        }

        private static void DrawHiddenSummary(SerializedProperty property, ArcadeLevelType levelType, Rect row)
        {
            List<string> allFields = new List<string>
            {
                "targetScore", "bonusTimeSeconds", "targetItemId", "targetGemCount",
                "targetPatternId", "helperToolCount", "requireExactTargetShape",
                "providedShapeIds", "allowRotation", "requireUseAllProvidedShapes"
            };

            List<string> visible = GetVisibleFieldNames(levelType);
            int hiddenCount = 0;
            for (int i = 0; i < allFields.Count; i++)
            {
                if (!visible.Contains(allFields[i]) && property.FindPropertyRelative(allFields[i]) != null)
                    hiddenCount++;
            }

            EditorGUI.LabelField(row, $"Hidden unused fields: {hiddenCount}", EditorStyles.miniLabel);
        }
    }
}
