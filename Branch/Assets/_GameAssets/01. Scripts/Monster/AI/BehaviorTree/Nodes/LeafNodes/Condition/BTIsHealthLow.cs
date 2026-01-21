using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTIsHealthLow", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Condition/BTIsHealthLow")]
    public class BTIsHealthLow : BTCondition
    {
        public float healthThreshold = 0.3f; // 체력 임계값 (예: 30%)
        protected override bool CheckCondition(NodeContext nodeContext)
        {
            float currentHealth = nodeContext.Blackboard.CurrentHealth;
            float maxHealth = nodeContext.Blackboard.MaxHealth;
            return (currentHealth / maxHealth) <= healthThreshold;
        }
    }
}