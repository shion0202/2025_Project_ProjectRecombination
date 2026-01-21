using _Test.Skills;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmonShield : MonoBehaviour, IDamagable
{
    public SoulAbsrption skillData;
    
    private float currentHealth;

    private void Start()
    {
        currentHealth = skillData.shieldHealth;
        skillData.isShieldActive = true;
    }
    
    private void Update()
    {
        if (currentHealth <= 0)
        {
            skillData.isShieldActive = false;
        }
    }
    
    public void TakeDamage(int damage)
    {
        Debug.Log($"AmonShield took {damage} damage.");
    }

    public void ApplyDamage(float inDamage, LayerMask targetMask = default, float unitOfTime = 1, float defenceIgnoreRate = 0)
    {
        TakeDamage((int)(inDamage * skillData.damageReductionRatio));
    }
}
