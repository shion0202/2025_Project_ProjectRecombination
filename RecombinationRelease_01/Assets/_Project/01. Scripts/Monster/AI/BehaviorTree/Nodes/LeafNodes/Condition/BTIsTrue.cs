using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTIsTrue", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Condition/BTIsTrue")]
    public class BTIsTrue : BTCondition
    {
        protected override bool CheckCondition(NodeContext context)
        {
            return true;
        }
    }
}