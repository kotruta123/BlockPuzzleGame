using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShapeData))]
public class ShapeDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty cells =
            serializedObject.FindProperty("cells");

        if (cells.arraySize != 25)
            cells.arraySize = 25;

        EditorGUILayout.LabelField(
            "Piece Designer",
            EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Click squares to turn cells on or off. " +
            "Use at least one cell. Maximum size: 5 x 5.",
            MessageType.Info);

        for (int y = 0; y < 5; y++)
        {
            EditorGUILayout.BeginHorizontal();

            for (int x = 0; x < 5; x++)
            {
                SerializedProperty cell =
                    cells.GetArrayElementAtIndex(y * 5 + x);

                Color oldColor = GUI.backgroundColor;

                GUI.backgroundColor = cell.boolValue
                    ? new Color(0.6f, 0.4f, 1f)
                    : Color.gray;

                if (GUILayout.Button(
                    cell.boolValue ? "X" : "",
                    GUILayout.Width(36),
                    GUILayout.Height(36)))
                {
                    cell.boolValue = !cell.boolValue;
                }

                GUI.backgroundColor = oldColor;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Clear Shape"))
        {
            for (int i = 0; i < 25; i++)
                cells.GetArrayElementAtIndex(i).boolValue = false;
        }

        serializedObject.ApplyModifiedProperties();

        ShapeData shape = (ShapeData)target;

        EditorGUILayout.LabelField(
            "Occupied cells",
            shape.GetCells().Count.ToString());

        if (shape.GetCells().Count == 0)
        {
            EditorGUILayout.HelpBox(
                "This shape is empty and will not be spawned.",
                MessageType.Warning);
        }
    }
}