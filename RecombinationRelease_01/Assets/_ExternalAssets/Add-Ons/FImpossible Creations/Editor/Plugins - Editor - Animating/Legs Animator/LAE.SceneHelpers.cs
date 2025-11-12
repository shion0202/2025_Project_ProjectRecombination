using FIMSpace.FEditor;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimatorEditor
    {
        public bool IsSceneViewVisible { get { return SceneView.lastActiveSceneView != null; } }
        public SceneView ScView { get { return SceneView.lastActiveSceneView; } }

        public void SceneHelper_FocusOnInSceneView(Transform t, float scale = 1f)
        {
            if (t == null) return;
            SceneView.lastActiveSceneView.Frame(new Bounds(t.position, Vector3.one * scale * 0.6f), false);
        }

        public Transform SceneHelper_FocusOnBone = null;

        void SceneHelper_DrawBoneFocus()
        {
            if (SceneHelper_FocusOnBone == null) return;

            Vector3 bonePos = SceneHelper_FocusOnBone.position;
            Vector3 lookDir = ScView.camera.transform.position - bonePos;
            Quaternion cameraLook = Quaternion.identity;
            if (lookDir != Vector3.zero) cameraLook = Quaternion.LookRotation(lookDir);

            float sideOff = Get.Util_SideMul(SceneHelper_FocusOnBone);

            Handles.color = new Color(0.4f, 1f, 0.65f, 0.5f);

            float avLength = 0f;
            for (int c = 0; c < SceneHelper_FocusOnBone.childCount; c++)
            {
                Vector3 childPos = SceneHelper_FocusOnBone.GetChild(c).position;
                FGUI_Handles.DrawBoneHandle(bonePos, childPos, 1f);
                avLength += Vector3.Distance(childPos, bonePos);
            }

            if (SceneHelper_FocusOnBone.childCount > 0) avLength /= (float)SceneHelper_FocusOnBone.childCount;
            else avLength = 0.65f;

            //Handles.color = new Color(0.4f, 1f, 0.65f, 0.2f);
            Handles.DrawWireDisc(bonePos, cameraLook * Vector3.forward, avLength * 0.3f);

            // Draw children
            Handles.color = new Color(0.4f, 1f, 0.65f, 0.125f);
            for (int c = 0; c < SceneHelper_FocusOnBone.childCount; c++)
            {
                Transform child = SceneHelper_FocusOnBone.GetChild(c);

                for (int i = 0; i < child.childCount; i++)
                {
                    Vector3 childPos = child.GetChild(i).position;
                    FGUI_Handles.DrawBoneHandle(child.position, childPos, 0.2f);
                }
            }

            Handles.color = new Color(0.7f, 0.7f, 0.7f, 0.25f);
            float off = Mathf.Lerp(editorScaleRef, avLength, 0.4f);
            lookDir.y = 0f; lookDir.Normalize();
            cameraLook = ScView.camera.transform.rotation;
            Vector3 helperPos = bonePos + cameraLook * Vector3.right * sideOff * off * 0.8f + Vector3.up * off * 0.2f;
            Handles.DrawLine(bonePos, helperPos);

            Handles.color = Color.white;
            Handles.Label(helperPos + Vector3.up * off * 0.08f, SceneHelper_FocusOnBone.name, FGUI_Resources.HeaderStyle);

        }

        void SceneHelper_DrawHipsHubs()
        {
            if (Get.Hips == null) return;
            if (Get.ExtraHipsHubs == null) return;
            if (Get.ExtraHipsHubs.Count == 0) return;

            for (int i = 0; i < Get.ExtraHipsHubs.Count; i++)
            {
                if (Get.ExtraHipsHubs[i] == null) continue;

                Handles.SphereHandleCap(0, Get.ExtraHipsHubs[i].position, Quaternion.identity, editorScaleRef * 0.1f, EventType.Repaint);

                if (!Get.LegsInitialized)
                {
                    Handles.DrawAAPolyLine(2, Get.Hips.position, Get.ExtraHipsHubs[i].position);
                }
                else
                {
                    if (Get.HipsHubs[i].HubBackBones?.Count > 0)
                    {
                        Handles.DrawAAPolyLine(2, Get.ExtraHipsHubs[i].position, Get.HipsHubs[i].HubBackBones[0].bone.position, Get.HipsHubs[i].HubBackBones[0].bone.position + Get.Up * editorScaleRef * 0.1f);

                        for (int b = 1; b < Get.HipsHubs[i].HubBackBones.Count; b++)
                        {
                            Handles.DrawAAPolyLine(2, Get.HipsHubs[i].HubBackBones[b - 1].bone.position, Get.HipsHubs[i].HubBackBones[b].bone.position, Get.HipsHubs[i].HubBackBones[b].bone.position + Get.Up * editorScaleRef * 0.1f);
                        }
                    }
                }

            }
        }


        void SceneHelper_DrawLegStartBoneSelector(LegsAnimator.Leg leg, float drawScale, Transform legsHub)
        {
            if (legsHub == null) return;

            for (int c = 0; c < legsHub.childCount; c++)
            {
                Transform cc = legsHub.GetChild(c);
                bool should = Util_Leg_ShouldDraw(cc);

                if (should)
                    Handles.color = new Color(0.4f, 1f, 0.65f, 0.225f);
                else
                    Handles.color = new Color(0.4f, 0.4f, 0.4f, 0.225f);

                Vector3 childPos = cc.position;
                FGUI_Handles.DrawBoneHandle(legsHub.position, childPos, 1f);

                if (!should) continue;


                if (cc.childCount > 0)
                {
                    float hSize = HandleUtility.GetHandleSize(childPos);
                    if (hSize > 0.01f)
                    {
                        float scaler = 1f / hSize;
                        float mouseOnDistance = Vector2.Distance(Event.current.mousePosition, HandleUtility.WorldToGUIPoint(childPos));
                        if (mouseOnDistance < 2.85f * scaler) FGUI_Handles.DrawBoneHandle(childPos, cc.GetChild(0).position, .7f);
                        //if ( c == 0) UnityEngine.Debug.Log("scaler = " + hSize); //UnityEngine.Debug.Log("drawScale = " + editorScaleRef + "   :  mouse pos = " + Event.current.mousePosition + " : wpoint =" + HandleUtility.WorldToGUIPoint(childPos) + "mose distance = " + mouseOnDistance);
                    }
                }

                if (Handles.Button(childPos, Quaternion.identity, drawScale, drawScale, Handles.CircleHandleCap))
                {
                    leg.BoneStart = cc;
                }

                Util_DrawBoneIndicator(cc, drawScale);
            }
        }


        void SceneHelper_DrawLegSelectorHelper()
        {
            if (Get.LegsInitialized) return;
            if (_selected_leg < 0) return;
            if (Get.Hips == null) return;

            //SceneHelper_FocusOnBone = null;
            Get.User_RefreshHelperVariablesOnParametersChange();

            if (Get.Legs.ContainsIndex(_selected_leg) == false)
            {
                _selected_leg = -1;
                return;
            }

            Quaternion viewRot = ScView.camera.transform.rotation;
            var leg = Get.Legs[_selected_leg];

            float drawScaleRaw = editorScaleRef * 0.06f;
            float drawScale = drawScaleRaw;

            if (leg.BoneStart == null)
            {

                #region Start bone selector

                Handles.BeginGUI();
                GUI.Label(new Rect(0, 10, Screen.width, 40), "Select Start Leg Bone", FGUI_Resources.HeaderStyleBig);
                Handles.EndGUI();

                SceneHelper_DrawLegStartBoneSelector(leg, drawScaleRaw, Get.Hips);

                for (int i = 0; i < Get.ExtraHipsHubs.Count; i++)
                {
                    SceneHelper_DrawLegStartBoneSelector(leg, drawScaleRaw, Get.ExtraHipsHubs[i]);
                }

                #endregion

            }
            else
            {
                Handles.BeginGUI();

                float sideWidth = Mathf.Max(180, Screen.width * 0.33f);

                GUI.Label(new Rect(0, 10, Screen.width, 40), leg.BoneStart.name.ToUpper() + " - Side: " + leg.Side.ToString(), FGUI_Resources.HeaderStyle);

                if (leg.BoneStart && leg.BoneMid && leg.BoneEnd)
                {
                    GUI.color = Color.black;
                    GUI.Box(new Rect(5, 8, sideWidth + 35, 124), GUIContent.none, FGUI_Resources.HeaderBoxStyleH);
                    GUI.Box(new Rect(5, 8, sideWidth + 35, 124), GUIContent.none, FGUI_Resources.HeaderBoxStyleH);
                    GUI.color = Color.white;

                    //GUI.color = new Color(1f, 1f, 1f, 0.8f);
                    GUI.color = Color.white * 1.4f;

                    if (GUI.Button(new Rect(20, 22, 22, 19), new GUIContent(FGUI_Resources.TexTargetingIcon, "Ping start leg in the hierarchy to easily find other legs 'Top Bones'"), FGUI_Resources.ButtonStyle)) { PingObject(leg.BoneStart); }
                    GUI.Label(new Rect(20, 12, sideWidth, 40), "Use " + leg.BoneStart.name + " setup for:", EditorStyles.centeredGreyMiniLabel);

                    if (GUI.Button(new Rect(20, 46, sideWidth, 32), "Try Automatically Setup\nRest Of The Legs in Hips", FGUI_Resources.ButtonStyle))
                    {
                        Get.Setup_TryAutoLegsSetup(leg, Get.Hips);
                        _selected_leg = -1;
                    }

                    GUI.Label(new Rect(20, 72, sideWidth, 40), "Apply Auto-Setup for Selective Leg Bone:", EditorStyles.centeredGreyMiniLabel);
                    EditorGUIUtility.labelWidth = 96;
                    Transform toSetup = (Transform)EditorGUI.ObjectField(new Rect(20, 104, sideWidth, 20), new GUIContent("Leg Top Bone", "Drag & Drop there leg start/thigh bone from hierarchy window to try automatically set it up with the same patter as the first bone was set up."), null, typeof(Transform), true);
                    if (toSetup != null) { Get.Setup_TryAutoLegSetup(leg, toSetup); _selected_leg = Get.Legs.Count - 1; }

                    EditorGUIUtility.labelWidth = 0;

                    GUI.color = Color.white;
                }


                Handles.EndGUI();


                Vector3 buttonOff = ScView.camera.transform.rotation * Vector3.right * Get.Util_SideMul(leg.BoneStart) * drawScale * 0.8f;
                //Vector3.up * drawScale
                if (Util_DrawButton(new GUIContent(Tex_smLegStart), buttonOff + leg.BoneStart.position + 2f * LabelIndicatorHelperOffset(leg.BoneStart, drawScale, false, true), 1f, false))
                { PingObject(leg.BoneStart); }
                Util_DrawBoneIndicator(leg.BoneStart, drawScale * 1.65f, true, "", false, true);

                Handles.color = new Color(0.5f, 1f, 0.55f, 0.75f);
                if (leg.BoneMid != null) FGUI_Handles.DrawBoneHandle(leg.BoneStart.position, leg.BoneMid.position, 1f);
                else Util_DrawChildBoneOf(leg.BoneStart, 0.6f);

                Transform legBone = leg.BoneStart;


                // Drawing buttons for target bones to be selected
                if (leg.BoneMid == null || leg.BoneEnd == null)
                {
                    int i = 1;
                    while (legBone.childCount > 0)
                    {
                        legBone = Get.Finders_GetRelevantChildOf(legBone);

                        bool already = false;
                        for (int l = 0; l < Get.Legs.Count; l++)
                        {
                            var lg = Get.Legs[l];
                            if (lg.BoneStart == legBone) { already = true; break; }
                            if (lg.BoneMid == legBone) { already = true; break; }
                            if (lg.BoneEnd == legBone) { already = true; break; }
                        }

                        if (already) continue;

                        Util_DrawLegSegmentSelectorButton(leg, legBone, drawScale, i % 2 == 0, true);
                        i += 1;
                    }
                }


                // Drawing icons of already choosen leg bones
                if (leg.BoneMid != null)
                {
                    Util_DrawBoneIndicator(leg.BoneMid, drawScale * 1.65f, true, "", true, true);

                    Handles.color = new Color(0.35f, .925f, 0.65f, 0.65f);
                    FGUI_Handles.DrawBoneHandle(leg.BoneMid.position, leg.BoneMid.position, 1f);
                    if (Util_DrawButton(new GUIContent(Tex_smLegMid), buttonOff + leg.BoneMid.position + 1.7f * LabelIndicatorHelperOffset(leg.BoneMid, drawScale, true, true), 1f, false))
                    { PingObject(leg.BoneMid); }

                    if (leg.BoneEnd != null) FGUI_Handles.DrawBoneHandle(leg.BoneMid.position, leg.BoneEnd.position, 1f);
                    else Util_DrawChildBoneOf(leg.BoneMid, 0.6f);
                }

                if (leg.BoneEnd != null)
                {
                    Util_DrawBoneIndicator(leg.BoneEnd, drawScale * 1.65f, true, "", false, true);

                    Handles.color = new Color(0.1f, .8f, 0.9f, 0.65f);
                    if (leg.BoneMid) FGUI_Handles.DrawBoneHandle(leg.BoneMid.position, leg.BoneEnd.position, 1f);
                    if (Util_DrawButton(new GUIContent(Tex_smLegEnd), buttonOff + leg.BoneEnd.position + 2f * LabelIndicatorHelperOffset(leg.BoneEnd, drawScale, false, true), 1f, false))
                    { PingObject(leg.BoneEnd); }

                    Util_DrawChildBoneOf(leg.BoneEnd, 0.6f);
                }

                //Transform lastP = AnimationTools.SkeletonRecognize.GetBottomMostChildTransform(leg.BoneStart);

            }



            if (leg.OppositeLegIndex != -1)
                if (Get.Legs.ContainsIndex(leg.OppositeLegIndex))
                {
                    var oppositeLeg = Get.Legs[leg.OppositeLegIndex];

                    if (oppositeLeg != null)
                    {
                        Handles.color = new Color(0.7f, 0.7f, 0.2f, 0.25f);
                        SceneHelper_Leg_DrawBones(oppositeLeg);

                        Handles.color = new Color(1f, 1f, 1f, 0.4f);
                        SceneHelper_DrawLegAsLines(oppositeLeg);
                    }
                }

        }


        void SceneHelper_DrawLegAsLines(LegsAnimator.Leg leg, float lineWidth = 4f)
        {
            if (leg.BoneStart == null) return;
            if (leg.BoneMid == null) return;
            Handles.DrawAAPolyLine(lineWidth, leg.BoneStart.position, leg.BoneMid.position);
            if (leg.BoneEnd == null) return;
            Handles.DrawAAPolyLine(lineWidth, leg.BoneMid.position, leg.BoneEnd.position);
        }

        void SceneHelper_DrawScaleReference()
        {
            float rScale = editorScaleRef;
            Handles.color = new Color(0.3f, 0.9f, 0.35f, 0.55f);

            Vector3 offsetSide = Get.transform.right * rScale * 0.8f;
            Vector3 offsetForw = Get.transform.forward * rScale * 0.2f;
            Vector3 sidePos = Get.transform.position + offsetSide;
            Vector3 sidePosU = Get.transform.position + offsetSide + Get.Up * editorScaleRef;

            Handles.DrawAAPolyLine(2f + rScale, sidePos + offsetForw, sidePos - offsetForw);
            Handles.DrawAAPolyLine(2f + rScale, sidePosU + offsetForw, sidePosU - offsetForw);
            Handles.DrawAAPolyLine(2f + rScale, sidePos, sidePosU);

            if (Application.isPlaying == false)
                Handles.Label(sidePos, new GUIContent(" [i]", "Scale reference height. It should be about half of the height of the chracter, if it's two-legs character.\nIf it's quadruped/spider, it should be around height of hips of the creature."));
        }


        void SceneHelper_DrawRaycastingCastRange()
        {
            if (Get.RaycastStyle == LegsAnimator.ERaycastStyle.NoRaycasting) return;

            float rScale = Get.ScaleReference;
            Handles.color = new Color(0.3f, 0.9f, 0.35f, 0.65f);

            Vector3 offsetSide = -Get.transform.right * rScale * 0.8f;
            Vector3 offsetRight = Get.transform.right * rScale * 0.2f;
            Vector3 sidePos = Get.Hips.position + offsetSide;
            Vector3 grnd = Get.BaseTransform.position;

            if (Get.RaycastStartHeight == LegsAnimator.ERaycastStartHeight.StaticScaleReference)
            {
                sidePos = Get.transform.position + offsetSide + Get.transform.up * Get.ScaleReference * Get.RaycastStartHeightMul;
            }
            else
            {
                grnd = Get.Hips.position;
                grnd = Get.BaseTransform.InverseTransformPoint(grnd);
                grnd.y = 0f;
                grnd = Get.BaseTransform.TransformPoint(grnd);
            }

            Vector3 sidePosGrnd = grnd + offsetSide;
            Vector3 sidePosArr = Vector3.LerpUnclamped(sidePos, sidePosGrnd, 0.8f);

            if (Get.RaycastStyle != LegsAnimator.ERaycastStyle.AlongBones)
            {
                Handles.DrawDottedLine(sidePos, Get.Hips.position, 3f);
                Handles.DrawWireDisc(sidePos, Get.Up, rScale * 0.2f);
                Handles.DrawWireDisc(sidePosGrnd, Get.Up, rScale * 0.2f);
            }

            if (Get.RaycastStyle == LegsAnimator.ERaycastStyle.StraightDown)
            {
                Handles.DrawAAPolyLine(2f + rScale, sidePos, sidePosGrnd);
                Handles.DrawAAPolyLine(2f + rScale, sidePosGrnd, sidePosArr + offsetRight);
                Handles.DrawAAPolyLine(2f + rScale, sidePosGrnd, sidePosArr - offsetRight);
            }

            if (Get.RaycastStyle != LegsAnimator.ERaycastStyle.StraightDown && Get.RaycastStyle != LegsAnimator.ERaycastStyle.AlongBones)
            {
                Handles.DrawAAPolyLine(2f + rScale, Get.Hips.position, sidePosGrnd);
                Vector3 dir = sidePosGrnd - Get.Hips.position;
                Quaternion towards = Quaternion.LookRotation(dir, Get.Up);
                Handles.DrawAAPolyLine(2f + rScale, sidePosGrnd, sidePosGrnd - dir * 0.1f + towards * Vector3.up * Get.ScaleReference * 0.125f);
                Handles.DrawAAPolyLine(2f + rScale, sidePosGrnd, sidePosGrnd - dir * 0.1f + towards * Vector3.down * Get.ScaleReference * 0.125f);
            }

            Vector3 castEnd = sidePosGrnd;
            castEnd += -Get.Up * rScale * Get.CastDistance;

            if (Get.RaycastStyle != LegsAnimator.ERaycastStyle.OriginToFoot && Get.RaycastStyle != LegsAnimator.ERaycastStyle.AlongBones)
            {
                Handles.DrawDottedLine(sidePosGrnd, castEnd, 3f);
                Handles.DrawWireDisc(castEnd, Get.Up, rScale * 0.2f);
            }

            if (Get.RaycastStyle == LegsAnimator.ERaycastStyle.AlongBones)
            {
                if (Get.Legs.Count > 0)
                {
                    for (int i = 0; i < Get.Legs.Count; i++)
                    {
                        if (Get.Legs[i].BoneStart && Get.Legs[i].BoneMid && Get.Legs[i].BoneEnd)
                        {
                            Handles.DrawAAPolyLine(Get.Legs[i].BoneStart.position, Get.Legs[i].BoneMid.position, Get.Legs[i].BoneEnd.position);
                            Handles.DrawDottedLine(Get.Legs[i].BoneEnd.position, Get.Legs[i].BoneEnd.position - Get.Up * rScale * Get.CastDistance, 3f);
                        }
                    }
                }
            }

            // Drawing spherecast guides

            if (Get.RaycastShape == LegsAnimator.ERaycastMode.Spherecast)
            {
                Handles.color = new Color(0.3f, 1f, 0.3f, 0.11f);

                float sphRadius = Get.ScaleReference * 0.065f * Get.SpherecastResize;

                for (int l = 0; l < Get.Legs.Count; l++)
                {
                    var leg = Get.Legs[l];
                    if (leg.BoneEnd == null || leg.BoneMid == null) continue;
                    Vector3 legDir = (leg.BoneEnd.position - leg.BoneMid.position).normalized;
                    Handles.SphereHandleCap(0, leg.BoneEnd.position - (legDir * sphRadius * 0.5f), Quaternion.identity, sphRadius, EventType.Repaint);
                }
            }

        }


        void SceneHelper_DrawGlueFloorLevel()
        {
            Handles.matrix = Get.BaseTransform.localToWorldMatrix;
            float f = Get.ScaleReferenceNoScale;
            float h = Get.GluingFloorLevelUseOnMoving ? Mathf.Lerp(Get.GluingFloorLevel, Get.GluingFloorLevelOnMoving, Get.IsMovingBlend) : Get.GluingFloorLevel;
            Vector3 a = new Vector3(f, h, f);
            Vector3 b = new Vector3(f, h, -f);
            Vector3 c = new Vector3(-f, h, -f);
            Vector3 d = new Vector3(-f, h, f);
            Handles.DrawAAPolyLine(3f, a, b, c, d, a);
            Handles.matrix = Matrix4x4.identity;
        }


        void SceneHelper_DrawRaycastingStepDown()
        {
            if (Get.RaycastStyle == LegsAnimator.ERaycastStyle.NoRaycasting) return;

            float rScale = Get.ScaleReference;
            Handles.color = new Color(0.6f, 0.4f, 0.1f, 0.4f);

            Vector3 hipsPos = Get.Hips.position;
            if (Get.LegsInitialized)
            {
                hipsPos = Get.RootToWorldSpace(Get.HipsSetup.InitHipsPositionRootSpace);
            }

            Vector3 offsetSide = Get.transform.right * rScale * 0.8f;
            Vector3 offsetRight = Get.transform.right * rScale * 0.2f;
            Vector3 offsetForw = Get.transform.forward * rScale * 0.2f;
            Vector3 sidePos = hipsPos + offsetSide;

            Vector3 bodyEnd = sidePos;
            bodyEnd += -Get.Up * rScale * Get.BodyStepDown;

            float hipsToGround = Get.HipsToGroundDistance();
            if (Get.LegsInitialized) hipsToGround = Get.HipsSetup.InitialHipsHeightLocal * Get.BaseTransform.lossyScale.y;

            Vector3 bodyEndLeg = bodyEnd - Get.Up * hipsToGround;

            if (Get.BodyStepDown > 0f)
            {
                Handles.DrawDottedLine(sidePos, hipsPos, 3f);
                Handles.SphereHandleCap(0, sidePos, Quaternion.identity, rScale * 0.1f, EventType.Repaint);


                Handles.DrawAAPolyLine(2f + rScale, sidePos, bodyEnd);
                Handles.DrawWireDisc(bodyEnd, Get.Up, rScale * 0.2f);

                Handles.DrawAAPolyLine(3f + rScale, bodyEnd + offsetRight, bodyEndLeg + offsetRight, bodyEndLeg + offsetRight + offsetForw);
                Handles.DrawAAPolyLine(3f + rScale, bodyEnd - offsetRight, bodyEndLeg - offsetRight, bodyEndLeg - offsetRight + offsetForw);
            }

            if (Get.MaxBodyStepUp > 0f)
            {
                Vector3 bodyUpper = sidePos + offsetSide;
                bodyUpper += Get.Up * rScale * Get.MaxBodyStepUp;
                Handles.DrawDottedLine(bodyUpper, sidePos + offsetSide, 3f);
                Handles.DrawDottedLine(sidePos + offsetSide, sidePos, 3f);

                Handles.SphereHandleCap(0, bodyUpper, Quaternion.identity, rScale * 0.1f, EventType.Repaint);
                bodyEndLeg = bodyUpper - Get.Up * hipsToGround;
                Handles.DrawWireDisc(bodyUpper, Get.Up, rScale * 0.2f);

                Handles.DrawAAPolyLine(3f + rScale, bodyUpper + offsetRight, bodyEndLeg + offsetRight, bodyEndLeg + offsetRight + offsetForw);
                Handles.DrawAAPolyLine(3f + rScale, bodyUpper - offsetRight, bodyEndLeg - offsetRight, bodyEndLeg - offsetRight + offsetForw);
            }

        }

        protected void SceneHelper_DrawExtraControll()
        {
            Handles.matrix = Get.BaseTransform.localToWorldMatrix;

            //if (Get.FloorLevel != 0f)
            {
                float planeScale = Get.ScaleReference * 0.425f;
                float yl = Get.GluingFloorLevel;

                Handles.color = new Color(0.4f, 0.45f, 1f, 0.5f);

                Handles.DrawAAPolyLine(2, new Vector3(-planeScale, yl, -planeScale),
                    new Vector3(planeScale, yl, -planeScale),
                    new Vector3(planeScale, yl, planeScale),
                    new Vector3(-planeScale, yl, planeScale),
                    new Vector3(-planeScale, yl, -planeScale));

                planeScale *= 0.5f;
                Handles.color *= 0.75f;
                Handles.DrawWireDisc(Vector3.zero, Get.Up, planeScale);
                Handles.DrawLine(new Vector3(0f, yl, planeScale), new Vector3(0f, yl, -planeScale));
                Handles.DrawLine(new Vector3(planeScale, yl, 0f), new Vector3(-planeScale, yl, 0f));
            }

            Handles.matrix = Matrix4x4.identity;
        }

        void SceneHelper_DrawRaycastingPreview(Color baseColor)
        {
            if (Application.isPlaying == false) return;

            float scaleRefSm = Get.ScaleReference * 0.1f;

            Handles.color = baseColor;
            for (int l = 0; l < Get.Legs.Count; l++)
            {
                var leg = Get.Legs[l];

                if (!leg.RaycastHitted)
                {
                    Handles.DrawWireDisc(leg.BoneEnd.position, Get.Up, scaleRefSm);
                    break;
                }

                RaycastHit hit = leg.LastGroundHit;

                Handles.DrawWireDisc(hit.point, hit.normal, scaleRefSm);
                Handles.DrawLine(hit.point, hit.point + hit.normal * scaleRefSm);
                Handles.SphereHandleCap(0, hit.point + hit.normal * scaleRefSm, Quaternion.identity, scaleRefSm * 0.5f, EventType.Repaint);
            }
        }


        void SceneHelper_DrawFeetLength()
        {
            //if (_setupik_selected_leg < 0)
            //    Handles.color = Color.green * 0.9f;
            //else
            //    Handles.color = Color.green * 0.4f;

            //for (int l = 0; l < Get.Legs.Count; l++)
            //{
            //    var leg = Get.Legs[l];
            //    if (leg.BoneEnd == null) continue;

            //    Vector3 heelStart = leg.BoneEnd.TransformPoint(leg.AnkleToHeel);
            //    Vector3 footEnd = leg.BoneEnd.TransformPoint(leg.AnkleToFeetEnd);
            //    Vector3 toEnd = footEnd - heelStart;
            //    Vector3 right = leg.BoneEnd.TransformDirection(leg.AnkleRight).normalized * toEnd.magnitude * 0.4f;
            //    Vector3 end = footEnd + toEnd * Get.FeetLengthAdjust;

            //    Handles.DrawAAPolyLine(3f, heelStart + right, end + right, end - right, heelStart - right);
            //}
        }

        void SceneHelper_DrawIKSetup(Color mainColor, int selected)
        {
            float scaleRef = Get.ScaleReference;
            float scaleRefShort = Get.ScaleReference * 0.2f;
            float scaleRefShort2 = scaleRefShort * 0.25f;

            for (int i = 0; i < Get.Legs.Count; i++)
            {
                var leg = Get.Legs[i];
                if (leg.HasAllBonesSet() == false) continue;

                bool isSel = selected == i;
                if (selected < -1) isSel = true;

                Handles.color = mainColor * (isSel ? 1f : 0.5f);
                Handles.DrawLine(leg.BoneStart.position, Get.Hips.position);
                FGUI_Handles.DrawBoneHandle(leg.BoneStart.position, leg.BoneMid.position, 0.6f);
                FGUI_Handles.DrawBoneHandle(leg.BoneMid.position, leg.BoneEnd.position, 0.6f);

                Vector3 heel = leg.BoneEnd.TransformPoint(leg.AnkleToHeel);

                if (Get.AnimateFeet)
                {
                    Handles.color = mainColor * (isSel ? 0.8f : 0.3f);

                    //Vector3 heelForw = heel + leg.BoneEnd.TransformDirection(leg.AnkleForward * scaleRefShort);
                    Vector3 footEnd = leg.BoneEnd.TransformPoint(leg.AnkleToFeetEnd);
                    Vector3 heelForw = footEnd + (footEnd - heel) * Get.FeetLengthAdjust;

                    Vector3 heelUp = heelForw + leg.BoneEnd.TransformDirection(leg.AnkleUp * scaleRefShort2);

                    Handles.DrawAAPolyLine(2 + scaleRef, leg.BoneEnd.position, heel, heelForw, heelUp, leg.BoneEnd.position);
                    Handles.DrawAAPolyLine(2 + scaleRef, heelForw, heel + leg.BoneEnd.TransformDirection(leg.AnkleRight * scaleRefShort2), heel - leg.BoneEnd.TransformDirection(leg.AnkleRight * scaleRefShort2), heelForw);

                    scaleRefShort2 *= 2f;
                    Vector3 feetEnd = Vector3.LerpUnclamped(heel, heelForw, leg.FootMiddlePosition);
                    Handles.DrawAAPolyLine(2 + scaleRef, feetEnd - Get.BaseTransform.right * scaleRefShort2, feetEnd + Get.BaseTransform.right * scaleRefShort2);
                    Handles.DrawAAPolyLine(2 + scaleRef, feetEnd, heel);
                }
                else
                {
                    Handles.DrawAAPolyLine(2 + scaleRef, leg.BoneEnd.position, heel);

                    Handles.color *= 0.6f;
                    Handles.DrawWireDisc(heel, Get.Up, scaleRefShort2);

                    if (isSel && selected > -2)
                    {
                        Handles.color *= 0.7f;
                        Handles.DrawWireDisc(leg.BoneEnd.position, Get.Up, scaleRefShort2);
                    }
                }

            }
        }


        void SceneHelper_DrawDefinedBones(Color? customColor = null)
        {
            if (_selected_leg >= 0) return;
            if (Get.Hips == null) return;

            Handles.color = new Color(0.25f, 0.9f, 0.7f, 0.8f);
            if (customColor != null) Handles.color = customColor.Value;

            for (int i = 0; i < Get.Legs.Count; i++)
            {
                var leg = Get.Legs[i];
                SceneHelper_Leg_DrawBones(leg);
            }

            if (SceneHelper_FocusOnBone == null)
            {
                Handles.SphereHandleCap(0, Get.Hips.position, Quaternion.identity, editorScaleRef * 0.07f, EventType.Repaint);
            }
        }


        void SceneHelper_DrawDefinedBonesHipsLink(Color? customColor = null)
        {
            if (_selected_leg >= 0) return;
            if (Get.Hips == null) return;

            Handles.color = new Color(0.3f, 0.9f, 0.75f, 0.4f);
            if (customColor != null) Handles.color = customColor.Value;

            for (int i = 0; i < Get.Legs.Count; i++)
            {
                var leg = Get.Legs[i];
                if (leg.BoneEnd == null) continue;
                Handles.DrawDottedLine(leg.BoneEnd.position, Get.Hips.position, 3f);
            }

        }

        void SceneHelper_Leg_DrawBones(LegsAnimator.Leg leg)
        {
            if (leg.BoneStart)
            {
                if (leg.BoneMid) FGUI_Handles.DrawBoneHandle(leg.BoneStart.position, leg.BoneMid.position);
                else Util_DrawChildBoneOf(leg.BoneStart);

                Handles.DrawDottedLine(leg.BoneStart.position, Get.Hips.position, 2f);
            }

            if (leg.BoneMid)
            {
                if (leg.BoneEnd) FGUI_Handles.DrawBoneHandle(leg.BoneMid.position, leg.BoneEnd.position);
                else Util_DrawChildBoneOf(leg.BoneMid);
            }

            if (leg.BoneEnd)
            {
                Util_DrawChildBoneOf(leg.BoneEnd);
            }
        }


        void Util_DrawLegSegmentSelectorButton(LegsAnimator.Leg leg, Transform t, float drawScale, bool mirror = false, bool upMode = false)
        {
            Handles.color = new Color(0.4f, 1f, 0.65f, 0.3f);
            Util_DrawChildBoneOf(t.parent, 0.4f);

            float mul = 1f;
            if (leg.BoneMid == t || leg.BoneEnd == t) mul = -1f;

            Util_DrawBoneIndicator(t, drawScale * mul * 0.7f, false, mul == -1f ? "Change" : "", mirror, upMode);

            Vector3 off = LabelIndicatorHelperOffset(t, drawScale, mirror) * 0.6f;
            float hSize = HandleUtility.GetHandleSize(t.position);
            float scaler = 1f / hSize;

            if (leg.BoneMid == null)
            {
                Handles.color = new Color(1f, 1f, 1f, 1f);
                if (Util_DrawButton(new GUIContent(Tex_LegMid), t.position + off * 0.2f, 1f))
                {
                    leg.BoneMid = t;
                    OnChange();
                }

            }

            if (leg.BoneEnd == null && leg.BoneMid != null)
            {
                Handles.color = new Color(1f, 1f, 1f, 1f);
                if (Util_DrawButton(new GUIContent(Tex_LegEnd), t.position + off * 0.2f, 1f, true, new Vector2(1, 0)))
                {
                    leg.BoneEnd = t;
                    leg.DefineLegSide(Get);
                    leg.RefreshLegAnkleToHeelAndFeetAndAxes(Get.BaseTransform);
                    OnChange();
                }
            }


            if (hSize > 0.01f)
            {
                float mouseOnDistance = 1000f;

                int sel = 0;
                if (leg.BoneMid == null)
                {
                    mouseOnDistance = Vector2.Distance(Event.current.mousePosition, HandleUtility.WorldToGUIPoint(t.position /*+ off*/));
                    sel = 1;
                }
                else
                if (leg.BoneEnd == null)
                {
                    float dist2 = Vector2.Distance(Event.current.mousePosition, HandleUtility.WorldToGUIPoint(t.position /*+ off + ScView.camera.transform.rotation * Vector3.right * drawScale * 1.25f*/));
                    if (dist2 < mouseOnDistance) { mouseOnDistance = dist2; sel = 2; }
                }

                if (mouseOnDistance < 1f * scaler)
                {
                    Util_DrawChildBoneOf(t, .6f);

                    if (t.childCount > 0)
                        if (sel > 0)
                        {
                            string label = sel == 1 ? "? Is it Lower Leg ?" : "? IS it Foot Bone ?";
                            Vector3 labelPos = Vector3.Lerp(t.position, t.GetChild(0).position, 0.35f);
                            labelPos += ScView.camera.transform.rotation * Vector3.right * drawScale * 0.15f;
                            Handles.Label(labelPos, label, EditorStyles.boldLabel);
                        }
                }
            }

        }


        void Util_DrawBoneIndicator(Transform b, float drawScale, bool drawSphere = false, string customString = "", bool mirror = false, bool upMode = false)
        {
            if (b == null) return;

            Handles.color = new Color(0.6f, 0.6f, 0.7f, 0.65f);

            if (drawSphere)
            {
                Handles.SphereHandleCap(0, b.position, Quaternion.identity, drawScale * 0.5f, EventType.Repaint);
            }

            Vector3 labelPos = b.position + LabelIndicatorHelperOffset(b, drawScale, mirror, upMode);
            labelPos += Vector3.up * drawScale * 0.4f;

            Handles.DrawLine(b.position, labelPos);
            Handles.color = Color.white;
            Handles.Label(labelPos + Vector3.up * drawScale * 0.5f, string.IsNullOrEmpty(customString) ? b.name : customString);
        }


        Vector3 LabelIndicatorHelperOffset(Transform b, float drawScale, bool mirror = false, bool upMode = false)
        {
            if (upMode) return ScView.camera.transform.rotation * new Vector3(mirror ? 0.2f : -.2f, mirror ? 0.8f : -0.8f, 0.3f) * Get.Util_SideMul(b) * drawScale * 3f;

            return ScView.camera.transform.rotation * new Vector3(mirror ? 0.9f : -.8f, 0.33f, 0f) * Get.Util_SideMul(b) * drawScale * 3f;
        }

        void Util_DrawChildBoneOf(Transform t, float fatness = 1f)
        {
            if (t.childCount <= 0) return;
            FGUI_Handles.DrawBoneHandle(t.position, Get.Finders_GetRelevantChildOf(t).position, fatness);
        }


        bool Util_Leg_ShouldDraw(Transform t)
        {
            //if (t == SceneHelper_FocusOnBone) return false;

            for (int i = 0; i < Get.Legs.Count; i++)
            {
                var leg = Get.Legs[i];
                if (leg.BoneStart == t) return false;
                if (leg.BoneMid == t) return false;
                if (leg.BoneEnd == t) return false;
            }

            return true;
        }

        public static GUIStyle ButtonStyle { get { if (__buttStyleHard != null) return __buttStyleHard; __buttStyleHard = new GUIStyle(EditorStyles.miniButton); __buttStyleHard.fixedHeight = 0; __buttStyleHard.padding = new RectOffset(3, 3, 3, 3); __buttStyleHard.normal.background = Resources.Load<Texture2D>("Fimp/Backgrounds/FbuttonH"); __buttStyleHard.hover.background = Resources.Load<Texture2D>("Fimp/FbuttonHover"); __buttStyleHard.focused.background = __buttStyleHard.hover.background; __buttStyleHard.active.background = Resources.Load<Texture2D>("Fimp/Backgrounds/ButtonStyle"); return __buttStyleHard; } }
        private static GUIStyle __buttStyleHard = null;

        bool Util_DrawButton(GUIContent content, Vector3 pos, float size, bool buttonBG = true, Vector2? rectOffset = null)
        {
            float sc = HandleUtility.GetHandleSize(pos);
            float hSize = Mathf.Sqrt(size) * 32 - sc * 16;

            if (hSize > 0f)
            {
                Handles.BeginGUI();
                Vector2 pos2D = HandleUtility.WorldToGUIPoint(pos);
                float hhSize = hSize / 2f;

                GUIStyle style = buttonBG ? ButtonStyle : EditorStyles.label;

                if (rectOffset != null) pos2D += rectOffset.Value * hSize;

                if (GUI.Button(new Rect(pos2D.x - hhSize, pos2D.y - hhSize, hSize, hSize), content, style))
                {
                    Handles.EndGUI();
                    return true;
                }

                Handles.EndGUI();
            }

            return false;
        }

        Color Util_IndexColor(int index, float s = 0.3f, float v = 0.45f, float hueOff = 0f, float hueMul = 1f)
        {
            float h = ((float)index * 0.1f * hueMul + 0.2f + hueOff) % 1;
            return Color.HSVToRGB(h, s, v);
        }

        void PingObject(UnityEngine.Object g)
        {
            EditorGUIUtility.PingObject(g);
        }

    }

}