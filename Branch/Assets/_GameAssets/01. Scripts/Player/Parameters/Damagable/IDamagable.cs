using UnityEngine;

public interface IDamagable
{
    public void ApplyDamage(float inDamage, LayerMask targetMask = default, float unitOfTime = 1.0f, float defenceIgnoreRate = 0.0f);
}
