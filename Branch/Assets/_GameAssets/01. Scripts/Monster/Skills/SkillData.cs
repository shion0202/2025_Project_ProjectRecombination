using Managers;
using Monster.AI.Blackboard;
using System;
using System.Collections;
using UnityEngine;

namespace _Test.Skills
{
    [Serializable]
    public abstract class SkillData : ScriptableObject
    {
        [Header("스킬 기본 정보")] 
        public int skillID;
        public string skillName;
        [TextArea] public string skillDescription;
        
        [Header("스킬 효과")] 
        public float damage;
        public float range;
        public float cooldown;
        public float castTime;
        public float animSpeed;

        public virtual IEnumerator Casting(Blackboard data)
        {
            Debug.Log($"skill {skillName} 캐스팅 시작: {castTime}초");
            yield return new WaitForSeconds(castTime);
            Debug.Log($"skill {skillName} 캐스팅 완료");
        }
        
        public abstract IEnumerator Activate(Blackboard data);
        
        private bool _isChasting = false;
        private float _chastingTime;
        private float _chastingStartTime;
        
        [ContextMenu("Set Parameters")]
        private void SetParameters()
        {
            RowData skillData = DataManager.Instance.SheetData.GetRow("MonsterSkill", skillID);
            skillName = skillData.GetStringStat(EStatType.Name);
            range = skillData.GetStat(EStatType.Range);
            damage = skillData.GetStat(EStatType.Damage);
            cooldown = skillData.GetStat(EStatType.CooldownTime);
            castTime = skillData.GetStat(EStatType.CastTime);
            animSpeed = skillData.GetStat(EStatType.AnimSpeed);
            skillDescription = skillData.GetStringStat(EStatType.Description);
        }
    }
}