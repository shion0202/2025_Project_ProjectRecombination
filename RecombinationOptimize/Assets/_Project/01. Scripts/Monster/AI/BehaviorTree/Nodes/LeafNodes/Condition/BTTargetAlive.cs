using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTIsTargetAlive", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Condition/BTIsTargetAlive")]
    public class BTIsTargetAlive : BTCondition
    {
        protected override bool CheckCondition(NodeContext context)
        {
            var blackboard = context.Blackboard;
            var target = blackboard.Target;
            return target is not null;
            
            // TODO: 실제 사용 시 target이 살아있는지 확인하는 로직 추가 필요
            // 게임 메니저를 통해 target이 살아있는지 확인하는 로직을 추가할 수 있습니다.
        }
    }
}