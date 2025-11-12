using Managers;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTIsCooldown", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Condition/BTIsCooldown")]
    public class BTIsCooldown : BTCondition
    {
        public int skillId = 4001;

        protected override bool CheckCondition(NodeContext nodeContext)
        {
            RowData skillData = nodeContext.Blackboard.Skills[skillId];
            if (skillData != null)
                return nodeContext.Blackboard.IsSkillReady(skillId);    // 쿨타임이 적용된 스킬인지 확인

            Debug.LogError($"Skill not found in Blackboard for skill ID: {skillId}");
            return false;
        }
    }
}