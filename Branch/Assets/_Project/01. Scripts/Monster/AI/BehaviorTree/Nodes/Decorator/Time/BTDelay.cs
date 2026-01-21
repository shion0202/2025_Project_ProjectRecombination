using System.Collections.Generic;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    // 지정된 시간 후에 자식 노드를 실행하는 노드
    [CreateAssetMenu(fileName = "BTDelay", menuName = "AI/BehaviorTree/Nodes/Decorator/BTDelay")]
    public class BTDelay : BTDecorator
    {
        public float delayTime;
        
        private float _startTime;
        private bool _isDelaying;
        private bool _isStarted;

        public override void OnEnter()
        {
            _startTime = Time.time; // 시작 노드를 언제 어떻게 초기화 하지??
            _isDelaying = true;
        }
        
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            
            if (!_isStarted)
            {
                OnEnter();
                _isStarted = true;
            }
            
            if (_isDelaying)
            {
                if (Time.time - _startTime >= delayTime)
                    _isDelaying = false;
                else
                    return state = NodeState.Running;
            }

            if (child is null)
                return NodeState.Success;
            
            return state = child.Evaluate(context, visited);
        }
    }
}