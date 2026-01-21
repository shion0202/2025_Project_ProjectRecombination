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
            // RowData skillData = nodeContext.Blackboard.HasSkill(skillId);
            if (nodeContext.Blackboard.HasSkill(skillId))
            {
                // return nodeContext.Blackboard.IsSkillReady(skillId);    // 쿨타임이 적용된 스킬인지 확인
                // TODO: 스킬이 쿨타임이 다 되었는지 확인하는 로직 필요 (Skill 로직 변경으로 인해 주석 처리함)
                return true;
            }

            Debug.LogError($"Skill not found in Blackboard for skill ID: {skillId}");
            return false;
        }
    }
}