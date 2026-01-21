using Monster.AI.Blackboard;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTIsDeath", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Condition/BTIsDeath")]
    public sealed class BTIsDeath : BTCondition
    {
        protected override bool CheckCondition(NodeContext context)
        {
            var blackboard = context.Blackboard;
            // 몬스터의 생명력이 0 이하인지 확인
            // return blackboard.Health <= 0f;
            return blackboard.TryGet(new BBKey<float>("health"), out float health) && health <= 0f;
        }
    }
}