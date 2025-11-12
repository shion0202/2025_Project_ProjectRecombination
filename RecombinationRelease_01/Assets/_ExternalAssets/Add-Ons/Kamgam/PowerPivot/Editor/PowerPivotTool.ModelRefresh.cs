using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using System.Collections.Generic;
using System.Text;

namespace Kamgam.PowerPivot
{
    partial class PowerPivotTool
    {
        public static void RefreshModel(Mesh mesh)
        {
            if (mesh == null)
                return;

            var pivots = AssetDatabase.FindAssets("t:PivotData");
            foreach (var guid in pivots)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<PivotData>(path);
                if (data.OriginalMesh == mesh || data.ModifiedMesh == mesh)
                {
                    RefreshModel(data);
                    break;
                }
            }
        }

        public static void RefreshModel(PivotData pivotData)
        {
            if (pivotData.OriginalMesh != null && pivotData.ModifiedMesh != null)
            {
                MeshModifier.CopyAndDisplaceMesh(pivotData.OriginalMesh, pivotData.ModifiedMesh, pivotData.PivotDelta);
            }
        }
    }
}
