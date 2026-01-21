using System.Collections.Generic;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    public class BTAction : BTNode
    {
        public int priority;
        // 이 클래스는 행동 노드의 기본 구현을 제공한다.
        // 행동 노드에 Evaluate 메서드가 구현될 필요가 있는지 의문이다.
        // > 규칙에 의해 사용하지 않아도 되지만 BTNode를 상속 받은 모든 하위 클래스틑 Evaluate 메서드를 구현해야 한다.
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            return CheckCycle(visited) ? NodeState.Failure : NodeState.Success;
        }
    }
}