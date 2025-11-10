using _Project._01._Scripts.Monster;
using Monster.AI.FSM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 코루틴으로 일정 시간동안 스탯 디버프 주기
public class ExplosionEMP : ExplosionMissile
{
    [SerializeField] private float deceleration = 0.5f;
    private PlayerController _player = null;

    protected void ApplyDeceleration(Transform target)
    {
        PlayerController player = target.GetComponent<PlayerController>();
        if (player)
        {
            _player = player;
            player.Stats.AddModifier(new StatModifier(EStatType.WalkSpeed, EStackType.PercentMul, -deceleration, this));
        }

        // 몬스터 디버프 적용 필요
    }

    protected override void DestroyExplosion()
    {
        if (_player)
        {
            _player.Stats.RemoveModifier(this);
        }

        // 몬스터 디버프 적용 필요

        Utils.Destroy(gameObject);
    }

    protected override void TakeDamage(Transform target, float coefficient = 1.0f)
    {
        IDamagable enemy = target.GetComponent<IDamagable>();
        if (enemy != null)
        {
            Transform otherParent = target.transform;
            if (_damagedTargets.Contains(otherParent)) return;
            _damagedTargets.Add(otherParent);

            enemy.ApplyDamage(_damage * coefficient, targetMask);
            ApplyDeceleration(target);

            Managers.GUIManager.Instance.StartHitCrosshair();
        }
        else
        {
            enemy = target.transform.GetComponentInParent<IDamagable>();
            if (enemy != null)
            {
                Transform otherParent = target.transform.GetComponentInParent<FSM>().transform;
                if (_damagedTargets.Contains(otherParent)) return;
                _damagedTargets.Add(otherParent);

                enemy.ApplyDamage(_damage * coefficient, targetMask);
                ApplyDeceleration(target);

                Managers.GUIManager.Instance.StartHitCrosshair();
            }
        }
    }
}
