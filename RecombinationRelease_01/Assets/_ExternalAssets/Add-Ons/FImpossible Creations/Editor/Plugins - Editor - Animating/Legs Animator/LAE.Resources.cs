using FIMSpace.FEditor;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimatorEditor 
    {
        // -------------------------------------------------

        public static Texture2D Tex_smLegStart { get { if (__texsmlegstrt != null) return __texsmlegstrt; __texsmlegstrt = Resources.Load<Texture2D>("Legs Animator/SPR_smStartLeg"); return __texsmlegstrt; } }
        private static Texture2D __texsmlegstrt = null;

        public static Texture2D Tex_smLegMid { get { if (__texsmlegmid != null) return __texsmlegmid; __texsmlegmid = Resources.Load<Texture2D>("Legs Animator/SPR_smMidLeg"); return __texsmlegmid; } }
        private static Texture2D __texsmlegmid = null;

        public static Texture2D Tex_smLegEnd { get { if (__texsmLegEnd != null) return __texsmLegEnd; __texsmLegEnd = Resources.Load<Texture2D>("Legs Animator/SPR_smEndLeg"); return __texsmLegEnd; } }
        private static Texture2D __texsmLegEnd = null;

        // -------------------------------------------------

        public static Texture2D Tex_LegStart { get { if (__texlegstart != null) return __texlegstart; __texlegstart = Resources.Load<Texture2D>("Legs Animator/SPR_StartLeg"); return __texlegstart; } }
        private static Texture2D __texlegstart = null;

        public static Texture2D Tex_LegMid { get { if (__texlegmid != null) return __texlegmid; __texlegmid = Resources.Load<Texture2D>("Legs Animator/SPR_MidLeg"); return __texlegmid; } }
        private static Texture2D __texlegmid = null;

        public static Texture2D Tex_LegEnd { get { if (__texlegend != null) return __texlegend; __texlegend = Resources.Load<Texture2D>("Legs Animator/SPR_EndLeg"); return __texlegend; } }
        private static Texture2D __texlegend = null;

        // -------------------------------------------------

        public static Texture2D Tex_OppositeSide { get { if (__texOpposite != null) return __texOpposite; __texOpposite = Resources.Load<Texture2D>("Legs Animator/SPR_Opposite"); return __texOpposite; } }
        private static Texture2D __texOpposite = null;

        public static Texture2D Tex_LeftSide { get { if (__texleftside != null) return __texleftside; __texleftside = Resources.Load<Texture2D>("Legs Animator/SPR_LLeg"); return __texleftside; } }
        private static Texture2D __texleftside = null;

        public static Texture2D Tex_RightSide { get { if (__texrightside != null) return __texrightside; __texrightside = Resources.Load<Texture2D>("Legs Animator/SPR_RLeg"); return __texrightside; } }
        private static Texture2D __texrightside = null;

        public static Texture2D Tex_LeftSideOff { get { if (__texleftsideoff != null) return __texleftsideoff; __texleftsideoff = Resources.Load<Texture2D>("Legs Animator/SPR_LLegOff"); return __texleftsideoff; } }
        private static Texture2D __texleftsideoff = null;

        public static Texture2D Tex_RightSideOff { get { if (__texrightsideoff != null) return __texrightsideoff; __texrightsideoff = Resources.Load<Texture2D>("Legs Animator/SPR_RLegOff"); return __texrightsideoff; } }
        private static Texture2D __texrightsideoff = null;

        // -------------------------------------------------

        public static Texture2D Tex_FootRotate { get { if (__texfootrot != null) return __texfootrot; __texfootrot = Resources.Load<Texture2D>("Legs Animator/SPR_FootRotate"); return __texfootrot; } }
        private static Texture2D __texfootrot = null;

        public static Texture2D Tex_FootStep { get { if (__texfootStep != null) return __texfootStep; __texfootStep = Resources.Load<Texture2D>("Legs Animator/FootStep"); return __texfootStep; } }
        private static Texture2D __texfootStep = null;
        public static Texture2D Tex_LegStep { get { if (__texLegStp != null) return __texLegStp; __texLegStp = Resources.Load<Texture2D>("Legs Animator/Stepping"); return __texLegStp; } }
        private static Texture2D __texLegStp = null;        
        public static Texture2D Tex_LegMotion { get { if (__texLegMot != null) return __texLegMot; __texLegMot = Resources.Load<Texture2D>("Legs Animator/SPR_LegMot2"); return __texLegMot; } }
        private static Texture2D __texLegMot = null;
        public static Texture2D Tex_Hips { get { if (__texHps != null) return __texHps; __texHps = Resources.Load<Texture2D>("Legs Animator/StepDown"); return __texHps; } }
        private static Texture2D __texHps = null;
        public static Texture2D Tex_Stabilize { get { if (__texStabil != null) return __texStabil; __texStabil = Resources.Load<Texture2D>("Legs Animator/Stabilize"); return __texStabil; } }
        private static Texture2D __texStabil = null;
        public static Texture2D Tex_Glue { get { if (__texGlue != null) return __texGlue; __texGlue = Resources.Load<Texture2D>("Legs Animator/SPR_LegGl"); return __texGlue; } }
        private static Texture2D __texGlue = null;
        public static Texture2D Tex_FootGlue { get { if (__texfootglue != null) return __texfootglue; __texfootglue = Resources.Load<Texture2D>("Legs Animator/FootGlue"); return __texfootglue; } }
        private static Texture2D __texfootglue = null;

        public static Texture2D Tex_AutoMotion { get { if (__texAutoMot != null) return __texAutoMot; __texAutoMot = Resources.Load<Texture2D>("Legs Animator/AutoMotion"); return __texAutoMot; } }
        private static Texture2D __texAutoMot = null;
        public static Texture2D Tex_EventIcon { get { if (__texEvt != null) return __texEvt; __texEvt = (Texture2D)EditorGUIUtility.IconContent("EventSystem Icon").image; return __texEvt; } }
        private static Texture2D __texEvt = null;
        public static Texture2D Tex_IK { get { if (__texIK != null) return __texIK; __texIK = Resources.Load<Texture2D>("Legs Animator/SPR_IK"); return __texIK; } }
        private static Texture2D __texIK = null;

        public static Texture2D Tex_HipsMotion { get { if (__texHipsMot != null) return __texHipsMot; __texHipsMot = Resources.Load<Texture2D>("Legs Animator/DynamicHips"); return __texHipsMot; } }
        private static Texture2D __texHipsMot = null;

    }

}