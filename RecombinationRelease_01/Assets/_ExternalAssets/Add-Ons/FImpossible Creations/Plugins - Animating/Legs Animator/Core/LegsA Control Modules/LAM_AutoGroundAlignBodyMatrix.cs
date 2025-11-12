#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    //[CreateAssetMenu(fileName = "LAM_Auto Ground Align Body", menuName = "FImpossible Creations/Legs Animator/Control Module - Auto Ground Align Body", order = 1)]
    public class LAM_AutoGroundAlignBodyMatrix : LegsAnimatorControlModuleBase
    {
        Vector3 averageNormal;
        Vector3 animatedAverageNormal;
        Quaternion lastOrientation;

        LegsAnimator.Variable _blendV;
        LegsAnimator.Variable _rotateV;
        LegsAnimator.Variable _alignSpdV;
        LegsAnimator.Variable _alignDownV;
        LegsAnimator.Variable _AxisBlendV;

        float _blend = 1f;

        public override bool AskForSpineBone { get { return true; } }

        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            animatedAverageNormal = LA.Up;
            lastOrientation = LA.BaseTransform.rotation;

            _blendV = helper.RequestVariable("Matrix Blend", 1f);
            _rotateV = helper.RequestVariable("Rotate Hips", 1f);
            _alignSpdV = helper.RequestVariable("Aligning Speed", .7f);
            _alignDownV = helper.RequestVariable("Spine Restore", .5f);
            _AxisBlendV = helper.RequestVariable("Rotation Axis Blend", Vector3.one);
        }


        public override void Leg_LatePreRaycastingUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper, LegsAnimator.Leg leg)
        {
            _blend = EffectBlend;

            if (!leg.RaycastHitted) return;
            // ! Let's still align body to predicted hits ! if (!leg.A_PreWasAligning) return;

            averageNormal += leg.LastGroundHit.normal;
        }

        public override void OnUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            Quaternion rot = lastOrientation;
            float blend = _blend * _blendV.GetFloat();

            if (blend < 1f) rot = Quaternion.SlerpUnclamped(Quaternion.identity, lastOrientation, blend);

            Matrix4x4 mx = Matrix4x4.TRS(LA.BaseTransform.position, rot * LA.BaseTransform.rotation , LA.BaseTransform.lossyScale);
            LA.User_OverwriteCastMatrix(mx);
        }

        public override void OnAfterAnimatorCaptureUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            float blend = _blend * _rotateV.GetFloat();
            if (blend < 0.001f) return;

            Vector3 eulers =  lastOrientation.eulerAngles;
            eulers.x = LAM_DirectionalMovement.FormatAngleToPM180(eulers.x);
            eulers.y = LAM_DirectionalMovement.FormatAngleToPM180(eulers.y);
            eulers.z = LAM_DirectionalMovement.FormatAngleToPM180(eulers.z);
            //LA._Hips_Modules_ExtraRotOffset += FEngineering.QToLocal(LA.BaseTransform.rotation, lastOrientation).eulerAngles;

            Quaternion rotOffset = Quaternion.identity;
            //rotOffset *= Quaternion.AngleAxis(eulers.x * blend, LA.BaseTransform.right);
            //rotOffset *= Quaternion.AngleAxis(eulers.y * blend, LA.BaseTransform.up);
            //rotOffset *= Quaternion.AngleAxis(eulers.z * blend, LA.BaseTransform.forward);
            rotOffset *= Quaternion.AngleAxis(eulers.x * blend, Vector3.right);
            rotOffset *= Quaternion.AngleAxis(eulers.y * blend, Vector3.up);
            rotOffset *= Quaternion.AngleAxis(eulers.z * blend, Vector3.forward);
            LA._LastHipsRotationOffsetOutsideInfo *= rotOffset;

            if (LA.SpineBone)
            {
                Quaternion preSpineRot = LA.SpineBone.rotation;
                //LA.HipsSetup.UniRotate.RotateBy(eulers, blend);

                LA.HipsSetup.bone.rotation = rotOffset * LA.HipsSetup.bone.rotation;

                //LA.SpineBone.rotation = Quaternion.Slerp(rotOffset * LA.SpineBone.rotation, LA.SpineBone.rotation, _alignDownV.GetFloat());
                LA.SpineBone.rotation = Quaternion.Slerp(LA.SpineBone.rotation, preSpineRot, _alignDownV.GetFloat());
            }
            else
            {
                LA.HipsSetup.bone.rotation = rotOffset * LA.HipsSetup.bone.rotation;
            }
        }

        public override void OnLateUpdatePreApply(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            if (averageNormal == Vector3.zero) averageNormal = LA.Up;
            else
                averageNormal.Normalize();

            if (_alignSpdV.GetFloat() < 0.999f)
            {
                float speedMul = Mathf.LerpUnclamped(5f, 20f, _alignSpdV.GetFloat());
                animatedAverageNormal = Vector3.Slerp(animatedAverageNormal, averageNormal, LA.DeltaTime * speedMul);
            }
            else animatedAverageNormal = averageNormal;

            lastOrientation = Quaternion.FromToRotation(Vector3.up, animatedAverageNormal);

            Vector3 axisMul = _AxisBlendV.GetVector3();
            if (axisMul != Vector3.one)
            {
                Vector3 eulers = lastOrientation.eulerAngles;
                axisMul = helper.Parent.BaseTransform.TransformDirection(axisMul);
                lastOrientation = Quaternion.Euler(eulers.x * axisMul.x, eulers.y * axisMul.y, eulers.z * axisMul.z);
            }

            averageNormal = Vector3.zero;
        }


        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            EditorGUILayout.HelpBox("Adjusting body raycasting matrix on the raycasted slope + offers hips rotation. Mostly useful for spider creatures but it can also help humanoids slopes movement.", UnityEditor.MessageType.Info);

            var mxVar = helper.RequestVariable("Matrix Blend", 1f);
            mxVar.SetMinMaxSlider(0f, 1f);
            mxVar.Editor_DisplayVariableGUI();

            var alignSpeedVar = helper.RequestVariable("Aligning Speed", .7f);
            alignSpeedVar.SetMinMaxSlider(0f, 1f);
            alignSpeedVar.Editor_DisplayVariableGUI();

            GUILayout.Space(4);
            EditorGUILayout.LabelField("Optional hips rotation", EditorStyles.centeredGreyMiniLabel);
            var rotateVar = helper.RequestVariable("Rotate Hips", 0f);
            rotateVar.SetMinMaxSlider(0f, 1f);
            rotateVar.Editor_DisplayVariableGUI();

            if (legsAnimator.SpineBone != null)
            {
                var downVar = helper.RequestVariable("Spine Restore", .5f);
                downVar.SetMinMaxSlider(0f, 1f);
                downVar.Editor_DisplayVariableGUI();
            }

            GUILayout.Space(5);

            var axisV = helper.RequestVariable("Rotation Axis Blend", Vector3.one);
            axisV.AssignTooltip("Giving possibility for locking alignment rotation to target axes.\nX = Forward/Back lean  Z = Sides Lean,\nset axis value 0 to disable adjustement leaning for the axis.");
            axisV.Editor_DisplayVariableGUI();
        }

        public override void Editor_OnSceneGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            for (int l = 0; l < legsAnimator.Legs.Count; l++)
            {
                legsAnimator.Legs[l]._Editor_Raycasting_DrawControls();
            }
        }


#endif
        #endregion



        #region Inspector Editor Class Ineritance
#if UNITY_EDITOR

        [UnityEditor.CanEditMultipleObjects]
        [UnityEditor.CustomEditor(typeof(LAM_AutoGroundAlignBodyMatrix))]
        public class LAM_AutoGroundAlignBodyMatrixEditor : LegsAnimatorControlModuleBaseEditor
        {
        }

#endif
        #endregion


    }
}