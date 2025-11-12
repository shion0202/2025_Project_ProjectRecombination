using UnityEngine;
using System.Collections;

namespace Forge3D
{
    public class SimpleGun : MonoBehaviour
    {

        // Update is called once per frame
        void Update()
        {
            // Activate on Left mouse button
            if (Input.GetMouseButton(0))
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
                    }
                }
            }
        }
    }
}