using System.Collections.Generic;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    public abstract class BTCondition : BTNode
    {
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            
            return CheckCondition(context) ? state = NodeState.Success : state = NodeState.Failure;
        }

        protected abstract bool CheckCondition(NodeContext context);
    }
}