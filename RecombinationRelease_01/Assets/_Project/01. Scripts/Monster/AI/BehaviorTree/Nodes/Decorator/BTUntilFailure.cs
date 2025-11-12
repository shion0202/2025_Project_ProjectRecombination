using System.Collections.Generic;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    // 자식 노드의 실행 결과가 실패가 발생할 때까지 반복 실행
    [CreateAssetMenu(fileName = "BTUntilFailure", menuName = "AI/BehaviorTree/Nodes/Decorator/BTUntilFailure")]
    public class BTUntilFailure : BTDecorator
    {
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            
            var nodeState = child.Evaluate(context, visited);

            if (nodeState == NodeState.Failure)
                return state = nodeState;

            return state = NodeState.Running;
        }
    }
}