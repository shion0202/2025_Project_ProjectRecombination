using System.Collections.Generic;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    // 여러 자식을 동시에 실행
    // 성공 여부는 별도로 지정
    [CreateAssetMenu(fileName = "BTParallel", menuName = "AI/BehaviorTree/Nodes/Composite/BTParallel")]
    public class BTParallel : BTComposite
    {
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            // 모든 자식 노드를 실행만 함
            foreach (var child in children)
            {
                child.Evaluate(context,  visited);
            }
            
            // 실행 즉시 성공 반환
            return state = NodeState.Success;
        }
    }
}