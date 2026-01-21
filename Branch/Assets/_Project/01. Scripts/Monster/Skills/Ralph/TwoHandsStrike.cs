using System.Collections;
using UnityEngine;

namespace _Test.Skills.Ralph
{
    [CreateAssetMenu(fileName = "TwoHandsStrike", menuName = "MonsterSkills/Ralph/TwoHandsStrike")]
    public class TwoHandsStrike: SkillData
    {
        [SerializeField] private AudioClip audioClip;
        public override IEnumerator Activate(Monster.AI.Blackboard.Blackboard data)
        {
            Debug.Log("양손 내리찍기 시전!");
            data.AudioSource.PlayOneShot(audioClip);
            data.AnimatorParameterSetter.Animator.SetTrigger("Smash");
            yield return null;
        }
    }
}