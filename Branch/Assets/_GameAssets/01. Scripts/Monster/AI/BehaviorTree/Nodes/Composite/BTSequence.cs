using System.Collections.Generic;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    // 모든 자식 노드를 순차적으로 실행하고,
    // 모든 자식 노드가 성공하면 성공 상태를 반환한다.
    // 하나라도 실패하면 실패 상태를 반환한다.
    // 만약 자식 노드 중 하나가 실행 중인 상태라면, 그 상태를 유지한다.
    // 이 노드는 일반적으로 여러 행동을 순차적으로 실행할 때 사용한다.
    [CreateAssetMenu(fileName = "BTSequence", menuName = "AI/BehaviorTree/Nodes/Composite/BTSequence")]
    public class BTSequence: BTComposite
    {
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            
            foreach (var child in children)
            {
                var childState = child.Evaluate(context, visited);
                if (childState == NodeState.Failure)
                {
                    state = NodeState.Failure;
                    return state;
                }
                if (childState == NodeState.Running)
                {
                    state = NodeState.Running;
                    return state;
                }
            }
            state = NodeState.Success;
            return state;
        }
    }
}