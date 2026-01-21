using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTIsObstacleBlocking", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Condition/BTIsObstacleBlocking")]
    public class BTIsObstacleBlocking : BTCondition
    {
        // public float threshold = 0.2f; // 20% 확률
        // public int skillID = 4001;
        
        protected override bool CheckCondition(NodeContext context)
        {
            // 몬스터가 장애물에 막혀있는지 여부를 판단
            // 예를 들어, 장애물이 있으면 true를 반환
            Blackboard.Blackboard blackboard = context.Blackboard;
            if (blackboard?.Target is null || blackboard.Agent is null)
                return false;
            
            Vector3 targetPosition = blackboard.Target.transform.position;
            Vector3 agentPosition = blackboard.Agent.transform.position;
            
            Vector3 directionToTarget = (targetPosition - agentPosition).normalized;
            float distanceToTarget = Vector3.Distance(agentPosition, targetPosition);

            // 레이캐스트를 사용하여 장애물이 있는지 확인
            if (Physics.Raycast(agentPosition, directionToTarget, out RaycastHit hitInfo, distanceToTarget))
            {
                // 레이캐스트가 무언가에 맞았을 때
                if (hitInfo.collider.gameObject != blackboard.Target.gameObject && hitInfo.collider.gameObject != blackboard.Agent.gameObject)
                {
                    // 맞은 오브젝트가 타겟이 아닌 경우, 장애물이 있다고 판단
                    
                    return false;
                }
            }
            return true;
        }
    }
}