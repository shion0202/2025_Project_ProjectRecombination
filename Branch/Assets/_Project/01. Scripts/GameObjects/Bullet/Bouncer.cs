using Managers;
using Monster.AI.FSM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bouncer : Bullet
{
    [SerializeField] protected int maxBounces = 3;          // 충돌 최대 횟수
    private int _bounceCount = 0;

    protected override void ShootByPlayer(Collision collision)
    {
        _bounceCount++;

        CreateImpaceEffect(collision);
        TakeDamage(collision.transform);

        CheckBounceCount(collision);
    }

    protected override void ShootByEnemy(Collision collision)
    {
        _bounceCount++;

        CreateImpaceEffect(collision);
        TakeDamage(collision.transform);

        CheckBounceCount(collision);
    }

    protected override void ImpactObstacle(Collision collision)
    {
        _bounceCount++;

        CreateImpaceEffect(collision);
        CheckBounceCount(collision);
    }

    protected bool CheckBounceCount(Collision collision)
    {
        if (_bounceCount >= maxBounces)
        {
            // 충돌 횟수 초과 시 투사체 파괴
            _bounceCount = 0;
            DestroyBullet(collision);
            return true;
        }

        return false;
    }

    public override void ResetBullet()
    {
        base.ResetBullet();

        _bounceCount = 0;
    }

    public override string ToString()
    {
        string baseLog = base.ToString();
        string log = $"{baseLog}\n" + $"Max Bounces: {maxBounces}, Current Bounces: {_bounceCount}";
        return log;
    }
}
