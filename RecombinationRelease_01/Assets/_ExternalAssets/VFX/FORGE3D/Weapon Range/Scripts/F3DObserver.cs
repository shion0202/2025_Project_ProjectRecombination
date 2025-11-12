using UnityEngine;
using System.Collections;

namespace Forge3D
{
    public class F3DObserver : MonoBehaviour
    {
        public Transform ShieldFlashLight;
        public Transform ShieldHit;

        public Transform[] ShieldHitFX;

        float ReactSpeed;
        float curTime;
        // Update is called once per frame
        void Update()
        {
            
            // Activate on Left mouse button
            if (F3DWeaponRangeController.isObserverActive && Input.GetMouseButton(0))
            {
                // Returns a ray going from camera through a screen point
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                // Casts a ray against colliders in the scene
                if (Physics.Raycast(ray, out hit, 500.0f))
                {
                    // Get Forcefield script component
                    Forcefield ffHit = hit.transform.GetComponentInParent<Forcefield>();

                    // Generate random hit power value and call Force Field script if successful
                    if (ffHit != null)
                    {
                        float hitPower = Random.Range(-2f, 2f);
                        ffHit.OnHit(hit.point, hitPower);
                        ReactSpeed = ffHit.ReactSpeed;
                        if (curTime >= ReactSpeed)
                        {
                            Instantiate(ShieldFlashLight, hit.point + hit.normal , Quaternion.identity);
                            Instantiate(ShieldHit, hit.point + hit.normal * 0.5f, Quaternion.identity);

                            if (ShieldHitFX != null)
                            {                                
                               AudioSource temp =  ((Transform)Instantiate(ShieldHitFX[Random.Range(0, ShieldHitFX.Length)], hit.point, Quaternion.identity)).GetComponent<AudioSource>();
                                temp.volume -= Random.Range(0f, 0.25f);
                                temp.pitch -= Random.Range(-0.03f, 0.03f);
                                temp.Play();
                            }

                            curTime = 0f;
                        }
                    }
                }
            }

            curTime += Time.deltaTime;
        }
    }
}