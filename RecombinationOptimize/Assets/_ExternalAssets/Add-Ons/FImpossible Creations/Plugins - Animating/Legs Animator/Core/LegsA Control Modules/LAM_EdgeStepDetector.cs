#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    //[CreateAssetMenu]
    public class LAM_EdgeStepDetector : LegsAnimatorControlModuleBase
    {
        LegsAnimator.Variable iterationsV;
        float initTime;

        public override void OnInit( LegsAnimator.LegsAnimatorCustomModuleHelper helper )
        {
            initTime = Time.time;
            iterationsV = helper.RequestVariable( "Iterations", 5 );
        }
        public override void OnReInitialize( LegsAnimator.LegsAnimatorCustomModuleHelper helper )
        {
            initTime = Time.time;
        }

        public override void Leg_LatePreRaycastingUpdate( LegsAnimator.LegsAnimatorCustomModuleHelper helper, LegsAnimator.Leg leg )
        {
            if( Time.time - initTime < 0.1f ) return; // Don't calculate for a short time after init to let character be grounded
            if( leg.User_RaycastHittedSource ) { leg.User_RestoreRaycasting(); return; } // Hitted - no need to find edge

            // Calculating box for boxcast from hips towards leg to find any ground
            Vector3 start = LegsAnim.ToRootLocalSpace( leg.ParentHub.LastKeyframePosition );
            Vector3 end = LegsAnim.ToRootLocalSpace( leg.lastRaycastingOrigin );

            start.y = end.y; // Same height origin as casting origin
            start.z = end.z; // Same front / back position

            RaycastHit hit = new RaycastHit();
            float castLength = Vector3.Distance( leg.lastRaycastingOrigin, leg.lastRaycastingEndPoint );

            #region Commented but maybe for future use

            // Box cast is not a good choice
            //start = LegsAnim.RootToWorldSpace( start );
            //end = LegsAnim.RootToWorldSpace( end );
            //Vector3 diff = end - start;
            //float ext = diff.magnitude / 2f;
            //Quaternion towards = Quaternion.LookRotation( diff, leg.Owner.Up );
            //Vector3 mid = Vector3.LerpUnclamped( start, end, 0.5f );

            //if( Physics.BoxCast( mid, new Vector3( leg.Owner.ScaleReference * 0.1f, 0.01f, ext ), -LegsAnim.Up, out hit, towards, castLength, LegsAnim.GroundMask, QueryTriggerInteraction.Ignore ) )
            //{
            //    UnityEngine.Debug.DrawRay( hit.point, hit.normal, Color.green, 1.01f );
            //}

            #endregion

            float iterations = (float)iterationsV.GetInt();

            for( float i = 1f; i <= iterations; i += 1 )
            {
                Vector3 pos = Vector3.LerpUnclamped( end, start, 0.1f + ( i / iterations ) );
                pos = LegsAnim.RootToWorldSpace( pos );

                if( Physics.Raycast( pos, -LegsAnim.Up, out hit, castLength * 1.01f, LegsAnim.GroundMask, QueryTriggerInteraction.Ignore ) )
                {
                    break;
                }
            }

            if( hit.transform == null ) { leg.User_RestoreRaycasting(); return; }

            //UnityEngine.Debug.DrawRay( hit.point, Vector3.down, Color.green, 1.01f );
            leg.User_OverrideRaycastHit( hit, false );
        }


        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_InspectorGUI( LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper )
        {
            EditorGUILayout.HelpBox( "Raycasting ground from leg towards hips position when not found ground below default leg position", UnityEditor.MessageType.None );

            if( legsAnimator.ZeroStepsOnNoRaycast )
            {
                GUILayout.Space( 4 );
                EditorGUILayout.HelpBox( "You're using Zero Steps On No Raycast, disable it to make Edge Detector work", UnityEditor.MessageType.Warning );
                var clickRect = GUILayoutUtility.GetLastRect();
                if( GUI.Button( clickRect, GUIContent.none, EditorStyles.label ) ) { legsAnimator.ZeroStepsOnNoRaycast = false; UnityEditor.EditorUtility.SetDirty( legsAnimator ); }
                GUILayout.Space( 4 );
            }

            LegsAnimator.Variable iterations = helper.RequestVariable( "Iterations", 5 );
            iterations.SetMinMaxSlider( 2, 6 );
            iterations.AssignTooltip( "How many raycasts from leg end towards hips should be casted to find ground in between" );
            iterations.Editor_DisplayVariableGUI();
        }

        public override void Editor_OnSceneGUI( LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper )
        {
            if( LegsAnim.LegsInitialized == false ) return;

            #region Commented but maybe for future use

            //foreach( var leg in legsAnimator.Legs )
            //{
            //    Vector3 start = LegsAnim.ToRootLocalSpace( leg.ParentHub.LastKeyframePosition );
            //    Vector3 end = LegsAnim.ToRootLocalSpace( leg.lastRaycastingOrigin );

            //    start.y = end.y; // Same height origin as casting origin
            //    start.z = end.z; // Same front / back position

            //    start = LegsAnim.RootToWorldSpace( start );
            //    end = LegsAnim.RootToWorldSpace( end );

            //    Vector3 diff = end - start;
            //    float ext = diff.magnitude / 2f;
            //    Quaternion towards = Quaternion.LookRotation( diff, leg.Owner.Up );
            //    Vector3 mid = Vector3.LerpUnclamped( start, end, 0.5f );

            //    Matrix4x4 rotMx = Matrix4x4.TRS( mid, towards, new Vector3( legsAnimator.ScaleReference * 0.1f, 0.01f, ext ) );

            //    Handles.matrix = rotMx;
            //    Handles.DrawWireCube( Vector3.zero, Vector3.one );
            //}

            #endregion
        }

#endif
        #endregion

    }
}