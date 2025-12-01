using Managers;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Monster.AI.Command
{
    [Serializable]
    public class MonsterAttack
    {
        public GameObject[] bulletPrefab;
        public Transform firePoint;

        public void Fire(int bulletType, GameObject shooter, Vector3 start, Vector3 end, Vector3 direction, float damage)
        {
            if (bulletPrefab == null || firePoint is null || bulletType < 0 || bulletType >= bulletPrefab.Length)
            {
                Debug.LogWarning("Bullet prefab or fire point is not assigned.");
                return;
            }
            // GameObject bullet = Object.Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            if (bulletPrefab[bulletType] is null)
            {
                Debug.LogWarning("Bullet prefab is not assigned.");
                return;
            }
            
            GameObject bullet = PoolManager.Instance.GetObject(bulletPrefab[bulletType], firePoint.position, Quaternion.LookRotation(direction));
            // bullet.transform.LookAt(direction * -1);
            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            if (bulletComponent != null)
            {
                bulletComponent.Init(shooter, null, start, end, direction, damage);
            }
            else
            {
                Debug.LogWarning("The instantiated bullet does not have a Bullet component.");
            }
        }
    }
}