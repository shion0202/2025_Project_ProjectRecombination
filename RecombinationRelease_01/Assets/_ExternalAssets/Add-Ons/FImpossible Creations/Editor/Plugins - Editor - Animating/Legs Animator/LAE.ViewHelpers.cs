using FIMSpace.FEditor;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimatorEditor
    {
        protected Color preC;
        protected Color preBG;
        protected float width;

        public static GUIStyle BGInBoxStyle { get { if (__inBoxStyle != null) return __inBoxStyle; __inBoxStyle = new GUIStyle(); __inBoxStyle.border = new RectOffset(4, 4, 4, 4); __inBoxStyle.padding = new RectOffset(10, 10, 6, 6); __inBoxStyle.margin = new RectOffset(0, 0, 0, 0); return __inBoxStyle; } }
        private static GUIStyle __inBoxStyle = null;
        public static Texture2D Tex_Pixel { get { if (__texpixl != null) return __texpixl; __texpixl = new Texture2D(1, 1); __texpixl.SetPixel(0, 0, Color.white); __texpixl.Apply(false, true); return __texpixl; } }
        private static Texture2D __texpixl = null;
        public static GUIStyle StyleColorBG { get { if (__StlcolBG != null) { if (__StlcolBG.normal.background != null) return __StlcolBG; } __StlcolBG = new GUIStyle(EditorStyles.label); Texture2D bg = Tex_Pixel; __StlcolBG.focused.background = bg; __StlcolBG.active.background = bg; __StlcolBG.normal.background = bg; __StlcolBG.border = new RectOffset(0, 0, 0, 0); return __StlcolBG; } }
        private static GUIStyle __StlcolBG = null;
        protected GUIContent _guic_autoFind { get { if (__guic_autoFind == null || __guic_autoFind.image == null) __guic_autoFind = new GUIContent("  Try Auto-Find All Needed Bones", FGUI_Resources.Tex_Bone); return __guic_autoFind; } }
        GUIContent __guic_autoFind = null;


        void GUI_Prepare()
        {
            if (Get.Legs == null) Get.Legs = new System.Collections.Generic.List<LegsAnimator.Leg>();

            preC = GUI.color;
            preBG = GUI.backgroundColor;
            width = EditorGUIUtility.currentViewWidth;
        }

        /// <summary> Begin horizontal </summary>
        void BH(GUIStyle style = null)
        {
            if (style == null)
                EditorGUILayout.BeginHorizontal();
            else
                EditorGUILayout.BeginHorizontal(style);
        }

        /// <summary> End horizontal </summary>
        void EH() { EditorGUILayout.EndHorizontal(); }

        /// <summary> Begin horizontal </summary>
        void BV(GUIStyle style = null)
        {
            if (style == null)
                EditorGUILayout.BeginHorizontal();
            else
                EditorGUILayout.BeginHorizontal(style);
        }

        /// <summary> End horizontal </summary>
        void EV() { EditorGUILayout.EndVertical(); }


        bool Button_Refresh(string tooltip = "")
        {
            if (GUILayout.Button(new GUIContent(FGUI_Resources.Tex_Refresh, tooltip), FGUI_Resources.ButtonStyle, GUILayout.Height(18), GUILayout.Width(23)))
            {
                _requestRepaint = true;
                return true;
            }

            return false;
        }

        void Helper_Header(string title, Texture tex)
        {
            EditorGUILayout.BeginHorizontal(FGUI_Resources.ViewBoxStyle);
            if (tex != null) EditorGUILayout.LabelField(new GUIContent(tex), GUILayout.Height(18), GUILayout.Width(20));
            GUILayout.Space(3);
            EditorGUILayout.LabelField(title, FGUI_Resources.HeaderStyle);
            GUILayout.Space(3);
            if (tex != null) EditorGUILayout.LabelField(new GUIContent(tex), GUILayout.Height(18), GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();
        }

        protected Color selCol = new Color(0.2f, 1f, 0.3f, 1f);
        protected readonly Color selCol1 = new Color(0.2f, 1f, 0.3f, 1f);
        protected readonly Color selCol2 = new Color(0.5f, 0.6f, 1.1f, 1f);
        protected readonly Color selCol3 = new Color(0.3f, .75f, 1f, 1f);


        #region Cateogry Buttons Methods


        protected void DrawCategoryButton(LegsAnimator.EEditorCategory target, Texture icon, string lang)
        {
            if (Get._EditorCategory == target) GUI.backgroundColor = selCol;

            int height = 28;
            int lim = 367;
            //if (choosedLang == ELangs.русский) lim = 500;

            if (EditorGUIUtility.currentViewWidth > lim)
            {
                if (GUILayout.Button(new GUIContent("  " + Lang(lang), icon), FGUI_Resources.ButtonStyle, GUILayout.Height(height)))
                {
                    Get._EditorCategory = target; if (GUI.backgroundColor == selCol && Event.current.button == 1) Get._EditorCategory -= 10;
                    _requestRepaint = true;
                }
            }
            else
                if (GUILayout.Button(new GUIContent(icon, Lang(lang)), FGUI_Resources.ButtonStyle, GUILayout.Height(height))) { Get._EditorCategory = target; if (GUI.backgroundColor == selCol && Event.current.button == 1) Get._EditorCategory -= 10; _requestRepaint = true; }

            GUI.backgroundColor = preBG;
        }


        protected void DrawCategoryButton(LegsAnimator.EEditorSetupCategory target, Texture icon, string lang, float overrideWidth = 0f)
        {
            if (Get._EditorSetupCategory == target) GUI.backgroundColor = selCol;
            int height = 20; int lim = 357;
            bool narrow = EditorGUIUtility.currentViewWidth < lim;
            if (overrideWidth != 0f && overrideWidth < 60f) narrow = true;

            if (!narrow)
            {
                if (_DrawCatButton(new GUIContent("  " + Lang(lang), icon), height, overrideWidth))
                {
                    Get._EditorSetupCategory = target; if (GUI.backgroundColor == selCol && Event.current.button == 1) Get._EditorCategory -= 10;
                    _requestRepaint = true;
                }
            }
            else
                if (_DrawCatButton(new GUIContent(icon, Lang(lang)), height, overrideWidth))
            {
                Get._EditorSetupCategory = target;
                if (GUI.backgroundColor == selCol && Event.current.button == 1) Get._EditorCategory -= 10;
                _requestRepaint = true;
            }

            GUI.backgroundColor = preBG;
        }

        bool _DrawCatButton(GUIContent cont, float height, float overrideWidth)
        {
            if (overrideWidth != 0f)
            {
                if (GUILayout.Button(cont, FGUI_Resources.ButtonStyle, GUILayout.Height(height), GUILayout.Width(overrideWidth)))
                {
                    return true;
                }
            }
            else
            {
                if (GUILayout.Button(cont, FGUI_Resources.ButtonStyle, GUILayout.Height(height)))
                {
                    return true;
                }
            }

            return false;
        }


        protected void DrawCategoryButton(LegsAnimator.EEditorMotionCategory target, Texture icon, string lang)
        {
            if (Get._EditorMotionCategory == target) GUI.backgroundColor = selCol;
            int height = 20; int lim = 247;

            if (EditorGUIUtility.currentViewWidth > lim)
            {
                if (GUILayout.Button(new GUIContent("  " + Lang(lang), icon), FGUI_Resources.ButtonStyle, GUILayout.Height(height)))
                {
                    Get._EditorMotionCategory = target; if (GUI.backgroundColor == selCol && Event.current.button == 1) Get._EditorCategory -= 10;
                    _requestRepaint = true;
                }
            }
            else
                if (GUILayout.Button(new GUIContent(icon, Lang(lang)), FGUI_Resources.ButtonStyle, GUILayout.Height(height))) { Get._EditorMotionCategory = target; if (GUI.backgroundColor == selCol && Event.current.button == 1) Get._EditorCategory -= 10; _requestRepaint = true; }

            GUI.backgroundColor = preBG;
        }


        protected void DrawCategoryButton(LegsAnimator.EEditorExtraCategory target, Texture icon, string lang)
        {
            if (Get._EditorExtraCategory == target) GUI.backgroundColor = selCol;
            int height = 20; int lim = 288;

            if (EditorGUIUtility.currentViewWidth > lim)
            {
                if (GUILayout.Button(new GUIContent("  " + Lang(lang), icon), FGUI_Resources.ButtonStyle, GUILayout.Height(height)))
                {
                    Get._EditorExtraCategory = target; if (GUI.backgroundColor == selCol && Event.current.button == 1) Get._EditorCategory -= 10;
                    _requestRepaint = true;
                }
            }
            else
                if (GUILayout.Button(new GUIContent(icon, Lang(lang)), FGUI_Resources.ButtonStyle, GUILayout.Height(height))) { Get._EditorExtraCategory = target; if (GUI.backgroundColor == selCol && Event.current.button == 1) Get._EditorCategory -= 10; _requestRepaint = true; }

            GUI.backgroundColor = preBG;
        }


        #endregion


        protected string Lang(string s)
        {
            return s;
        }

        protected void RedrawScene()
        {
            SceneView.RepaintAll();
        }

    }

}