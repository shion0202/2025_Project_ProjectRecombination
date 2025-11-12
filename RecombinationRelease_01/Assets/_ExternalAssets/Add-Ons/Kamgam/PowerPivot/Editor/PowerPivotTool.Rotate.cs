using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using System.Collections.Generic;

namespace Kamgam.PowerPivot
{
    partial class PowerPivotTool
    {
        // R O T A T E

        void initRotationHandle()
        {
            if (Tools.pivotRotation == PivotRotation.Global)
            {
                cursorRotation = Quaternion.identity;
            }
            else
            {
                if (Selection.activeGameObject != null)
                {
                    cursorRotation = Selection.activeGameObject.transform.rotation;
                }
            }
            cursorEulerRotationInput = cursorRotation.eulerAngles;
        }

        void Rotate()
        {
            // calc rotation change delta in quaternions
            var oldRotation = cursorRotation;
            cursorRotation = Handles.RotationHandle(cursorRotation, cursorPosition);
            Quaternion rotationDelta = cursorRotation * Quaternion.Inverse(oldRotation);
            // Convert to rotation around an axis.
            rotationDelta.ToAngleAxis(out float angle, out Vector3 rotationAxis);
            if (angle > 0.001f)
            {
                rotateBy(rotationDelta);
            }
        }

        void rotateBy(Quaternion rotationDelta)
        {
            // apply
            foreach (var obj in targets)
            {
                var go = obj as GameObject;
                if (go != null)
                {
                    Undo.RecordObject(go.transform, "Rotate via cursor");

                    // calc position before and after rotation and the apply the delta to localPosition.
                    Vector3 oldLocalCursorPos = go.transform.InverseTransformPoint(cursorPosition);
                    go.transform.rotation = rotationDelta * go.transform.rotation;
                    Vector3 newGlobalCursorPos = go.transform.TransformPoint(oldLocalCursorPos);
                    Vector3 posDeltaInParent;
                    if (go.transform.parent != null)
                        posDeltaInParent = go.transform.parent.InverseTransformVector(cursorPosition - newGlobalCursorPos);
                    else
                        posDeltaInParent = cursorPosition - newGlobalCursorPos;
                    go.transform.localPosition += posDeltaInParent;
                }
            }

            updateCursorRelativePosition();
            ClearCursorUndo();
            GlobalTransformObserver.SetTransform(Selection.activeGameObject.transform);
        }
    }
}
