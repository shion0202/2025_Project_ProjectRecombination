#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(StatData))]
public class StatDataDrawer : PropertyDrawer
{
    private const float lineHeight = 18f;
    private const float verticalSpacing = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // 5줄: statType, value, stringValue, min, max
        return (lineHeight + verticalSpacing) * 5;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var rect = new Rect(position.x, position.y, position.width, lineHeight);

        // 1. 스탯 종류
        var statTypeProp = property.FindPropertyRelative("statType");
        EditorGUI.PropertyField(rect, statTypeProp);

        // 2. 값
        rect.y += lineHeight + verticalSpacing;
        var valueProp = property.FindPropertyRelative("value");
        EditorGUI.PropertyField(rect, valueProp, new GUIContent("Value"));

        // 3. 문자열 값
        rect.y += lineHeight + verticalSpacing;
        var stringProp = property.FindPropertyRelative("stringValue");
        EditorGUI.PropertyField(rect, stringProp, new GUIContent("String"));

        // 4. 최소값
        rect.y += lineHeight + verticalSpacing;
        var minProp = property.FindPropertyRelative("minValue");
        EditorGUI.PropertyField(rect, minProp, new GUIContent("Min"));

        // 5. 최대값
        rect.y += lineHeight + verticalSpacing;
        var maxProp = property.FindPropertyRelative("maxValue");
        EditorGUI.PropertyField(rect, maxProp, new GUIContent("Max"));

        EditorGUI.EndProperty();
    }
}
#endif
