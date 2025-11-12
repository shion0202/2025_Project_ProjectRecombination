using System.Collections;
using System.Collections.Generic;
using Monster.AI.Command;
using Monster;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    [CreateAssetMenu(fileName = "BTActionDeath", menuName = "AI/BehaviorTree/Nodes/LeafNodes/Actions/BTActionDeath")]
    public class BTActionDeath : BTAction
    {
        public override NodeState Evaluate(NodeContext context, HashSet<BTNode> visited)
        {
            if (CheckCycle(visited))
                return NodeState.Failure;
            // var blackboard = context.Blackboard;
            // // Debug.Log("Monster is dead: " + monsterStats.name);
            // // 몬스터가 사망했을 때의 처리 로직
            // // 예를 들어, NavMeshAgent를 정지시키고 상태를 Dead로 설정
            // blackboard.NavMeshAgent.isStopped = true; // NavMeshAgent를 정지시
            // blackboard.NavMeshAgent.ResetPath(); // 현재 경로를 초기화
            // // 몬스터의 상태를 Dead로 설정
            // blackboard.State = MonsterState.Death;
            // StartCoroutine(Coroutine());
            
            // Idle 명령을 행동 대기열에 추가
            
            context.Enqueue(new DeathCommand(), priority);
            
            return state = NodeState.Success;
        }
    }
}