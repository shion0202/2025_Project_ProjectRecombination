using _Project.Scripts.GUI;
using Monster.AI.Command;
using System.Collections.Generic;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTActionSystemMessage", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Actions/BTActionSystemMessage")]
    public class BTActionSystemMessage : BTAction
    {
        public string speaker;
        public string message;
        
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            
            // // Wander 명령을 행동 대기열에 추가
            // context.Enqueue(new WanderCommand());
            SystemMessageBus.Publish(new SystemMessage
            {
                Speaker = string.IsNullOrEmpty(speaker) ? context.Blackboard.Agent.name : speaker,
                Text = string.IsNullOrEmpty(message) ? "기본 시스템 메시지입니다." : message
            });
            
            Debug.Log(ToString());
            
            return state = NodeState.Success;
        }
        
        public override string ToString()
        {
            return $"BTMessage: {speaker}: {message}";
        }
    }
}