using System.Collections.Generic;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    // 노드가 성공 상태가 될 때까지 실행
    [CreateAssetMenu(fileName = "BTUntilSuccess", menuName = "AI/BehaviorTree/Nodes/Decorator/BTUntilSuccess")]
    public class BTUntilSuccess : BTDecorator
    {
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            
            var nodeState = child.Evaluate(context, visited);

            if (nodeState == NodeState.Success)
                return state = nodeState;

            return state = NodeState.Running;
        }
    }
}