using UnityEngine;

namespace Assets.Agoston_R.Simple_Laser.Scripts.Raycaster
{
    public class PhysicsRaycaster2D : IPhysicsRaycaster
    {
        private readonly LaserHitTransformer laserHitTransformer = new LaserHitTransformer();
        private readonly float minDepth;
        private readonly float maxDepth;

        public PhysicsRaycaster2D(float minDepth, float maxDepth)
        {
            this.minDepth = minDepth;
            this.maxDepth = maxDepth;
        }

        public bool Raycast(Transform transform, float maxDistance, LayerMask layerMask, out LaserHit laserHit)
        {
            var origin = transform.position;
            var hit = Physics2D.Raycast(origin, transform.right, maxDistance, layerMask, minDepth, maxDepth);
            laserHit = laserHitTransformer.Transform(origin, hit);
            return hit.collider != null;
        }
    }
}
