#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace Kamgam.PowerPivot
{
    /// <summary>
    /// It requires some BoneData (which contains a list of string paths like 'root/hips/spine').<br />
    /// If Resolve() is called it will try to find bine pose data for the bones. <br />
    /// It searches in multiple locations:<br />
    /// 1) First is searches for nearby SkinnedMeshRenderers (two levels up and down in the hierarchy).<br />
    /// 2) If no renderers are found it will try to find a sibling named like BoneData.RootName and use
    ///    that as root bone. NOTICE: This may lead to distortions if the order of the transforms in the
    ///    hierarchy does not match the mesh bind pose order (if often does not).
    /// <br /><br />
    /// You can use this at runtime though it is recommended you use it just once in the editor like this:<br />
    /// 1) Add the 'BoneDataApplier' as a Component to your SkinnedMeshRenderer gameObject.<br />
    /// 2) Drag in the BoneData asset into the BoneData slot.<br />
    /// 3) Make sure the original SkinnedMeshRenderer is nearby (sibling level in hierarchy).
    /// 4) Hit the 'Resolve' button.<br />
    /// 5) Delete the 'BoneDataApplier'.<br />
    /// 6) Save the scene (so the new bones are saved within the renderer).<br />
    /// </summary>
    [ExecuteAlways]
    public class BoneDataResolver : MonoBehaviour
    {
        public BoneData BoneData;

        /// <summary>
        /// Auto resolve OnEnable?<br />
        /// Happens only if the current renderer has no valid bones assigned.
        /// </summary>
        [Tooltip("Auto resolve OnEnable?\nHappens only if the current renderer has no valid bones assigned.")]
        public bool ResolveOnEnable = true;

        public bool EditorLogsEnabled = true;

        protected SkinnedMeshRenderer _renderer;
        public SkinnedMeshRenderer Renderer
        {
            get
            {
                if (_renderer == null)
                {
                    _renderer = this.GetComponent<SkinnedMeshRenderer>();
                }
                return _renderer;
            }
        }

        public bool RendererHasValidBones()
        {
            return Renderer.bones != null && Renderer.bones.Length > 0 && Renderer.bones[0] != null;
        }

        public void OnEnable()
        {
            // Auto resolve on enable but only if needed.
            if (ResolveOnEnable && !RendererHasValidBones())
            {
                Resolve();
            }
        }

        public bool Resolve()
        {
            if (BoneData != null && Renderer != null)
            {
                Transform existingRootBone = Renderer.rootBone;
                Transform rootBone = null;

                // Try to get root bone from other renderers nearby
                var parent = Renderer.transform.parent;

                // If there is only one renderer then try the parent of the parent (but not any further up).
                // We check for the parent of the parent because that's how the MeshExtractor may have set
                // up the prefab (renderer as child or prefab root) and we want to search one level up of the
                // prefab.
                if (parent != null)
                {
                    var renderers = parent.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
                    if (renderers.Length == 1)
                    {
                        parent = parent.parent;
                    }
                }

                Transform searchBase = parent;
                if (parent == null)
                {
                    // No parent? Then find in scene root.
                    GameObject rootGO = GameObject.Find("/" + BoneData.RootName);
                    if (rootGO != null)
                    {
                        searchBase = rootGO.transform;
                    }
                }
                if (searchBase != null)
                {
                    var renderers = searchBase.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
                    foreach (var renderer in renderers)
                    {
                        if (renderer == Renderer)
                            continue;

                        // Don't check the whole hierarchy. We are lazy and thus use the first one matching the root bone name.
                        if (renderer.rootBone != null &&
                            renderer.rootBone.transform.name == BoneData.RootName)
                        {
                            // Use this a the new rootBone
                            rootBone = renderer.rootBone;
#if UNITY_EDITOR
                            if (EditorLogsEnabled)
                            {
                                Debug.Log("Found matching root bone on RENDERER '" + renderer + "'. Will apply them to '" + Renderer.name + "'");
                            }
#endif
                            break;
                        }
                    }
                }

                // No skinned Mesh renderer found to take the root bone from.
                // Let's look if any of the sibling is named like the root bone.
                if (Renderer.transform.parent != null)
                {
                    searchBase = Renderer.transform.parent;
                }
                else
                {
                    // No parent? Then find in scene root.
                    GameObject rootGO = GameObject.Find("/" + BoneData.RootName);
                    if (rootGO != null)
                    {
                        searchBase = rootGO.transform;
                    }
                }
                if (searchBase != null)
                {
                    var renderers = searchBase.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);

                    // Don't check the whole hierarchy. We are lazy and thus use the first one matching the root bone name.
                    var root = searchBase.Find(BoneData.RootName);
                    if (root != null)
                    {
                        // Use this a the new rootBone
                        rootBone = root;
#if UNITY_EDITOR
                        if (EditorLogsEnabled)
                        {
                            Debug.Log("Found matching root bone on TRANSFORM in '" + searchBase.name + "'. Will apply it to '" + Renderer.name + "'.");
                        }
#endif
                    }
                }

                // No roots found in renderers nearby, fall back to the existing root (may be null too though).
                if (rootBone == null)
                {
                    rootBone = existingRootBone;
                }

                return Resolve(rootBone);
            }

            return false;
        }

        public bool Resolve(GameObject rootBone)
        {
            if (rootBone == null)
                return false;

            return Resolve(rootBone.transform);
        }

        public bool Resolve(Transform rootBone)
        {
            if (BoneData != null && Renderer != null)
            {
                if (rootBone == null)
                    return false;

                Renderer.rootBone = rootBone;
                BoneData.ResolveAndApplyTo(rootBone, Renderer);
                
                Renderer.sharedMesh.RecalculateBounds();
                Renderer.localBounds = Renderer.sharedMesh.bounds;

#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// If a renderer is used to resolve the bones then they are just copied over and BoneData is bypassed completely.
        /// </summary>
        /// <param name="renderer"></param>
        /// <returns></returns>
        public bool Resolve(SkinnedMeshRenderer renderer)
        {
            if (BoneData != null && Renderer != null)
            {
                // Skip if renderer has no valid bones.
                if (renderer == null || renderer.bones == null || renderer.bones.Length == 0 || renderer.bones[0] == null)
                    return false;

                Renderer.rootBone = renderer.rootBone;
                Renderer.bones = renderer.bones;
                
                Renderer.sharedMesh.RecalculateBounds();
                Renderer.localBounds = Renderer.sharedMesh.bounds;

#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a hierarchy of transforms matching the bone paths.<br />
        /// Transforms are added as a children of the given parent object.<br />
        /// NOTICE: While this will create the corret hierarchy. The POSITIONS,
        /// ROTATIONS and SCALES will all be off and probably NOT match your
        /// inital rig.
        /// </summary>
        /// <param name="parentOfRootBone"></param>
        /// <returns>Returns the create root transform.</returns>
        public Transform CreateTransforms(Transform parentOfRootBone)
        {
            if (BoneData == null)
                return null;
            
            return BoneData.CreateTransforms(parentOfRootBone);
        }

        /// <summary>
        /// For reach bind pose in the mesh a null transform bone will be added to the renderer.<br />
        /// This has no functional purose. It simply exists to avoid
        /// the 'Bones do not match bindpose.' errors.
        /// </summary>
        public void AssignNullTransforms()
        {
            if (Renderer == null)
                return;

            if (Renderer.sharedMesh != null)
            {
                if(Renderer.sharedMesh.bindposes != null)
                {
                    int numOfBones = Renderer.sharedMesh.bindposes.Length;
                    Renderer.bones = new Transform[numOfBones];
                }
            }
            else if(BoneData != null)
            {
                int numOfBones = BoneData.GetPaths().Count;
                Renderer.bones = new Transform[numOfBones];
            }
        }
    }






#if UNITY_EDITOR
    [CustomEditor(typeof(BoneDataResolver))]
    public class SkinnedMeshBoneDataResolverEditor : Editor
    {
        protected bool _infoFoldout = false;
        protected bool _bonesOnMeshFoldout = false;
        protected bool _bonesInDataFoldout = false;

        protected SkinnedMeshRenderer _rendererToCopyBonesFrom;

        public override void OnInspectorGUI()
        {
            var col = GUI.color;
            GUI.color = Color.cyan;
            _infoFoldout = EditorGUILayout.Foldout(_infoFoldout, "READE ME !!!");
            GUI.color = col;
            if (_infoFoldout)
            {
                EditorGUILayout.HelpBox(new GUIContent(
                        "It requires some BoneData (which contains a list of string paths like 'root/hips/spine').\n" +
                        "If Resolve() is called it will try to find bine pose data for the bones. \n" +
                        "It searches in multiple locations:\n" +
                        "1) First is searches for nearby SkinnedMeshRenderers (two levels up and down in the hierarchy).\n" +
                        "2) If no renderers are found it will try to find a sibling named like BoneData.RootName and use\n" +
                        "   that as root bone. NOTICE: This may lead to distortions if the order of the transforms in the\n" +
                        "   hierarchy does not match the mesh bind pose order (if often does not).\n" +
                        "\n\n" +
                        "You can use this at runtime though it is recommended you use it just once in the editor like this:\n" +
                        "1) Add the 'BoneDataApplier' as a Component to your SkinnedMeshRenderer gameObject.\n" +
                        "2) Drag in the BoneData asset into the BoneData slot.\n" +
                        "3) Make sure the original SkinnedMeshRenderer is nearby (sibling level in hierarchy).\n" +
                        "4) Hit the 'Resolve' button.\n" +
                        "5) Delete the 'BoneDataApplier'.\n" +
                        "6) Save the scene (so the new bones are saved within the renderer)."
                        ));
            }

            GUILayout.Label(new GUIContent("Renderer to copy bones from:", "If set then the bones from this renderer will be copied. This completely bypasses the BoneData since in that case it is not needed."));
            _rendererToCopyBonesFrom = (SkinnedMeshRenderer)UnityEditor.EditorGUILayout.ObjectField(_rendererToCopyBonesFrom, typeof(SkinnedMeshRenderer), allowSceneObjects: true);

            base.OnInspectorGUI();

            var resolver = (target as BoneDataResolver);

            if (resolver == null || resolver.Renderer == null)
                return;

            GUI.enabled = resolver.BoneData != null;
            if (GUILayout.Button("Resolve"))
            {
                var result = _rendererToCopyBonesFrom == null ? resolver.Resolve() : resolver.Resolve(_rendererToCopyBonesFrom);
                if (result)
                {
                    Debug.Log(resolver.BoneData.GetPaths().Count + " Bones applied.");
                }
                else
                {
                    Debug.LogWarning("SkinnedMeshRenderer or BoneData is missing. Maybe no matching sbiling named '" + resolver.BoneData.RootName + "' or SkinnedMeshRenderer with a root bone named '" + resolver.BoneData.RootName+"' was found.");
                }
            }
            GUI.enabled = true;

            bool needsBoneTransforms = resolver.Renderer != null && (resolver.Renderer.bones == null || (resolver.Renderer.bones.Length > 0 && resolver.Renderer.bones[0] == null));
            GUI.enabled = resolver.BoneData != null && resolver.Renderer != null && needsBoneTransforms;
            if (GUILayout.Button("Create Bone Transforms"))
            {
                var parentOfRootBone = resolver.transform.parent;
                if (parentOfRootBone == null)
                    parentOfRootBone = resolver.transform;
                var newRoot = resolver.CreateTransforms(parentOfRootBone);
                resolver.Renderer.rootBone = newRoot;
                var result = resolver.Resolve();
                if(!result)
                {
                    Debug.LogWarning("SkinnedMeshRenderer or BoneData is missing.");
                }
            }
            GUI.enabled = true;

            if (GUILayout.Button(new GUIContent("Reset To NULL", "Resets all the bone data to null to avoid 'Bones do not match bindpose.' errors. All the bones will be null and just do nothing. Be sure to assign proper bones later.")))
            {
                resolver.AssignNullTransforms();
            }

            GUILayout.Space(7);
            if (resolver.BoneData != null)
            {
                _bonesInDataFoldout = EditorGUILayout.Foldout(_bonesInDataFoldout, new GUIContent(resolver.Renderer.bones.Length + " Bone paths in BoneData '"+resolver.BoneData.name+"':"));
                if (_bonesInDataFoldout)
                {
                    foreach (var path in resolver.BoneData.GetPaths())
                    {
                        GUILayout.Label(path);
                    }
                }
            }
            else
            {
                GUILayout.Label("No BoneData");
            }

            GUILayout.Space(7);

            if (resolver.Renderer != null)
            {
                _bonesOnMeshFoldout = EditorGUILayout.Foldout(_bonesOnMeshFoldout, new GUIContent(resolver.Renderer.bones.Length + " Bones assigned to SkinnedMeshRenderer '" + resolver.Renderer.name + "':"));
                if (_bonesOnMeshFoldout)
                {
                    foreach (var bone in resolver.Renderer.bones)
                    {
                        if (bone == null)
                        {
                            GUILayout.Label("Null");
                            continue;
                        }

                        string indent = "  ";
                        var current = bone;
                        while (current != null && current != resolver.Renderer.rootBone)
                        {
                            indent += " ";
                            current = current.parent;
                        }
                        GUILayout.Label(indent + bone.name);
                    }
                }

                // Show message if renderer has properly assigned bones and it is okay to delete the resolver.
                var renderer = resolver.Renderer;
                if (renderer  != null && renderer.bones != null && renderer.bones.Length > 0 && renderer.bones[0] != null
                    && renderer.bones.Length == renderer.sharedMesh.bindposes.Length)
                {
                    var color = GUI.color;
                    GUI.color = Color.green;
                    GUILayout.Label("Your bones are set. You can delete the BoneDataResolver.");
                    GUI.color = color;
                }
                else if (renderer != null && renderer.bones != null && renderer.bones.Length > 0 && renderer.bones[0] == null)
                {
                    var color = GUI.color;
                    GUI.color = Color.yellow;
                    var style = GUI.skin.label;
                    style.wordWrap = true;
                    GUILayout.Label("Seems like your bones are NULL. Move the object next to a SkinnedMeshRenderer with bones and hit 'Resolve'.", style);
                    GUI.color = color;
                }
            }
            else
            {
                GUILayout.Label("No Mesh");
            }
        }
    }
#endif
}