using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Masters_FallingSortBin))]
public class Masters_FallingSortBinEditor : Editor {
    public override void OnInspectorGUI() {
        serializedObject.Update();

        SerializedProperty unitProp = serializedObject.FindProperty("unitName");
        EditorGUILayout.PropertyField(unitProp, new GUIContent("Unit Name"));

        Masters_FallingSortUnitName unit = (Masters_FallingSortUnitName)unitProp.enumValueIndex;

        if (unit == Masters_FallingSortUnitName.Unit8_ChattingBees) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unit8Category"), new GUIContent("Category"));
        } else if (unit == Masters_FallingSortUnitName.Unit9_SmartAlternatives) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unit9Category"), new GUIContent("Category"));
        } else if (unit == Masters_FallingSortUnitName.Unit12_SequenceYourThoughts) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unit12Category"), new GUIContent("Category"));
        } else if (unit == Masters_FallingSortUnitName.Unit13_ConnectorsOfTimeAndPlace) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unit13Category"), new GUIContent("Category"));
        } else if (unit == Masters_FallingSortUnitName.Unit15_PresentationPointers) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unit15Category"), new GUIContent("Category"));
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Bin References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("snapPointRectTransform"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("dropThresholdRectTransform"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("categoryTMP"));

        serializedObject.ApplyModifiedProperties();
    }
}
