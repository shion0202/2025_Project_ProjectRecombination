using _Test.Skills;
using Managers;
using Monster.AI.BehaviorTree.Nodes;
using System.Collections;
using UnityEngine;

namespace Monster.AI.FSM
{
    public class FlyFSM : FSM
    {
        [SerializeField] private GameObject attackCollider;
        
        [Header("Audio Clips")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip spawnClip;
        [SerializeField] private AudioClip deathClip;
        [SerializeField] private AudioClip attackClip;
        
        private AmonMeleeCollision _attackCollider;

        private bool _isDeath;

        private Skill _useSkill;

        protected override void Init()
        {
            blackboard.Init();
            isInit = true;
        }
        
        protected override void Think()
        {
            if (!isEnabled || _isDeath) return;
            if (blackboard.State.GetStates() == "Spawn") return;

            // 0 순위: 사망 체크
            if (blackboard.CurrentHealth <= 0)
            {
                ChangeState("Death");
                return;
            }
            
            // 1 순위: 스킬 사용 여부 체크
            if (blackboard.IsAnySkillRunning)
            {
                if (_isDeath)
                {
                    blackboard.StopAllCoroutines();
                }
                else
                {
                    float f = Vector3.Distance(blackboard.Agent.transform.position, blackboard.Target.transform.position);
                    if (_useSkill is { CurrentState: not Skill.SkillState.isReady and Skill.SkillState.isEnded } &&
                        f > _useSkill.skillData.range)
                    {
                        blackboard.StopAllCoroutines();
                        _useSkill = null;
                        ChangeState("Chase");
                    }
                }

                return;
            }
            
            // 2 순위: 스킬 쿨타임 및 거리 체크 후 스킬 사용
            if (blackboard.Skills is not null && blackboard.Skills.Length > 0)
            {
                // 스킬 0 우선 사용
                if (blackboard.Skills[0].CurrentState == Skill.SkillState.isReady)
                {
                    float skillRange = blackboard.Skills[0].skillData.range;
                    float distanceToTarget = Vector3.Distance(blackboard.Agent.transform.position, blackboard.Target.transform.position);
                    
                    if (distanceToTarget <= skillRange)
                    {
                        _useSkill = blackboard.Skills[0];
                        ChangeState("UsingSkill1");
                        return;
                    }
                }
                
                // 스킬 1 다음으로 사용
                if (blackboard.Skills[1].CurrentState == Skill.SkillState.isReady)
                {
                    float skillRange = blackboard.Skills[1].skillData.range;
                    float distanceToTarget = Vector3.Distance(blackboard.Agent.transform.position, blackboard.Target.transform.position);
                    
                    if (distanceToTarget <= skillRange)
                    {
                        _useSkill = blackboard.Skills[1];
                        ChangeState("UsingSkill2");
                        return;
                    }
                }
            }
            
            // 3 순위: 플레이어와의 거리 체크 후 추적 또는 대기 상태로 전환
            float distance = Vector3.Distance(blackboard.Agent.transform.position, blackboard.Target.transform.position);
            if (distance > blackboard.MinDetectionRange)
            {
                
                ChangeState("Chase");
                return;
            }
            
            ChangeState("Idle");
        }

        protected override void Act()
        {
            if (!isEnabled || _isDeath || blackboard?.State is null || blackboard.IsAnySkillRunning) return;
            
            string state = blackboard.State.GetStates() ?? "None";

            switch (state)
            {
                case "None":
                    break;
                case "Idle":
                    ActIdle();
                    break;
                case "Spawn":
                    ActSpawn();
                    break;
                case "Death":
                    ActDeath();
                    break;
                case "Chase":
                    ActChase();
                    break;
                case "UsingSkill1":
                    blackboard.Skills[0].Execute(blackboard);
                    break;
                case "UsingSkill2":
                    blackboard.Skills[1].Execute(blackboard);
                    break;
            }
        }

        private void ActSpawn()
        {
            audioSource.PlayOneShot(spawnClip);
            ChangeState("Idle");
        }

        private void ActChase()
        {
            float distanceToTarget = Vector3.Distance(blackboard.Agent.transform.position, blackboard.Target.transform.position);

            if (distanceToTarget <= blackboard.MinDetectionRange)
            {
                // ChangeState("Idle");
                blackboard.NavMeshAgent.isStopped = true;
                blackboard.NavMeshAgent.ResetPath();
                return;
            }
            
            blackboard.AnimatorParameterSetter.Animator.SetBool("isMoving", true);
            blackboard.NavMeshAgent.speed = blackboard.RunSpeed;
            blackboard.NavMeshAgent.SetDestination(blackboard.Target.transform.position);
            blackboard.NavMeshAgent.isStopped = false;
        }

        private void ActDeath()
        {
            if (_isDeath) return;
            _isDeath = true;
            
            blackboard.NavMeshAgent.isStopped = true;
            
            if (blackboard.DeathEffect is not null)
            {
                var effect = blackboard.DeathEffect;
                
                // 이팩트가 있으면 활성화 시키고 이팩트가 종료 될때까지 대기
                effect.SetActive(true);
                var particleSystem = effect.GetComponent<ParticleSystem>();
                if (particleSystem is null) return;

                if (!particleSystem.isPlaying)
                    particleSystem.Play();
            }
            
            audioSource.PlayOneShot(deathClip);
            blackboard.RagdollController.ActivateRagdoll();
            
            StartCoroutine(PoolReleaseAfterDeathEffect());
        }
        
        private void ResetForPool()
        {
            // 모든 코루틴 정지
            try { StopAllCoroutines(); } catch { }

            // 블랙보드 관련 코루틴/상태 정리
            if (blackboard != null)
            {
                try { blackboard.StopAllCoroutines(); } catch { }

                // Ragdoll 비활성화
                if (blackboard.RagdollController != null)
                    blackboard.RagdollController.DeactivateRagdoll();

                // NavMeshAgent 초기화
                if (blackboard.NavMeshAgent != null)
                {
                    blackboard.NavMeshAgent.isStopped = true;
                    blackboard.NavMeshAgent.ResetPath();
                }

                // Animator 플래그 초기화
                if (blackboard.AnimatorParameterSetter?.Animator != null)
                {
                    var animator = blackboard.AnimatorParameterSetter.Animator;
                    animator.SetBool("isMoving", false);
                    animator.Rebind();
                    animator.Update(0f);
                }

                // 스킬/타깃 정리
                _useSkill = null;
                // blackboard.IsAnySkillRunning = false; // 블랙보드에 이런 필드가 있다면 초기화
                // 필요한 추가 초기화가 있다면 blackboard.Init()으로 처리
                blackboard.Init();
            }

            // FSM 플래그 초기화
            _isDeath = false;
            isInit = false;
        }

        private IEnumerator PoolReleaseAfterDeathEffect()
        {
            yield return new WaitForSeconds(10f);
            ResetForPool();
            PoolManager.Instance.ReleaseObject(gameObject);
        }

        private void ActIdle()
        {
            blackboard.AnimatorParameterSetter.Animator.SetBool("isMoving", false);
            blackboard.NavMeshAgent.isStopped = true;
        }

        protected override void EnterState(string stateName)
        {
            if (!isEnabled || _isDeath) return;
            
            InitAnimationFlags();
            blackboard.NavMeshAgent.isStopped = false;

            switch (stateName)
            {
                case "Idle":
                    blackboard.NavMeshAgent.isStopped = true;
                    break;
            }
        }

        #region Animation Event Methods

        public void AnimationEvent_MeleeAttack()
        {
            if (blackboard.Target is null || _useSkill is null) return;
            
            float damage = _useSkill.skillData.damage;

            GameObject meleeCollider = Utils.Instantiate(attackCollider, blackboard.Agent.transform);
            _attackCollider = meleeCollider.GetComponent<AmonMeleeCollision>();
            if (_attackCollider)
                _attackCollider.Init(damage, new Vector3(2f, 2f, 2f), new Vector3(0f, 2f, 1f));
            
            audioSource.PlayOneShot(attackClip);
            
            StartCoroutine(MeleeCollisionDisableDelay());
        }

        private IEnumerator MeleeCollisionDisableDelay()
        {
            yield return new WaitForSeconds(0.2f);
            if (_attackCollider)
            {
                Utils.Destroy(_attackCollider.gameObject);
            }
        }
        
        public void AnimationEvent_Death()
        {
            // 파티클이 재생 중일 수 있으므로, 파티클도 함께 비활성화합니다.
            if (blackboard.DeathEffect is not null)
            {
                blackboard.DeathEffect.SetActive(false);
            }
            
            // 자식 오브젝트들 중 AmonMeleeCollision 컴포넌트를 찾아 모두 제거
            AmonMeleeCollision[] meleeCollisions = GetComponentsInChildren<AmonMeleeCollision>();
            foreach (AmonMeleeCollision meleeCollision in meleeCollisions)
            {
                Destroy(meleeCollision.gameObject);
            }
            
            isInit = false;
            // gameObject.SetActive(false); // 풀 매니저를 사용하므로 이쪽을 권장
            PoolManager.Instance.ReleaseObject(gameObject);
        }

        #endregion
    }
}