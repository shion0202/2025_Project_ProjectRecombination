using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    [System.Serializable]
    public class MotionInfluenceProcessor
    {
        /// <summary> Using separated influence on each axis when enabled </summary>
        public bool AdvancedInfluence = false;
        public Vector3 AxisMotionInfluence = Vector3.one;
        public Vector3 AxisMotionInfluenceBackwards = Vector3.one;

        /// <summary> Can be used to lower blending when character is moving / idling </summary>
        private float MultiplyBlend = 1f;
        [NonSerialized] public float ExtraBoost = 1f;
        private float _sd_mb = 0f;
        public void TransitionBlend(float target, float duration, float delta)
        {
            MultiplyBlend = Mathf.SmoothDamp(MultiplyBlend, target, ref _sd_mb, duration, 10000000f, delta);
        }

        public void Reset()
        {
            previousPosition = root.position;
            localOffset = Vector3.zero;
            rootOffset = Vector3.zero;
        }


        private Transform root;

        public Vector3 OutputInfluenceOffset { get; private set; }
        public Vector3 previousPosition { get; private set; }
        public Vector3 rootOffset { get; private set; }
        public Vector3 localOffset { get; private set; }

        public void Init(Transform root)
        {
            this.root = root;
            previousPosition = root.position;
            localOffset = Vector3.zero;
        }

        public void Update()
        {
            rootOffset = root.position - previousPosition;
            previousPosition = root.position;
            localOffset = root.InverseTransformVector(rootOffset);

            float blend = MultiplyBlend * ExtraBoost;
            if (blend != 1f) localOffset *= blend;

            Motion_MotionInfluence();
        }


        public void OverrideOffset(Vector3 offset)
        {
            rootOffset = offset;
            localOffset = root.InverseTransformVector(rootOffset);

            Motion_MotionInfluence();
        }

        private void Motion_MotionInfluence()
        {
            if (!AdvancedInfluence)
            {
                if (AxisMotionInfluence != Vector3.one) OutputInfluenceOffset =  root.TransformVector(ScaleMotionInfluence(localOffset, AxisMotionInfluence));
            }
            else OutputInfluenceOffset =  root.TransformVector(ScaleMotionInfluence(localOffset, AxisMotionInfluence, AxisMotionInfluenceBackwards));
        }

        private Vector3 ScaleMotionInfluence(Vector3 toScale, Vector3 influenceMul)
        {
            return new Vector3(
                toScale.x * (1f - influenceMul.x),
                toScale.y * (1f - influenceMul.y),
                toScale.z * (1f - influenceMul.z));
        }

        private Vector3 ScaleMotionInfluence(Vector3 toScale, Vector3 influenceMulForw, Vector3 influenceMulBack)
        {
            if (toScale.x > 0f) toScale.x *= (1f - influenceMulForw.x); else toScale.x *= (1f - influenceMulBack.x);
            if (toScale.y > 0f) toScale.y *= (1f - influenceMulForw.y); else toScale.y *= (1f - influenceMulBack.y);
            if (toScale.z > 0f) toScale.z *= (1f - influenceMulForw.z); else toScale.z *= (1f - influenceMulBack.z);
            return toScale;
        }


        public Vector3 CalculateInversedInfluence()
        {
            if (!AdvancedInfluence)
            {
                if (AxisMotionInfluence != Vector3.one) return root.TransformVector(ScaleMotionInfluenceInverse(localOffset, AxisMotionInfluence));
            }
            else return root.TransformVector(ScaleMotionInfluenceInverse(localOffset, AxisMotionInfluence, AxisMotionInfluenceBackwards));
            
            return rootOffset;
        }

        private Vector3 ScaleMotionInfluenceInverse(Vector3 toScale, Vector3 influenceMul)
        {
            return new Vector3(
                toScale.x * (influenceMul.x),
                toScale.y * (influenceMul.y),
                toScale.z * (influenceMul.z));
        }

        private Vector3 ScaleMotionInfluenceInverse(Vector3 toScale, Vector3 influenceMulForw, Vector3 influenceMulBack)
        {
            if (toScale.x > 0f) toScale.x *= (influenceMulForw.x); else toScale.x *= (influenceMulBack.x);
            if (toScale.y > 0f) toScale.y *= (influenceMulForw.y); else toScale.y *= (influenceMulBack.y);
            if (toScale.z > 0f) toScale.z *= (influenceMulForw.z); else toScale.z *= (influenceMulBack.z);
            return toScale;
        }

#if UNITY_EDITOR

        public static void _EditorDrawGUI(SerializedProperty processor)
        {

            SerializedProperty motInfl = processor.FindPropertyRelative("AxisMotionInfluence");
            SerializedProperty motInflAdv = processor.FindPropertyRelative("AdvancedInfluence");
            SerializedProperty backwards = processor.FindPropertyRelative("AxisMotionInfluenceBackwards");


            if (motInflAdv.boolValue == false) // Simple motion influence slider
            {
                float pre = motInfl.vector3Value.x;
                Vector3 newVal = motInfl.vector3Value;

                EditorGUILayout.BeginHorizontal();
                newVal.x = EditorGUILayout.Slider(new GUIContent(motInfl.displayName, motInfl.tooltip), motInfl.vector3Value.x, 0f, 1f); EditorGUIUtility.labelWidth = 4;
                EditorGUILayout.PropertyField(motInflAdv, new GUIContent(" ", "Switch to advanced motion influence settings"), GUILayout.Width(24)); EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.EndHorizontal();

                if (newVal.x != pre)
                {
                    motInfl.vector3Value = Vector3.one * newVal.x;
                    backwards.vector3Value = motInfl.vector3Value;
                }
            }
            else // Motion influence per axis
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(motInfl); EditorGUIUtility.labelWidth = 4;
                EditorGUILayout.PropertyField(motInflAdv, new GUIContent(" ", "Switch to advanced motion influence settings"), GUILayout.Width(24)); EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(backwards);
            }

        }

#endif


    }

}