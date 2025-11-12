using System.Collections.Generic;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTCooldown", menuName = "AI/BehaviorTree/Nodes/Decorator/BTCooldown")]
    public class BTCooldown : BTDecorator
    {
        public float cooldownTime;

        private float _lastExecuted;
        
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            
            // 쿨타입과 시간 경과를 비교해서 쿨타임 중인 경우 자식 실행 x
            if (Time.time - _lastExecuted < cooldownTime)
                return state = NodeState.Failure;
            
            if (child == null)
            {
                Debug.LogError("Child node is not set for BTCooldown.");
                return NodeState.Failure;
            }
            
            // 자식 노드를 실행하고 실행 결과를 받아온다.
            var nodeState = child.Evaluate(context, visited);
            
            // 자식 노드의 실행이 끝나면 현재 시간을 저장
            if (nodeState != NodeState.Running)
                _lastExecuted = Time.time;
            
            // 노드 상태를 갱신하여 반환
            return state = nodeState;
        }
    }
}