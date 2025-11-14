using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace  Monster.AI.BehaviorTree.Nodes
{
    // 여러 자식 노드를 가질 수 있는 노드의 모체
    public class BTComposite : BTNode
    {
        [SerializeField] public List<BTNode> children = new();

        public override void OnValidateNode()
        {
            foreach (var child in children)
                child.input = this;
        }
        
        // 이 클래스는 행동 노드의 기본 구현을 제공한다.
        // 행동 노드에 Evaluate 메서드가 구현될 필요가 있는지 의문이다.
        // > 규칙에 의해 사용하지 않아도 되지만 BTNode를 상속 받은 모든 하위 클래스틑 Evaluate 메서드를 구현해야 한다.
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            // This method should be overridden in derived classes
            Debug.LogError("Evaluate method not implemented in BTComposite");
            return NodeState.Failure;
        }
    }
}

