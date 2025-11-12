using System.Collections.Generic;
using Monster.AI.Command;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTActionPatrol", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Actions/BTActionPatrol")]
    public class BTActionPatrol : BTAction
    {
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            
            // patrol 명령을 행동 대기열에 추가
            context.Enqueue(new PatrolCommand(), priority);
            
            return state = NodeState.Running;
        }
    }
}