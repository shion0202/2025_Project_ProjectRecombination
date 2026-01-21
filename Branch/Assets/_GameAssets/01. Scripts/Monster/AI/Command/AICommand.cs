using System;
using System.Collections;
using UnityEngine;

namespace Monster.AI.Command
{
    public abstract class AICommand
    {
        public enum CommandState
        {
            Ready,
            Executing,
            Finished,
        }

        public CommandState State = CommandState.Ready;
        public virtual void OnEnter(Blackboard.Blackboard blackboard, Action processError = null)
        {
            State = CommandState.Executing;
        }
        public abstract void Execute(Blackboard.Blackboard blackboard, Action onComplete);
        public virtual void OnExit(Blackboard.Blackboard blackboard)
        {
            State = CommandState.Finished;
        }
        protected static bool CheckAnimator(Blackboard.Blackboard blackboard, string animationName)
        {
            if (blackboard?.AnimatorParameterSetter is null)
                return false;
            if (!blackboard.AnimatorParameterSetter.Animator.HasState(0, Animator.StringToHash(animationName)))
                return false;
            // 현재 재생 중인 에니메이션이 animationName일 경우
            if (blackboard.AnimatorParameterSetter.Animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
                return false;
            return true;
        }
    }
}