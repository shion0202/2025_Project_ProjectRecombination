using System.Collections.Generic;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    // 자식 노드의 실행 결과를 반대로 뒤집는다.
    [CreateAssetMenu(fileName = "BTInverter", menuName = "AI/BehaviorTree/Nodes/Decorator/BTInverter")]
    public class BTInverter : BTDecorator
    {
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            
            var nodeState = child.Evaluate(context, visited);
            
            if (nodeState == NodeState.Failure)
                state = NodeState.Success;
            else if (nodeState == NodeState.Success)
                state = NodeState.Failure;
            else
                state = NodeState.Running;
            
            return state;
        }
    }
}