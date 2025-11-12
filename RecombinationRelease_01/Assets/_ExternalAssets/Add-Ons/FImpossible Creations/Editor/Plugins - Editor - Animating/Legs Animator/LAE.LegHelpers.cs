using UnityEditor;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimatorEditor
    {
        void Leg_Select(int index)
        {
            _selected_leg = index;
            RedrawScene();
        }

        void Leg_IKSetup_Select(int index)
        {
            _setupik_selected_leg = index;
            RedrawScene();
        }

        public void Leg_AssignStartBone(LegsAnimator.Leg leg, Transform t)
        {
            if ( leg.BoneStart != t)
            {
                leg.BoneStart = t;
                if (t != null) leg.DefineLegSide(Get);
            }
        }

        SerializedProperty GetLegProperty(int index)
        {
            if (Get.Legs.ContainsIndex(index) == false) return null;
            return sp_Legs.GetArrayElementAtIndex(index);
        }

    }

}