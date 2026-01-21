using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    // 단일 자식을 가지며, 행동을 수정하거나 조건을 부여한다.
    public abstract class BTDecorator : BTNode
    {
        [SerializeField] public BTNode child;

        public override void OnValidateNode()
        {
            if (child != null)
                child.input = this;
        }
    }
}
