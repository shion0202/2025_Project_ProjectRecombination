using System.Collections.Generic;
using Monster.AI.Command;
using Monster;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTActionIdle", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Actions/BTActionIdle")]
    public class BTActionIdle : BTAction
    {
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            
            // Idle 명령을 행동 대기열에 추가
            context.Enqueue(new IdleCommand(), priority);
            
            state = NodeState.Success;
            return state;
        }
    }
}