using _Game.Scripts.Modes.Levels;
using UnityEditor;
using UnityEngine;

namespace _Game.Editor
{
    [CustomPropertyDrawer(typeof(ToolRuleDefinition))]
    public class ToolRuleDefinitionDrawer : PropertyDrawer
    {
        private const float Spacing = 3f;
        private const float HelpBoxHeight = 42f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty useCustom = property.FindPropertyRelative("_useCustomRules");
            SerializedProperty allowTools = property.FindPropertyRelative("_allowTools");
            SerializedProperty allowSingle = property.FindPropertyRelative("_allowSingleCell");
            SerializedProperty allowRemove = property.FindPropertyRelative("_allowRemoveSpawnBlock");
            SerializedProperty allowBomb = property.FindPropertyRelative("_allowBombSquare");

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
            EditorGUI.HelpBox(new Rect(position.x, row.y, position.width, HelpBoxHeight), BuildHelp(levelType, useCustom != null && useCustom.boolValue), MessageType.Info);
            row.y += HelpBoxHeight + Spacing;

            DrawProperty(ref row, position.width, useCustom);
            if (useCustom != null && useCustom.boolValue)
            {
                DrawProperty(ref row, position.width, allowTools);

                bool masterEnabled = allowTools == null || allowTools.boolValue;
                using (new EditorGUI.DisabledScope(!masterEnabled))
                {
                    DrawProperty(ref row, position.width, allowSingle);
                    DrawProperty(ref row, position.width, allowRemove);
                    DrawProperty(ref row, position.width, allowBomb);
                }
            }
            else
            {
                string defaultPolicy = levelType == ArcadeLevelType.Puzzle
                    ? "Default: Puzzle disables all gameplay tools."
                    : "Default: this Arcade type allows gameplay tools.";
                EditorGUI.LabelField(new Rect(position.x, row.y, position.width, EditorGUIUtility.singleLineHeight), defaultPolicy, EditorStyles.miniLabel);
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + Spacing + HelpBoxHeight + Spacing;
            SerializedProperty useCustom = property.FindPropertyRelative("_useCustomRules");
            height += EditorGUIUtility.singleLineHeight + Spacing;
            if (useCustom != null && useCustom.boolValue)
                height += (EditorGUIUtility.singleLineHeight + Spacing) * 4f;
            else
                height += EditorGUIUtility.singleLineHeight + Spacing;
            return height;
        }

        private static void DrawProperty(ref Rect row, float width, SerializedProperty property)
        {
            if (property == null) return;
            float height = EditorGUI.GetPropertyHeight(property, true);
            EditorGUI.PropertyField(new Rect(row.x, row.y, width, height), property, true);
            row.y += height + Spacing;
        }

        private static GUIContent BuildHeader(GUIContent label, ArcadeLevelType levelType)
        {
            return new GUIContent($"{label.text} ({levelType})", label.tooltip);
        }

        private static string BuildHelp(ArcadeLevelType levelType, bool custom)
        {
            if (!custom && levelType == ArcadeLevelType.Puzzle)
                return "Puzzle levels disable tools by default so the provided block solution stays fair and deterministic. Enable custom rules only if this level is intentionally tool-assisted.";

            if (!custom)
                return "Uses the project default tool policy for this Arcade type. Enable custom rules to allow or block individual tools.";

            return "Custom tool rules override the default policy for this level.";
        }

        private static ArcadeLevelType ResolveLevelType(SerializedProperty property)
        {
            SerializedProperty typeProperty = property.serializedObject.FindProperty("_levelType");
            if (typeProperty == null)
                return ArcadeLevelType.Collectable;

            int value = typeProperty.intValue;
            return System.Enum.IsDefined(typeof(ArcadeLevelType), value)
                ? (ArcadeLevelType)value
                : ArcadeLevelType.Collectable;
        }
    }
}
