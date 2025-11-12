using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using System.Collections.Generic;

namespace Kamgam.PowerPivot
{
    [EditorTool("Power Pivot")]
    partial class PowerPivotTool : EditorTool, ISerializationCallbackReceiver
    {
        public override GUIContent toolbarIcon
        {
            get
            {
                return EditorGUIUtility.IconContent("d_Transform Icon"); // d_ToolHandleLocal@2x
            }
        }

        public enum PTool
        {
            Cursor, Move, Scale, Rotate
        }

        public static PowerPivotTool Instance;

        public void OnEnable()
        {
            Instance = this;

            currentTool = PTool.Move;
            toggleDeleteAfterChildExtraction = true;

            Menu.SetChecked(PowerPivotSettings.ActivatePowerPivotMenuEntry, true);
        }

        protected PTool currentTool;
        public static PTool? defaultTool;
         PTool toolBeforeCursor;

        protected bool snapEnabled = false;

        // scaling state
        protected Vector3 scaleHandleScale;
        protected Dictionary<Transform, Vector3> scaleStartScales = new Dictionary<Transform, Vector3>();
        protected Dictionary<Transform, Vector3> scaleStartPositions = new Dictionary<Transform, Vector3>();

        // flags & temp
        protected bool undoRedoPerformed;
        protected bool selectionChanged;
        protected PivotMode lastPivotMode;
        protected PivotRotation lastPivotRotation;
        protected bool mouseIsDown;
        protected double lastMouseDragTime;
        protected double snapStartDelayRefTime;

        /// <summary>
        /// A method to activate the tool. Though remember, this is not awlays called.
        /// The tool is actually a ScriptableObject which is instantiated and deserialized by Unity.
        /// </summary>
        /// <param name="tool"></param>
        public static void Activate(PTool? tool = null)
        {
            defaultTool = tool;

#if UNITY_2020_2_OR_NEWER
            ToolManager.SetActiveTool(typeof(PowerPivotTool));
#else
            EditorTools.SetActiveTool(typeof(PowerPivotTool));
#endif
            SceneView.lastActiveSceneView.Focus();

            Menu.SetChecked(PowerPivotSettings.ActivatePowerPivotMenuEntry, true);
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            // Seems like this is not called after first instantiation? Using OnEnable instead.
        }

        public void OnToolChanged()
        {
            if (PowerPivotActiveState.IsActive)
            {
                SceneView.lastActiveSceneView.Focus();

                snapEnabled = false;

                cursorPositionCacheKey = getCursorCacheKey(Selection.gameObjects);
                cursorRotation = Quaternion.identity;
                updateCursor(CursorUpdateCause.ToolChanged);

                Undo.undoRedoPerformed -= onUndoRedoPerformed;
                Undo.undoRedoPerformed += onUndoRedoPerformed;

                Selection.selectionChanged -= onSelectionChanged;
                Selection.selectionChanged += onSelectionChanged;

                // Needed to react to changes in the default inspector (we want the cursor to move along)
                GlobalTransformObserver.OnChanged -= onTransformChanged;
                GlobalTransformObserver.OnChanged += onTransformChanged;
                if (UtilsEditor.IsInScene(target as GameObject))
                    GlobalTransformObserver.SetTransform((target as GameObject).transform);

                initWindowSize();

                if (defaultTool.HasValue)
                {
                    switchToolTo(defaultTool.Value);
                    defaultTool = null;
                }

                RegisterCursorUndo(cursorPosition);
                mouseIsDown = false;
            }
            else
            {
                // don't react to transform changes if the tool is not active
                GlobalTransformObserver.ClearTransform();
            }
        }

        void onTransformChanged(Transform transform)
        {
            restoreCursorFromLocalPosition();
            updateCursor();
            SceneView.lastActiveSceneView?.Repaint();
        }

        // Equivalent to Editor.OnSceneGUI.
        public override void OnToolGUI(EditorWindow window)
        {
            if (undoRedoPerformed)
            {
                initScaleHandle();
                initRotationHandle();
            }

            if (!(window is SceneView sceneView))
                return;

            // stop if nothing is selected
            var targetAsGameObject = target as GameObject;
            if (UtilsEditor.IsNotInScene(targetAsGameObject))
            {
                GlobalTransformObserver.ClearTransform();
                return;
            }

            // handle key presses & draw handles
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            var current = Event.current;

            // Fallback in case the global key event listener failed.
            // With this we at least have key events while the scene view is focused.
            if (!GlobalKeyEventHandler.RegistrationSucceeded)
                HandleKeyPressEvents(current);

#if UNITY_2022_1_OR_NEWER
            // In Unity 2022+ the Escape key automatically disables the tool BEFORE the global handler has called
            // HandleKeyPressEvents(), see GlobalKeyEventHandler.
            // Therefor we need to call HandleKeyPressEvents manually from here.
            if (GlobalKeyEventHandler.RegistrationSucceeded)
            {
                if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
                {
                    HandleKeyPressEvents(current);
                }
            }
#endif

            if (current.type == EventType.MouseDown)
            {
                mouseIsDown = true;

                Undo.IncrementCurrentGroup();

                if (currentTool == PTool.Rotate)
                    initRotationHandle();
                else if (currentTool == PTool.Scale)
                {
                    initScaleHandle();
                }
                else if (currentTool == PTool.Cursor)
                {
                    RegisterCursorUndo(cursorPosition);
                }
            }
            else if (current.type == EventType.MouseUp)
            {
                mouseIsDown = false;

                if (currentTool == PTool.Rotate)
                    initRotationHandle();
                else if (currentTool == PTool.Scale)
                {
                    initScaleHandle();
                }
            }
            else if (current.type == EventType.MouseDrag)
            {
                snapStartDelayRefTime = EditorApplication.timeSinceStartup;
                mouseIsDown = true;
                lastMouseDragTime = EditorApplication.timeSinceStartup;
            }
            else if (current.type == EventType.MouseMove)
            {
                // fixing mouse down
                if (mouseIsDown && EditorApplication.timeSinceStartup - lastMouseDragTime > 0.05f)
                    mouseIsDown = false;

                // Snapping while the move tool is active can be tricky because we have to find out if the user actually
                // wants to snap the cursor to a new position OR if the object should be move (while holding the snapKey).
                // To find out we do four things (A,B,C, D):
                // A use a delay (if the mouse is moved without pressing the snapKey for a while then assume the cursor should snap).
                if (mouseIsDown)
                    snapStartDelayRefTime = EditorApplication.timeSinceStartup;
                bool allowSnap = currentTool != PTool.Move || EditorApplication.timeSinceStartup - snapStartDelayRefTime > 1.0f;
                // B If the mouse is very far from the cursor then always assume it should snap immediately.
                if (!allowSnap)
                {
                    var cam = SceneView.lastActiveSceneView.camera;
                    if (cam != null)
                    {
                        float sqrDistance = UtilsEditor.SqrDistanceToGUIPointInScreenSpace(cam, current.mousePosition, cursorPosition);
                        if (sqrDistance > 400) // 20px
                            allowSnap = true;
                    }
                }
                // C if the the mouse is down then never snap.
                if (mouseIsDown)
                    allowSnap = false;
                // D if the mouse was dragged just recently then delay enabling snapping. This is done to avoid unwanted snapping just
                // after the move+snap was used (users do not always release the mouse and snapKey at the same time)
                if (currentTool == PTool.Move && !mouseIsDown && EditorApplication.timeSinceStartup - lastMouseDragTime < 0.6f)
                    allowSnap = false;

                if (snapEnabled && allowSnap)
                {
                    SnapCursor(sceneView, controlID);
                    current.Use();

                    if (currentTool == PTool.Move)
                    {
                        foreach (var obj in targets)
                        {
                            var go = obj as GameObject;
                            if (go != null)
                            {
                                Undo.RecordObject(go.transform, "Move via snapping");
                            }
                        }
                    }
                }
            }


            if (selectionChanged)
            {
                updateCursor(CursorUpdateCause.SelectionChanged);

                if (currentTool == PTool.Scale)
                    initScaleHandle();

                if (targetAsGameObject != null)
                    GlobalTransformObserver.SetTransform(targetAsGameObject.transform);
            }

            if (undoRedoPerformed)
            {
                sceneView.Focus();
                updateCursor(CursorUpdateCause.UndoPerformed);
            }

            if (lastPivotMode != Tools.pivotMode)
            {
                lastPivotMode = Tools.pivotMode;
                sceneView.Focus();
                updateCursor(CursorUpdateCause.PivotModeChanged, forceRefresh: true);
            }

            if (lastPivotRotation != Tools.pivotRotation)
            {
                lastPivotRotation = Tools.pivotRotation;
                sceneView.Focus();
                updateCursor(CursorUpdateCause.PivotRotationChanged);

                if (currentTool == PTool.Rotate)
                    initRotationHandle();
            }

            if (target != null)
            {
                switch (currentTool)
                {
                    case PTool.Move:
                        Move();
                        break;
                    case PTool.Scale:
                        Scale();
                        break;
                    case PTool.Rotate:
                        Rotate();
                        break;
                    case PTool.Cursor:
                        MoveCursor(sceneView);
                        break;
                }

                drawCursorGizmo(sceneView);

                if (Selection.activeGameObject != null)
                    drawOriginalCursorGizmo(Selection.activeGameObject.transform);

                var settings = PowerPivotSettings.GetOrCreateSettings();
                if (settings.ShowWindow)
                    drawWindow(sceneView, controlID + 1);
            }

            undoRedoPerformed = false;
            selectionChanged = false;
        }

        // Auto activates the tool on certain key presses.
        public static void HandleGlobalKey(Event current)
        {
            try
            {
                if (!PowerPivotActiveState.IsActive)
                {
                    var settings = PowerPivotSettings.GetOrCreateSettings();

                    // Switch to tool if settings.ContextualActivationKey is pressed while using the normal Scale or Rotate
                    if (current.type == EventType.KeyDown && current.keyCode == settings.SnapKey && settings.ContextualActivation
                        && !IsModifierKeyPressed(current)
                        && (SceneViewIsActive() || IsMouseInSceneView()))
                    {
                        if (Tools.current == Tool.Rotate)
                        {
                            Activate(PTool.Rotate);
                        }
                        else if (Tools.current == Tool.Scale)
                        {
                            Activate(PTool.Scale);
                        }
                        else if (Tools.current == Tool.Move && settings.ContextualActivationOnMove)
                        {
                            Activate(PTool.Move);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Logger.LogError(e.Message);
            }
        }

        // Handle key events if the tool is active.
        public void HandleKeyPressEvents(Event current)
        {
            //try
            {
                if (current == null)
                    return;

                // Skip if first person editor FPS camera mode is active
                if (Tools.viewTool == ViewTool.FPS)
                    return;

                var settings = PowerPivotSettings.GetOrCreateSettings();

                if (current.type == EventType.KeyDown)
                {
                    bool useKey = false;
                    if (!IsModifierKeyPressed(current))
                    {
                        if (current.keyCode == KeyCode.Q)
                        {
                            useKey = true;
                            switchToolTo(PTool.Cursor);
                        }
                        if (current.keyCode == settings.MoveKey)
                        {
                            useKey = true;
                            switchToolTo(PTool.Move);
                        }
                        else if (current.keyCode == settings.RotateKey)
                        {
                            useKey = true;
                            switchToolTo(PTool.Rotate);
                        }
                        else if (current.keyCode == settings.ScaleKey)
                        {
                            useKey = true;
                            currentTool = PTool.Scale;
                            switchToolTo(PTool.Scale);
                        }
                        else if (current.keyCode == KeyCode.T)
                        {
                            useKey = true;
                            Logger.Log("Sorry, the rect tool is not supported. Press Esc to return to normal tools.");
                        }
                        else if (current.keyCode == settings.SnapKey && (SceneViewIsActive() || IsMouseInSceneView()))
                        {
                            if (snapEnabled == false)
                            {
                                snapEnabled = true;
                                snapStartDelayRefTime = EditorApplication.timeSinceStartup;
                                buildScenesRaycastCache();
                                RegisterCursorUndo(cursorPosition);
                                SceneView.lastActiveSceneView.Focus();
                            }
                        }
                        else if (current.keyCode == settings.FrameSelectedKey)
                        {
                            if (settings.OverrideFraming)
                            {
                                FocusOnCursor();
                                useKey = true;
                            }
                            Logger.Log("Default frame selection was replaced. Focusing cursor position now.");
                        }
                    }

                    if ((SceneViewIsActive() || IsMouseInSceneView())
                        && current.isKey && current.control
                        )
                    {
                        if (current.keyCode == settings.UndoKey && HasCursorUndoActions())
                        {
                            UndoCursor();
                            useKey = true;
                        }
                        else if (current.keyCode == settings.RedoKey && HasCursorRedoActions())
                        {
                            RedoCursor();
                            useKey = true;
                        }
                    }

                    if (current.keyCode == settings.DeactivationKey)
                    {
                        useKey = currentTool == PTool.Cursor;
                        exitTool();
                    }

                    if (useKey)
                    {
                        current.Use();
                    }
                }
                else if (current.type == EventType.KeyUp)
                {
                    if (current.keyCode == settings.SnapKey)
                    {
                        snapEnabled = false;
                    }
                }
            }
            //catch (System.Exception e)
            {
               // Logger.LogError(e.Message);
            }
        }

        public void FocusOnCursor()
        {
            var cam = SceneView.lastActiveSceneView.camera;
            if (cam != null)
            {
                var delta = cam.transform.position - cursorPosition;
                SceneView.lastActiveSceneView.Frame(new Bounds(cursorPosition, Vector3.one * (delta.magnitude * 0.577f)), false);
            }
            else
            {
                SceneView.lastActiveSceneView.Frame(new Bounds(cursorPosition, Vector3.one * 5f), false);
            }
        }


        public static bool SceneViewIsActive()
        {
            return EditorWindow.focusedWindow == SceneView.lastActiveSceneView;
        }

        public static bool IsMouseInSceneView()
        {
            return EditorWindow.mouseOverWindow != null && SceneView.sceneViews.Contains(EditorWindow.mouseOverWindow);
        }

        public static bool IsModifierKeyPressed(Event evt)
        {
            return evt.isKey && (evt.control || evt.shift || evt.alt || evt.capsLock);
        }

        public void StopSnapping()
        {
            snapEnabled = false;
        }

        public void Deactivate()
        {
            exitTool();
        }

        void exitTool()
        {
            switch (currentTool)
            {
                case PTool.Cursor:
                    switchToolTo(toolBeforeCursor);
                    break;
                case PTool.Move:
                    Tools.current = Tool.Move;
                    break;
                case PTool.Scale:
                    Tools.current = Tool.Scale;
                    break;
                case PTool.Rotate:
                    Tools.current = Tool.Rotate;
                    break;
                default:
                    Tools.current = Tool.Move;
                    break;
            }

            Menu.SetChecked(PowerPivotSettings.ActivatePowerPivotMenuEntry, false);
        }

        void switchToolTo(PTool tool)
        {
            switch (tool)
            {
                case PTool.Cursor:
                    if(currentTool != PTool.Cursor)
                        toolBeforeCursor = currentTool;
                    currentTool = PTool.Cursor;
                    updateCursor(CursorUpdateCause.ToolChanged);
                    break;

                case PTool.Move:
                    currentTool = PTool.Move;
                    updateCursor(CursorUpdateCause.ToolChanged);
                    break;

                case PTool.Scale:
                    currentTool = PTool.Scale;
                    updateCursor(CursorUpdateCause.ToolChanged);
                    initScaleHandle();
                    break;

                case PTool.Rotate:
                    currentTool = PTool.Rotate;
                    updateCursor(CursorUpdateCause.ToolChanged);
                    break;
            }

            ClearCursorUndo();
        }

        void onUndoRedoPerformed()
        {
            undoRedoPerformed = true;
        }

        void onSelectionChanged()
        {
            selectionChanged = true;
        }
    }
}
