using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kamgam.PowerPivot
{
    public static class AssetExporter
    {
#if UNITY_EDITOR

        public const string PIVOT_DATA_SUFFIX = "_PivotData";
        public const string BONE_DATA_SUFFIX = "_BoneData";

        public static void SaveMeshAsAsset(Mesh mesh, BoneData boneData, PivotData pivotData, string assetPath, bool logFilePaths = true)
        {
            if (mesh == null)
                return;

            // Ensure the path starts with "Assets/".
            if (!assetPath.StartsWith("Assets"))
            {
                if (assetPath.StartsWith("/"))
                {
                    assetPath = "Assets" + assetPath;
                }
                else
                {
                    assetPath = "Assets/" + assetPath;
                }
            }

            string dirPath = System.IO.Path.GetDirectoryName(Application.dataPath + "/../" + assetPath);
            if (!System.IO.Directory.Exists(dirPath))
            {
                System.IO.Directory.CreateDirectory(dirPath);
            }

            // Create or replace Mesh asset
            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existingMesh != null)
            {
                Undo.RegisterCompleteObjectUndo(existingMesh, "Create new mesh");
            }
            AssetDatabase.CreateAsset(mesh, assetPath);

            // Create or replace BoneData asset
            /*
            if (boneData != null)
            {
                var extension = System.IO.Path.GetExtension(assetPath); // extension with dot
                var path = assetPath.Substring(0, assetPath.Length - extension.Length);
                var boneDataAssetPath = path + BONE_DATA_SUFFIX + extension;
                var existingBoneData = AssetDatabase.LoadAssetAtPath<BoneData>(boneDataAssetPath);
                if (existingBoneData != null)
                {
                    Undo.RegisterCompleteObjectUndo(existingBoneData, "Create new bone data");
                    AssetDatabase.DeleteAsset(boneDataAssetPath);
                }
                AssetDatabase.CreateAsset(boneData, boneDataAssetPath);
            }
            */

            // Create or replace PivotData asset
            CreatePivotDataAsset(mesh, pivotData, assetPath, logFilePaths);

            AssetDatabase.SaveAssets();
            // Important to force the reimport to avoid the "SkinnedMeshRenderer: Mesh has
            // been changed to one which is not compatibile with the expected mesh data size
            // and vertex stride." error.
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            if (logFilePaths)
                Logger.LogMessage($"Saved new mesh under <color=yellow>'{assetPath}'</color>.");
        }

        public static void CreatePivotDataAsset(Mesh mesh, PivotData pivotData, string assetPath, bool logFilePaths = true)
        {
            // Create or replace PivotData asset
            if (pivotData != null)
            {
                var extension = System.IO.Path.GetExtension(assetPath); // extension with dot
                var path = assetPath.Substring(0, assetPath.Length - extension.Length);
                var pivotDataAssetPath = path + PIVOT_DATA_SUFFIX + extension;
                var existingPivotData = AssetDatabase.LoadAssetAtPath<PivotData>(pivotDataAssetPath);
                if (existingPivotData != null)
                {
                    Undo.RegisterCompleteObjectUndo(existingPivotData, "Create new pivot data");
                    AssetDatabase.DeleteAsset(pivotDataAssetPath);
                }
                AssetDatabase.CreateAsset(pivotData, pivotDataAssetPath);
            }
        }

        public static string MakePathRelativeToProjectRoot(string assetPath)
        {
            // Make path relative
            assetPath = assetPath.Replace("\\", "/");
            var dataPath = Application.dataPath.Replace("\\", "/");
            assetPath = assetPath.Replace(dataPath, "");

            // Ensure the path starts with "Assets/".
            if (!assetPath.StartsWith("Assets"))
            {
                if (assetPath.StartsWith("/"))
                {
                    assetPath = "Assets" + assetPath;
                }
                else
                {
                    assetPath = "Assets/" + assetPath;
                }
            }

            return assetPath;
        }
#endif

    }
}

