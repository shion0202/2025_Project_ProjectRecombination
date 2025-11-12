using Monster.AI.Blackboard;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTIsHit", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Condition/BTIsHit")]
    public class BTIsHit : BTCondition
    {
        protected override bool CheckCondition(NodeContext context)
        {
            var blackboard = context.Blackboard;
            var currentHealth = blackboard.TryGet(new BBKey<float>("health"), out var health) ? health : 0f;
            var maxHealth = blackboard.TryGet(new BBKey<float>("maxHealth"), out var maxHealthValue) ? maxHealthValue : 1f;
            
            // 몬스터가 피격당했는지 확인
            return currentHealth < maxHealth;
        }
    }
}