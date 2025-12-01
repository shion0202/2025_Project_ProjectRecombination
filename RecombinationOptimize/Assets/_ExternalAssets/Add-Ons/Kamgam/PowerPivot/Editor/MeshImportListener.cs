using UnityEngine;
using UnityEditor;

namespace Kamgam.PowerPivot
{
    public class MeshImportListener : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            var settings = PowerPivotSettings.GetOrCreateSettings();
            if (!settings.AutoRefreshModelsWithModifiedPivots)
                return;

            var pivotDatas = AssetDatabase.FindAssets("t:PivotData");
            if (pivotDatas == null || pivotDatas.Length == 0)
            {
                return;
            }

            foreach (string path in importedAssets)
            {
                // Only handle model files
                if (   path.EndsWith(".fbx", System.StringComparison.InvariantCultureIgnoreCase)
                    || path.EndsWith(".blend", System.StringComparison.InvariantCultureIgnoreCase)
                    || path.EndsWith(".ma", System.StringComparison.InvariantCultureIgnoreCase)
                    || path.EndsWith(".dae", System.StringComparison.InvariantCultureIgnoreCase)
                    || path.EndsWith(".3ds", System.StringComparison.InvariantCultureIgnoreCase)
                    || path.EndsWith(".dxf", System.StringComparison.InvariantCultureIgnoreCase)
                    || path.EndsWith(".obj", System.StringComparison.InvariantCultureIgnoreCase)
                    || path.EndsWith(".gltf", System.StringComparison.InvariantCultureIgnoreCase)
                    )
                {
                    var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (gameObject != null)
                    {
                        var meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
                        foreach (var filter in meshFilters)
                        {
                            // foreach each mesh inside the object
                            var newPath = MeshModifier.GetNewMeshFilePath(filter.sharedMesh, null, true);
                            var data = MeshModifier.LoadPivotDataFromNewMeshPath(newPath);
                            if (data != null)
                            {
                                Logger.Log("Refreshing modified pivot model: " + path + "\n" +
                                    "You can turn this off in the PowerPivot settings under 'AutoRefreshModelsWithModifiedPivots'.");
                                PowerPivotTool.RefreshModel(data);
                            }
                        }

                        var skinnedRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                        foreach (var renderer in skinnedRenderers)
                        {
                            // foreach each mesh inside the object
                            var newPath = MeshModifier.GetNewMeshFilePath(renderer.sharedMesh, null, true);
                            var data = MeshModifier.LoadPivotDataFromNewMeshPath(newPath);
                            if (data != null)
                            {
                                Logger.LogMessage("Refreshing modified pivot model: " + path + "\n" +
                                    "You can turn this off in the PowerPivot settings under 'AutoRefreshModelsWithModifiedPivots'.");
                                PowerPivotTool.RefreshModel(data);
                            }
                        }
                    }
                }
            }
        }

    }
}