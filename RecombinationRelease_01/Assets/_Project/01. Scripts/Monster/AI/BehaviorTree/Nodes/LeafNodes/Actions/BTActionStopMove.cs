using Monster.AI.Command;
using System.Collections.Generic;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTActionStopMove", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Actions/BTActionStopMove")]
    public class BTActionStopMove : BTAction
    {
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            
            // patrol 명령을 행동 대기열에 추가
            // context.Enqueue(new PatrolCommand());
            // NavMeshAgent를 사용하여 이동을 중지
            if (context.Blackboard.NavMeshAgent == null) return state = NodeState.Failure;
            context.Blackboard.NavMeshAgent.isStopped = true;
            
            return state = NodeState.Success;
        }
    }
}