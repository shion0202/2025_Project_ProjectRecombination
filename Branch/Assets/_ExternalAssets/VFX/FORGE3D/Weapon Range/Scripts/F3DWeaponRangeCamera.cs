using UnityEngine;
using System.Collections;

namespace Forge3D
{
    public class F3DWeaponRangeCamera : MonoBehaviour
    {
        public Camera MainCamera;
        public Transform WPHangar, WPHangarCentral, WPStart, WPEffects, WPEffectsCentral;

        float globalCamTranslationSpeed = 0f;
        float globalCamLookSpeed = 0f;

        
        public float mouseLimit = 0.5f;

        public float CamTranslationSpeed = 0f;
        public float CamLookSpeed = 0f;
        public float CamOrbitTranslationSpeed = 0.5f;
        public float CamOrbitLookSpeed = 0.5f;
        public float EffectsCameraZoomMin, EffectsCameraZoomMax;
        public float HangarCameraZoomMin, HangarCameraZoomMax;

        public float EffectsMouseYLimitLow, EffectsMouseYLimitHigh;
        public float HangarMouseYLimitLow, HangarMouseYLimitHigh;

        public enum WPCameraState
        {
            Idle,
            Entry,
            HangarOrbit,
            HangarEntry,
            Effects,
            EffectsEntry,
            EffectsOrbit
        }
        public enum CameraLerpType
        {
            EaseIn,
            EaseOut,
            SmooterSmooth
        }

        WPCameraState cameraState_ = WPCameraState.Idle;

        Vector3 lastCameraPos;
        //////////////////////////////
        float mouseX, mouseY;
        float mouseWheel;

        public WPCameraState cameraState
        {
            get { return cameraState_; }
            set
            {
                curOrbitTime = 0f;
                t = 0f;
                mouseX = mouseY = 0f;
               
                lastCameraPos = MainCamera.transform.position;

                if (value == WPCameraState.HangarOrbit)
                {
                    mouseWheel = HangarCameraZoomMax;
                    mouseLimitMin = HangarMouseYLimitLow;
                    mouseLimitMax = HangarMouseYLimitHigh;
                }
                else if (value == WPCameraState.EffectsOrbit)
                {
                    mouseWheel = EffectsCameraZoomMax;
                    mouseLimitMin = EffectsMouseYLimitLow;
                    mouseLimitMax = EffectsMouseYLimitHigh;
                }

                cameraState_ = value;
            }
        }


        void Awake()
        {
            mouseWheel = EffectsCameraZoomMax;
        }
        
        public void ResetCamera()
        {
            MainCamera.transform.position = WPStart.position;
            MainCamera.transform.rotation = Quaternion.LookRotation(WPEffectsCentral.position - MainCamera.transform.position, Vector3.up);
        }

        float mouseLimitMin, mouseLimitMax;
        Vector3 overlapOffset;
        bool overlapping = false;
        Vector3 camVelocity = Vector3.zero;

        void OrbitCamera(Vector3 origin, Vector3 forward, float zoomMin, float zoomMax, bool checkOverlap = false)
        {
            if (F3DWeaponRangeController.isObserverActive && Input.GetKey(KeyCode.Mouse1))
            {
                mouseX -= Mathf.Clamp(Input.GetAxis("Mouse X"), -mouseLimit, mouseLimit);
                mouseY += Mathf.Clamp(Input.GetAxis("Mouse Y"), -mouseLimit, mouseLimit);
            }

            mouseWheel -= Input.GetAxis("Mouse ScrollWheel") * 5;
            mouseWheel = Mathf.Clamp(mouseWheel, zoomMin, zoomMax);

            mouseY = Mathf.Clamp(mouseY, mouseLimitMin, mouseLimitMax);
            

            if (checkOverlap && Physics.CheckSphere(MainCamera.transform.position, 5f))
            //if (checkOverlap && Physics.Raycast(MainCamera.transform.position, MainCamera.transform.forward, 5f))
            {
                overlapping = true;
                // Debug.DrawLine(MainCamera.transform.position, MainCamera.transform.position + MainCamera.transform.forward * 5f, Color.red);
                // overlapOffset = Vector3.Lerp(overlapOffset, forward * 15f, Time.deltaTime * 0.5f);

                overlapOffset = Vector3.SmoothDamp(overlapOffset, forward * 15f, ref camVelocity, 1f);
                mouseWheel += overlapOffset.magnitude * Time.deltaTime * 2f;
            }
            else
            {
                overlapping = false;

                //overlapOffset = Vector3.Lerp(overlapOffset, Vector3.zero, Time.deltaTime * 0.1f);

                overlapOffset = Vector3.SmoothDamp(overlapOffset, Vector3.zero, ref camVelocity, 2f);
            }

            mouseWheel = Mathf.Clamp(mouseWheel, zoomMin, zoomMax);
            

            Vector3 camPos = forward * mouseWheel;
            camPos = Vector3.ClampMagnitude(camPos, 20f);

            camPos = Quaternion.AngleAxis(mouseX, Vector3.up) * camPos;
            Vector3 camCross = Vector3.Cross(camPos, Vector3.up);

            camPos = Quaternion.AngleAxis(mouseY, camCross) * camPos;
            
            MainCamera.transform.position = Vector3.Lerp(MainCamera.transform.position, origin + camPos, Time.deltaTime * globalCamTranslationSpeed);
            MainCamera.transform.rotation = Quaternion.Lerp(MainCamera.transform.rotation, Quaternion.LookRotation(origin - MainCamera.transform.position, Vector3.up), Time.deltaTime * globalCamLookSpeed);

            Debug.DrawLine(WPEffectsCentral.position, WPEffectsCentral.position + camPos);
        }
        //////////////////////////////

        float curOrbitTime = 0f;
        float curOrbitLerpTime = 6f;
        float t;

        void TickLerp(CameraLerpType type)
        {
            curOrbitTime += Time.deltaTime;
            if (curOrbitTime >= curOrbitLerpTime)
                curOrbitTime = curOrbitLerpTime;

            t = curOrbitTime / curOrbitLerpTime;

            switch (type)
            {
                case CameraLerpType.SmooterSmooth:
                    t = t * t * t * (t * (6f * t - 15f) + 10f);
                    break;
                case CameraLerpType.EaseIn:
                    t = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);                        
                    break;
                case CameraLerpType.EaseOut:
                    t = Mathf.Sin(t * Mathf.PI * 0.5f);
                    break;

                default:
                    break;
            }
        }

        void FixedUpdate()
        {

        }


        void OnDrawGizmos()
        {
            if (overlapping)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(MainCamera.transform.position, 5f);
            }
        }

        // Update is called once per frame
        void LateUpdate()
        {
            

                switch (cameraState_)
                {
                    // EffectsRoom looking at the door
                    case WPCameraState.HangarEntry:
                        {
                            curOrbitLerpTime = 2f;
                            TickLerp(CameraLerpType.SmooterSmooth);
                                                     

                            MainCamera.transform.position = Vector3.Lerp(lastCameraPos, WPEffects.position, t);
                            MainCamera.transform.rotation = Quaternion.Lerp(MainCamera.transform.rotation, Quaternion.LookRotation(WPHangar.forward, Vector3.up), t);
                        }
                        break;

                    // Hangar orbiting
                    case WPCameraState.HangarOrbit:
                        {
                            curOrbitLerpTime = 4f;
                            TickLerp(CameraLerpType.SmooterSmooth);

                            globalCamTranslationSpeed = Mathf.Lerp(0.5f, CamOrbitTranslationSpeed, t);
                            globalCamLookSpeed = Mathf.Lerp(0.5f, CamOrbitLookSpeed, t);

                          

                            OrbitCamera(WPHangarCentral.position, WPHangarCentral.forward, HangarCameraZoomMin, HangarCameraZoomMax, true);
                        }
                        break;

                    // Hangar going back to the effects room
                    case WPCameraState.EffectsEntry:
                        {
                            curOrbitLerpTime = 2f;
                            TickLerp(CameraLerpType.SmooterSmooth);


                            MainCamera.transform.position = Vector3.Lerp(lastCameraPos, WPHangar.position, t);
                            MainCamera.transform.rotation = Quaternion.Lerp(MainCamera.transform.rotation, Quaternion.LookRotation(WPEffects.forward, Vector3.up), t);
                        }
                        break;


                    // Camera orbiting the reactor core
                    case WPCameraState.EffectsOrbit:
                        {
                            curOrbitLerpTime = 2f;
                            TickLerp(CameraLerpType.SmooterSmooth);
                            globalCamTranslationSpeed = Mathf.Lerp(0.5f, CamOrbitTranslationSpeed, t);
                            globalCamLookSpeed = Mathf.Lerp(0.5f, CamOrbitLookSpeed, t);

                            OrbitCamera(WPEffectsCentral.position, WPEffectsCentral.forward, EffectsCameraZoomMin, EffectsCameraZoomMax);
                        }
                        break;

                    // Initial entry going through the door facing the reactor core
                    case WPCameraState.Entry:
                        {
                            curOrbitLerpTime = 5f;
                            TickLerp(CameraLerpType.SmooterSmooth);

                            MainCamera.transform.position = Vector3.Lerp(WPStart.position, WPEffects.position, t);
                            MainCamera.transform.rotation = Quaternion.Lerp(MainCamera.transform.rotation, Quaternion.LookRotation(WPEffectsCentral.position - MainCamera.transform.position, Vector3.up), t);
                        }
                        break;



                    case WPCameraState.Idle:
                    default:
                        break;
                }
            
        }
    }
}