using UnityEngine;

namespace Assets.Agoston_R.Simple_Laser.Scripts.Raycaster
{
    public class LaserHitTransformer
    {
        public LaserHit Transform(Vector3 origin, RaycastHit hit)
        {
            return new LaserHit(hit.transform, origin, hit.point, hit.normal);
        }

        public LaserHit Transform(Vector2 origin, RaycastHit2D hit)
        {
            return new LaserHit(hit.transform,origin, hit.point, hit.normal);
        }
    }
}
