using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using System.Collections.Generic;

namespace Kamgam.PowerPivot
{
    partial class PowerPivotTool
    {
        // M O V E

        void Move()
        {
            var prevPosition = cursorPosition;
            var newPosition = Handles.PositionHandle(cursorPosition, cursorRotation);
            var delta = newPosition - prevPosition;

            if (Vector3.Magnitude(delta) > 0.0001f)
                moveBy(delta);
        }

        void moveBy(Vector3 delta)
        {
            foreach (var obj in targets)
            {
                var go = obj as GameObject;
                if (go != null)
                {
                    Undo.RecordObject(go.transform, "Move via cursor");
                    go.transform.position += delta;
                }
            }

            // This updates the relative position and therefore depends on the transform of the go to be updated.
            // Thus it is important that it is called AFTER the transform updates.
            // rotateBy() this is done via updateCursorRelativePosition but we don't want to do this here as
            // it would be redundant.
            cursorPosition += delta;

            ClearCursorUndo();
            GlobalTransformObserver.SetTransform(Selection.activeGameObject.transform);
        }
    }
}
