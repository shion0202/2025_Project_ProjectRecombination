using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Kamgam.PowerPivot
{
    public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : System.IComparable
    {
        public int Compare(TKey x, TKey y)
        {
            int result = x.CompareTo(y);

            if (result == 0)
                return 1;   // Handle equality as beeing greater.
            else
                return result;
        }
    }
    partial class PowerPivotTool
    {
        List<SkinnedMeshRenderer> sceneSkinnedMeshRenderersCache = new List<SkinnedMeshRenderer>();
        Dictionary<SkinnedMeshRenderer, List<Vector3>> sceneSkinnedMeshVerticesCache = new Dictionary<SkinnedMeshRenderer, List<Vector3>>();
        Dictionary<SkinnedMeshRenderer, List<Vector3>> sceneSkinnedMeshNormalsCache = new Dictionary<SkinnedMeshRenderer, List<Vector3>>();

        List<MeshFilter> sceneMeshFiltersCache = new List<MeshFilter>();
        Dictionary<MeshFilter, List<Vector3>> sceneMeshVerticesCache = new Dictionary<MeshFilter, List<Vector3>>();
        Dictionary<MeshFilter, List<Vector3>> sceneMeshNormalsCache = new Dictionary<MeshFilter, List<Vector3>>();

        List<SpriteRenderer> sceneSpriteRenderesCache = new List<SpriteRenderer>();
        Dictionary<SpriteRenderer, Vector2[]> sceneSpriteVerticesCache = new Dictionary<SpriteRenderer, Vector2[]>();

        SortedList<float, Vector4> tmpSnapCandidates = new SortedList<float, Vector4>(new DuplicateKeyComparer<float>());
        Bounds tmpSnapBounds = new Bounds();

        void SnapCursor(SceneView sceneView, int controlID)
        {
            if (Event.current.type == EventType.MouseMove)
            {
                // find all objects roughly under the cursor
                var cam = SceneView.lastActiveSceneView.camera;
                if (cam != null)
                {
                    var settings = PowerPivotSettings.GetOrCreateSettings();
                    float preferSelectedSqrDistance = settings.PreferSelectedDistance * settings.PreferSelectedDistance;
                    // All vertices that are within this radius of each other in screen space are considered to be equal.
                    // If there are mutiple vetices within that radius then the selection is based on the z value (the
                    // closest vertex will be taken).
                    float preferZSqrDistance = settings.PreferZDistance * settings.PreferZDistance;
                    float maxPreferredSqrDistance = Mathf.Max(preferSelectedSqrDistance, preferZSqrDistance);
                    float ignorePreferredSqrDistance = settings.IgnoreZDistance * settings.IgnoreZDistance;
                    bool usePreference = !Event.current.shift;

                    var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    var rayOriginInScreenSpace = cam.WorldToScreenPoint(ray.origin);

                    // find closest vertex ..
                    float minSqrDistance = float.MaxValue;
                    Vector3 minVertex = cursorPosition;
                    tmpSnapCandidates.Clear();
                    bool found = false;
                    bool foundPreferredSelected = false;

                    // .. in MeshFilters
                    foreach (var meshFilter in sceneMeshFiltersCache)
                    {
                        // Skip deleted objects
                        if (meshFilter.gameObject == null)
                            continue;

                        // skip if hidden
                        if (UtilsSceneVis.IsVisibilityActive() && !UtilsSceneVis.IsVisible(meshFilter.gameObject))
                            continue;

                        var meshRenderer = meshFilter.gameObject.GetComponent<MeshRenderer>();
                        if (meshRenderer != null)
                        {
                            scanRendererForSnapVertices(
                                meshRenderer, cam, settings, preferSelectedSqrDistance, maxPreferredSqrDistance, usePreference,
                                ref ray, rayOriginInScreenSpace, ref minSqrDistance, ref minVertex, ref found, ref foundPreferredSelected,
                                meshFilter, sceneMeshVerticesCache, sceneMeshNormalsCache
                                );
                        }
                    }

                    // .. in SkinnedMeshRenderers
                    foreach (var meshRenderer in sceneSkinnedMeshRenderersCache)
                    {
                        // Skip deleted objects
                        if (meshRenderer.gameObject == null)
                            continue;

                        // skip if hidden
                        if (UtilsSceneVis.IsVisibilityActive() && !UtilsSceneVis.IsVisible(meshRenderer.gameObject))
                            continue;

                        scanRendererForSnapVertices(
                            meshRenderer, cam, settings, preferSelectedSqrDistance, maxPreferredSqrDistance, usePreference,
                            ref ray, rayOriginInScreenSpace, ref minSqrDistance, ref minVertex, ref found, ref foundPreferredSelected,
                            meshRenderer, sceneSkinnedMeshVerticesCache, sceneSkinnedMeshNormalsCache
                            );
                    }

                    // .. in SpriteRenderers
                    foreach (var spriteRenderer in sceneSpriteRenderesCache)
                    {
                        // Skip deleted objects
                        if (spriteRenderer.gameObject == null)
                            continue;

                        // bounding box check
                        if (spriteRenderer.bounds.IntersectRay(ray))
                        {
                            if (sceneSpriteVerticesCache.ContainsKey(spriteRenderer))
                            {
                                bool isSelected = Selection.Contains(spriteRenderer.gameObject);

                                var vertices = sceneSpriteVerticesCache[spriteRenderer];
                                var c = vertices.Length;
                                for (int i = 0; i < c; i++)
                                {
                                    var posInScreenSpace = cam.WorldToScreenPoint(spriteRenderer.transform.TransformPoint(vertices[i]));
                                    var distance = rayOriginInScreenSpace - posInScreenSpace; // distance in screen space
                                    float zDistance = -distance.z;
                                    distance.z = 0f;
                                    var sqrDistance = distance.sqrMagnitude;
                                    if (sqrDistance < minSqrDistance)
                                    {
                                        minSqrDistance = sqrDistance;
                                        minVertex = spriteRenderer.transform.TransformPoint(vertices[i]);
                                        found = true;
                                    }

                                    // collect candidates for perferred snapping
                                    if (usePreference && sqrDistance < maxPreferredSqrDistance)
                                    {
                                        var v = spriteRenderer.transform.TransformPoint(vertices[i]);
                                        tmpSnapCandidates.Add(sqrDistance, new Vector4(v.x, v.y, v.z, isSelected ? -1f : zDistance));
                                        if (isSelected && sqrDistance < preferSelectedSqrDistance)
                                            foundPreferredSelected = true;
                                    }
                                }
                            }
                        }
                    }

                    // Apply preferences for selected objects or vertices which are colser to the camera.
                    if (usePreference && found && tmpSnapCandidates.Count > 0)
                    {
                        if (foundPreferredSelected)
                        {
                            // prefer selected
                            foreach (var c in tmpSnapCandidates)
                            {
                                // if selected then w is -1f
                                if (c.Value.w < 0f)
                                {
                                    minVertex = new Vector3(c.Value.x, c.Value.y, c.Value.z);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // prefer based on z distance (z distance is store in .w)
                            Vector3 zMinVertex = minVertex;
                            float minDistance = float.MaxValue;
                            foreach (var c in tmpSnapCandidates)
                            {
                                // if not selected then w is the zDistance to camera
                                if (c.Value.w < minDistance)
                                {
                                    minDistance = c.Value.w;
                                    zMinVertex = new Vector3(c.Value.x, c.Value.y, c.Value.z);
                                }
                            }

                            // only apply if the current vertex and the zMinVertex are some distance appart
                            if (Vector3.SqrMagnitude(zMinVertex - minVertex) > ignorePreferredSqrDistance) // x²
                            {
                                minVertex = zMinVertex;
                            }
                        }
                    }

                    if (found && settings.SnapToPivot)
                    {
                        // snap to pivot position
                        if (Selection.activeGameObject != null)
                        {
                            var posInScreenSpace = cam.WorldToScreenPoint(Selection.activeGameObject.transform.position);
                            var distance = rayOriginInScreenSpace - posInScreenSpace;
                            distance.z = 0f;
                            var sqrDistance = distance.sqrMagnitude;
                            if (sqrDistance < minSqrDistance)
                            {
                                // minSqrDistance = sqrDistance;
                                minVertex = Selection.activeGameObject.transform.position;
                            }
                        }
                    }

                    cursorPosition = minVertex;
                    updateCursorRelativePosition();
                }

            }
            
            // draw deco gizmos
            if (snapEnabled)
            {
                var rot = Quaternion.LookRotation(sceneView.camera.transform.position - cursorPosition);
                var size = HandleUtility.GetHandleSize(cursorPosition);
                if ((sceneView.camera.transform.position - cursorPosition).sqrMagnitude > 0)
                    rot = Quaternion.LookRotation(sceneView.camera.transform.position - cursorPosition);
                Handles.RectangleHandleCap(controlID, cursorPosition, rot, size * 0.15f, EventType.Repaint);
            }
        }

        private void scanRendererForSnapVertices<TKey>(Renderer renderer, Camera cam, PowerPivotSettings settings,
            float preferSelectedSqrDistance, float maxPreferredSqrDistance, bool usePreference, ref Ray ray,
            Vector3 rayOriginInScreenSpace, ref float minSqrDistance, ref Vector3 minVertex, ref bool found, ref bool foundPreferredSelected,
            TKey component, Dictionary<TKey, List<Vector3>> verticesCache, Dictionary<TKey, List<Vector3>> normalsCache)
            where TKey : Component
        {
            bool isSelected = Selection.Contains(renderer.gameObject);

            // bounding box check (grow bounds a bit to make edge vertices easier to hit)
            tmpSnapBounds.center = renderer.bounds.center;
            var size = renderer.bounds.size;
            size *= 1.6f;
            tmpSnapBounds.size = size + Vector3.one;

            // start checking all vertices
            if (renderer != null && tmpSnapBounds.IntersectRay(ray))
            {
                // For debugging purposes
                // debugDrawBounds(tmpBounds);

                var localRay = new Ray(
                    renderer.transform.InverseTransformPoint(ray.origin),
                    renderer.transform.InverseTransformVector(ray.direction)
                );

                if (verticesCache.ContainsKey(component) && normalsCache.ContainsKey(component))
                {
                    var vertices = verticesCache[component];
                    var normals = normalsCache[component];
                    var c = vertices.Count;

                    for (int i = 0; i < c; i++)
                    {
                        // Ignore if normal is not facing the camera (back culling)
                        if (settings.IgnoreBackFacingVertices && Vector3.Dot(localRay.direction, normals[i]) > 0)
                            continue;

                        // Convert distance vector from local to global so we can compare across all vertices.
                        // The camera may use a perspective, thus global distance is illogical for the user.
                        // We have to transform it to screenspace, set z to 0 and then make the distance comparison.
                        var posInScreenSpace = cam.WorldToScreenPoint(component.transform.TransformPoint(vertices[i]));
                        var distance = rayOriginInScreenSpace - posInScreenSpace;
                        float zDistance = -distance.z;
                        distance.z = 0f;
                        var sqrDistance = distance.sqrMagnitude;
                        if (sqrDistance < minSqrDistance)
                        {
                            minSqrDistance = sqrDistance;
                            minVertex = component.transform.TransformPoint(vertices[i]);
                            found = true;
                        }

                        // collect candidates for perferred snapping
                        if (usePreference && sqrDistance < maxPreferredSqrDistance)
                        {
                            var v = component.transform.TransformPoint(vertices[i]);
                            tmpSnapCandidates.Add(sqrDistance, new Vector4(v.x, v.y, v.z, isSelected ? -1f : zDistance));
                            if (isSelected && sqrDistance < preferSelectedSqrDistance)
                                foundPreferredSelected = true;
                        }
                    }
                }
            }
        }

        float sqrDistanceToRay(Ray ray, Vector3 point)
        {
            return Vector3.Cross(ray.direction, point - ray.origin).sqrMagnitude;
        }


        void clearSceneRaycastCache()
        {
            sceneSkinnedMeshRenderersCache.Clear();
            sceneSkinnedMeshVerticesCache.Clear();
            sceneSkinnedMeshNormalsCache.Clear();

            sceneMeshFiltersCache.Clear();
            sceneMeshVerticesCache.Clear();
            sceneMeshNormalsCache.Clear();

            sceneSpriteVerticesCache.Clear();
            sceneSpriteRenderesCache.Clear();
        }

        void buildScenesRaycastCache()
        {
            clearSceneRaycastCache();

            SkinnedMeshRenderer[] skinnedMeshRenderers = null;
            MeshFilter[] meshFilters = null;
            SpriteRenderer[] spriteRenderers = null;

            if (UtilsEditor.IsInPrefabStage())
            {
                var root = UtilsEditor.GetPrefabStageRoot();

                // Find the real root of the prefab stage.
                var rootParent = root.transform.parent; // A root can have a parent? Apparently yes!
                if (rootParent != null)
                    root = rootParent.gameObject;

                if (root.gameObject != null)
                {
                    skinnedMeshRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: false);
                    meshFilters = root.GetComponentsInChildren<MeshFilter>(includeInactive: false);
                    spriteRenderers = root.GetComponentsInChildren<SpriteRenderer>(includeInactive: false);
                }
            }
            else
            {
                skinnedMeshRenderers = UnityVersionUtils.FindObjectsOfType<SkinnedMeshRenderer>();
                meshFilters = UnityVersionUtils.FindObjectsOfType<MeshFilter>();
                spriteRenderers = UnityVersionUtils.FindObjectsOfType<SpriteRenderer>();
            }

            if (skinnedMeshRenderers != null)
            {
                foreach (var meshRenderer in skinnedMeshRenderers)
                {
                    // Not sure if we want this. Wait for user feedback.
                    if (/*!isLayerInLayers(meshRenderer.gameObject.layer, Tools.lockedLayers) && */
                           isLayerInLayers(meshRenderer.gameObject.layer, Tools.visibleLayers))
                    {
                        sceneSkinnedMeshRenderersCache.Add(meshRenderer);

                        var vertices = new List<Vector3>();
                        // bake vertices (TODO: investigate if too much garbage is created here)
                        Mesh mesh = new Mesh();
                        meshRenderer.BakeMesh(mesh);
                        mesh.GetVertices(vertices);
                        // fast alternative(though without baking)
                        /*
                        var vertices = new List<Vector3>();
                        meshRenderer.sharedMesh.GetVertices(vertices);
                        */
                        sceneSkinnedMeshVerticesCache.Add(meshRenderer, vertices);

                        var normals = new List<Vector3>();
                        if (meshRenderer.sharedMesh)
                        {
                            meshRenderer.sharedMesh.GetNormals(normals);
                            sceneSkinnedMeshNormalsCache.Add(meshRenderer, normals);
                        }
                    }
                }
            }

            if (meshFilters != null)
            {
                foreach (var meshFilter in meshFilters)
                {
                    if (/*!isLayerInLayers(meshFilter.gameObject.layer, Tools.lockedLayers) && */ // Not sure if we want this. Wait for user feedback.
                           isLayerInLayers(meshFilter.gameObject.layer, Tools.visibleLayers))
                    {
                        sceneMeshFiltersCache.Add(meshFilter);

                        var vertices = new List<Vector3>();
                        if (meshFilter.sharedMesh != null)
                        {
                            meshFilter.sharedMesh.GetVertices(vertices);
                            sceneMeshVerticesCache.Add(meshFilter, vertices);
                        }

                        var normals = new List<Vector3>();
                        if (meshFilter.sharedMesh != null)
                        {
                            meshFilter.sharedMesh.GetNormals(normals);
                            sceneMeshNormalsCache.Add(meshFilter, normals);
                        }
                    }
                }
            }

            if (spriteRenderers != null)
            {
                foreach (var renderer in spriteRenderers)
                {
                    if (/*!isLayerInLayers(renderer.gameObject.layer, Tools.lockedLayers) && */ // Not sure if we want this. Wait for user feedback.
                           isLayerInLayers(renderer.gameObject.layer, Tools.visibleLayers))
                    {
                        sceneSpriteRenderesCache.Add(renderer);
                        sceneSpriteVerticesCache.Add(renderer, renderer.sprite.vertices);
                    }
                }
            }
        }

        bool isLayerInLayers(int layer, int layers)
        {
            return ((1 << layer) & layers) != 0;
        }
    }
}
