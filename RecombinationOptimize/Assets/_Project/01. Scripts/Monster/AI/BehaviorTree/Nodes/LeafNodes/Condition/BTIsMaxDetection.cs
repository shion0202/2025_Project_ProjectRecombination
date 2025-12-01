using _Project.Scripts.GUI;
using Monster.AI.Blackboard;
using Unity.VisualScripting;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTIsMaxDetection", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Condition/BTIsMaxDetection")]
    public class BTIsMaxDetection : BTCondition
    {
        protected override bool CheckCondition(NodeContext context)
        {
            Blackboard.Blackboard blackboard = context.Blackboard;
            // Check if the target is within the detection range
            if (blackboard.Target is null) return false;
            
            Vector3 targetPosition = blackboard.Target.transform.position;
            Vector3 agentPosition = blackboard.Agent.transform.position;

            if (!blackboard.TryGet(new BBKey<float>("maxDetectionRange"), out float maxDetectionRange) ||
                 !(maxDetectionRange > 0f)) return false;
            // Calculate the distance between the agent and the target
            float distance = Vector3.Distance(agentPosition, targetPosition);

            // If the distance is less than or equal to the detection range, return true
            return distance <= maxDetectionRange;
            
        }
    }
}