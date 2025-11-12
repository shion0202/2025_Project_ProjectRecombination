using FIMSpace.FEditor;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimatorEditor
    {
        public AnimationClip HumanoidTPose;
        float referencePoseTime = 0f;
        AnimationClip referencePose = null;
         static readonly GUILayoutOption[] _button_w20h18 = new GUILayoutOption[] { GUILayout.Width(20), GUILayout.Height(18) };

        public void DisplaySetupPoseGUI()
        {
            if (Get.LegsInitialized) GUI.enabled = false;

            FGUI_Inspector.DrawUILineCommon(12);

            EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxStyle);

            EditorGUILayout.LabelField("Setup Pose (Optional)", FGUI_Resources.HeaderStyle);
            GUILayout.Space(3);
            EditorGUILayout.HelpBox("Store the setup pose to ensure that the initialization process performs correctly in various circumstances.", MessageType.None);
            GUILayout.Space(3);

            string title = " Store Setup Pose";
            if (Get.SetupPose.IsSet(Get))
            {
                title = " Refresh Setup Pose";
            }
            else
            {
                GUI.backgroundColor = new Color(1f, 1f, 0.825f, 1f);
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(new GUIContent(title, FGUI_Resources.Tex_Save), GUILayout.Height(19)))
            {
                Get.StoreSetupPose();
                OnChange();
            }

            if (Get.SetupPose.IsSet(Get))
            {
                if (GUILayout.Button(new GUIContent(" Check Setup Pose", FGUI_Resources.Tex_Debug), GUILayout.Height(19))) { Get.RestoreSetupPose(); }

                GUILayout.Space(2);
                FGUI_Inspector.RedGUIBackground();
                if (GUILayout.Button(new GUIContent(FGUI_Resources.Tex_Remove, "Clear the stored reference pose if you don't want to use it."), FGUI_Resources.ButtonStyle, _button_w20h18)) { Get.SetupPose.Clear(); OnChange(); }
                FGUI_Inspector.RestoreGUIBackground();
            }

            EditorGUILayout.EndHorizontal();


            GUILayout.Space(3);

            if (Get.Mecanim && Get.Mecanim.isHuman && HumanoidTPose)
            {
                if (GUILayout.Button(new GUIContent(" Force Character To T-Pose", EditorGUIUtility.IconContent("AnimationClip Icon").image), GUILayout.Height(19)))
                {
                    referencePose = HumanoidTPose;
                    HumanoidTPose.SampleAnimation(Get.Mecanim.gameObject, 0f);
                    Get.StoreSetupPose();
                    OnChange();
                }
            }

            if (Get.Mecanim)
            {
                GUILayout.Space(3);
                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 200;
                referencePose = EditorGUILayout.ObjectField("Force Character Into Clip Pose:", referencePose, typeof(AnimationClip), false) as AnimationClip;
                EditorGUIUtility.labelWidth = 0;
                if (referencePose != null)
                {
                    float preTime = referencePoseTime;
                    referencePoseTime = GUILayout.HorizontalSlider(referencePoseTime, 0f, 1f);
                    if (preTime != referencePoseTime) { referencePose.SampleAnimation(Get.Mecanim.gameObject, referencePoseTime * referencePose.length); }
                }
                EditorGUILayout.EndHorizontal();

                if (referencePose != null)
                    EditorGUILayout.HelpBox("Use current scene pose to save it as setup pose.", MessageType.None);
            }

            GUI.backgroundColor = Color.white;
            GUILayout.Space(3);
            EditorGUILayout.EndVertical();

            GUI.enabled = true;
        }

    }
}