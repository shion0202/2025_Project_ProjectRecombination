#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    [CreateAssetMenu(fileName = "LAM_RedirectRaycasting", menuName = "FImpossible Creations/Legs Animator/Control Module - Redirect Raycasting", order = 1)]
    public class LAM_RedirectRaycasting : LegsAnimatorControlModuleBase
    {
        LegsAnimator.Variable _LocalDirection;

        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            base.OnInit(helper);
            _LocalDirection = helper.RequestVariable("Rotate Raycast", new Vector3(75f, 0f, 0f));
        }

        public override void OnUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            Quaternion rot = Quaternion.Euler(_LocalDirection.GetVector3());
            Matrix4x4 mx = Matrix4x4.TRS(LA.BaseTransform.position, rot * LA.BaseTransform.rotation, LA.BaseTransform.lossyScale);
            LA.User_OverwriteCastMatrix(mx);
        }

        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            EditorGUILayout.HelpBox("Changing direction of raycasting, can be useful for humanoid climbing mechanics.", UnityEditor.MessageType.Info);
            GUILayout.Space(5);
            _LocalDirection = helper.RequestVariable("Rotate Raycast", new Vector3(75f, 0f, 0f));
            _LocalDirection.Editor_DisplayVariableGUI();
        }

        public override void Editor_OnSceneGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            base.Editor_OnSceneGUI(legsAnimator, helper);

            for (int l = 0; l < legsAnimator.Legs.Count; l++)
            {
                legsAnimator.Legs[l]._Editor_Glue_DrawControls();
                legsAnimator.Legs[l]._Editor_Raycasting_DrawControls();
            }
        }

#endif
        #endregion

    }
}