using System.Collections.Generic;
using UnityEngine;

namespace Kamgam.PowerPivot
{
    public class BoneData : ScriptableObject
    {
        [SerializeField]
        public string RootName;

        /// <summary>
        /// Bones paths relatie to the RootBone of a SkinnedMeshRenderer.<br />
        /// While the RootBone actually has not miuch to do with the bones 
        /// (it is for mechanim) it is a nice starting point and we assume all 
        /// bone transforms are children of the root bone.
        /// </summary>
        [SerializeField]
        public List<string> _paths;

        protected void definePaths()
        {
            if (_paths == null)
                _paths = new List<string>();
        }

        public List<string> GetPaths()
        {
            definePaths();
            return _paths;
        }

        public void ExtractFromRenderer(SkinnedMeshRenderer renderer)
        {
            definePaths();
            _paths.Clear();

            if (renderer == null || renderer.bones == null || renderer.rootBone == null)
                return;

            var bones = renderer.bones;
            // All bones except the root
            for (int i = 0; i < bones.Length; i++)
            {
                var path = getPathRelativeTo(bones[i], renderer.rootBone);
                // empty path for root (first) bone
                if(i == 0)
                {
                    RootName = bones[0].name;
                    path = "";
                }
                _paths.Add(path);
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public Transform[] FindBonesRelativeTo(Transform rootBone)
        {
            definePaths();

            if (rootBone == null)
                return null;

            var bones = new Transform[_paths.Count];

            // Add root as bone 0
            bones[0] = rootBone;
            if (_paths != null && _paths.Count > 1)
            {
                // Skip first (root)
                for (int i = 1; i < _paths.Count; i++)
                {
                    bones[i] = rootBone.Find(_paths[i]);
                }
            }

            return bones;
        }

        public void ResolveAndApplyTo(Transform rootBone, GameObject gameObject)
        {
            var renderer = gameObject.GetComponent<SkinnedMeshRenderer>();
            ResolveAndApplyTo(rootBone, renderer);
        }

        public void ResolveAndApplyTo(Transform rootBone, SkinnedMeshRenderer renderer)
        {
            if (renderer == null || renderer.rootBone == null)
                return;

            var bones = FindBonesRelativeTo(rootBone);
            if (bones != null)
            {
                renderer.bones = bones;
            }
        }

        protected string getPathRelativeTo(Transform target, Transform root)
        {
            string path = "";

            var current = target;
            do
            {
                if (string.IsNullOrEmpty(path))
                {
                    path = current.name;
                }
                else
                {
                    path = current.name + "/" + path;
                }
                current = current.parent;
            }
            while (current != null && current != root);

            return path;
        }

        /// <summary>
        /// Creates the transform hierarchy matching the bone paths.<br />
        /// Transforms are added as a children of the given game object.<br />
        /// NOTICE: While this will create the corrent hierarchy. The POSITIONS,
        /// ROTATIONS and SCALES will all be default and probably NOT match your
        /// inital setup.
        /// </summary>
        /// <param name="parentOfRootBone"></param>
        /// <returns>Returns the create root transform.</returns>
        public Transform CreateTransforms(Transform parentOfRootBone)
        {
            definePaths();
            if (_paths.Count == 0)
                return null;

            Transform root = null;
            if (parentOfRootBone == null)
            {
                // No parent? Then try to find in scene root.
                var go = GameObject.Find("/" + _paths[0]);
                if(go != null)
                {
                    root = go.transform;
                }
            }
            else
            {
                root = parentOfRootBone.Find(RootName);
            }
            if (root == null)
            {
                root = createTransformInParent(parentOfRootBone, RootName);
            }

            // i = 1 because we skip the root
            for (int i = 1; i < _paths.Count; i++)
            {
                var parent = root;
                var pathParts = _paths[i].Split('/');
                for (int p = 0; p < pathParts.Length; p++)
                {
                    var current = parent.Find(pathParts[p]);
                    if (current == null)
                    {
                        current = createTransformInParent(parent, pathParts[p]);

                        // Guess humanoid bones based on english language (roughly)
                        float x = 0f;
                        float y = 0.1f;
                        if (current.name.ToLower().EndsWith("_l") || current.name.ToLower().StartsWith("l_") || current.name.ToLower().Contains("left") || _paths[i].ToLower().Contains("left"))
                            x = -0.1f;
                        if (current.name.ToLower().EndsWith("_r") || current.name.ToLower().StartsWith("r_") || current.name.ToLower().Contains("right") || _paths[i].ToLower().Contains("right"))
                            x = 0.1f;
                        if (current.name.ToLower().Contains("spine") || current.name.ToLower().Contains("hip"))
                            x = 0f;
                        if (_paths[i].ToLower().Contains("leg") || _paths[i].ToLower().Contains("foot"))
                            y = -0.1f;
                        current.localPosition = new Vector3(x, y);
                    }
                    parent = current;
                }
            }

            return root;
        }

        private Transform createTransformInParent(Transform parent, string name)
        {
            Transform transform;

            var go = new GameObject(name);
            transform = go.transform;

            if (parent != null)
            {
                transform.SetParent(parent);
            }

            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            transform.localPosition = Vector3.zero;

            return transform;
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(BoneData))]
    public class BoneDataEditor : UnityEditor.Editor
    {
        protected bool _extractFoldout = false;
        protected SkinnedMeshRenderer _rendererToExtractFrom;

        public override void OnInspectorGUI()
        {
            GUILayout.Label("INFO");
            UnityEditor.EditorGUILayout.HelpBox(new GUIContent(
                "This stores a list of paths relative to the ROOT BONE of a rig.\n" +
                "It is generated by the Mesh Extractor if bone weights are exported.\n" +
                "You can use this in combination with the BoneDataApplier to update the bones of a SkinnedMeshRenderer."));

            _extractFoldout = UnityEditor.EditorGUILayout.Foldout(_extractFoldout, new GUIContent("Extract Manually"));
            if (_extractFoldout)
            { 
                GUILayout.Label("Drop a SkinnedMeshRenderer in here and hit 'Update' to fetch the paths from the renderer.");

                _rendererToExtractFrom = (SkinnedMeshRenderer)UnityEditor.EditorGUILayout.ObjectField(_rendererToExtractFrom, typeof(SkinnedMeshRenderer), allowSceneObjects: true);

                GUI.enabled = _rendererToExtractFrom != null;
                if (GUILayout.Button("Extract"))
                {
                    if (_rendererToExtractFrom.rootBone != null)
                    {
                        (target as BoneData).ExtractFromRenderer(_rendererToExtractFrom);
                        UnityEditor.EditorUtility.SetDirty(target);
                        UnityEditor.AssetDatabase.SaveAssets();
                    }
                    else
                    {
                        Debug.LogError("No root bone found on SkinnedMeshRenderer.");
                    }
                }
                GUI.enabled = true;
            }

            base.OnInspectorGUI();
        }
    }
#endif
}