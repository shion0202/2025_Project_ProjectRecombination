using UnityEngine;

namespace Assets.Agoston_R.Simple_Laser.Scripts.Raycaster
{
    public class PhysicsRaycaster3D : IPhysicsRaycaster
    {
        private readonly QueryTriggerInteraction queryTriggerInteraction;
        private readonly LaserHitTransformer laserHitTransformer = new LaserHitTransformer();

        public PhysicsRaycaster3D(QueryTriggerInteraction queryTriggerInteraction)
        {
            this.queryTriggerInteraction = queryTriggerInteraction;
        }

        public bool Raycast(Transform transform, float maxDistance, LayerMask layerMask, out LaserHit laserHit)
        {
            var origin = transform.position;
            var hitSuccessful = Physics.Raycast(origin, transform.forward, out var hit, maxDistance, layerMask, queryTriggerInteraction);
            laserHit = laserHitTransformer.Transform(origin, hit);
            return hitSuccessful;
        }
    }
}
