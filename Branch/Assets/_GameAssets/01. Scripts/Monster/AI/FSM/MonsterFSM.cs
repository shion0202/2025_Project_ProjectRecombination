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

        [Header("공격 시전 중 회전 속도 (도/초)")]
        [SerializeField] private float attackTurnSpeed = 360f;

        private static readonly int IsWalkHash = Animator.StringToHash("IsWalk");
        private static readonly int IsRunHash = Animator.StringToHash("IsRun");

        // 추격 중 동료 분리 계산용 버퍼. 매 프레임 새로 만들지 않도록 공유한다 (메인 스레드에서 즉시 사용).
        private static readonly Collider[] SeparationHits = new Collider[16];

        #region private Fields

        // 상태별 로직에 필요한 내부 변수들
        private float _waitTimer;
        private Skill _useSkill;
        private bool _isDeath;
        private Coroutine _hitStunRoutine;
        private int[] _triggerHashes;

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

            string stateName = blackboard.State?.GetStates() ?? "None";

            // 사망은 스킬 실행 여부와 무관하게 바로 처리한다.
            // 아래 스킬 실행 검사에 막히면 공격 애니메이션이 끝날 때까지 사망이 미뤄진다.
            if (stateName == "Death")
            {
                ActDeath();
                return;
            }

            if (blackboard.IsAnySkillRunning)
            {
                // 시전(예비 동작) 중에는 제자리에서 타겟 쪽으로 몸만 돌린다.
                if (_useSkill is { CurrentState: Skill.SkillState.isCasting })
                    FaceTarget(attackTurnSpeed * Time.deltaTime);
                return; // 스킬이 실행 중이면 상태 전환을 하지 않음
            }

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
                case "Patrol":
                    ActPatrol();
                    break;
                case "Chase":
                    ActChase();
                    break;
                case "Attack":
                    ActAttack();
                    break;
                // Hit은 EnterState에서 한 번만 처리한다. (매 프레임 ActHit을 부르면 경직 코루틴이 쌓여
                // 경직이 끝난 뒤에도 늦게 도착한 코루틴이 상태를 Idle로 되돌려 행동이 끊겼다)
            }
        }

        // 상태 진입 시 1회 호출되는 초기화 메서드 (기존 코드와 동일)
        protected override void EnterState(string stateName)
        {
            blackboard.NavMeshAgent.isStopped = false;

            // 이동 애니메이션은 상태마다 새로 정한다.
            // 이전 상태의 IsRun이 남아 있으면 공격/대기 중에도 달리기 모션으로 돌아간다.
            SetMoveAnimation(stateName == "Patrol", stateName == "Chase");

            // 소비되지 않고 남은 공격 트리거(FireReady 등)가 이동 중에 뒤늦게 발동하면
            // 조준/공격 자세로 미끄러지므로, 공격이 아닌 상태로 들어갈 때 모두 지운다.
            if (stateName is "Idle" or "Chase" or "Patrol")
                ResetAnimationTriggers();

            switch (stateName)
            {
                case "Idle":
                case "Attack":
                case "Death":
                    StopAgent();
                    break;
                case "Patrol":
                    blackboard.PatrolInfo.isPatrol = true;
                    blackboard.PatrolInfo.CurrentWayPointIndex = blackboard.PatrolInfo.GetNextWayPointIndex();
                    blackboard.NavMeshAgent.SetDestination(blackboard.PatrolInfo.GetCurrentWayPoint());
                    blackboard.NavMeshAgent.speed = blackboard.WalkSpeed;
                    break;
                case "Chase":
                    blackboard.NavMeshAgent.speed = blackboard.RunSpeed;
                    break;
                case "Hit":
                    StopAgent();
                    ActHit();
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

            // 시전 중이던 스킬을 정리한다. (공격 애니메이션을 기다리는 스킬 코루틴이 사망 뒤에도 남지 않게)
            InterruptSkills();

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
            Vector3 destination = blackboard.Target.transform.position;
            agent.speed = blackboard.RunSpeed;

            // 주변 동료와 겹침을 피하기 위한 간단한 분리(separation) 처리
            float separationRadius = Mathf.Max(agent.radius * 2f, 1f);
            int enemyLayerMask = LayerMask.GetMask("Enemy");
            Vector3 separation = Vector3.zero;
            int neighbors = 0;

            int hitCount = Physics.OverlapSphereNonAlloc(agent.transform.position, separationRadius, SeparationHits, enemyLayerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                var hit = SeparationHits[i];
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
                destination += separation.normalized * separationStrength;
            }

            agent.SetDestination(destination);

            // NavMeshAgent가 위치 업데이트를 처리하도록 유지 (CharacterController와 중복 이동 제거)
            agent.updatePosition = true;
        }

        private void ActAttack()
        {
            if (_useSkill is null || blackboard.Target is null) return;
            if (_isDeath)  return;

            // 타겟을 바라보게 합니다. (수평 회전만. LookAt은 높이차만큼 몸을 기울였다)
            FaceTarget(360f);

            _useSkill.Execute(blackboard);
        }

        /// <summary>
        /// 공격(스킬 시전/실행) 중에는 경직 없이 피격 이펙트만 보여준다.
        /// 기존에도 공격 중 피격으로 스킬이 실제로 끊기지는 않았는데(StopCoroutine을 스킬 소유자가 아닌
        /// FSM에서 호출해 효과가 없었음), 스킬이 애니메이션 끝까지 이어지게 되면서 이 동작을 명시적으로 유지한다.
        /// </summary>
        public override void ApplyDamage(float inDamage, LayerMask targetMask = default, float unitOfTime = 1.0f, float defenceIgnoreRate = 0.0f)
        {
            if (blackboard is null || _isDeath) return;

            OnHit(inDamage);
            if (blackboard.CurrentHealth <= 0) return; // 사망 처리는 Think에서

            if (blackboard.IsAnySkillRunning)
            {
                base.ActHit();
                return;
            }

            ChangeState("Hit");
        }

        protected override void ActHit()
        {
            base.ActHit();

            _useSkill = null;
            DelAmonMeleeCollision();

            if (_hitStunRoutine != null) StopCoroutine(_hitStunRoutine);
            _hitStunRoutine = StartCoroutine(AfterHitEffect());
        }

        private IEnumerator AfterHitEffect()
        {
            yield return new WaitForSeconds(hitStunTime);
            _hitStunRoutine = null;

            // 그 사이 사망 등으로 상태가 바뀌었으면 건드리지 않는다.
            if (!_isDeath && blackboard.State.GetStates() == "Hit")
                ChangeState("Idle");
        }

        private void InterruptSkills()
        {
            if (blackboard?.Skills is null) return;

            foreach (Skill skill in blackboard.Skills)
                skill?.Interrupt(blackboard);

            _useSkill = null;
        }

        private void StopAgent()
        {
            NavMeshAgent agent = blackboard.NavMeshAgent;
            if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return; // NavMesh 밖에서 ResetPath는 에러를 낸다
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero; // 감속하며 미끄러지지 않게 즉시 정지
        }

        private void SetMoveAnimation(bool isWalk, bool isRun)
        {
            Animator animator = blackboard.AnimatorParameterSetter?.Animator;
            if (animator is null) return;

            animator.SetBool(IsWalkHash, isWalk);
            animator.SetBool(IsRunHash, isRun);
        }

        private void ResetAnimationTriggers()
        {
            Animator animator = blackboard.AnimatorParameterSetter?.Animator;
            if (animator is null) return;

            // animator.parameters는 호출마다 배열을 새로 만들므로 트리거 목록은 한 번만 모아둔다.
            if (_triggerHashes is null)
            {
                var hashes = new List<int>();
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.type == AnimatorControllerParameterType.Trigger)
                        hashes.Add(param.nameHash);
                }
                _triggerHashes = hashes.ToArray();
            }

            foreach (int hash in _triggerHashes)
                animator.ResetTrigger(hash);
        }

        /// <summary>타겟 쪽으로 수평 회전한다. maxDegrees만큼만 돌린다.</summary>
        private void FaceTarget(float maxDegrees)
        {
            if (blackboard.Target is null) return;

            Vector3 lookDir = blackboard.Target.transform.position - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(lookDir), maxDegrees);
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