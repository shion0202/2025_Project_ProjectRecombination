using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Kamgam.PowerPivot
{
    [InitializeOnLoad]
    public static class GlobalTransformObserver
    {
        static bool hasTransform;
        static Transform transform;

        static Vector3 localPosition;
        static Vector3 localScale;
        static Quaternion localRotation;

        public static event Action<Transform> OnChanged;

        static GlobalTransformObserver()
        {
            EditorApplication.update -= onUpdate;
            EditorApplication.update += onUpdate;
        }

        static void onUpdate()
        {
            try
            {
                if (!hasTransform || transform == null)
                    return;

                if (   localPosition != transform.localPosition
                    || localScale != transform.localScale
                    || localRotation != transform.localRotation)
                {
                    localPosition = transform.localPosition;
                    localScale = transform.localScale;
                    localRotation = transform.localRotation;

                    OnChanged(transform);
                }
            }
            catch
            { 
                // fail silently
                // TODO: log if log level is high enough
            }
        }

        public static void SetTransform(Transform transform)
        {
            GlobalTransformObserver.transform = transform;
            hasTransform = transform != null;

            if (hasTransform)
            {
                localPosition = transform.localPosition;
                localScale = transform.localScale;
                localRotation = transform.localRotation;
            }
        }

        public static void ClearTransform()
        {
            transform = null;
            hasTransform = false;
        }
    }
}
