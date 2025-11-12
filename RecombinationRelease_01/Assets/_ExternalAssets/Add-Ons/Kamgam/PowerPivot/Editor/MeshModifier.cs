using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace Kamgam.PowerPivot
{
    public partial class MeshModifier
    {
        public const string POWER_PIVOT_MARKER = ".pp";

        /// <summary>
        /// The file extensions first part is used to decide whether to
        /// simply update the mesh inside the asset or to create a new
        /// mesh asset file.
        /// </summary>
        const string FILE_EXTENSION = POWER_PIVOT_MARKER + ".asset";

        public GameObject GameObject;
        public Component Component => SkinnedMeshRenderer != null ? SkinnedMeshRenderer : MeshFilter;
        public SkinnedMeshRenderer SkinnedMeshRenderer;
        public MeshRenderer MeshRenderer;
        public MeshFilter MeshFilter;
        public Mesh NewMesh;
        public bool DeleteNewMeshAfterResetToOriginal;
        public bool IsSkinned => SkinnedMeshRenderer != null;
        public bool HasBones => SkinnedMeshRenderer != null && SkinnedMeshRenderer.bones.Length > 0;

        public bool HasOriginalMesh => _originalMesh != null;

        protected Mesh _originalMesh;
        protected Mesh _meshCopy;
        protected BoneData _boneData;
        protected Material[] _materials;
        protected bool _newMeshExistsAsAsset;

        /// <summary>
        /// New Mesh Modifier operating on the first sharedMesh it finds on the given component (MeshRenderer, MeshFilter or SkinnedMeshRenderer).
        /// </summary>
        public MeshModifier(GameObject go)
        {
            GameObject = go;

            findMeshComponents(go);

            _meshCopy = new Mesh();
        }

        public void AssignSharedMesh(Mesh mesh)
        {
            if (mesh == null)
                return;

            if (IsSkinned)
            {
                SkinnedMeshRenderer.sharedMesh = mesh;
            }
            else
            {
                MeshFilter.sharedMesh = mesh;
            }
        }

        private void findMeshComponents(GameObject go)
        {
            MeshRenderer = go.GetComponent<MeshRenderer>();
            MeshFilter = go.GetComponent<MeshFilter>();
            SkinnedMeshRenderer = go.GetComponent<SkinnedMeshRenderer>();

            if ((MeshFilter == null || MeshRenderer == null) && SkinnedMeshRenderer == null)
            {
                Logger.LogWarning(go + " does not contain any meshes. Aborting action.");
            }
            else if ((MeshFilter != null && MeshFilter.sharedMesh == null) || (SkinnedMeshRenderer != null && SkinnedMeshRenderer.sharedMesh == null))
            {
                Logger.LogWarning(go + " does not contain any meshes. Aborting action");
            }
        }

        private Mesh findOriginalMesh(Mesh currentMesh)
        {
            if (currentMesh == null)
                return null;

            var sourcePath = AssetDatabase.GetAssetPath(currentMesh);
            // The current mesh already is an edited mesh. Try to find the original for it by
            // removing the file extension from the path.
            if (sourcePath.EndsWith(FILE_EXTENSION))
            {
                Mesh newOriginalMesh = LoadOriginalFromNewMeshPath(sourcePath);
                return newOriginalMesh;
            }
            else
            {
                return currentMesh;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pivotDelta">How much the pivot should be moved.</param>
        /// <param name="updateColliders">Whether or not the colliders should be displaced too.</param>
        public Mesh CopyOrUpdateMesh(Vector3 pivotDelta, bool updateColliders)
        {
            var currentMesh = GetSharedMeshFromComponent();

            if (currentMesh == null)
                return null;

            var originalMesh = findOriginalMesh(currentMesh);

            Material[] materials = null;
            BoneData boneData = null;
            if (IsSkinned)
            {
                materials = SkinnedMeshRenderer.sharedMaterials;
                boneData = ExtractBoneData(SkinnedMeshRenderer);
            }
            else
            {
                materials = MeshRenderer.sharedMaterials;
            }

            var currentPath = AssetDatabase.GetAssetPath(currentMesh);

            // If the current mesh already is an edited mesh then load it and use it as the NewMesh.
            if (currentPath.EndsWith(FILE_EXTENSION))
            {
                Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(currentPath);
                if (existingMesh != null)
                {
                    displaceVertices(existingMesh, existingMesh, pivotDelta);
                    existingMesh.RecalculateBounds();
                    EditorUtility.SetDirty(existingMesh);
                }

                if (IsSkinned)
                {
                    var bounds = SkinnedMeshRenderer.bounds;
                    bounds.center -= pivotDelta;
                    SkinnedMeshRenderer.bounds = bounds;
                }

                if (updateColliders)
                    displaceColliders(GameObject, pivotDelta);

                // Update pivot data if it exists
                PivotData pivotData = LoadPivotDataFromNewMeshPath(currentPath);
                if(pivotData != null)
                {
                    pivotData.PivotDelta += pivotDelta;
                    PivotData.Save(pivotData);
                }
                else
                {
                    if (originalMesh != null)
                    {
                        pivotData = PivotData.CreateOrUpdate(pivotData, originalMesh, existingMesh, pivotDelta);
                        string filePath = GetNewMeshFilePath(originalMesh, null, makeRelative: true);
                        AssetExporter.CreatePivotDataAsset(existingMesh, pivotData, filePath);
                    }
                }

                return currentMesh;
            }
            else
            {
                // New mesh path
                string filePath = GetNewMeshFilePath(currentMesh, null, makeRelative: true);

                // Check if a file already exists there.
                Mesh newMesh = null;
                if (filePath != null)
                {
                    newMesh = AssetDatabase.LoadAssetAtPath<Mesh>(filePath);
                }

                // Check if pivot data already exists
                PivotData pivotData = null;
                if (filePath != null)
                {
                    pivotData = LoadPivotDataFromNewMeshPath(filePath);
                }
                pivotData = PivotData.CreateOrUpdate(pivotData, currentMesh, newMesh, pivotDelta);

                string objName = System.IO.Path.GetFileName(filePath).Replace(".asset", "");
                if (newMesh == null)
                {
                    newMesh = new Mesh();
                    newMesh.name = objName;
                    pivotData.ModifiedMesh = newMesh;

                    CopyAndDisplaceMesh(currentMesh, newMesh, pivotDelta);
                    AssetExporter.SaveMeshAsAsset(newMesh, boneData, pivotData, filePath, logFilePaths: true);
                }
                else
                {
                    newMesh.name = objName;
                    CopyAndDisplaceMesh(currentMesh, newMesh, pivotDelta);
                }
                EditorUtility.SetDirty(newMesh);
                AssetDatabase.SaveAssetIfDirty(newMesh);

                if (IsSkinned)
                {
                    var bounds = SkinnedMeshRenderer.bounds;
                    bounds.center -= pivotDelta;
                    SkinnedMeshRenderer.bounds = bounds;
                }

                if (updateColliders)
                    displaceColliders(GameObject, pivotDelta);

                return newMesh;
            }
        }

        public static void CopyAndDisplaceMesh(Mesh source, Mesh target, Vector3 pivotDelta)
        {
            target.Clear();
            target.indexFormat = source.vertexCount > 65536 ? IndexFormat.UInt32 : IndexFormat.UInt16;

            // Blend shapes
            target.vertices = source.vertices;
            target.normals = source.normals;
            target.tangents = source.tangents;
            target.colors = source.colors;
            target.uv = source.uv;
            target.uv2 = source.uv2;
            target.uv3 = source.uv3;
            target.uv4 = source.uv4;
            target.uv5 = source.uv5;
            target.uv6 = source.uv6;
            target.uv7 = source.uv7;
            target.uv8 = source.uv8;
            target.SetBoneWeights(source.GetBonesPerVertex(), source.GetAllBoneWeights());
            target.bindposes = source.bindposes;
            target.subMeshCount = source.subMeshCount;
            for (int m = 0; m < target.subMeshCount; m++)
            {
                target.SetTriangles(source.GetTriangles(m), m);
            }
            target.ClearBlendShapes();
            for (int i = 0; i < source.blendShapeCount; i++)
            {
                string name = source.GetBlendShapeName(i);
                int frameCount = source.GetBlendShapeFrameCount(i);
                for (int f = 0; f < frameCount; f++)
                {
                    var weight = source.GetBlendShapeFrameWeight(i, f);
                    Vector3[] deltaVertices = new Vector3[source.vertexCount];
                    Vector3[] deltaNormals = new Vector3[source.vertexCount];
                    Vector3[] deltaTangents = new Vector3[source.vertexCount];
                    source.GetBlendShapeFrameVertices(i, f, deltaVertices, deltaNormals, deltaTangents);
                    target.AddBlendShapeFrame(name, weight, deltaVertices, deltaNormals, deltaTangents);
                }
            }

            // Notice: Vertices are displaced AFTER the blend shapes have been copied.
            displaceVertices(source, target, pivotDelta);

            target.RecalculateBounds();
        }

        static void displaceVertices(Mesh source, Mesh target, Vector3 pivotDelta)
        {
            var vertices = source.vertices;
            int vCount = vertices.Length;
            float dX = -pivotDelta.x;
            float dY = -pivotDelta.y;
            float dZ = -pivotDelta.z;
            for (int i = 0; i < vCount; i++)
            {
                vertices[i].x += dX;
                vertices[i].y += dY;
                vertices[i].z += dZ;
            }
            target.vertices = vertices;
        }

        protected void displaceColliders(GameObject go, Vector3 pivotDelta)
        {
            // 3D
            var boxColliders = go.GetComponents<BoxCollider>();
            foreach (var boxCollider in boxColliders)
            {
                var center = boxCollider.center;
                center -= pivotDelta;
                boxCollider.center = center;
            }

            var sphereColliders = go.GetComponents<SphereCollider>();
            foreach (var sphereCollider in sphereColliders)
            {
                var center = sphereCollider.center;
                center -= pivotDelta;
                sphereCollider.center = center;
            }

            var capsuleColliders = go.GetComponents<CapsuleCollider>();
            foreach (var capsuleCollider in capsuleColliders)
            {
                var center = capsuleCollider.center;
                center -= pivotDelta;
                capsuleCollider.center = center;
            }

            var wheelColliders = go.GetComponents<WheelCollider>();
            foreach (var wheelCollider in wheelColliders)
            {
                var center = wheelCollider.center;
                center -= pivotDelta;
                wheelCollider.center = center;
            }

            var meshColliders = go.GetComponents<MeshCollider>();
            foreach (var meshCollider in meshColliders)
            {
                displaceVertices(meshCollider.sharedMesh, meshCollider.sharedMesh, pivotDelta);
            }

            // 2D
            var boxColliders2D = go.GetComponents<BoxCollider2D>();
            foreach (var boxCollider2D in boxColliders2D)
            {
                var offset = boxCollider2D.offset;
                offset.x -= pivotDelta.x;
                offset.y -= pivotDelta.y;
                boxCollider2D.offset = offset;
            }

            var circleColliders2D = go.GetComponents<CircleCollider2D>();
            foreach (var circleCollider2D in circleColliders2D)
            {
                var offset = circleCollider2D.offset;
                offset.x -= pivotDelta.x;
                offset.y -= pivotDelta.y;
                circleCollider2D.offset = offset;
            }

            var capsuleColliders2D = go.GetComponents<CapsuleCollider2D>();
            foreach (var capsuleCollider2D in capsuleColliders2D)
            {
                var offset = capsuleCollider2D.offset;
                offset.x -= pivotDelta.x;
                offset.y -= pivotDelta.y;
                capsuleCollider2D.offset = offset;
            }

            var polygonColliders2D = go.GetComponents<PolygonCollider2D>();
            foreach (var polygonCollider2D in polygonColliders2D)
            {
                var pCount = polygonCollider2D.points.Length;
                for (int i = 0; i < pCount; i++)
                {
                    polygonCollider2D.points[i].x -= pivotDelta.x;
                    polygonCollider2D.points[i].y -= pivotDelta.y;
                }
            }
        }

        public bool HasMesh()
        {
            return NewMesh != null;
        }

        public bool IsUsingEditedMesh()
        {
            var mesh = GetSharedMeshFromComponent();
            if (mesh == null)
                return false;

            if (string.IsNullOrEmpty(mesh.name))
                return false;

            return mesh.name.EndsWith(POWER_PIVOT_MARKER);
        }

        public Material[] GetSharedMaterialsFromComponent()
        {
            if (IsSkinned)
            {
                if (SkinnedMeshRenderer != null)
                {
                    return SkinnedMeshRenderer.sharedMaterials;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (MeshRenderer != null)
                {
                    return MeshRenderer.sharedMaterials;
                }
                else
                {
                    return null;
                }
            }
        }

        public Mesh GetSharedMeshFromComponent()
        {
            if (IsSkinned)
            {
                if (SkinnedMeshRenderer != null)
                {
                    return SkinnedMeshRenderer.sharedMesh;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (MeshFilter != null)
                {
                    return MeshFilter.sharedMesh;
                }
                else
                {
                    return null;
                }
            }
        }

        public void ResetAll()
        {
            if (_originalMesh == null)
            {
                return;
            }

            var currentMesh = GetSharedMeshFromComponent();
            EditorGUIUtility.PingObject(currentMesh);
            string currentPath = AssetDatabase.GetAssetPath(currentMesh);

            var origPath = AssetDatabase.GetAssetPath(_originalMesh);
            // If the original mesh already was an edited mesh then we will have to use the copied mesh.
            if (origPath.EndsWith(FILE_EXTENSION))
            {
                CopyAndDisplaceMesh(_meshCopy, GetSharedMeshFromComponent(), Vector3.zero);
                applyMeshAndMaterials(GetSharedMeshFromComponent(), _materials);
            }
            // Otherwise we can simply reset to the original mesh
            else
            {
                applyMeshAndMaterials(_originalMesh, _materials);

                // Delete
                if (DeleteNewMeshAfterResetToOriginal && !string.IsNullOrEmpty(currentPath) && currentPath.EndsWith(FILE_EXTENSION) && currentPath.StartsWith("Assets"))
                {
                    AssetDatabase.DeleteAsset(currentPath);
                    EditorGUIUtility.PingObject(_originalMesh);
                }
            }
        }

        public void ResetToOriginal()
        {
            if (_originalMesh == null)
            {
                Logger.LogWarning("We have no original mesh");
                return;
            }

            var currentMesh = GetSharedMeshFromComponent();
            EditorGUIUtility.PingObject(currentMesh);

            // Try to find the original asset first. If that fails then revert to ResetAll().
            var currentPath = AssetDatabase.GetAssetPath(currentMesh);
            if (!string.IsNullOrEmpty(currentPath))
            {
                if (IsNameOfEditedMesh(currentPath))
                {
                    var originalMesh = LoadOriginalFromNewMeshPath(currentPath);
                    if (originalMesh != null)
                    {
                        applyMeshAndMaterials(originalMesh, _materials);

                        // Delete
                        if (DeleteNewMeshAfterResetToOriginal && !string.IsNullOrEmpty(currentPath) && currentPath.EndsWith(FILE_EXTENSION) && currentPath.StartsWith("Assets"))
                        {
                            AssetDatabase.DeleteAsset(currentPath);
                            EditorGUIUtility.PingObject(_originalMesh);
                        }
                    }
                    else
                    {
                        ResetAll();
                    }
                }
            }
            else
            {
                ResetAll();
            }
        }

        void applyMeshAndMaterials(Mesh mesh, Material[] materials)
        {
            // Assign mesh and materials
            if (IsSkinned)
            {
                if (SkinnedMeshRenderer != null)
                {
                    SkinnedMeshRenderer.sharedMaterials = materials;
                    SkinnedMeshRenderer.sharedMesh = mesh;
                    SkinnedMeshRenderer.sharedMesh.MarkModified();

                    EditorUtility.SetDirty(SkinnedMeshRenderer);
                    EditorUtility.SetDirty(SkinnedMeshRenderer.gameObject);
                }
            }
            else
            {
                if (MeshRenderer != null && MeshFilter != null)
                {
                    MeshRenderer.sharedMaterials = materials;
                    MeshFilter.sharedMesh = mesh;
                    MeshFilter.sharedMesh.MarkModified();

                    EditorUtility.SetDirty(MeshRenderer);
                    EditorUtility.SetDirty(MeshFilter);
                    EditorUtility.SetDirty(MeshRenderer.gameObject);
                }
            }
        }

        bool fileExists(string filePathInsideAssetsDir)
        {
            string absoluteFilePath = Application.dataPath + System.IO.Path.DirectorySeparatorChar + filePathInsideAssetsDir;
            return System.IO.File.Exists(absoluteFilePath);
        }

        void deleteFile(string filePathInsideAssetsDir)
        {
            string absoluteFilePath = Application.dataPath + System.IO.Path.DirectorySeparatorChar + filePathInsideAssetsDir;

            if (System.IO.File.Exists(absoluteFilePath))
            {
                System.IO.File.Delete(absoluteFilePath);
            }

            if (System.IO.File.Exists(absoluteFilePath + ".meta"))
            {
                System.IO.File.Delete(absoluteFilePath + ".meta");
            }
        }
    }
}
