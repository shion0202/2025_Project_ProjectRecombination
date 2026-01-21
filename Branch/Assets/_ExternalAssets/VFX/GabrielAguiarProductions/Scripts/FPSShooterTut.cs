using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSShooterTut : MonoBehaviour
{
    public Camera cam;
    public GameObject projectile;
    public GameObject muzzle;
    public Transform firePoint;
    public float projectileSpeed = 30;
    public float fireRate = 4;
    public float arcRange = 1;

    private Vector3 destination;
    private float timeToFire;

    void Update()
    {
        if(Input.GetButton("Fire1") && Time.time >= timeToFire)
        {
            timeToFire = Time.time + 1/fireRate;
            ShootProjectile();
        }
    }

    void ShootProjectile()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        destination = ray.GetPoint(1000);
        InstantiateProjectile(firePoint);        
    }

    void InstantiateProjectile(Transform firePoint)
    {
        var projectileObj = Instantiate (projectile, firePoint.position, Quaternion.identity) as GameObject;
        projectileObj.GetComponent<Rigidbody>().velocity = (destination - firePoint.position).normalized * projectileSpeed;

        iTween.PunchPosition(projectileObj, new Vector3 (Random.Range(-arcRange, arcRange), Random.Range(-arcRange, arcRange), 0), Random.Range(0.5f, 2));

        var muzzleObj = Instantiate (muzzle, firePoint.position, Quaternion.identity) as GameObject;
        Destroy (muzzleObj, 2);
    }
}
