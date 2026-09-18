using System.Collections;
using UnityEngine;

namespace _Test.Skills.Ralph
{
    [CreateAssetMenu(fileName = "OneHandsStrike", menuName = "MonsterSkills/Ralph/OneHandsStrike")]
    public class OneHandsStrike: SkillData
    {
        [SerializeField] private AudioClip audioClip;
        public override IEnumerator Activate(Monster.AI.Blackboard.Blackboard data)
        {
            Debug.Log("한손 공격 시전!");
            data.AudioSource.PlayOneShot(audioClip);
            yield return PlayAnimationAndWait(data.AnimatorParameterSetter.Animator, "Attack1H");
        }
    }
}