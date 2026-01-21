using System.Collections.Generic;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    // 자식 노드를 지정한 횟수 만큼 반복 실행한다.
    [CreateAssetMenu(fileName = "BTRepeater", menuName = "AI/BehaviorTree/Nodes/Decorator/BTRepeater")]
    public class BTRepeater : BTDecorator
    {
        public int currentCount = 0;
        public int repeatCount;
        public NodeState chaseState;
        
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            
            // 반복 횟수가 지정된 횟수 이상이 된 경우> 실패 반환
            if (currentCount >= repeatCount)
                return state = NodeState.Failure;
            
            var nodeState = child.Evaluate(context, visited);
            currentCount++;
            
            // 종료 조건으로 설정한 Node 상태와 같으면 성공 반환
            if (nodeState == chaseState)
                return state = NodeState.Success;
            
            state = NodeState.Running;
            
            return state;
        }
    }
}