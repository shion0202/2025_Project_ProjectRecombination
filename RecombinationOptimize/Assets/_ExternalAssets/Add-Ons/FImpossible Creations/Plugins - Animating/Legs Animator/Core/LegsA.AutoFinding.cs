#if UNITY_EDITOR
using FIMSpace.AnimationTools;
using FIMSpace.FEditor;
using UnityEditor;
#endif
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{

    public partial class LegsAnimator
    {
        protected virtual void Finder_EnsureLegsCount(int legsCount)
        {
            for (int i = 0; i < legsCount; i++)
            {
                if (Legs.Count <= i) Legs_AddNewLeg();
            }
        }

        public virtual void Finder_AutoFindLegsIfHuman(Animator anim)
        {
            if (!anim) return;
            if (!anim.isHuman) return;

            Finder_EnsureLegsCount(2);

            if (Legs[0].BoneStart == null)
            {
                Legs[0].BoneStart = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                Legs[0].Side = ELegSide.Left;
            }

            if (Legs[0].BoneMid == null) Legs[0].BoneMid = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            if (Legs[0].BoneEnd == null) Legs[0].BoneEnd = anim.GetBoneTransform(HumanBodyBones.LeftFoot);

            if (Legs[1].BoneStart == null)
            {
                Legs[1].BoneStart = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                Legs[1].Side = ELegSide.Right;
            }

            if (Legs[1].BoneMid == null) Legs[1].BoneMid = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            if (Legs[1].BoneEnd == null) Legs[1].BoneEnd = anim.GetBoneTransform(HumanBodyBones.RightFoot);

            if (Legs[0].OppositeLegIndex < 0) Legs[1].AssignOppositeLegIndex(0);

            if (Legs[0].Side == ELegSide.Undefined) Legs[0].Side = ELegSide.Left;
            if (Legs[1].Side == ELegSide.Undefined) Legs[1].Side = ELegSide.Right;

            if (SpineBone == null) SpineBone = anim.GetBoneTransform(HumanBodyBones.Spine);

            Finders_RefreshAllLegsAnkleAxes();
            User_RefreshHelperVariablesOnParametersChange();
        }


        public Animator Finding_TryFindMecanim()
        {
            if (!Mecanim)
            {
                Mecanim = FTransformMethods.FindComponentInAllChildren<Animator>(BaseTransform);
                if (!Mecanim) Mecanim = BaseTransform.GetComponentInParent<Animator>();
            }

            return Mecanim;
        }

        public virtual void Finder_AutoDefineOppositeLegs()
        {
            List<Vector3> localPos = new List<Vector3>();

            for (int i = 0; i < Legs.Count; i++)
            {
                if (Legs[i].BoneEnd == null)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.DisplayDialog("No all legs are set up!", "Some of the legs bones are lacking and can't define opposite legs!", "Ok");
#endif
                    return;
                }

                localPos.Add(BaseTransform.InverseTransformPoint(Legs[i].BoneEnd.position));
            }

            for (int l = 0; l < Legs.Count; l++)
            {
                if (Legs[l].OppositeLegIndex > -1) continue; // Already Set

                int nearestI = -1;
                float nearestDist = float.MaxValue;
                for (int o = 0; o < Legs.Count; o++)
                {
                    if (l == o) continue;
                    if (Mathf.Sign(localPos[l].x) == Mathf.Sign(localPos[o].x)) continue;
                    float dist = Mathf.Abs(localPos[l].z - localPos[o].z);

                    if (Legs[o].Side == ELegSide.Undefined)
                    { if (localPos[o].x < 0) Legs[o].Side = ELegSide.Left; else Legs[o].Side = ELegSide.Right; }

                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestI = o;
                    }
                }

                if (nearestI != -1) Legs[l].AssignOppositeLegIndex(nearestI);
            }
        }


        public virtual void Finding_LegBonesByNamesAndParenting()
        {
            Finding_TryFindMecanim();

            if (Legs == null) Legs = new List<Leg>();

            if (Mecanim)
                if (Mecanim.isHuman)
                {
                    Finder_AutoFindLegsIfHuman(Mecanim);
                    return;
                }

            if (Legs.Count < 2) Finder_EnsureLegsCount(2);

#if UNITY_EDITOR

            // Not humanoid animator or no animator so we searching in skinned mesh bones or all children of root
            SkinnedMeshRenderer skin = FEditor.FGUI_Finders.GetBoneSearchArray(BaseTransform);
            Transform[] searchIn = null;
            if (skin) searchIn = skin.bones;
            if (searchIn == null) searchIn = BaseTransform.GetComponentsInChildren<Transform>();

            string[] upLegKeys = new string[] { "upperleg", "thigh" };
            string[] lowLegKeys = new string[] { "lowerleg", "calf", "shin", "knee" };
            string[] footKeys = new string[] { "foot", "ankle", "anke", "calf" };

            Finder_EnsureLegsCount(2);
            var lleg = Legs[0];
            var rleg = Legs[1];

            if (lleg.Side == ELegSide.Right) { lleg = Legs[1]; rleg = Legs[0]; }

            for (int i = 0; i < searchIn.Length; i++)
            {
                if (IsSetupValid()) { break; } // If leg bones setted up then stop array

                string nameLower = searchIn[i].transform.name.ToLower();
                if (!lleg.BoneStart) // Searching for left upper leg
                    if (Finders_IsRightOrLeft(searchIn[i], BaseTransform) == false)
                        if (FGUI_Finders.HaveKey(nameLower, upLegKeys))
                            if (!nameLower.Contains("twist"))
                            { lleg.BoneStart = searchIn[i]; continue; }

                if (!rleg.BoneStart) // Searching for right upper leg
                    if (Finders_IsRightOrLeft(searchIn[i], BaseTransform) == true)
                        if (FGUI_Finders.HaveKey(nameLower, upLegKeys))
                            if (!nameLower.Contains("twist"))
                            { rleg.BoneStart = searchIn[i]; continue; }

                if (!lleg.BoneMid) // Searching for left lower leg
                    if (Finders_IsRightOrLeft(searchIn[i], BaseTransform) == false)
                        if (FGUI_Finders.HaveKey(nameLower, lowLegKeys))
                            if (!nameLower.Contains("twist"))
                            { lleg.BoneMid = searchIn[i]; continue; }

                if (!rleg.BoneMid) // Searching for right lower leg
                    if (Finders_IsRightOrLeft(searchIn[i], BaseTransform) == true)
                        if (FGUI_Finders.HaveKey(nameLower, lowLegKeys))
                            if (!nameLower.Contains("twist"))
                            { rleg.BoneMid = searchIn[i]; continue; }

                if (lleg.BoneEnd == null) // Searching for left foot
                {
                    if (Finders_IsRightOrLeft(searchIn[i], BaseTransform) == false)
                    {
                        if (FGUI_Finders.HaveKey(nameLower, footKeys))
                            if (!nameLower.Contains("twist"))
                            { lleg.BoneEnd = searchIn[i]; continue; }
                    }
                }

                if (!rleg.BoneEnd) // Searching for right foot
                    if (Finders_IsRightOrLeft(searchIn[i], BaseTransform) == true)
                        if (FGUI_Finders.HaveKey(nameLower, footKeys))
                            if (!nameLower.Contains("twist"))
                            { rleg.BoneEnd = searchIn[i]; continue; }
            }

            if (Legs[0].BoneStart == null)
            {
                string[] legKeys = new string[] { "leg" };

                // Try another search, less specific
                for (int i = 0; i < searchIn.Length; i++)
                {
                    if (IsSetupValid()) { break; } // If leg bones setted up then stop array

                    string nameLower = searchIn[i].transform.name.ToLower();
                    if (!lleg.BoneStart) // Searching for left upper leg
                        if (Finders_IsRightOrLeft(searchIn[i], BaseTransform) == false)
                            if (FGUI_Finders.HaveKey(nameLower, legKeys))
                                if (!nameLower.Contains("twist"))
                                { lleg.BoneStart = searchIn[i]; continue; }
                }
            }

            if (IsSetupValid())
            {
                if (Legs[0].BoneStart != null)
                {
                    if (Finders_IsRightOrLeft(Legs[0].BoneStart, BaseTransform) == true)
                    {
                        Legs[0].Side = ELegSide.Right;
                        Legs[1].Side = ELegSide.Left;
                    }
                    else
                    {
                        Legs[0].Side = ELegSide.Left;
                        Legs[1].Side = ELegSide.Right;
                    }

                    Legs[0].AssignOppositeLegIndex(1);
                }

                Finders_RefreshAllLegsAnkleAxes();
                User_RefreshHelperVariablesOnParametersChange();
                _Editor_EnsureCount += 5;
                return;
            }

            Finding_SearchForHips();

            // If foot found but other bones not
            if (lleg.BoneEnd)
            {
                lleg.BoneStart = Finding_FindUpperLeg(lleg.BoneEnd);

                if (!lleg.BoneStart)
                {
                    if (!lleg.BoneMid) lleg.BoneMid = lleg.BoneEnd.parent;
                    if (lleg.BoneMid) if (!lleg.BoneStart) lleg.BoneStart = lleg.BoneMid.parent;
                }
                else
                {
                    if (lleg.BoneStart) if (lleg.BoneStart.childCount > 0) lleg.BoneMid = Finders_GetRelevantChildOf(lleg.BoneStart);
                }
            }
            else
            {
                Finding_FindLowerLegsWithUpper();
            }

            if (rleg.BoneEnd)
            {
                rleg.BoneStart = Finding_FindUpperLeg(rleg.BoneEnd);

                if (!rleg.BoneStart)
                {
                    if (!rleg.BoneMid) rleg.BoneMid = rleg.BoneEnd.parent;
                    if (rleg.BoneMid) if (!rleg.BoneStart) rleg.BoneStart = rleg.BoneMid.parent;
                }
                else
                {
                    if (rleg.BoneStart) if (rleg.BoneStart.transform.childCount > 0) rleg.BoneMid = Finders_GetRelevantChildOf(rleg.BoneStart);
                }
            }
            else
            {
                Finding_FindLowerLegsWithUpper();
            }

            if (lleg.OppositeLegIndex < 0) lleg.AssignOppositeLegIndex(1);
            if (lleg.Side == ELegSide.Undefined) if (lleg.BoneEnd) lleg.DefineLegSide(this, rleg);
            if (rleg.Side == ELegSide.Undefined) if (rleg.BoneEnd) rleg.DefineLegSide(this, lleg);

            if (rleg.BoneStart == null)
            {
                if (rleg.OppositeLegIndex > -1)
                {
                    var oppos = rleg.GetOppositeLegReference(this);
                    if (oppos != null) oppos.OppositeLegIndex = -1;
                }

                Legs.RemoveAt(1);

                if (lleg.BoneStart == null)
                {
                    Legs.RemoveAt(0);
                    EditorUtility.DisplayDialog("Not Found", "Could not find leg bones automatically. You need to do it manually. Check tutorials or manual, for tips how to do it.", "ok");
                }
                else
                    EditorUtility.DisplayDialog("Not Found", "Could not find all leg bones automatically, but one seems to be found.\nWarning! It may have been chosen incorrectly!", "ok");
            }

            Finders_RefreshAllLegsAnkleAxes();
            User_RefreshHelperVariablesOnParametersChange();

#endif

        }



        public virtual void Finding_SearchForHips()
        {
            if (Hips) return;

            Finding_TryFindMecanim();

            if (Mecanim) if (Mecanim.isHuman)
                { Hips = Mecanim.GetBoneTransform(HumanBodyBones.Hips); /*Hips.SetRelation(BaseTransform);*/ return; }

#if UNITY_EDITOR

            SkinnedMeshRenderer skin = FGUI_Finders.GetBoneSearchArray(BaseTransform);
            Transform[] searchIn = null;
            if (skin) searchIn = skin.bones;
            if (searchIn == null) searchIn = BaseTransform.GetComponentsInChildren<Transform>();

            string[] pelvisKeys = new string[] { "pelvis", "hips" };
            for (int i = 0; i < searchIn.Length; i++)
            {
                string nameLower = searchIn[i].transform.name.ToLower();
                if (Hips) return;
                if (FGUI_Finders.HaveKey(nameLower, pelvisKeys)) { Hips = searchIn[i]; /*Hips.SetRelation(BaseTransform);*/ }
            }

            // Not found by keywords then finding by coords and parenting
            //Debug.Log("[Biped Animator Bone Finder] Could not find bone for hips. Now auto searching by guessing! Please check if correct hips bone is found!");

            // Defining scale of the skeleton
            float highest = 0f;
            float mostLeft = 0f;
            float mostRight = 0f;

            for (int i = 0; i < searchIn.Length; i++)
            {
                Vector3 boneLoc = BaseTransform.InverseTransformPoint(searchIn[i].position);
                if (boneLoc.y > highest) highest = boneLoc.y;
                if (boneLoc.x < mostLeft) mostLeft = boneLoc.x;
                if (boneLoc.x > mostRight) mostRight = boneLoc.x;
            }

            Transform baseBone = transform;
            if (skin) if (skin.rootBone) baseBone = skin.rootBone;
            List<Transform> hipsPropabilities = new List<Transform>();
            List<Transform> chestPropabilities = new List<Transform>();

            // If can start finding in target bone
            if (baseBone.childCount != 0)
            {
                searchIn = baseBone.GetComponentsInChildren<Transform>();
                for (int i = 0; i < searchIn.Length; i++)
                {
                    Transform t = searchIn[i];
                    if (t.childCount > 2)
                    {
                        Vector3 location = BaseTransform.InverseTransformPoint(t.position);
                        if (location.y > highest * 0.05f && location.y < highest * 0.6f) // Bone which is somewhere inside whole skeleton middle point
                        {
                            // Bone which is not too much on the right or left
                            if (location.x > mostLeft * 0.3f && location.x < mostRight * 0.3f)
                            {
                                hipsPropabilities.Add(t);
                            }
                        }
                    }
                }
            }


            Transform nearestToBeHips = null;
            int nearest = int.MaxValue;
            //float desiredHipsPoint = highest * 0.3f;  // Finding nearest to 0.4 height point

            //// Checking found propabilities for most correct one for hips default location
            //for (int i = 0; i < hipsPropabilities.Count; i++)
            //{
            //    Vector3 location = BaseTransform.InverseTransformPoint(hipsPropabilities[i].position);
            //    float dist = Mathf.Abs(location.y - desiredHipsPoint);
            //    if (dist < nearest) nearestToBeHips = hipsPropabilities[i];
            //}

            // Getting hips propability which is nearest to the root
            for (int i = 0; i < hipsPropabilities.Count; i++)
            {
                int depth = SkeletonRecognize.SkeletonInfo.GetDepth(hipsPropabilities[i], BaseTransform);
                if (depth < nearest) nearestToBeHips = hipsPropabilities[i];
            }

            Hips = nearestToBeHips;
            //if (nearestToBeHips) Hips.SetRelation(BaseTransform);

            if (Hips != null) return;

            Hips = Finders_QuickHipsSearch();
            User_RefreshHelperVariablesOnParametersChange();

#endif

        }


        public Transform Finders_QuickHipsSearch()
        {
            Transform baseTr = BaseTransform;

            Animator anim = FTransformMethods.FindComponentInAllChildren<Animator>(baseTr);
            if (anim) if (anim.isHuman)
                {
                    Finder_AutoFindLegsIfHuman(anim);
                    return anim.GetBoneTransform(HumanBodyBones.Hips);
                }

            Transform finding = FTransformMethods.FindChildByNameInDepth("pelv", baseTr);

            if (finding)
            {
                SkinnedMeshRenderer skin = baseTr.GetComponentInChildren<SkinnedMeshRenderer>();

                if (skin)
                {
                    if (finding == skin.rootBone || skin.bones.Contains(finding) || finding.childCount >= 3)
                        return finding;
                }
                else return finding;
            }

            finding = FTransformMethods.FindChildByNameInDepth("hips", baseTr);

            if (finding)
            {
                if (finding.childCount >= 3) return finding;
                else
                {
                    if (finding.parent != null)
                    {
                        finding = finding.parent;
                        if (finding.childCount >= 3) return finding;

                        if (finding.parent != null)
                        {
                            finding = finding.parent;
                            if (finding.childCount >= 3) return finding;
                        }
                    }
                }
            }

            return null;
        }



        private void Finding_UpperLegsWithPelvis()
        {
            if (!Hips) return;

            Finder_EnsureLegsCount(2);

            var leftLeg = Legs[0];
            var rightLeg = Legs[1];
            if (Legs[0].Side == ELegSide.Right) { leftLeg = Legs[1]; rightLeg = Legs[0]; }

            if (leftLeg == null) return;

            // Finding upper legs by defining that it's lower and on the left of pelvis bone
            if (leftLeg.BoneStart == null)
            {
                Transform uppLeg = null;
                Vector3 pelvisLocation = BaseTransform.InverseTransformPoint(Hips.position);
                for (int i = 0; i < Hips.childCount; i++)
                {
                    if (uppLeg) break;
                    Vector3 loc = BaseTransform.InverseTransformPoint(Hips.GetChild(i).position);
                    if (loc.x < pelvisLocation.x) if (loc.y < pelvisLocation.y) uppLeg = Hips.GetChild(i);
                }

                leftLeg.BoneStart = uppLeg;
            }

            if (rightLeg == null) return;

            if (rightLeg.BoneStart == null)
            {
                Transform uppLeg = null;
                Vector3 pelvisLocation = BaseTransform.InverseTransformPoint(Hips.position);
                for (int i = 0; i < Hips.childCount; i++)
                {
                    if (uppLeg) break;
                    Vector3 loc = BaseTransform.InverseTransformPoint(Hips.GetChild(i).position);
                    if (loc.x > pelvisLocation.x) if (loc.y < pelvisLocation.y) uppLeg = Hips.GetChild(i);
                }

                rightLeg.BoneStart = uppLeg;
            }

        }




        private Transform Finding_FindUpperLeg(Transform foot)
        {
            Transform upLeg = null;

            if (Hips)
            {
#if UNITY_EDITOR
                if (FGUI_Finders.IsChildOf(foot, Hips)) // Correct parenting check
                {
                    Transform prePelvis = foot;
                    while (prePelvis.parent != Hips && prePelvis != null) prePelvis = prePelvis.parent;
                    upLeg = prePelvis;
                }
#endif
            }
            else
            {
                // Upper leg must be child of transform which have more than two children
                // Shoulder must be child of transform which have more than two children
                Transform pelvis = null;
                Transform pelvisCheck = foot.parent;

                if (pelvisCheck)
                    if (pelvisCheck.parent)
                    {
                        pelvisCheck = pelvisCheck.parent;
                        if (pelvisCheck)
                        {
                            if (pelvisCheck.childCount > 2) pelvis = pelvisCheck;
                            if (!pelvis) if (pelvisCheck.parent)
                                {
                                    pelvisCheck = pelvisCheck.parent;
                                    if (pelvisCheck.childCount > 2) pelvis = pelvisCheck;
                                    if (!pelvis) if (pelvisCheck.parent)
                                        {
                                            pelvisCheck = pelvisCheck.parent;
                                            if (pelvisCheck.childCount > 2) pelvis = pelvisCheck;
                                            if (!pelvis) if (pelvisCheck.parent)
                                                {
                                                    pelvisCheck = pelvisCheck.parent;
                                                    if (pelvisCheck.childCount > 2) pelvis = pelvisCheck;
                                                }
                                        }
                                }
                        }
                    }

                upLeg = pelvisCheck;
                if (pelvis)
                {
                    Hips = pelvis;
                }
            }

            return upLeg;
        }


        private void Finding_FindLowerLegsWithUpper()
        {
            Finder_EnsureLegsCount(2);
            var leftLeg = Legs[0];
            var rightLeg = Legs[1];
            if (Legs[0].Side == ELegSide.Right) { leftLeg = Legs[1]; rightLeg = Legs[0]; }

            if (leftLeg.BoneStart != null)
            {
                if (leftLeg.BoneStart.childCount > 0) if (!leftLeg.BoneMid) leftLeg.BoneMid = Finders_GetRelevantChildOf(leftLeg.BoneStart);
                if (leftLeg.BoneMid) if (leftLeg.BoneMid.childCount > 0) if (leftLeg.BoneEnd == null) leftLeg.BoneEnd = Finders_GetRelevantChildOf(leftLeg.BoneMid);
            }

            if (rightLeg.BoneStart != null)
            {
                if (rightLeg.BoneStart.childCount > 0) if (!rightLeg.BoneMid) rightLeg.BoneMid = Finders_GetRelevantChildOf(rightLeg.BoneStart);
                if (rightLeg.BoneMid) if (rightLeg.BoneMid.childCount > 0) if (rightLeg.BoneEnd == null) rightLeg.BoneEnd = Finders_GetRelevantChildOf(rightLeg.BoneMid);
            }
        }

        public Transform Finders_GetRelevantChildOf(Transform parent)
        {
            Transform child = null;

            for (int c = 0; c < parent.childCount; c++)
            {
                Transform currentChild = parent.GetChild(c);

                if (child == null) child = currentChild;
                else
                {
                    // Child transform which has most next child transforms
                    if (currentChild.childCount > child.childCount) child = currentChild;
                }
            }

            return child;
        }


        protected static bool? Finders_IsRightOrLeft(Transform child, Transform itsRoot)
        {
            Vector3 transformed = itsRoot.InverseTransformPoint(child.position);
            if (transformed.x < 0f) return false;
            else
            if (transformed.x > 0f) return true;
            return null;
        }


        public void Finders_RefreshAllLegsAnkleAxes()
        {
            for (int l = 0; l < Legs.Count; l++)
            {
                var leg = Legs[l];
                leg.RefreshLegAnkleToHeelAndFeetAndAxes(BaseTransform);
            }
        }


    }

}