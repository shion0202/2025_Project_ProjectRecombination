using _Test.Skills;
using Managers;
using Monster.AI.Blackboard;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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

        [Header("경직 시간")]
        [SerializeField] private float hitStunTime = 0.5f;

        #region private Fields
        
        // 상태별 로직에 필요한 내부 변수들
        private float _waitTimer;
        private Skill _useSkill;
        private bool _isDeath;

        // private AmonMeleeCollision _amonMeleeCollision;
        private AmonMeleeCollision _meleeCollision;
        // private bool _isHit;

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
            
            if (blackboard.State.GetStates() == "Hit")
            {
                return; // 피격 상태에서는 판단을 하지 않음
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
                case "Hit":
                    ActHit();
                    break;
            }
        }

        // 상태 진입 시 1회 호출되는 초기화 메서드 (기존 코드와 동일)
        protected override void EnterState(string stateName)
        {
            // InitAnimationFlags();
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
                case "Hit":
                    blackboard.NavMeshAgent.isStopped = true;
                    blackboard.NavMeshAgent.ResetPath();
                    break;
            }
        }
        
        #endregion

        #region State Actions
        
        private void ActSpawn()
        {
            // 스폰 사운드 클립 재생
            audioSource.PlayOneShot(spawnClip);
            foreach (MonsterDissolve dissolve in blackboard.Dissolve)
                dissolve.StartDissolve(true);
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
            
            // 사망 시 자신을 포함한 모든 자식 오브젝트의 레이어를 Default로 변경
            int defaultLayer = LayerMask.NameToLayer("MonsterDead");
            gameObject.layer = defaultLayer;
            foreach (Transform t in transform.GetComponentsInChildren<Transform>(true))
            {
                if (t == transform) continue;
                t.gameObject.layer = defaultLayer;
            }
            
            // 사망시 자식으로 가진 AmonMeleeCollision 모두 제거
            AmonMeleeCollision[] meleeCollisions = GetComponentsInChildren<AmonMeleeCollision>();
            foreach (AmonMeleeCollision meleeCollision in meleeCollisions)
            {
                Destroy(meleeCollision.gameObject);
            }
            
            audioSource.PlayOneShot(deathClip);
            if (blackboard.LegAnimator) blackboard.LegAnimator.enabled = false;
            blackboard.RagdollController.ActivateRagdoll();
            
            foreach (MonsterDissolve dissolve in blackboard.Dissolve)
                dissolve.StartDissolve(false);
            
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
                
                // 사망 시 자신을 포함한 모든 하위(자식, 손자 등) 오브젝트의 레이어를 Enemy로 변경
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                gameObject.layer = enemyLayer;
                foreach (Transform t in transform.GetComponentsInChildren<Transform>(true))
                {
                    if (t == transform) continue;
                    t.gameObject.layer = enemyLayer;
                }

                blackboard.DeathEffect?.SetActive(false);

                // 스킬/타깃 정리
                _useSkill = null;
                // 블랙보드에 이런 필드가 있다면 초기화
                // 필요한 추가 초기화가 있다면 blackboard.Init()으로 처리
                blackboard.Init();
            }

            // FSM 플래그 초기화
            _isDeath = false;
            isInit = false;
        }

        private IEnumerator PoolReleaseAfterDeathEffect()
        {
            foreach (MonsterDissolve dissolve in blackboard.Dissolve)
                while (!dissolve.isDissolved) yield return null;
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
            if (_isDeath) return;
            if (blackboard.Target is null) return;
            
            // 추격 상태의 행동: 매 프레임 타겟의 위치로 목적지를 갱신합니다.
            var agent = blackboard.NavMeshAgent;
            agent.SetDestination(blackboard.Target.transform.position);
            agent.speed = blackboard.RunSpeed;
            
            // 주변 동료와 겹침을 피하기 위한 간단한 분리(separation) 처리
            float separationRadius = Mathf.Max(agent.radius * 2f, 1f);
            int enemyLayerMask = LayerMask.GetMask("Enemy");
            Vector3 separation = Vector3.zero;
            int neighbors = 0;
            
            Collider[] hits = new Collider[16];
            int hitCount = Physics.OverlapSphereNonAlloc(agent.transform.position, separationRadius, hits, enemyLayerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                var hit = hits[i];
                if (hit == null || hit.gameObject == gameObject) continue;
                Vector3 toSelf = agent.transform.position - hit.transform.position;
                float distSqr = toSelf.sqrMagnitude;
                if (distSqr > 1e-6f)
                {
                    separation += toSelf / distSqr; // 거리가 가까울수록 더 강하게 밀어냄
                    neighbors++;
                }
            }
            
            if (neighbors > 0)
            {
                separation /= neighbors;
                float separationStrength = agent.radius * 1.2f;
                Vector3 adjustedTarget = blackboard.Target.transform.position + separation.normalized * separationStrength;
                agent.SetDestination(adjustedTarget);
            }
            
            // NavMeshAgent가 위치 업데이트를 처리하도록 유지 (CharacterController와 중복 이동 제거)
            agent.updatePosition = true;
        }

        private void ActAttack()
        {
            if (_useSkill is null || blackboard.Target is null) return;
            if (_isDeath)  return;
            
            // 타겟을 바라보게 합니다.
            transform.LookAt(blackboard.Target.transform);
            
            _useSkill.Execute(blackboard);
        }

        protected override void ActHit()
        {
            base.ActHit();
            
            if (blackboard != null)
            {
                try
                {
                    // 사용 중인 스킬 코루틴 정지
                    foreach(var skill in blackboard.Skills)
                    {
                        StopCoroutine(skill.CUseSkill);
                    }
                } catch { }
            
                if (blackboard.AnimatorParameterSetter?.Animator != null)
                {
                    // Animator animator = blackboard.AnimatorParameterSetter.Animator;
                    // // 초기 애니메이션 플래그 재설정
                    // InitAnimationFlags();
                    //
                    // animator.Rebind();
                    // animator.Update(0f);
                }
            
                _useSkill = null;
            }

            DelAmonMeleeCollision();
            
            StartCoroutine(AfterHitEffect());
        }

        private IEnumerator AfterHitEffect()
        {
            yield return new WaitForSeconds(hitStunTime);
            ChangeState("Idle");
        }

        #endregion

        #region Helper & Event Methods
        
        private void FireBullet(int bulletType = 0)
        {
            if (blackboard.Target == null || _useSkill == null) return;

            Vector3 startPos = blackboard.AttackInfo.firePoint.position;
            Vector3 targetPos = blackboard.Target.transform.position + Vector3.up * 1.5f;
            Vector3 direction = (targetPos - startPos).normalized;
            
            blackboard.AttackInfo.Fire(bulletType, blackboard.Agent, blackboard.AttackInfo.firePoint.position, Vector3.zero, direction, _useSkill.skillData.damage);
        }
        
        public void AnimationEvent_Fire()
        {
            if (blackboard.Target == null || _useSkill == null) return;

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