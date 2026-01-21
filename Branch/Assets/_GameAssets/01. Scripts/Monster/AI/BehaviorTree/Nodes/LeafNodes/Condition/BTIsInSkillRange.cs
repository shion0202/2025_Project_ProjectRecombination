using _Test.Skills;
using Managers;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTIsInSkillRange", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Condition/BTIsInSkillRange")]
    public class BTIsInSkillRange : BTCondition
    {
        public int skillId = 4001;

        protected override bool CheckCondition(NodeContext nodeContext)
        {
            // 스킬 ID로 사용 가능한 스킬인지 판단
            // RowData skillData = nodeContext.Blackboard.Skills[skillId];
            if (!nodeContext.Blackboard.HasSkill(skillId))
            {
                Debug.LogError($"Skill not found in Blackboard for skill ID: {skillId}");
                return false;
            }
            
            SkillData skillData = nodeContext.Blackboard.GetSkillData(skillId);
            float skillRange = skillData.range;
            
            // 타겟과의 거리 계산
            Vector3 targetPosition = nodeContext.Blackboard.Target.transform.position;
            Vector3 selfPosition = nodeContext.Blackboard.Agent.transform.position;
            float distanceToTarget = Vector3.Distance(selfPosition, targetPosition);
            
            // 스킬 범위 내에 있는지 확인
            return distanceToTarget <= skillRange;
        }
    }
}