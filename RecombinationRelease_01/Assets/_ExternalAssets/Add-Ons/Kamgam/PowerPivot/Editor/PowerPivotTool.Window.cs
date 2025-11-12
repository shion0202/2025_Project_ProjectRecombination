using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Kamgam.PowerPivot
{
    public static class BackgroundTexture
    {
        private static Dictionary<Color, Texture2D> textures = new Dictionary<Color, Texture2D>();

        public static Texture2D Get(Color color)
        {
            if (textures.ContainsKey(color) && textures[color] != null) 
                return textures[color];

            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();

            if (textures.ContainsKey(color))
                textures[color] = texture;
            else
                textures.Add(color, texture);

            return texture;
        }
    }

    partial class PowerPivotTool
    {
        protected Rect cursorDetailsWindowRect;
        protected bool toggleDeleteAfterChildExtraction = false;
        protected bool foldoutChangePivot = false;
        protected bool updateColliders = true;

        void initWindowSize()
        {
            if (SceneView.lastActiveSceneView != null)
            {
                var settings = PowerPivotSettings.GetOrCreateSettings();
                cursorDetailsWindowRect.position = settings.WindowPosition;
                cursorDetailsWindowRect.width = 250;
                cursorDetailsWindowRect.height = 90;

                // If the window is not yet set or if it's outside the scene view then reset position.
                if (
                       cursorDetailsWindowRect.position.x > SceneView.lastActiveSceneView.position.width
                    || cursorDetailsWindowRect.position.x < 0
                    || cursorDetailsWindowRect.position.y > SceneView.lastActiveSceneView.position.height
                    || cursorDetailsWindowRect.position.y < 0
                    )
                {
                    // bottom right corner by default
                    cursorDetailsWindowRect.position = new Vector2(
                        SceneView.lastActiveSceneView.position.width - 270,
                        SceneView.lastActiveSceneView.position.height - 200
                        );
                    settings.WindowPosition = cursorDetailsWindowRect.position;
                    EditorUtility.SetDirty(settings);
                }
            }
        }

        void drawWindow(SceneView sceneView, int controlID)
        {
            if (cursorPosition == null || cursorDetailsWindowRect.width == 0)
                return;

            Handles.BeginGUI();
            var oldRect = cursorDetailsWindowRect;
            cursorDetailsWindowRect = GUILayout.Window(controlID, cursorDetailsWindowRect, drawWindowContent, "Power Pivot (hold \"v\" to snap)");
            if (Vector2.SqrMagnitude(oldRect.position - cursorDetailsWindowRect.position) > 0.01f)
            {
                var settings = PowerPivotSettings.GetOrCreateSettings();
                settings.WindowPosition = cursorDetailsWindowRect.position;
                EditorUtility.SetDirty(settings);
            }
            Handles.EndGUI();
        }

        void drawWindowContent(int controlID)
        {
            var settings = PowerPivotSettings.GetOrCreateSettings();

            var bgColor = UtilsEditor.IsLightTheme() ? new Color(0.75f, 0.75f, 0.75f) : new Color(0.25f, 0.25f, currentTool == PTool.Cursor ? 0.35f : 0.25f);
            var tex = BackgroundTexture.Get(bgColor);
            GUI.DrawTexture(new Rect(5, 22, cursorDetailsWindowRect.width - 10, cursorDetailsWindowRect.height - 26), tex);

            // unfocus if any of the "switch tool keys" is pressed
            if (IsToolKeyPressed(Event.current))
            {
                GUI.FocusControl(null);
            }

            BeginHorizontalIndent(5);

            GUILayout.Space(5);

            // close button
            var closeBtnStyle = GUIStyle.none;
            closeBtnStyle.normal.background = BackgroundTexture.Get(UtilsEditor.IsLightTheme() ? new Color(0.45f, 0.45f, 0.45f) : bgColor);
            closeBtnStyle.hover.background = BackgroundTexture.Get(new Color(0.5f, 0.5f, 0.5f));
            var closeBtnContent = EditorGUIUtility.IconContent(UnityVersionUtils.GetCloseIcon());
            closeBtnContent.tooltip = "Close the cursor tool (Esc).";
            if (GUI.Button(new Rect(cursorDetailsWindowRect.width - 25, 2, 20, 20), closeBtnContent, closeBtnStyle))
            {
                foldoutChangePivot = false;
                exitTool();
            }

            // tool type buttons
            var inactiveStyle = new GUIStyle(GUI.skin.button);

            GUILayout.BeginHorizontal();

            bool toolIsActive = currentTool == PTool.Cursor;
            GUI.enabled = !toolIsActive;
            if (DrawButton("", tooltip: "Change the cursor", icon: "d_ToolHandlePivot@2x", style: null
                , GUILayout.Height(22)
                ))
            {
                switchToolTo(PTool.Cursor);
            }

            toolIsActive = currentTool == PTool.Move;
            GUI.enabled = !toolIsActive;
            if (DrawButton("", tooltip: "Move", icon: toolIsActive ? "d_MoveTool On@2x" : "d_MoveTool@2x", style: null
                , GUILayout.Height(22)
                ))
            {
                switchToolTo(PTool.Move);
                foldoutChangePivot = false;
            }

            toolIsActive = currentTool == PTool.Rotate;
            GUI.enabled = !toolIsActive;
            if (DrawButton("", tooltip: "Rotate around the cursor.", icon: toolIsActive ? "d_RotateTool On@2x" : "d_RotateTool@2x", style: null
                , GUILayout.Height(22)
                ))
            {
                switchToolTo(PTool.Rotate);
                foldoutChangePivot = false;
            }

            toolIsActive = currentTool == PTool.Scale;
            GUI.enabled = !toolIsActive;
            if (DrawButton("",
                tooltip: "Scale around the cursor.",
                icon: toolIsActive ? "d_ScaleTool On@2x" : "d_ScaleTool@2x",
                style: null,
                GUILayout.Height(22)
                ))
            {
                switchToolTo(PTool.Scale);
                foldoutChangePivot = false;
            }

            GUI.enabled = true; 

            GUILayout.Space(5);

            GUILayout.EndHorizontal();


            // Tool values
            GUILayout.BeginHorizontal();

            EditorGUIUtility.wideMode = false;

            // Convert cursor pos to local pos if necessary (i.e. the rotation is in local space).
            bool isLocalCursor = Tools.pivotRotation == PivotRotation.Local && Selection.activeGameObject != null;
            var targetTransform = isLocalCursor ? Selection.activeGameObject.transform : null;
            Vector3 localCursorPos = isLocalCursor ? targetTransform.InverseTransformPoint(cursorPosition) : cursorPosition;
            TrimVectorForDisplay(ref localCursorPos);

            // draw vector field for current tool
            if (currentTool == PTool.Cursor)
            {
                var newLocalCursorPos = EditorGUILayout.Vector3Field("Cursor Pos.: ", localCursorPos);
                var delta = newLocalCursorPos - localCursorPos;
                if (Vector3.Magnitude(delta) > 0.0001f)
                {
                    if (isLocalCursor)
                        cursorPosition = targetTransform.transform.TransformPoint(newLocalCursorPos);
                    else
                    {
                        delta = cursorRotation * delta;
                        cursorPosition += delta;
                    }
                    updateCursorRelativePosition();
                    RegisterCursorUndo(cursorPosition, minTimeDelta: 2);
                }
            }
            else if (currentTool == PTool.Move)
            {
                if (isLocalCursor)
                {
                    if (targetTransform.parent == null)
                    {
                        var oldPos = Quaternion.Inverse(targetTransform.localRotation) * cursorPosition;
                        TrimVectorForDisplay(ref oldPos);
                        var newPos = EditorGUILayout.Vector3Field("Position: ", oldPos);
                        var delta = newPos - oldPos;
                        if (Vector3.Magnitude(delta) > 0.0001f)
                        {
                            // calc global delta
                            delta = targetTransform.localRotation * newPos - cursorPosition;
                            moveBy(delta);
                        }
                    }
                    else
                    {
                        // Convert cursor position into the rotated coordinate system of targetTransform.parent.
                        // Rotation is from the targetTransform and position is the localPosition.
                        // We do this to have the ability to enter values per axis without changing the other axis
                        // (easier to reason about for the user).
                        Vector3 cursorPosInParent = targetTransform.parent.InverseTransformPoint(cursorPosition);
                        cursorPosInParent.Scale(targetTransform.parent.localScale);
                        var oldPos = (Quaternion.Inverse(targetTransform.localRotation) * cursorPosInParent);
                        TrimVectorForDisplay(ref oldPos);
                        var newPos = EditorGUILayout.Vector3Field("Position: ", oldPos);
                        var delta = newPos - oldPos;
                        if (Vector3.Magnitude(delta) > 0.001f)
                        {
                            // calc global delta
                            delta = targetTransform.localRotation * newPos - cursorPosInParent;
                            delta = targetTransform.parent.TransformDirection(delta);
                            moveBy(delta);
                        }
                    }
                }
                else
                {
                    var newCursorPos = EditorGUILayout.Vector3Field("Position: ", cursorPosition);
                    var delta = newCursorPos - cursorPosition;
                    if (Vector3.Magnitude(delta) > 0.001f)
                    {
                        delta = cursorRotation * delta;
                        moveBy(delta);
                    }
                }
            }
            else if (currentTool == PTool.Scale)
            {
                if (Selection.gameObjects.Length > 1)
                    GUI.enabled = false; // there is no reference scale for multiple objects (n2h: init with 1 and show delta since last input)

                var targetGo = target as GameObject;
                if (targetGo != null)
                {
                    // overwrite cursorRotation (always use local rotation for scale commands on a single object)
                    if (Tools.pivotRotation == PivotRotation.Global && targetGo != null && targets.HasExactlyOneGameObject())
                        cursorRotation = targetGo.transform.rotation;

                    var oldScale = Selection.gameObjects.Length > 1 ? Vector3.one : targetGo.transform.localScale;
                    TrimVectorForDisplay(ref oldScale);
                    var newScale = EditorGUILayout.Vector3Field("Scale: ", oldScale);
                    var scaleFactor = new Vector3(
                        oldScale.x == 0 ? 1f : newScale.x / oldScale.x,
                        oldScale.y == 0 ? 1f : newScale.y / oldScale.y,
                        oldScale.z == 0 ? 1f : newScale.z / oldScale.z);

                    if (Mathf.Abs(scaleFactor.x) > 0.001f && Mathf.Abs(scaleFactor.y) > 0.001f && Mathf.Abs(scaleFactor.z) > 0.001f && (scaleFactor - Vector3.one).magnitude > 0.001f)
                    {
                        initScaleHandle();
                        scaleTo(targetGo, scaleFactor, scaleStartScales, scaleStartPositions, cursorPosition, cursorRotation);
                    }
                }

                GUI.enabled = true;
            }
            else if (currentTool == PTool.Rotate)
            {
                if (!isLocalCursor)
                    GUI.enabled = false; // in global mode there is no reference angle to compare to (n2h: init with 0 and show delta since last input)

                // detect if rotation was changed externally, if yes then update cursorEulerRotationInput euler angles.
                var cursorInputDelta = Quaternion.Euler(cursorEulerRotationInput) * Quaternion.Inverse(cursorRotation);
                if (cursorInputDelta.eulerAngles.sqrMagnitude > 0.0001f)
                {
                    if (isLocalCursor)
                        cursorEulerRotationInput = targetTransform.rotation.eulerAngles;
                    else
                        cursorEulerRotationInput = cursorRotation.eulerAngles;
                }

                // display values in euler angles
                TrimVectorForDisplay(ref cursorEulerRotationInput);
                cursorEulerRotationInput = EditorGUILayout.Vector3Field("Rotation: ", cursorEulerRotationInput);

                // calc rotation change delta in quaternions
                var newRotationQuaternion = Quaternion.Euler(cursorEulerRotationInput.x, cursorEulerRotationInput.y, cursorEulerRotationInput.z);
                var rotationDelta = newRotationQuaternion * Quaternion.Inverse(cursorRotation);
                var deltaEuler = rotationDelta.eulerAngles;

                // apply if changed
                if (deltaEuler.magnitude > 0.001f)
                {
                    rotateBy(rotationDelta);

                    if (isLocalCursor)
                        cursorRotation = targetTransform.rotation;
                }

                GUI.enabled = true;
            }

            // Toggle local/global cursor rotation
            GUI.enabled = currentTool != PTool.Scale || targets.HasMoreThanOneGameObject();
                if (Tools.pivotRotation == PivotRotation.Local)
            {
                if (DrawButton(
                    "", "Switch cursor to GLOBAL rotation.", "d_ToolHandleLocal", null,
                    GUILayout.MaxHeight(20), GUILayout.MaxWidth(25)))
                {
                    Tools.pivotRotation = PivotRotation.Global;
                }
            }
            else
            {
                if (DrawButton(
                    "", "Switch cursor to LOCAL rotation.", "d_ToolHandleGlobal", null,
                    GUILayout.MaxHeight(20), GUILayout.MaxWidth(25)))
                {
                    Tools.pivotRotation = PivotRotation.Local;
                }
            }
            GUI.enabled = true;

            EditorGUIUtility.wideMode = false;

            GUILayout.EndHorizontal();

            // Mesh export UI below cursor UI
            if (currentTool == PTool.Cursor)
            {
                GUILayout.Space(2);
                GUIStyle myFoldoutStyle = new GUIStyle(EditorStyles.foldout);
                if(!foldoutChangePivot)
                {
                    if (GUILayout.Button("Change Pivot Mode"))
                    {
                        foldoutChangePivot = true;
                    }
                }
                if (foldoutChangePivot)
                {
                    GUILayout.BeginHorizontal();

                    var gameObject = Selection.activeGameObject;

                    Mesh mesh = null;

                    bool hasSkinnedMesh = false;
                    if (gameObject != null)
                    {
                        var skinnedMesh = gameObject.GetComponent<SkinnedMeshRenderer>();
                        if (skinnedMesh != null)
                            mesh = skinnedMesh.sharedMesh;
                        hasSkinnedMesh = skinnedMesh != null;
                    }

                    bool hasMesh = false;
                    if (gameObject != null)
                    {
                        var meshFilter = gameObject.GetComponent<MeshFilter>();
                        if (meshFilter != null)
                            mesh = meshFilter.sharedMesh;
                        hasMesh = meshFilter != null;
                    }

                    GUI.enabled = hasMesh && !hasSkinnedMesh;
                    updateColliders = GUILayout.Toggle(updateColliders, new GUIContent("Colliders", "Should the colliders be updated (displaced) too?"));
                    GUI.enabled = hasMesh || hasSkinnedMesh;
                    if (GUILayout.Button(new GUIContent("Save", "Copies the mesh, moves the vertices and then saves and assigns it.")))
                    {
                        if (gameObject != null)
                        {
                            var pivotDelta = gameObject.transform.InverseTransformPoint(cursorPosition);

                            Undo.RecordObject(gameObject, "pivot change - go");
                            Undo.RecordObject(gameObject.transform, "pivot change - transform");
                            var colliders = gameObject.GetComponents<Collider>();
                            foreach (var collider in colliders)
                            {
                                Undo.RecordObject(collider, "pivot change - collider");
                            }
                            var colliders2D = gameObject.GetComponents<Collider2D>();
                            foreach (var collider2D in colliders2D)
                            {
                                Undo.RecordObject(collider2D, "pivot change - collider2D");
                            }

                            var modifier = new MeshModifier(gameObject);
                            Undo.RegisterCompleteObjectUndo(modifier.Component, "mesh change");

                            var newMesh = modifier.CopyOrUpdateMesh(pivotDelta, updateColliders);
                            modifier.AssignSharedMesh(newMesh);
                            PrefabUtility.RecordPrefabInstancePropertyModifications(modifier.Component);

                            // Move the cursor back to local zero pos
                            cursorPosition = gameObject.transform.TransformPoint(Vector3.zero);

                            if (!modifier.IsSkinned || !modifier.HasBones)
                            {
                                // Move the cursor back to local zero pos
                                cursorPosition = gameObject.transform.TransformPoint(Vector3.zero);
                                // Move object by pivot delta so it stays in place.
                                var posDeltaInWorld = gameObject.transform.TransformVector(pivotDelta);
                                var pos = gameObject.transform.position;
                                pos += posDeltaInWorld;
                                gameObject.transform.position = pos;
                            }
                        }
                    }
                    if (GUILayout.Button(new GUIContent("Refresh", "Checks the original model for changes and re-applies the pivot if needed.")))
                    {
                        RefreshModel(mesh);
                    }
                    GUI.enabled = true;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(2);

            // child/parent buttons
            if (settings.ShowParentingButtons)
            {
                GUILayout.BeginHorizontal();

                var btnStyle = new GUIStyle(GUI.skin.button);
                if (UtilsEditor.IsLightTheme())
                {
                    btnStyle.normal.textColor = Color.black;
                    btnStyle.hover.textColor = Color.grey;
                    btnStyle.active.textColor = Color.grey;
                }

                GUI.enabled = Selection.activeGameObject != null;
                if (GUILayout.Button(new GUIContent("Create parent", "Create an empty game object at the cursor position as parent for the selected objects?"), btnStyle))
                {
                    addParent();
                    // exit cursor tool automatically
                    if (currentTool == PTool.Cursor)
                        switchToolTo(PTool.Move);
                }

                GUI.enabled = Selection.gameObjects.Length == 1 && Selection.activeGameObject != null && Selection.activeGameObject.transform.childCount > 0;
                if (GUILayout.Button(new GUIContent("Extract children", "Reverts the \"Create parent\" action by moving the children out of the selected object.\nEnable the toggle on the right to delete the parent afterwards."), btnStyle))
                    removeParent(toggleDeleteAfterChildExtraction);

                toggleDeleteAfterChildExtraction = GUILayout.Toggle(toggleDeleteAfterChildExtraction, new GUIContent("", "Delete the parent after children have been extracted."));
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }


            EndHorizontalIndent(bothSides: true);

            GUILayout.Space(4);

            GUI.DragWindow();
        }

        void addParent()
        {
            if (Selection.gameObjects.Length == 0)
                return;

            if (UtilsEditor.IsNotInScene(Selection.activeGameObject))
                return;

            // Check if objects are on the same level in the hierarchy. Warn the user if not.
            bool first = true;
            Transform objectsParent = null;
            foreach (var go in Selection.gameObjects)
            {
                if (UtilsEditor.IsNotInScene(go))
                    continue;

                if (objectsParent != go.transform.parent && !first)
                {
                    bool proceed = EditorUtility.DisplayDialog("Warning (mixed hierarchy)", "The selected objects are not all at the same level in the hierarchy. Moving them to a new parent will be quite a change.\n\n Are you sure you want to do move these objects?", "Yes (proceed)", "No (cancel)");
                    if (!proceed)
                        return;
                    break;
                }
                objectsParent = go.transform.parent;
                first = false;
            }

            // create parent
            var parent = new GameObject();
            parent.transform.parent = Selection.activeGameObject.transform.parent;
            parent.transform.position = cursorPosition;
            parent.transform.rotation = cursorRotation;
            parent.transform.localScale = Vector3.one;
            parent.transform.SetSiblingIndex(Selection.activeGameObject.transform.GetSiblingIndex());
            parent.name = Selection.activeGameObject.name + " Group";

            // assign parent
            var gameObjects = Selection.gameObjects;
            System.Array.Sort(gameObjects, (a,b) => a.transform.GetSiblingIndex() - b.transform.GetSiblingIndex());
            foreach (var go in gameObjects)
            {
                if (UtilsEditor.IsNotInScene(go))
                    continue;

                Undo.RegisterFullObjectHierarchyUndo(go, "Reparenting");
                go.transform.SetParent(parent.transform, worldPositionStays: true);
                go.transform.SetAsLastSibling();
            }

            Undo.RegisterCreatedObjectUndo(parent, "Create new empty parent");

            // select parent
            Selection.objects = new GameObject[] { parent };
        }

        void removeParent(bool deleteAfterChildExtraction)
        {
            var oldParent = Selection.activeGameObject;

            if (UtilsEditor.IsNotInScene(oldParent))
                return;

            // Check if the selected object has some components other than the transform
            int componentCount = oldParent.transform.GetComponents<Component>().Length;
            if (componentCount > 1)
            {
                bool proceed = EditorUtility.DisplayDialog("Warning (multiple components)", "The selected object has multiple components attached. These will be deleted along with the object.\n\n Are you sure you want to remove the object?", "Yes (proceed)", "No (cancel)");
                if (!proceed)
                    return;
            }

            // * Sadly UNDO crashes unity here (2019 up tp 2021).
            // Undos work but once you redo (ctrl + y) it crashes, TODO: isolate and report as bug

            // reparent objects
            var newSelection = new GameObject[oldParent.transform.childCount];
            int g = 0;
            for (int i = oldParent.transform.childCount-1; i >= 0; i--)
            {
                var child = oldParent.transform.GetChild(i);
                //Undo.RegisterFullObjectHierarchyUndo(child.gameObject, "Extracting"); // *see above
                child.SetParent(oldParent.transform.parent, worldPositionStays: true);
                child.SetSiblingIndex(oldParent.transform.GetSiblingIndex()+1);
                newSelection[g] = child.gameObject;
                g++;
            }

            Selection.objects = newSelection;

            if (deleteAfterChildExtraction)
            {
                UtilsEditor.SmartDestroy(oldParent);
                //Undo.DestroyObjectImmediate(oldParent); // *see above
            }
        }

        void TrimVectorForDisplay(ref Vector3 v)
        {
            if (Mathf.Abs(v.x) < 0.001f) v.x = 0f; else v.x = Mathf.Round(v.x * 1000f) / 1000f;
            if (Mathf.Abs(v.y) < 0.001f) v.y = 0f; else v.y = Mathf.Round(v.y * 1000f) / 1000f;
            if (Mathf.Abs(v.z) < 0.001f) v.z = 0f; else v.z = Mathf.Round(v.z * 1000f) / 1000f;
        }

        public static bool IsToolKeyPressed(Event evt)
        {
            var settings = PowerPivotSettings.GetOrCreateSettings();

            return Event.current.type == EventType.KeyDown
                && Event.current.keyCode != KeyCode.None
                && !IsModifierKeyPressed(evt)
                && (
                       evt.keyCode == KeyCode.Q
                    || evt.keyCode == settings.MoveKey
                    || evt.keyCode == settings.RotateKey
                    || evt.keyCode == settings.ScaleKey
                    || evt.keyCode == KeyCode.T
                    || evt.keyCode == settings.SnapKey
                );
        }

        public static bool DrawButton(string text, string tooltip = null, string icon = null, GUIStyle style = null, params GUILayoutOption[] options)
        {
            GUIContent content;

            // icon
            if (!string.IsNullOrEmpty(icon))
                content = EditorGUIUtility.IconContent(icon);
            else
                content = new GUIContent();

            // text
            content.text = text;

            // tooltip
            if (!string.IsNullOrEmpty(tooltip))
                content.tooltip = tooltip;

            if (style == null)
                style = new GUIStyle(GUI.skin.button);

            return GUILayout.Button(content, style, options);
        }

        public static void BeginHorizontalIndent(int indentAmount = 10, bool beginVerticalInside = true)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(indentAmount);
            if (beginVerticalInside)
                GUILayout.BeginVertical();
        }

        public static void EndHorizontalIndent(float indentAmount = 10, bool begunVerticalInside = true, bool bothSides = false)
        {
            if (begunVerticalInside)
                GUILayout.EndVertical();
            if (bothSides)
                GUILayout.Space(indentAmount);
            GUILayout.EndHorizontal();
        }
    }
}
