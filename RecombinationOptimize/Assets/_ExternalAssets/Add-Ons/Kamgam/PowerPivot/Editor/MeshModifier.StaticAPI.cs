using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace Kamgam.PowerPivot
{
    public partial class MeshModifier
    {
        public static bool IsNameOfEditedMesh(string nameOrPath)
        {
            if (nameOrPath == null)
                return false;

            return nameOrPath.EndsWith(FILE_EXTENSION) || nameOrPath.EndsWith(POWER_PIVOT_MARKER);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="originalMesh"></param>
        /// <param name="newMesh"></param>
        /// <param name="makeRelative">If true then the path will be made relative to Assets/. If false then the path will remain as is (no change).</param>
        /// <returns></returns>
        public static string GetNewMeshFilePath(Mesh originalMesh, Mesh newMesh, bool makeRelative = false)
        {
            string absolutePath = null;

            string originalMeshPath = AssetDatabase.GetAssetPath(originalMesh);
            if (string.IsNullOrEmpty(originalMeshPath))
            {
                if (newMesh != null)
                {
                    originalMeshPath = "Assets/Mesh" + (newMesh.GetInstanceID() + newMesh.vertexCount).ToString();
                }
                else
                {
                    originalMeshPath = "Assets/Mesh" + Random.Range(1000, 99800).ToString();
                }

                absolutePath = System.IO.Path.GetDirectoryName(originalMeshPath) + System.IO.Path.DirectorySeparatorChar + System.IO.Path.GetFileName(originalMeshPath) + "." + FILE_EXTENSION;
            }
            else
            {
                var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(originalMeshPath);
                if (assets == null || assets.Length == 0)
                {
                    absolutePath = System.IO.Path.GetDirectoryName(originalMeshPath) + System.IO.Path.DirectorySeparatorChar + System.IO.Path.GetFileName(originalMeshPath) + "." + FILE_EXTENSION;
                }
                else
                {
                    // The asset file contains more than one asset. Therefore we need to find the our original mesh in there.
                    var originalMeshAsObject = originalMesh as Object;
                    foreach (var asset in assets)
                    {
                        if (asset == originalMeshAsObject)
                        {
                            string nameForPath = NameToValidPath(asset.name);
                            absolutePath = System.IO.Path.GetDirectoryName(originalMeshPath) + System.IO.Path.DirectorySeparatorChar + System.IO.Path.GetFileName(originalMeshPath) + "." + nameForPath + FILE_EXTENSION;
                            break;
                        }
                    }
                }
            }

            if (makeRelative)
            {
                string relativePath = MakePathRelativeToAssets(absolutePath);
                return relativePath;
            }
            else
            {
                return absolutePath;
            }
        }

        public static string NameToValidPath(string name)
        {
            string pathName = System.Text.RegularExpressions.Regex.Replace(name, "[^a-zA-Z0-9_-]", "");
            if (string.IsNullOrEmpty(pathName))
            {
                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(name);
                return System.Convert.ToBase64String(plainTextBytes);
            }
            else
            {
                return pathName;
            }
        }

        public static Mesh LoadOriginalFromNewMeshPath(string path)
        {
            path = MakePathRelativeToAssets(path);

            // Extract original path and asset name from path and then load the mesh.
            var pathWithoutExtension = path;
            if (path.EndsWith(FILE_EXTENSION))
            {
                pathWithoutExtension = path.Replace(FILE_EXTENSION, "");
            }
            if (path.EndsWith(POWER_PIVOT_MARKER))
            {
                pathWithoutExtension = path.Replace(POWER_PIVOT_MARKER, "");
            }
            var pathParts = pathWithoutExtension.Split('.');
            var name = pathParts[pathParts.Length - 1];

            var originalAssetPath = pathWithoutExtension;
            if (string.IsNullOrEmpty(name))
            {
                // Remove trailing dot
                originalAssetPath = originalAssetPath.Substring(0, originalAssetPath.Length - 1);

                return AssetDatabase.LoadAssetAtPath<Mesh>(originalAssetPath);
            }
            else
            {
                // Remove dot and name
                originalAssetPath = pathWithoutExtension.Replace("." + name, "");

                var assets = AssetDatabase.LoadAllAssetsAtPath(originalAssetPath);
                foreach (var asset in assets)
                {
                    var assetName = NameToValidPath(asset.name);
                    if (assetName == name && asset is Mesh)
                    {
                        return asset as Mesh;
                    }
                }
            }

            return null;
        }

        public static PivotData LoadPivotDataFromNewMeshPath(string path)
        {
            path = MakePathRelativeToAssets(path);
            path = path.Replace(POWER_PIVOT_MARKER, POWER_PIVOT_MARKER + AssetExporter.PIVOT_DATA_SUFFIX);

            return AssetDatabase.LoadAssetAtPath<PivotData>(path);
        }

        public static string MakePathRelativeToAssets(string path)
        {
            // Make path relative
            var relativePath = path;
            relativePath = relativePath.Replace("\\", "/");
            var dataPath = Application.dataPath.Replace("\\", "/");
            relativePath = relativePath.Replace(dataPath, "");

            // Ensure the path starts with "Assets/".
            if (!relativePath.StartsWith("Assets"))
            {
                if (relativePath.StartsWith("/"))
                {
                    relativePath = "Assets" + relativePath;
                }
                else
                {
                    relativePath = "Assets/" + relativePath;
                }
            }

            return relativePath;
        }

        public static BoneData ExtractBoneData(SkinnedMeshRenderer renderer)
        {
            BoneData boneData = null;

            // Extract bone info as a BoneData asset.    
            if (renderer.rootBone != null)
            {
                boneData = ScriptableObject.CreateInstance<BoneData>();
                boneData.ExtractFromRenderer(renderer);
            }
            else
            {
                if (renderer.sharedMesh.boneWeights.Length > 0)
                {
                    Logger.LogWarning("The SkinnedMeshRenderers '" + renderer.name + "' has no root bone! Your bones will not align!\n" +
                        "This is required for mesh bones extraction. " +
                        "Please set the root bone to the root of the rig/armature before exporting.\n" +
                        "Skipping bone-data export for now (your bones will probably not align wit the exported weights).");
                }
            }

            return boneData;
        }
    }
}
