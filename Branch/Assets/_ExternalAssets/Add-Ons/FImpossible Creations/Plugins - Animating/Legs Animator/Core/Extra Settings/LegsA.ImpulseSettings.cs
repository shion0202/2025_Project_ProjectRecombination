using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{

    public partial class LegsAnimator
    {

        [Tooltip("Power multiplier for pelvis push events")]
        public float ImpulsesPowerMultiplier = 1f;
        public float ImpulsesDurationMultiplier = 1f;
        [Range(0f,1f)]
        [Tooltip("Damping impulses which are pushing body above ground level")]
        public float ImpulsesDampUpPushes = 0f;


        public PelvisImpulseSettings DebugPushHipsImpulse = new PelvisImpulseSettings(Vector3.down, 0.6f, 1f);

        [System.Serializable]
        public class PelvisImpulseSettings
        {
            public string OptionalName = "Impulse";

            [Space(3)]
            public float PowerMultiplier = 1f;

            [Tooltip("Duration of translation impulse in seconds")]
            public float ImpulseDuration = 0.5f;

            [Space(5)]
            public Vector3 WorldTranslation = Vector3.zero;
            public Vector3 LocalTranslation = new Vector3(0f, -0.2f, 0.1f);

            [Space(5)]
            public Vector3 HipsRotate = Vector3.zero;

            [Space(5)]
            [Range(0f,1f)]
            public float InheritElasticness = 0.75f;

            [FPD_FixedCurveWindow(0f, 0f, 1f, 1f)]
            public AnimationCurve ImpulseCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            [FPD_FixedCurveWindow(0f, 0f, 1f, 1f)]
            public AnimationCurve YAxisMultiplyCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1f);

            [Space(5)]
            [Tooltip("Local Offset Z-forward will bo rotated to face the legs animator's current desired move direction value")]
            public bool AlignWithDesiredMoveDirection = false;

            public PelvisImpulseSettings Copy()
            {
                return (PelvisImpulseSettings)MemberwiseClone();
            }

            public PelvisImpulseSettings()
            {
                ImpulseCurve = GetDefaultCurveInstance();
            }

            public static AnimationCurve GetDefaultCurveInstance()
            {
                AnimationCurve impulseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 0f);
                impulseCurve.AddKey(new Keyframe(0.2f, 1f));
                impulseCurve.SmoothTangents(1, 0.5f);
                return impulseCurve;
            }

            public PelvisImpulseSettings(Vector3 vector3, float duration, float power):this()
            {
                LocalTranslation = vector3;
                ImpulseDuration = duration;
                PowerMultiplier = power;
            }
        }

    }
}