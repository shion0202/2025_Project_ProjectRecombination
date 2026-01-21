using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kamgam.PowerPivot
{
    public class PivotData : ScriptableObject
    {
        public Vector3 PivotDelta;

        public Mesh OriginalMesh;
        public Mesh ModifiedMesh;

        public static PivotData CreateOrUpdate(
            PivotData data, Mesh originalMesh, Mesh newMesh, Vector3 pivotDelta
            )
        {
            if (data == null)
                data = ScriptableObject.CreateInstance<PivotData>();

            data.OriginalMesh = originalMesh;
            data.ModifiedMesh = newMesh;
            data.PivotDelta = pivotDelta;

            Save(data);

            return data;
        }

        public static void Save(PivotData data)
        {
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(PivotData))]
    public class PivotDataEditor : UnityEditor.Editor
    {
        PivotData obj;

        public void OnEnable()
        {
            obj = target as PivotData;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button(new GUIContent("Refresh", "Regenerates the modified mesh based on the original mesh and the pivot delta.")))
            {
                PowerPivotTool.RefreshModel(obj);
            }
        }
    }
#endif
}