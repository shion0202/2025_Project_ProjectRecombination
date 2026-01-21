using Monster.AI.Command;
using System.Collections.Generic;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTActionEvasion", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Actions/BTActionEvasion")]
    public class BTActionEvasion : BTAction
    {
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            
            // Evasion 행동은 몬스터가 회피 동작을 수행하는 행동이다.
            // 이 행동은 일반적으로 플레이어의 공격을 피하거나 위험한 상황에서 벗어날 때 사용
            // var blackboard = context.Blackboard;

            // Evasion 명령을 행동 대기열에 추가
            context.Enqueue(new EvasionCommand(), priority);
            
            state = NodeState.Success;
            return state;
        }
    }
}