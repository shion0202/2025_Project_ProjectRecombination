using UnityEngine;

public struct LaserHit
{
    public Vector3 Origin { get; private set; }
    public Vector3 HitPoint { get; private set; }

    public Vector3 Normal { get; private set; }

    public LaserHit(Vector3 origin, Vector3 hitPoint, Vector3 normal)
    {
        this.Origin = origin;
        this.HitPoint = hitPoint;
        this.Normal = normal.normalized;
    }
}
