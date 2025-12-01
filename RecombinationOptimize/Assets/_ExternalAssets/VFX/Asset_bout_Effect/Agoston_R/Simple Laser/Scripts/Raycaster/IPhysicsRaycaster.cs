using UnityEngine;

namespace Assets.Agoston_R.Simple_Laser.Scripts.Raycaster
{
    public interface IPhysicsRaycaster
    {
        public bool Raycast(Transform transform, float maxDistance, LayerMask layerMask, out LaserHit laserHit);
    }
}
