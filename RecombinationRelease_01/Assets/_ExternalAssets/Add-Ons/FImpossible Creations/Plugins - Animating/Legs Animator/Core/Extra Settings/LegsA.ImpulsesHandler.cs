using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{

    public partial class LegsAnimator
    {
        protected List<ImpulseExecutor> Impulses = new List<ImpulseExecutor>();

        public struct ImpulseExecutor
        {
            public float Elapsed;

            public float PowerMultiplier;
            public float ImpulseDuration;
            public Vector3 WorldTranslation;
            public Vector3 LocalTranslation;
            public float InheritElasticness;
            public Vector3 HipsRotation;
            public bool AlignDesired;

            public AnimationCurve ImpulseCurve;
            public AnimationCurve YAxisMultiplyCurve;

            public static AnimationCurve DefaultCurve { get { if (_defaultCurve == null) _defaultCurve = PelvisImpulseSettings.GetDefaultCurveInstance(); return _defaultCurve; } }
            private static AnimationCurve _defaultCurve = null;

            public static AnimationCurve DefaultCurve11 { get { if (_defaultCurve11 == null) _defaultCurve11 = AnimationCurve.Linear(0f, 1f, 1f, 1f); return _defaultCurve11; } }
            private static AnimationCurve _defaultCurve11 = null;

            public ImpulseExecutor(PelvisImpulseSettings settings, float powerMultiplier = 1f, float durationMultiplier = 1f)
            {
                Elapsed = 0f;
                PowerMultiplier = settings.PowerMultiplier * powerMultiplier;
                ImpulseDuration = settings.ImpulseDuration * durationMultiplier;
                WorldTranslation = settings.WorldTranslation;
                LocalTranslation = settings.LocalTranslation;
                InheritElasticness = settings.InheritElasticness;
                HipsRotation = settings.HipsRotate;
                ImpulseCurve = settings.ImpulseCurve;
                YAxisMultiplyCurve = settings.YAxisMultiplyCurve;
                AlignDesired = settings.AlignWithDesiredMoveDirection;
            }

            public ImpulseExecutor(Vector3 localOffset, float duration, float elastic = 1f, AnimationCurve curve = null, bool alignWithDesiredDir = false)
            {
                Elapsed = 0f;
                PowerMultiplier = 1f;
                ImpulseDuration = duration;
                WorldTranslation = Vector3.zero;
                LocalTranslation = localOffset;
                InheritElasticness = elastic;
                ImpulseCurve = curve;
                if (curve == null) ImpulseCurve = DefaultCurve;
                YAxisMultiplyCurve = DefaultCurve11;
                HipsRotation = Vector3.zero;
                AlignDesired = alignWithDesiredDir;
            }

            public ImpulseExecutor(Vector3 localOffset, Vector3 hipsRotation, float duration, float elastic = 1f, AnimationCurve curve = null, bool alignWithDesiredDir = false)
            {
                Elapsed = 0f;
                PowerMultiplier = 1f;
                ImpulseDuration = duration;
                WorldTranslation = Vector3.zero;
                HipsRotation = hipsRotation;
                LocalTranslation = localOffset;
                InheritElasticness = elastic;
                ImpulseCurve = curve;
                if (curve == null) ImpulseCurve = DefaultCurve;
                YAxisMultiplyCurve = DefaultCurve11;
                AlignDesired = alignWithDesiredDir;
            }

            public ImpulseExecutor(float duration, Vector3 worldOffset, float elastic = 1f, AnimationCurve curve = null, bool alignWithDesiredDir = false)
            {
                Elapsed = 0f;
                PowerMultiplier = 1f;
                ImpulseDuration = duration;
                WorldTranslation = worldOffset;
                HipsRotation = Vector3.zero;
                LocalTranslation = Vector3.zero;
                InheritElasticness = elastic;
                ImpulseCurve = curve;
                if (curve == null) ImpulseCurve = DefaultCurve;
                YAxisMultiplyCurve = DefaultCurve11;
                AlignDesired = alignWithDesiredDir;
            }

            public void Update(float delta)
            {
                Elapsed += delta;
            }

            public bool Finished { get { return Elapsed >= ImpulseDuration; } }
            public float Progress { get { return ImpulseDuration == 0f ? 1f : Elapsed / ImpulseDuration; } }
            public float Evaluation { get { return ImpulseCurve.Evaluate(Progress); } }
            public float Elastic { get { return InheritElasticness; } }
            public float Power { get { return PowerMultiplier; } }
            public Vector3 CurrentLocalOffset { get { return LocalTranslation * Evaluation * Power; } }
            public float CurrentLocalYAxisMultiplier { get { return YAxisMultiplyCurve.Evaluate(Progress); } }
            public Vector3 CurrentWorldOffset { get { return WorldTranslation * Evaluation * Power; } }
        }

        bool _ImpulsesDoWorld = false;
        bool _ImpulsesDoLocal = false;
        bool _ImpulsesDoHips = false;
        Vector3 _ImpulsesWorldPush = Vector3.zero;
        Vector3 _ImpulsesWorldPushInherit = Vector3.zero;
        Vector3 _ImpulsesLocalPush = Vector3.zero;
        Vector3 _ImpulsesLocalPushInherit = Vector3.zero;
        Vector3 _ImpulsesHipsRotation = Vector3.zero;
        Vector3 _ImpulsesRotationElastic = Vector3.zero;

        Vector3 _Hips_RotationElasticLocalOffset = Vector3.zero;

        void Hips_Calc_UpdateImpulses()
        {
            _ImpulsesDoLocal = false;
            _ImpulsesDoWorld = false;
            _ImpulsesDoHips = false;


            if (Impulses.Count == 0) return;

            if (ImpulsesDurationMultiplier < 0.001) ImpulsesDurationMultiplier = 1f;

            _ImpulsesWorldPush = Vector3.zero;
            _ImpulsesWorldPushInherit = Vector3.zero;
            _ImpulsesLocalPush = Vector3.zero;
            _ImpulsesLocalPushInherit = Vector3.zero;
            _ImpulsesHipsRotation = Vector3.zero;

            Vector3 desirDirNorm = DesiredMovementDirection.normalized;

            // Execute impulses in right order
            for (int i = 0; i < Impulses.Count; i++)
            {
                var impulse = Impulses[i];
                impulse.Update(DeltaTime / ImpulsesDurationMultiplier);

                if (impulse.WorldTranslation != Vector3.zero)
                {
                    Vector3 push = impulse.CurrentWorldOffset * ImpulsesPowerMultiplier;

                    if (impulse.Elastic <= 0f)
                        _ImpulsesWorldPush += push;
                    else if (impulse.Elastic >= 1f)
                        _ImpulsesWorldPushInherit += push;
                    else
                    {
                        _ImpulsesWorldPush += (1f - impulse.Elastic) * push;
                        _ImpulsesWorldPushInherit += impulse.Elastic * push;
                    }
                }

                if (impulse.LocalTranslation != Vector3.zero)
                {
                    Vector3 push = impulse.CurrentLocalOffset * (ImpulsesPowerMultiplier * ScaleReferenceNoScale);
                    push.y *= impulse.CurrentLocalYAxisMultiplier;

                    bool defaultLocal = true;

                    if (impulse.AlignDesired)
                    {
                        if (DesiredMovementDirection != Vector3.zero)
                        {
                            defaultLocal = false;

                            // Remap for desired dir and apply to world space impulse
                            Quaternion remap = BaseTransform.rotation * Quaternion.FromToRotation(BaseTransform.forward.normalized, desirDirNorm);
                             push = remap * push;

                            if (impulse.Elastic <= 0f)
                                _ImpulsesWorldPush += push;
                            else if (impulse.Elastic >= 1f)
                                _ImpulsesWorldPushInherit += push;
                            else
                            {
                                _ImpulsesWorldPush += (1f - impulse.Elastic) * push;
                                _ImpulsesWorldPushInherit += impulse.Elastic * push;
                            }
                        }

                    }


                    if (defaultLocal)
                    {
                        if (impulse.Elastic <= 0f)
                            _ImpulsesLocalPush += push;
                        else if (impulse.Elastic >= 1f)
                            _ImpulsesLocalPushInherit += push;
                        else
                        {
                            _ImpulsesLocalPush += (1f - impulse.Elastic) * push;
                            _ImpulsesLocalPushInherit += impulse.Elastic * push;
                        }
                    }

                }

                if (impulse.HipsRotation != Vector3.zero)
                {
                    Vector3 rotImpulse = impulse.HipsRotation;

                    if ( impulse.AlignDesired)
                    {
                        if (Vector3.Dot(BaseTransform.forward.normalized, desirDirNorm) < 0f)
                        {
                            rotImpulse.z = -rotImpulse.z;
                        }
                    }

                    _ImpulsesHipsRotation += rotImpulse * (ImpulsesPowerMultiplier * impulse.Evaluation * impulse.Power);
                }

                Impulses[i] = impulse;
            }

            // Check for removing
            for (int i = Impulses.Count - 1; i >= 0; i--)
            {
                if (Impulses[i].Finished) Impulses.RemoveAt(i);
            }

            if (_ImpulsesWorldPush != Vector3.zero || _ImpulsesWorldPushInherit != Vector3.zero)
                _ImpulsesDoWorld = true;

            if (_ImpulsesLocalPush != Vector3.zero || _ImpulsesLocalPushInherit != Vector3.zero)
                _ImpulsesDoLocal = true;

            if (_ImpulsesHipsRotation != Vector3.zero)
                _ImpulsesDoHips = true;
        }


        void Hips_Calc_ApplyImpulsesInherit()
        {
            if (_ImpulsesDoLocal)
            {
                _Hips_StabilityLocalOffset += (_ImpulsesLocalPushInherit * _MainBlend);
            }

            if (_ImpulsesDoWorld)
            {
                _Hips_StabilityLocalOffset += ToRootLocalSpaceVec(_ImpulsesWorldPushInherit * _MainBlend);
            }

            if (_ImpulsesDoHips || _ImpulsesRotationElastic != Vector3.zero)
            {
                _Hips_RotationMuscle.Update(DeltaTime, _ImpulsesHipsRotation);
                _ImpulsesRotationElastic = _Hips_RotationMuscle.ProceduralEulerAngles;

                _Hips_Modules_ExtraRotOffset += _ImpulsesRotationElastic;
            }
        }

        void Hips_Calc_ApplyImpulses()
        {
            if (_ImpulsesDoLocal)
            {
                _LastAppliedHipsFinalPosition += RootToWorldSpaceVec(_ImpulsesLocalPush * _MainBlend);
            }

            if (_ImpulsesDoWorld)
            {
                _LastAppliedHipsFinalPosition += _ImpulsesWorldPush * _MainBlend;
            }
        }
    }
}