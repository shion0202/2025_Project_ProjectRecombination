using Monster.AI.Blackboard;
using Managers;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTIsCombatNearby", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Condition/BTIsCombatNearby")]
    public class BTIsCombatNearby : BTCondition
    {
        protected override bool CheckCondition(NodeContext context)
        {
            Blackboard.Blackboard blackboard = context.Blackboard;

            if (!blackboard.TryGet(new BBKey<float>("maxDetectionRange"), out float detectionRange)) return false;

            // Debug.Log($"Checking for nearby combat...{detectionRange}");
            GameObject[] monsterControllers = MonsterManager.Instance.GetBattleMonsters();

            if (monsterControllers is null || monsterControllers.Length == 0) return false;
                
            Vector3 standardPosition = blackboard.Agent.transform.position;

            foreach (GameObject controller in monsterControllers)
            {
                var agent = controller.GetComponentInChildren<Blackboard.Blackboard>().Agent;
                if (agent is null || agent == blackboard.Agent) continue; // null 체크 및 자기 자신 제외
                var otherPosition = agent.transform.position;
                if (Vector3.Distance(standardPosition, otherPosition) <= detectionRange)
                {
                    Debug.Log("Nearby combat detected!");
                    return true; // 근처에 전투 중인 몬스터가 있음
                }
            }

            return false;
        }
    }
}