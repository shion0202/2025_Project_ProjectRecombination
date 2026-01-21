#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GradientTextureGenerator))]
public class GradientTextureGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GradientTextureGenerator script = (GradientTextureGenerator)target;
        if (GUILayout.Button("그래디언트 텍스처 저장"))
        {
            script.BakeGradient();
        }
    }
}
#endif