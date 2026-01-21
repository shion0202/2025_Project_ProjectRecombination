using System.Collections.Generic;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTActionChangeState", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Actions/BTActionChangeState")]
    public class BTActionChangeState : BTAction
    {
        public string newState;
        
        
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            
            Blackboard.Blackboard blackboard = context.Blackboard;
            if (blackboard == null)
            {
                Debug.LogError("Blackboard is null in BTActionChangeState.");
                return NodeState.Failure;
            }
            
            blackboard.State.ClearStates();
            blackboard.State.AddState(newState);
            return state = NodeState.Success;
        }
    }
}