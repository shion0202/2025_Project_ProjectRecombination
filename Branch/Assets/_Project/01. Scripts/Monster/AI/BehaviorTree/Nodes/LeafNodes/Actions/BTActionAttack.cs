using _Test.Skills;
using Monster.AI.Command;
using Managers;
using System.Collections.Generic;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTActionAttack", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Actions/BTActionAttack")]
    public class BTActionAttack : BTAction
    {
        public int skillId = 4001; // 스킬 ID, 예시로 1번 스킬 사용
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            
            // var skillData = DataManager.Instance.GetRowDataByIndex("MonsterSkill", skillId);
            Blackboard.Blackboard blackboard = context.Blackboard;
            // SkillData skillData = blackboard.HasSkill(skillId);
            if (blackboard.HasSkill(skillId))
            {
                Debug.LogError($"Skill data not found for skill ID: {skillId}");
                return NodeState.Failure;
            }
            
            // 공격 명령을 행동 대기열에 추가
            // CommandContext commandContext = new CommandContext( new AttackCommand(), skillData);
            context.Enqueue(new AttackCommand(context.Blackboard, skillId), priority);
            
            return state = NodeState.Success;
        }
    }
}