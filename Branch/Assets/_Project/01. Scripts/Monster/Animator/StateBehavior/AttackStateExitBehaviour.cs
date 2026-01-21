using Monster.AI.FSM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackStateExitBehaviour : StateMachineBehaviour
{
    // 이 애니메이션 상태가 '끝나고 나갈 때' 호출되는 함수입니다.
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // MonsterFSM 스크립트를 찾아서 상태를 변경하라고 알려줍니다.
        MonsterFSM fsm = animator.GetComponent<MonsterFSM>();
        if (fsm != null)
        {
            // 공격이 끝났으니 다시 생각(Think)해서 다음 행동을 결정하게 합니다.
            // 또는 바로 ChangeState("Chase") 등으로 전환할 수도 있습니다.
            fsm.OnAttackAnimationEnd();
        }
    }
}
