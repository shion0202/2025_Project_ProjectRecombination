using UnityEngine;
using System.Collections;

namespace Forge3D
{
    public class F3DWeaponRangeController : MonoBehaviour
    {
        public F3DWeaponRangeCamera wpCamera;

        public Animator ShipAnimator;

        public Transform PassageDoorA, PassageDoorB;
        public Vector3 PassageDoorA_Offset, PassageDoorB_Offset;
        
        public F3DWeaponRangeLightController[] LightControllers;

        public static bool isObserverActive = false;

      

        public Transform KeyMap, KeyMapPanel;

        public AudioSource ShipEngine;

        

        // Use this for initialization
        void Awake()
        {
         //   ShipAnimator.Stop();
        }

        void Start()
        {
            
            StartCoroutine("PlaySequence");
        }

        IEnumerator PlaySequence()
        {
            wpCamera.ResetCamera();
            ResetLightControllers();

            //////////////////////////////
            // LIGHT CONTROLLERS IDs
            //////////////////////////////
            // 0 - Effects Chamber Celling
            // 1 - Hangar Celling 
            // 2 - Hangar Walls
            // 3 - Passage Room 
            //////////////////////////////

            StartCoroutine(SwitchLight(3, true, 2f));
            StartCoroutine(SwitchLight(0, true, 5f));

            
            StartCoroutine(SetDoor(true, 3f));
            StartCoroutine(SetDoor(false, 4f));

            StartCoroutine(SetCameraState(F3DWeaponRangeCamera.WPCameraState.Entry, 0f));
            StartCoroutine(SetCameraState(F3DWeaponRangeCamera.WPCameraState.EffectsOrbit, 5f));

            StartCoroutine(SetObserverControl(true, 5f));
           

            yield return null;
        }
        
        
        IEnumerator SetDoor(bool state, float delay)
        {
            
            yield return new WaitForSeconds(delay);

            if (state)
            {
                for (float i = 0; i <= 1.0f; i += 0.025f)
                {
                    PassageDoorA.localPosition = PassageDoorA_Offset * i;
                    PassageDoorB.localPosition = PassageDoorB_Offset * i;
                    yield return new WaitForSeconds(0.01f);
                }
             
            }
        
            else
            {
                for (float i = 1.0f; i >= 0.0f; i -= 0.025f)
                {
                    PassageDoorA.localPosition = PassageDoorA_Offset*i;
                    PassageDoorB.localPosition = PassageDoorB_Offset*i;
                    yield return new WaitForSeconds(0.01f);
                  }
            }            
        }

        void ResetLightControllers()
        {
            for (int i = 0; i < LightControllers.Length; i++)
            {
                LightControllers[i].SwitchLights(false);
            }
        }

        IEnumerator SwitchLight(int index, bool state, float delay)
        {
            yield return new WaitForSeconds(delay);

            foreach (F3DWeaponRangeLightController lc in LightControllers)            
                if (lc.ControllerID == index)
                {
                    lc.SwitchLights(state);
                    break;
                }            
        }

        IEnumerator SetCameraState(F3DWeaponRangeCamera.WPCameraState state, float delay)
        {
            yield return new WaitForSeconds(delay);
            wpCamera.cameraState = state;
        }

        IEnumerator SetObserverControl(bool state, float delay)
        {
            yield return new WaitForSeconds(delay);
            isObserverActive = state;
        }

        void StartShipAnimation()
        {
            // AnimatorTransitionInfo atInf = ShipAnimator.GetAnimatorTransitionInfo(0);

            //ShipAnimator.SetTrigger(HoveringHash);
            //  Debug.Log("Start animation");
            //   ShipAnimator.Play(DockHash, 0);
            ShipAnimator.SetBool("Dock", true);
        }

        float shipEngineHumMaxDistance = 40;

        void Update()
        {
            ShipEngine.maxDistance = Mathf.Lerp(ShipEngine.maxDistance, shipEngineHumMaxDistance, Time.deltaTime);

            if(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                KeyMap.gameObject.SetActive(true);
                KeyMapPanel.gameObject.SetActive(true);
            }
            else
            {
                KeyMap.gameObject.SetActive(false);
                KeyMapPanel.gameObject.SetActive(false);
            }

            if (isObserverActive)
            {
                if (wpCamera.cameraState != F3DWeaponRangeCamera.WPCameraState.EffectsOrbit && Input.GetKeyDown(KeyCode.Alpha1))
                {
                    shipEngineHumMaxDistance = 40f;
                    isObserverActive = false;
                    StartCoroutine(SetObserverControl(true, 3f));

                //    StopCoroutine("SetDoor");
                    StartCoroutine(SetDoor(true, 0f));
                    StartCoroutine(SetDoor(false, 3f));

                    StartCoroutine(SwitchLight(1, false, 0.5f));
                    StartCoroutine(SwitchLight(2, false, 1f));

                    StartCoroutine(SwitchLight(0, true, 3f));


                    wpCamera.cameraState = F3DWeaponRangeCamera.WPCameraState.EffectsEntry;                                        
                    StartCoroutine(SetCameraState(F3DWeaponRangeCamera.WPCameraState.EffectsOrbit, 1.7f));
                }
                else if (wpCamera.cameraState != F3DWeaponRangeCamera.WPCameraState.HangarOrbit && Input.GetKeyDown(KeyCode.Alpha2))
                {
                    shipEngineHumMaxDistance = 90f;


                    StartShipAnimation();
                    isObserverActive = false;
                    StartCoroutine(SetObserverControl(true, 3f));

                    StartCoroutine(SwitchLight(0, false, 0.5f));
                    

                    StartCoroutine(SwitchLight(1, true, 4f));
                    StartCoroutine(SwitchLight(2, true, 5f));

                  //  StopCoroutine("SetDoor");
                    StartCoroutine(SetDoor(true, 0f));
                    StartCoroutine(SetDoor(false, 3f));

                    wpCamera.cameraState = F3DWeaponRangeCamera.WPCameraState.HangarEntry;                    
                    StartCoroutine(SetCameraState(F3DWeaponRangeCamera.WPCameraState.HangarOrbit, 1.7f));
                }
            }
        }

    }
}