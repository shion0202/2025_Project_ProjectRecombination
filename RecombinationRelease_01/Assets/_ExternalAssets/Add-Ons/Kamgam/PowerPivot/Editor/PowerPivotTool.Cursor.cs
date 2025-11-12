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
        public enum CursorUpdateCause { Unknown, UndoPerformed, PivotRotationChanged, PivotModeChanged, SelectionChanged, ToolChanged }

        // Cursor position:
        // We cache one position per selection. This way the user does not have to
        // reposition the cursor every time the selection changes.
        protected Dictionary<string, Vector3> cursorPositionCache = new Dictionary<string, Vector3>();
        protected Dictionary<string, Vector3> cursorLocalPositionCache = new Dictionary<string, Vector3>();
        protected StringBuilder cursorCacheKeyBuilder = new StringBuilder();
        protected string cursorPositionCacheKey;

        /// <summary>
        /// Global cursor position.
        /// </summary>
        protected Vector3 cursorPosition
        {
            get
            {
                initCursorCacheKeyIfneeded();
                var pos = getCursorPositionFromCache(cursorPositionCacheKey);
                if (pos.HasValue)
                {
                    return pos.Value;
                }
                else
                {
                    return Vector3.zero;
                }
            }

            set
            {
                initCursorCacheKeyIfneeded();
                setCursorPositionInCache(cursorPositionCacheKey, value);
            }
        }

        /// <summary>
        /// Global cursor rotation.
        /// </summary>
        protected Quaternion cursorRotation;

        // Used for coninuous input of rotation (avoid gimbal lock)
        protected Vector3 cursorEulerRotationInput;

        /// <summary>
        /// The cursors relative position to the last selected object (or the reference object if multiple have been selected).
        /// This is used to restore the cursor position on Undo.
        /// </summary>
        protected Vector3? cursorRelativePositionLocal = null;

        protected Vector3? cursorRelativePositionWorld
        {
            get
            {
                if (!cursorRelativePositionLocal.HasValue)
                    return null;

                if (Selection.activeGameObject == null)
                    return null;

                return Selection.activeGameObject.transform.TransformPoint(cursorRelativePositionLocal.Value);
            }
        }

        public void ClearCursorCache()
        {
            cursorPositionCache.Clear();
            cursorLocalPositionCache.Clear();
        }

        protected void initCursorCacheKeyIfneeded()
        {
            if (cursorPositionCacheKey == null)
            {
                cursorPositionCacheKey = getCursorCacheKey(Selection.gameObjects);
            }
        }

        protected string getCursorCacheKey(IEnumerable<GameObject> gameObjects)
        {
            cursorCacheKeyBuilder.Clear();
            foreach (var go in gameObjects)
            {
                cursorCacheKeyBuilder.Append(go.GetInstanceID());
            }

            // In case we want to cache different cursor positions for mode/rotations.
            // objectsKeyBuilder.Append(Tools.pivotMode == PivotMode.Center ? "c" : "p");
            // objectsKeyBuilder.Append(Tools.pivotRotation == PivotRotation.Local ? "l" : "g");

            // avoid long keys by hashing if necessary
            if (cursorCacheKeyBuilder.Length <= 40)
                return cursorCacheKeyBuilder.ToString();
            else
                return UtilsHash.SHA1(cursorCacheKeyBuilder.ToString());
        }

        protected Vector3? getCursorPositionFromCache(string key)
        {
            if (cursorPositionCache.ContainsKey(key))
                return cursorPositionCache[key];
            else
                return null;
        }

        protected Vector3? getCursorLocalPositionFromCache(string key)
        {
            if (cursorLocalPositionCache.ContainsKey(key))
                return cursorLocalPositionCache[key];
            else
                return null;
        }

        protected void setCursorPositionInCache(string key, Vector3 position)
        {
            // store as local position in cache
            var go = target as GameObject;
            if (go != null)
            {
                var localPosition = go.transform.InverseTransformPoint(position);
                if (cursorLocalPositionCache.ContainsKey(key))
                    cursorLocalPositionCache[key] = localPosition;
                else
                    cursorLocalPositionCache.Add(key, localPosition);
            }

            // store in cache
            if (cursorPositionCache.ContainsKey(key))
                cursorPositionCache[key] = position;
            else
                cursorPositionCache.Add(key, position);
        }

        protected void removeCursorPositionInCache(string key)
        {
            if (cursorPositionCache.ContainsKey(key))
                cursorPositionCache.Remove(key);

            if (cursorLocalPositionCache.ContainsKey(key))
                cursorLocalPositionCache.Remove(key);
        }

        // Called from within OnGUI
        protected void drawCursorGizmo(SceneView sceneView)
        {
            var settings = PowerPivotSettings.GetOrCreateSettings();
            var colorA = settings.GizmoColorA;
            var colorB = settings.GizmoColorB;
            colorA.a = settings.GizmoOpacity;
            colorB.a = settings.GizmoOpacity;

            // draw cursor indicator
            Quaternion rot = Quaternion.identity;
            if ((sceneView.camera.transform.position - cursorPosition).sqrMagnitude > 0)
                rot = Quaternion.LookRotation(sceneView.camera.transform.position - cursorPosition);
            var size = HandleUtility.GetHandleSize(cursorPosition);

            // dot
            drawCircle(cursorPosition, rot, size * 0.01f, colorB, colorB, 3);
            // circles
            if (currentTool != PTool.Cursor)
            {
                drawCircle(cursorPosition, rot, size * 0.20f, colorA, colorB, 10);
                drawCircle(cursorPosition, rot, size * 0.19f, colorA, colorB, 10);
                drawCircle(cursorPosition, rot, size * 0.18f, colorA, colorB, 10);
            }
        }

        // Called from within OnGUI
        protected void drawOriginalCursorGizmo(Transform transform)
        {
            // don't draw if cursor pos is too close
            if (currentTool != PTool.Rotate && currentTool != PTool.Cursor && Vector3.SqrMagnitude(transform.position - cursorPosition) < 0.01f)
                return;

            // draw cursor indicator
            var size = HandleUtility.GetHandleSize(transform.position);

            var up = transform.TransformDirection(Vector3.up).normalized;
            var right = transform.TransformDirection(Vector3.right).normalized;
            var forward = transform.TransformDirection(Vector3.forward).normalized;

            Handles.color = Color.green;
            Handles.DrawDottedLine(transform.position, transform.position + up * size * 0.33f, 1f);

            Handles.color = Color.red;
            Handles.DrawDottedLine(transform.position, transform.position + right * size * 0.33f, 1f);

            Handles.color = Color.blue;
            Handles.DrawDottedLine(transform.position, transform.position + forward * size * 0.33f, 1f);
        }

        /// <summary>
        /// Draws a circle.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="color"></param>
        /// <param name="duration">Set to 0 in Editor to keep it for one frame.</param>
        /// <param name="circleSegments"></param>
        void drawCircle(Vector3 center, Quaternion rot, float radius, Color colorA, Color colorB,  int circleSegments = 10)
        {
            var col = Handles.color;

            // getCirclePoints
            var angleInRad = 0f;  // angle that will be increased each loop
            var step = Mathf.PI * 2f / circleSegments;
            Vector3 point = Vector3.zero;
            Vector3 previousPoint = Vector3.zero;
            Color color;
            int i;
            for (int x = 0; x <= circleSegments; ++x)
            {
                i = x % circleSegments;
                color = (i % 2 == 0 ? colorA : colorB);
                Handles.color = color;
                point.x = radius * Mathf.Cos(angleInRad);
                point.y = radius * Mathf.Sin(angleInRad);
                point = rot * point;
                angleInRad += step;
                if (x > 0)
                {
                    Handles.DrawLine(center + previousPoint, center + point);
                }
                previousPoint = point;
            }

            Handles.color = col;
        }

        void updateCursor(CursorUpdateCause reason = CursorUpdateCause.Unknown, bool forceRefresh = false)
        {
            // restore from local cache
            if (reason == CursorUpdateCause.ToolChanged || reason == CursorUpdateCause.SelectionChanged)
            {
                restoreCursorFromLocalPosition();
            }

            // update cursor position (or read from cache)
            cursorPositionCacheKey = getCursorCacheKey(Selection.gameObjects);
            var cachedPos = getCursorPositionFromCache(cursorPositionCacheKey);
            if (cachedPos.HasValue && !forceRefresh)
            {
                cursorPosition = cachedPos.Value;
            }
            else
            {
                cursorPosition = calculateDefaultHandlePivotPosition();
                setCursorPositionInCache(cursorPositionCacheKey, cursorPosition);
            }

            // rotation
            if (Selection.gameObjects.Length > 0 && Selection.activeGameObject != null)
                cursorRotation = Tools.pivotRotation == PivotRotation.Local ? Selection.activeGameObject.transform.rotation : Quaternion.identity;
            else
                cursorRotation = Quaternion.identity;

            if (reason == CursorUpdateCause.SelectionChanged)
            {
                updateCursorRelativePosition();
                ClearCursorUndo();
            }
            // Restore gizmo position after undo
            else if (reason == CursorUpdateCause.UndoPerformed)
            {
                if (cursorRelativePositionWorld.HasValue)
                {
                    cursorPosition = cursorRelativePositionWorld.Value;
                }
            }
        }

        /// <summary>
        /// Whenever cursorPosition is set we also store that position as a localPosition relative
        /// to the current target GameObject. Now this method takes that localPosition, converts
        /// it back to a global position and set cursorPosition to that position.
        /// </summary>
        void restoreCursorFromLocalPosition()
        {
            var settings = PowerPivotSettings.GetOrCreateSettings();

            if (settings.UpdateCursorWithObject == PowerPivotSettings.UpdateCursorOptions.Never)
                return;

            // do not restore if the current rotation mode is global
            if (Tools.pivotRotation == PivotRotation.Global && settings.UpdateCursorWithObject == PowerPivotSettings.UpdateCursorOptions.OnlyIfLocal)
                return;

            string key = getCursorCacheKey(Selection.gameObjects);
            var localPos = getCursorLocalPositionFromCache(key);

            if (!localPos.HasValue)
                return;

            var go = target as GameObject;

            if (UtilsEditor.IsNotInScene(go))
                return;

            setCursorPositionInCache(key, go.transform.TransformPoint(localPos.Value));
        }

        void updateCursorRelativePosition()
        {
            
            if (Selection.gameObjects.Length == 0 || Selection.activeGameObject == null)
            {
                cursorRelativePositionLocal = null;
            }
            else
            {
                cursorRelativePositionLocal = Selection.activeGameObject.transform.InverseTransformPoint(cursorPosition);
            }
        }

        /// <summary>
        /// Mirrors the default beviour of Unity pivots.
        /// </summary>
        /// <returns></returns>
        Vector3 calculateDefaultHandlePivotPosition()
        {
            Vector3 result = Vector3.zero;

            var gameObjects = Selection.gameObjects;
            if (gameObjects.Length >= 1)
            {
                if (Tools.pivotMode == PivotMode.Pivot)
                {
                    // pivot of last selected (from target or selection)
                    var targetGo = target as GameObject;
                    if (targetGo != null)
                        result = targetGo.transform.position;
                    else if (Selection.activeGameObject != null)
                        result = Selection.activeGameObject.transform.position;
                }
                else
                {
                    // center of all selected objects
                    // TODO: expand bounds and then take the center instead of avg.
                    var avgPos = Vector3.zero;
                    int numOfObjects = 0;
                    MeshRenderer meshRenderer;
                    foreach (var go in Selection.gameObjects)
                    {
                        numOfObjects++;
                        if (go.TryGetComponent(out meshRenderer))
                        {
                            avgPos += meshRenderer.bounds.center;
                        }
                        else
                        {
                            avgPos += go.transform.position;
                        }
                    }
                    avgPos /= numOfObjects;
                    result = avgPos;
                }
            }

            return result;
        }

        void MoveCursor(SceneView sceneView)
        {
            var settings = PowerPivotSettings.GetOrCreateSettings();

            // draw small scale handle 
            var matrix = Handles.matrix;
            Handles.matrix = Matrix4x4.Scale(Vector3.one / 1.1f) * matrix;
            var newCursorPosition = Handles.PositionHandle(cursorPosition * 1.1f, cursorRotation) / 1.1f;
            Handles.matrix = matrix;

            if (Vector3.Magnitude(newCursorPosition - cursorPosition) > 0.0001f)
            {
                cursorPosition = newCursorPosition;

                updateCursorRelativePosition();
                GlobalTransformObserver.SetTransform(Selection.activeGameObject.transform);
            }

            // draw deco gimzos
            var rot = Quaternion.LookRotation(sceneView.camera.transform.position - cursorPosition);
            var size = HandleUtility.GetHandleSize(cursorPosition);
            if ((sceneView.camera.transform.position - cursorPosition).sqrMagnitude > 0)
                rot = Quaternion.LookRotation(sceneView.camera.transform.position - cursorPosition);
            var cA = settings.GizmoCursorColor;
            var cB = settings.GizmoColorB;
            var a = settings.GizmoOpacity;
            drawCircle(cursorPosition, rot, 0.17f * size, new Color(cA.r, cA.g, cA.b, cA.a * a * 0.8f), new Color(cB.r, cB.g, cB.b, a * 1.0f), 10);
            drawCircle(cursorPosition, rot, 0.16f * size, new Color(cA.r, cA.g, cA.b, cA.a * a * 0.8f), new Color(cB.r, cB.g, cB.b, a * 0.8f), 10);
            drawCircle(cursorPosition, rot, 0.14f * size, new Color(cA.r, cA.g, cA.b, cA.a * a * 0.6f), new Color(cB.r, cB.g, cB.b, a * 0.6f), 10);
            drawCircle(cursorPosition, rot, 0.12f * size, new Color(cA.r, cA.g, cA.b, cA.a * a * 0.4f), new Color(cB.r, cB.g, cB.b, a * 0.4f), 10);
            drawCircle(cursorPosition, rot, 0.10f * size, new Color(cA.r, cA.g, cA.b, cA.a * a * 0.2f), new Color(cB.r, cB.g, cB.b, a * 0.2f), 10);
        }

        #region Undo Stack
        /// <summary>
        /// We have to maintain our own undo/redo stacks for the cursor position because there is no object to register in undo.
        /// </summary>
        protected Stack<Vector3> cursorHistoryUndoStack = new Stack<Vector3>();
        protected Stack<Vector3> cursorHistoryRedoStack = new Stack<Vector3>();

        protected double lastCursorUndoRegistrationTime = 0;

        protected void ClearCursorUndo()
        {
            cursorHistoryUndoStack.Clear();
            cursorHistoryRedoStack.Clear();
        }

        /// <summary>
        /// Register (or update) a new undo action.
        /// </summary>
        /// <param name="cursorPosition"></param>
        /// <param name="minTimeDelta">Register a new undo if the last one is older than # seconds, otherwise update the current one.</param>
        /// <param name="forceNewGroup"></param>
        protected void RegisterCursorUndo(Vector3 cursorPosition, double minTimeDelta = 0d, bool forceNewGroup = false)
        {
            // abort if the new cursor pos is identical to the old one
            if (cursorHistoryUndoStack.Count > 0 && (cursorHistoryUndoStack.Peek() - cursorPosition).magnitude < 0.0001f)
                return;

            if (forceNewGroup)
            {
                cursorHistoryUndoStack.Push(cursorPosition);
            }
            else
            {
                // Register a new undo if the last one is older than # seconds, otherwise update the current one.
                if (EditorApplication.timeSinceStartup - lastCursorUndoRegistrationTime < minTimeDelta)
                {
                    if (cursorHistoryUndoStack.Count > 0)
                        cursorHistoryUndoStack.Pop();
                }
                cursorHistoryUndoStack.Push(cursorPosition);
            }
            lastCursorUndoRegistrationTime = EditorApplication.timeSinceStartup;
            cursorHistoryRedoStack.Clear();
        }

        protected bool HasCursorUndoActions()
        {
            return cursorHistoryUndoStack.Count > 0;
        }

        protected bool HasCursorRedoActions()
        {
            return cursorHistoryRedoStack.Count > 0;
        }

        protected void UndoCursor()
        {
            if (cursorHistoryUndoStack.Count > 0)
            {
                cursorHistoryRedoStack.Push(cursorPosition);
                cursorPosition = cursorHistoryUndoStack.Pop();
            }
        }

        protected void RedoCursor()
        {
            if (cursorHistoryRedoStack.Count > 0)
            {
                cursorHistoryUndoStack.Push(cursorPosition);
                cursorPosition = cursorHistoryRedoStack.Pop();
            }
        }
        #endregion
    }
}
