using UnityEditor;
using UnityEngine;
using OurGame.Systems;

[CustomEditor(typeof(BackpackDefinition))]
public class BackpackDefinitionEditor : Editor
{
    private SerializedProperty hologramRootProperty;
    private SerializedProperty pocketDefinitionsProperty;

    void OnEnable()
    {
        hologramRootProperty = serializedObject.FindProperty("hologramRoot");
        pocketDefinitionsProperty = serializedObject.FindProperty("pocketDefinitions");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(hologramRootProperty);
        EditorGUILayout.Space(8f);

        DrawPocketDefinitions();

        EditorGUILayout.Space(8f);
        DrawActionButtons();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPocketDefinitions()
    {
        EditorGUILayout.LabelField("Pocket Definitions", EditorStyles.boldLabel);

        if (pocketDefinitionsProperty == null)
            return;

        for (int i = 0; i < pocketDefinitionsProperty.arraySize; i++)
        {
            SerializedProperty element = pocketDefinitionsProperty.GetArrayElementAtIndex(i);
            if (element == null)
                continue;

            SerializedProperty pocketName = element.FindPropertyRelative("pocketName");
            SerializedProperty displayName = element.FindPropertyRelative("displayName");
            SerializedProperty rows = element.FindPropertyRelative("rows");
            SerializedProperty columns = element.FindPropertyRelative("columns");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Pocket {i + 1}", EditorStyles.boldLabel);

            GUI.enabled = i > 0;
            if (GUILayout.Button("Up", GUILayout.Width(48f)))
                pocketDefinitionsProperty.MoveArrayElement(i, i - 1);

            GUI.enabled = i < pocketDefinitionsProperty.arraySize - 1;
            if (GUILayout.Button("Down", GUILayout.Width(48f)))
                pocketDefinitionsProperty.MoveArrayElement(i, i + 1);

            GUI.enabled = true;
            if (GUILayout.Button("-", GUILayout.Width(28f)))
            {
                pocketDefinitionsProperty.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(pocketName);
            EditorGUILayout.PropertyField(displayName);
            EditorGUILayout.PropertyField(rows);
            EditorGUILayout.PropertyField(columns);
            EditorGUILayout.EndVertical();
        }

        if (pocketDefinitionsProperty.arraySize == 0)
            EditorGUILayout.HelpBox("No pocket definitions configured. Use 'Add Pocket' to create one.", MessageType.Info);
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Pocket"))
            AddPocketElement();

        if (GUILayout.Button("Clear Empty Entries"))
            RemoveEmptyEntries();

        EditorGUILayout.EndHorizontal();
    }

    private void AddPocketElement()
    {
        int newIndex = pocketDefinitionsProperty.arraySize;
        pocketDefinitionsProperty.InsertArrayElementAtIndex(newIndex);

        SerializedProperty element = pocketDefinitionsProperty.GetArrayElementAtIndex(newIndex);
        if (element == null)
            return;

        element.FindPropertyRelative("pocketName").stringValue = string.Empty;
        element.FindPropertyRelative("displayName").stringValue = string.Empty;
        element.FindPropertyRelative("rows").intValue = 1;
        element.FindPropertyRelative("columns").intValue = 1;
    }

    private void RemoveEmptyEntries()
    {
        for (int i = pocketDefinitionsProperty.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty element = pocketDefinitionsProperty.GetArrayElementAtIndex(i);
            SerializedProperty pocketName = element.FindPropertyRelative("pocketName");
            if (!string.IsNullOrWhiteSpace(pocketName.stringValue))
                continue;

            pocketDefinitionsProperty.DeleteArrayElementAtIndex(i);
        }
    }
}
