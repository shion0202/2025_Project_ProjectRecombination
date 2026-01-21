using UnityEditor;
using UnityEngine;
using Monster.AI.BehaviorTree;

[CustomEditor(typeof(BehaviorTree))]
public class BTInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(10);
        if (GUILayout.Button("Open Behavior Tree Editor"))
        {
            BTEditorWindow.Open();
        }
    }
}

