using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Kamgam.PowerPivot
{
    partial class PowerPivotTool
    {
        // S C A L E

        void initScaleHandle()
        {
            scaleHandleScale = Vector3.one;

            scaleStartScales.Clear();
            scaleStartPositions.Clear();
            foreach (var obj in targets)
            {
                var go = obj as GameObject;
                if (go != null)
                {
                    scaleStartScales.Add(go.transform, go.transform.localScale);
                    scaleStartPositions.Add(go.transform, go.transform.position);
                }
            }
        }

        void Scale()
        {
            var settings = PowerPivotSettings.GetOrCreateSettings();
            var targetGo = target as GameObject;

            // overwrite cursorRotation (always use local rotation for scale commands on a single object)
            if (Tools.pivotRotation == PivotRotation.Global && targetGo != null && targets.HasExactlyOneGameObject())
                cursorRotation = targetGo.transform.rotation;

            var size = HandleUtility.GetHandleSize(cursorPosition);
            var prevScaleHandleScale = scaleHandleScale;
            scaleHandleScale = Handles.ScaleHandle(scaleHandleScale, cursorPosition, cursorRotation, size);

            // Simulate "Plane Axis Scaling" if SHIFT is pressed
            if (Vector3.Magnitude(scaleHandleScale - prevScaleHandleScale) > 0.001f)
            {
                if (settings.PlaneScalingKey != KeyCode.None
                    && (settings.PlaneScalingKey == KeyCode.LeftShift && Event.current.shift
                     || settings.PlaneScalingKey == KeyCode.LeftControl && Event.current.control
                     || settings.PlaneScalingKey == KeyCode.LeftAlt && Event.current.alt
                     || Event.current.keyCode == settings.PlaneScalingKey))
                {
                    if (Mathf.Abs(scaleHandleScale.x - prevScaleHandleScale.x) > 0.001f)
                    {
                        scaleHandleScale.y = scaleHandleScale.x;
                        scaleHandleScale.z = scaleHandleScale.x;
                        scaleHandleScale.x = prevScaleHandleScale.x;
                    }
                    else if (Mathf.Abs(scaleHandleScale.y - prevScaleHandleScale.y) > 0.001f)
                    {
                        scaleHandleScale.x = scaleHandleScale.y;
                        scaleHandleScale.z = scaleHandleScale.y;
                        scaleHandleScale.y = prevScaleHandleScale.y;
                    }
                    else if (Mathf.Abs(scaleHandleScale.z - prevScaleHandleScale.z) > 0.001f)
                    {
                        scaleHandleScale.x = scaleHandleScale.z;
                        scaleHandleScale.y = scaleHandleScale.z;
                        scaleHandleScale.z = prevScaleHandleScale.z;
                    }
                }

                scaleTo(targetGo, scaleHandleScale, scaleStartScales, scaleStartPositions, cursorPosition, cursorRotation);
            }
        }

        void scaleTo(GameObject targetGo, Vector3 newScale,
            Dictionary<Transform, Vector3> scaleStartScales, Dictionary<Transform, Vector3> scaleStartPositions,
            Vector3 cursorPosition, Quaternion cursorRotation)
        {
            // apply
            if (targets.HasExactlyOneGameObject())
            {
                Undo.RecordObject(targetGo.transform, "Scale via cursor");
                scaleAroundForSingleObject(targetGo.transform, scaleStartScales[targetGo.transform], cursorPosition, cursorRotation, newScale);
            }
            else
            {
                // Ignore PivotRotation.Global if only one object is selected (Unity does the same).
                // N2H: find out how to disable the button while doing this (seems there is no public)
                bool takeRotationIntoAccount = Tools.pivotRotation == PivotRotation.Local || targets.HasExactlyOneGameObject();

                foreach (var obj in targets)
                {
                    var go = obj as GameObject;
                    if (go != null && scaleStartScales.ContainsKey(go.transform))
                    {
                        Undo.RecordObject(go.transform, "Scale multiple via cursor");

                        scaleAroundForMultipleObjects(
                            go.transform,
                            scaleStartScales[go.transform],
                            scaleStartPositions[go.transform],
                            cursorPosition,
                            cursorRotation,
                            scaleHandleScale,
                            takeRotationIntoAccount,
                            rotationTransform: targetGo.transform // use the last selected as rotation source (why? Because Unity does the same)
                            );
                    }
                }
            }

            updateCursorRelativePosition();
            ClearCursorUndo();
            GlobalTransformObserver.SetTransform(Selection.activeGameObject.transform);
        }

        // Scaling multiple objects correctly is not easily possible. It would either require the tool to add
        // a parent transform or modify the mesh geometry. Both of which we do not want. The vurrent solution
        // is to do as Unity itself does. Pick the major scaling axis, apply localScaling to each object and
        // move each object along that direction. Caveat: this approach leads to the cursor not being aligned
        // with the object(s) anymore, thus we also have a single object scaling method which preserves the
        // alignment (thought that one can not be used for multiple objects). By "aligment" I mean the behaviour
        // that the cursor remains at the vertex it was snapped to (or more precisely the object is moved while
        // scaling accordingly).
        void scaleAroundForMultipleObjects(Transform transform, Vector3 startScale, Vector3 startPos, Vector3 cursorPosition, Quaternion cursorRotation, Vector3 scaleFactor, bool takeRotationIntoAccount = false, Transform rotationTransform = null)
        {
            // pos
            var posDelta = startPos - cursorPosition;

            // invert rotation
            if (takeRotationIntoAccount)
                posDelta = Quaternion.Inverse(cursorRotation) * posDelta;

            // invert local scale
            posDelta.Scale(scaleFactor);

            // restore rotation
            if (takeRotationIntoAccount)
                posDelta = cursorRotation * posDelta;

            transform.position = cursorPosition + posDelta;

            // None uniform scaling on multiple objects. We can not accurately execute this, so we do the same as unity does: pick the closest axis and scale that.
            if (scaleFactor.x != scaleFactor.y || scaleFactor.z != scaleFactor.y)
            {
                // Calc axis disalignment angle
                if (Mathf.Abs(scaleFactor.x - 1f) > 0.0001f)
                    scaleFactor = pickScaleBasedOnAxis(transform, cursorRotation, scaleFactor, Vector3.right, scaleFactor.x);
                else if (Mathf.Abs(scaleFactor.y - 1f) > 0.0001f)
                    scaleFactor = pickScaleBasedOnAxis(transform, cursorRotation, scaleFactor, Vector3.up, scaleFactor.y);
                else if (Mathf.Abs(scaleFactor.z - 1f) > 0.0001f)
                    scaleFactor = pickScaleBasedOnAxis(transform, cursorRotation, scaleFactor, Vector3.forward, scaleFactor.z);
            }

            // scale
            var finalScale = startScale;
            finalScale.Scale(scaleFactor);
            transform.localScale = finalScale;
        }

        // See comments on scaleAroundForMultipleObjects().
        void scaleAroundForSingleObject(Transform transform, Vector3 startScale, Vector3 cursorPosition, Quaternion cursorRotation, Vector3 scaleFactor)
        {
            // modify scale
            var finalScale = startScale;
            finalScale.Scale(scaleFactor);

            // apply scale (and memorize relative cursor pos)
            var preScaleCursorLocalPos = transform.InverseTransformPoint(cursorPosition);
            transform.localScale = finalScale;
            var postScaleCursorPos = transform.TransformPoint(preScaleCursorLocalPos);

            // update position
            var globalPosDelta = cursorPosition - postScaleCursorPos;
            transform.position += globalPosDelta;
        }

        Vector3 pickScaleBasedOnAxis(Transform transform, Quaternion cursorRotation, Vector3 scaleFactor, Vector3 axis, float singleScaleFactor)
        {
            var localAxis = transform.InverseTransformDirection(cursorRotation * axis);
            var angleDeltaX = Mathf.Abs(localAxis.x);
            var angleDeltaY = Mathf.Abs(localAxis.y);
            var angleDeltaZ = Mathf.Abs(localAxis.z);
            scaleFactor.x = scaleFactor.y = scaleFactor.z = 1f;
            if (angleDeltaX > angleDeltaY && angleDeltaX > angleDeltaZ)
                scaleFactor.x = singleScaleFactor;
            if (angleDeltaY > angleDeltaX && angleDeltaY > angleDeltaZ)
                scaleFactor.y = singleScaleFactor;
            if (angleDeltaZ > angleDeltaX && angleDeltaZ > angleDeltaY)
                scaleFactor.z = singleScaleFactor;
            return scaleFactor;
        }
    }
}
