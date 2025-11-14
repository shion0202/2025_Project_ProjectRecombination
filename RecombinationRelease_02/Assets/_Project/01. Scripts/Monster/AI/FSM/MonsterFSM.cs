using _Test.Skills;
using Managers;
using Monster.AI.Blackboard;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Monster.AI.FSM
{
    public class MonsterFSM : FSM
    {
        [SerializeField] private GameObject ralphTwoHandsAttackCollider;
        
        [Header("Audio Clips")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip spawnClip;
        [SerializeField] private AudioClip deathClip;
        [SerializeField] private AudioClip walkClip;
        [SerializeField] private AudioClip meleeAudioClip;
        
        #region private Fields
        
        // 상태별 로직에 필요한 내부 변수들
        private float _waitTimer;
        private Skill _useSkill;
        private bool _isDeath;

        // private AmonMeleeCollision _amonMeleeCollision;
        private AmonMeleeCollision _meleeCollision;
        #endregion

        #region Core FSM Methods: Think & Act (Overrided)

        protected override void Init()
        {
            blackboard.Init();
            isInit = true;
        }
        
        /// <summary>
        /// AI의 두뇌 역할: 모든 조건을 검사하여 어떤 상태로 전환할지 결정(판단)합니다.
        /// 기존의 Tink() 메서드와 동일합니다.
        /// </summary>
        protected override void Think()
        {
            if (!isEnabled || _isDeath) return;
            if (blackboard.State.GetStates() == "Spawn") return; // 스폰 상태에서는 판단을 하지 않음
            
            if (blackboard.CurrentHealth <= 0)
            {
                ChangeState("Death");
                return;
            }
            
            // 전투 상태 일 때 상태 변경 체크
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
            
            // 사용 가능한 스킬 검사 (우선순위가 가장 높음)
            if (blackboard.Skills is not null && blackboard.Skills.Length != 0)
            {
                foreach (var skill in blackboard.Skills)
                {
                    // int skillId = skill.skillData.skillID;
                    if (skill.CurrentState != Skill.SkillState.isReady) continue;

                    float skillRange = skill.skillData.range;
                    float distanceToPlayer = Vector3.Distance(transform.position, blackboard.Target.transform.position);
                        
                    if (distanceToPlayer <= skillRange)
                    {
                        _useSkill = skill;
                        ChangeState("Attack");
                        return;
                    }
                }
            }
            
            // 플레이어와의 거리 검사
            float distance = Vector3.Distance(transform.position, blackboard.Target.transform.position);
            if (distance > blackboard.MinDetectionRange)
            {
                ChangeState("Chase");
                return;
            }

            ChangeState("Idle");
        }

        /// <summary>
        /// AI의 몸 역할: 현재 상태(State)에 따라 실제 행동을 수행합니다.
        /// 기존의 모든 Handle...State() 메서드를 통합했습니다.
        /// </summary>
        protected override void Act()
        {
            if (!isEnabled || blackboard?.State is null || _isDeath) return;

            if (blackboard.IsAnySkillRunning)
            {
                if (_isDeath)
                    blackboard.StopAllCoroutines();
                return; // 스킬이 실행 중이면 상태 전환을 하지 않음
            }
            
            string stateName = blackboard.State?.GetStates() ?? "None";
            
            switch (stateName)
            {
                case "None":
                    // 아무 것도 하지 않음
                    break;
                case "Spawn":
                    ActSpawn();
                    break;
                case "Idle":
                    // Idle 상태에서는 특별한 행동이 없으므로 EnterState에서 처리한 isStopped = true가 유지됩니다.
                    break;
                case "Death":
                    ActDeath();
                    break;
                case "Patrol":
                    ActPatrol();
                    break;
                case "Chase":
                    ActChase();
                    break;
                case "Attack":
                    ActAttack();
                    break;
            }
        }

        // 상태 진입 시 1회 호출되는 초기화 메서드 (기존 코드와 동일)
        protected override void EnterState(string stateName)
        {
            InitAnimationFlags();
            blackboard.NavMeshAgent.isStopped = false;
            Debug.Log("Entered " + stateName);

            switch (stateName)
            {
                case "Idle":
                    blackboard.NavMeshAgent.isStopped = true;
                    break;
                case "Death":
                    blackboard.NavMeshAgent.isStopped = true;
                    blackboard.NavMeshAgent.ResetPath();
                    break;
                case "Patrol":
                    blackboard.PatrolInfo.isPatrol = true;
                    blackboard.PatrolInfo.CurrentWayPointIndex = blackboard.PatrolInfo.GetNextWayPointIndex();
                    blackboard.NavMeshAgent.SetDestination(blackboard.PatrolInfo.GetCurrentWayPoint());
                    blackboard.NavMeshAgent.speed = blackboard.WalkSpeed;
                    blackboard.AnimatorParameterSetter.Animator.SetBool("IsWalk", true);
                    break;
                case "Chase":
                    blackboard.NavMeshAgent.speed = blackboard.RunSpeed;
                    blackboard.AnimatorParameterSetter.Animator.SetBool("IsRun", true);
                    break;
                case "Attack":
                    blackboard.NavMeshAgent.isStopped = true;
                    break;
            }
        }
        
        #endregion

        #region State Actions (Act에서 호출되는 행동 함수들)
        
        private void ActSpawn()
        {
            // 스폰 사운드 클립 재생
            audioSource.PlayOneShot(spawnClip);
            ChangeState("Idle");
        }
        
        private void ActDeath()
        {
            if (_isDeath)  return;
            _isDeath = true;
            
            // blackboard.AnimatorParameterSetter.Animator.SetTrigger("Death");
            
            // 2. 죽음 이팩트가 있는지 확인
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
            if (blackboard.LegAnimator) blackboard.LegAnimator.enabled = false;
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

        private void ActPatrol()
        {
            if (_isDeath)  return;
            // 목표 지점에 도착했는지 확인하고 다음 행동을 결정합니다.
            if (blackboard.NavMeshAgent.remainingDistance <= blackboard.NavMeshAgent.stoppingDistance && !blackboard.NavMeshAgent.pathPending)
            {
                blackboard.PatrolInfo.isPatrol = false; // isPatrol을 false로 만들어 Think()가 다음 판단(대기 또는 새 순찰)을 하도록 유도
                blackboard.AnimatorParameterSetter.Animator.SetBool("IsWalk", false);
            }
        }

        private void ActChase()
        {
            if (_isDeath)  return;
            // 추격 상태의 행동: 매 프레임 타겟의 위치로 목적지를 갱신합니다.
            blackboard.NavMeshAgent.SetDestination(blackboard.Target.transform.position);
            
            var distance = Vector3.Distance(transform.position, blackboard.Target.transform.position);
            if (distance <= blackboard.MinDetectionRange)
            {
                blackboard.AnimatorParameterSetter.Animator.SetBool("IsRun", false);
            }
        }

        private void ActAttack()
        {
            if (_useSkill is null || blackboard.Target is null) return;
            if (_isDeath)  return;
            
            // 타겟을 바라보게 합니다.
            transform.LookAt(blackboard.Target.transform);
            
            _useSkill.Execute(blackboard);
        }

        #endregion

        #region Helper & Event Methods
        
        private void FireBullet(int bulletType = 0)
        {
            Vector3 startPos = blackboard.AttackInfo.firePoint.position;
            Vector3 targetPos = blackboard.Target.transform.position + Vector3.up * 1.5f;
            Vector3 direction = (targetPos - startPos).normalized;
            
            blackboard.AttackInfo.Fire(bulletType, blackboard.Agent, blackboard.AttackInfo.firePoint.position, Vector3.zero, direction, _useSkill.skillData.damage);
        }
        
        public void AnimationEvent_Fire()
        {
            if (_useSkill.skillData.skillID is 4003 or 4002)
                FireBullet(1);
            else
                FireBullet();
        }
        
        public void AnimationEvent_Melee()
        {
            if (blackboard.Target == null || _useSkill == null) return;

            float damage = _useSkill.skillData.damage;
            var amonMeleeCollision = Utils.Instantiate(ralphTwoHandsAttackCollider, blackboard.Agent.transform);
            _meleeCollision = amonMeleeCollision.GetComponent<AmonMeleeCollision>();
            if (_meleeCollision)
            {
                _meleeCollision.Init(damage, new Vector3(4f,4f,4f), new Vector3(1f,1f,2f));
            }
            
            audioSource.PlayOneShot(meleeAudioClip);
        }

        public void AnimationEvent_OnHandAttack()
        {
            if (blackboard.Target == null || _useSkill == null) return;

            float damage = _useSkill.skillData.damage;
            var amonMeleeCollision = Utils.Instantiate(ralphTwoHandsAttackCollider, blackboard.Agent.transform);
            _meleeCollision = amonMeleeCollision.GetComponent<AmonMeleeCollision>();
            if (_meleeCollision)
            {
                _meleeCollision.Init(damage, new Vector3(2f,2f,4f), new Vector3(1f,1f,2f));
            }
            
            audioSource.PlayOneShot(meleeAudioClip);
        }

        public void AnimationEvent_Death()
        {
            // 파티클이 재생 중일 수 있으므로, 파티클도 함께 비활성화합니다.
            if (blackboard.DeathEffect is not null)
            {
                blackboard.DeathEffect.SetActive(false);
            }
            
            isInit = false;
            // gameObject.SetActive(false); // 풀 매니저를 사용하므로 이쪽을 권장
            PoolManager.Instance.ReleaseObject(gameObject);
        }
        
        public void AnimationEvent_WalkSound()
        {
            audioSource.PlayOneShot(walkClip);
        }
        
        public void OnAttackAnimationEnd()
        {
            if (_meleeCollision)
            {
                Utils.Destroy(_meleeCollision.gameObject);
            }
        }

        private IEnumerator WaitForParticleEnd(ParticleSystem ps)
        {
            if (ps is null) yield break;

            // 파티클 시스템이 재생 중일 때까지 대기
            yield return new WaitForSeconds(ps.main.duration);

            // 파티클 시스템이 끝난 후 오브젝트 비활성화
            isInit = false;
            // gameObject.SetActive(false);
            PoolManager.Instance.ReleaseObject(gameObject);
        }

        #endregion
    }
}