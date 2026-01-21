using System.Collections.Generic;
using Monster.AI.Command;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTActionWander", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Actions/BTActionWander")]
    public class BTActionWander : BTAction
    {
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            
            // Wander 명령을 행동 대기열에 추가
            context.Enqueue(new WanderCommand(), priority);
            
            return state = NodeState.Success;
        }
    }
}