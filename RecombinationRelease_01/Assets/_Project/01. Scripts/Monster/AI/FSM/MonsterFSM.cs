using Managers;
using Monster.AI.Blackboard;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Monster.AI.FSM
{
    public class MonsterFSM : MonoBehaviour, IDamagable
    {
        #region private Fields

        [SerializeField] private Blackboard.Blackboard blackboard;
        [SerializeField] private bool isInit;
        
        // 상태별 로직에 필요한 내부 변수들
        private float _waitTimer;
        private RowData _skillRowData;
        private bool _isAttack;
        private int _attackCount;

        #endregion
        
        #region Unity Methods

        private void Start()
        {
            Init();
        }

        // 1. 판단(Think)은 Update에서 처리합니다.
        private void Update()
        {
            if (!isInit) Init();
            if (blackboard is null) return;

            blackboard.UpdateCooldownList();
            Think(); // 매 프레임 '무슨 상태가 될지'를 결정합니다.
        }

        // 2. 행동(Act)은 Update 이후에 처리합니다. (LateUpdate도 좋고, Update 마지막에 호출해도 좋습니다)
        // 여기서는 명확한 분리를 위해 LateUpdate를 사용하겠습니다.
        private void LateUpdate()
        {
            if (!isInit || blackboard is null) return;
            
            Act(); // 결정된 현재 상태에 따라 '무엇을 할지'를 실행합니다.
        }

        #endregion

        #region Core FSM Methods: Think & Act

        /// <summary>
        /// AI의 두뇌 역할: 모든 조건을 검사하여 어떤 상태로 전환할지 결정(판단)합니다.
        /// 기존의 Tink() 메서드와 동일합니다.
        /// </summary>
        private void Think()
        {
            if (blackboard.State.GetStates() == "None")
            {
                ChangeState("Idle");
                return;
            }
            
            if (blackboard.CurrentHealth <= 0)
            {
                if (blackboard.State.GetStates() != "Death") ChangeState("Death");
                return;
            }
            
            // Debug.Log($"Current HP: {blackboard.CurrentHealth}/{blackboard.MaxHealth}");
            
            // 공격이 진행 중일 때는 다른 판단을 하지 않도록 하여 상태를 유지합니다.
            if (_isAttack)
            {
                if (blackboard.State.GetStates() != "Attack") ChangeState("Attack");
                return;
            }
            
            // 사용 가능한 스킬 검사 (우선순위가 가장 높음)
            if (blackboard.Skills is not null && blackboard.Skills.Count != 0)
            {
                foreach (var skill in blackboard.Skills)
                {
                    var skillId = skill.Value.GetStat(EStatType.ID);
                    if (!blackboard.IsSkillReady((int)skillId)) continue;

                    float skillRange = skill.Value.GetStat(EStatType.Range);
                    float distanceToPlayer = Vector3.Distance(transform.position, blackboard.Target.transform.position);
                        
                    if (distanceToPlayer <= skillRange)
                    {
                        _skillRowData = skill.Value;
                        ChangeState("Attack");
                        return;
                    }
                        
                    // 사거리 밖에 있으면 추격 상태로 전환
                    if (distanceToPlayer > skillRange && distanceToPlayer <= blackboard.MaxDetectionRange)
                    {
                        ChangeState("Chase");
                        return;
                    }
                }
            }
            
            // 플레이어와의 거리 검사
            float distance = Vector3.Distance(transform.position, blackboard.Target.transform.position);
            if (distance <= blackboard.MaxDetectionRange || blackboard.CurrentHealth < blackboard.MaxHealth)
            {
                ChangeState("Chase");
                return;
            }
            
            // Manager로 부터 주변에 몬스터가 전투와 관련된 상태인지 확인
            GameObject[] monsters = MonsterManager.Instance.GetBattleMonsters();
            foreach (var monster in monsters)
            {
                // 자신이 아닌 몬스터만 검사
                if (monster == gameObject) continue;

                // 인지 범위 내에 있는지 검사
                float dist = Vector3.Distance(transform.position, monster.transform.position);
                if (dist > blackboard.MaxDetectionRange) continue;

                ChangeState("Chase");
                return;
            }

            if (blackboard.PatrolInfo.wayPoints is { Length: > 0 })
            {
                // 위의 모든 조건이 아닐 경우, 기본 행동(순찰 또는 대기)으로 돌아갑니다.
                if (blackboard.PatrolInfo.isPatrol)
                {
                    ChangeState("Patrol");
                    return;
                }

                if (_waitTimer <= 0f)
                {
                    ChangeState("Patrol");
                    _waitTimer = Random.Range(3f, 7f);
                }
            }
            else
            {
                ChangeState("Idle");
                _waitTimer -= Time.deltaTime;
            }
        }

        /// <summary>
        /// AI의 몸 역할: 현재 상태(State)에 따라 실제 행동을 수행합니다.
        /// 기존의 모든 Handle...State() 메서드를 통합했습니다.
        /// </summary>
        private void Act()
        {
            string stateName = blackboard.State?.GetStates() ?? "None";
            
            switch (stateName)
            {
                case "None":
                    // 아무 것도 하지 않음
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
        
        #endregion

        #region State Actions (Act에서 호출되는 행동 함수들)

        private void ActDeath()
        {
            // 죽음 애니메이션이 끝나면 오브젝트를 비활성화합니다.
            // AnimatorStateInfo animatorStateInfo = blackboard.AnimatorParameterSetter.CurrentAnimatorStateInfo;
            // if (animatorStateInfo.IsName("Death") && animatorStateInfo.normalizedTime >= 1.0f)
            // {
            //     blackboard.DeathEffect?.SetActive(true);
            //     isInit = false;
            //     gameObject.SetActive(false);
            // }
            
            // 1. 죽음 애니메이션을 가지고 있는지 확인
            foreach (var state in blackboard.AnimatorParameterSetter.BoolParameterNames)
            {
                if (state == "IsDeath")
                {
                    AnimatorStateInfo animatorStateInfo = blackboard.AnimatorParameterSetter.CurrentAnimatorStateInfo;
                    if (animatorStateInfo.IsName("Death") && animatorStateInfo.normalizedTime >= 1.0f)
                    {
                        blackboard.DeathEffect?.SetActive(true);
                        isInit = false;
                        gameObject.SetActive(false);
                    }
                    return; // 죽음 애니메이션이 있으면 여기서 종료
                }
            }
            // 2. 죽음 이팩트가 있는지 확인
            if (blackboard.DeathEffect is not null)
            {
                var effect = blackboard.DeathEffect;
                
                // 이팩트가 있으면 활성화 시키고 이팩트가 종료 될때까지 대기
                effect.SetActive(true);
                var particleSystem = effect.GetComponent<ParticleSystem>();
                if (particleSystem is not null)
                {
                    if (!particleSystem.isPlaying)
                        particleSystem.Play();
                    
                    StartCoroutine(WaitForParticleEnd(particleSystem));
                }
            }
        }

        private void ActPatrol()
        {
            // 목표 지점에 도착했는지 확인하고 다음 행동을 결정합니다.
            if (blackboard.NavMeshAgent.remainingDistance <= blackboard.NavMeshAgent.stoppingDistance && !blackboard.NavMeshAgent.pathPending)
            {
                blackboard.PatrolInfo.isPatrol = false; // isPatrol을 false로 만들어 Think()가 다음 판단(대기 또는 새 순찰)을 하도록 유도
                blackboard.AnimatorParameterSetter.Animator.SetBool("IsWalk", false);
            }
        }

        private void ActChase()
        {
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
            if (_skillRowData is null || blackboard.Target is null) return;
            
            // 타겟을 바라보게 합니다.
            transform.LookAt(blackboard.Target.transform);

            int selectedSkillId = (int)_skillRowData.GetStat(EStatType.ID);
            float skillCooldown = _skillRowData.GetStat(EStatType.CooldownTime);

            switch (selectedSkillId)
            {
                case 4001: // 단일 공격
                    if (!_isAttack)
                    {
                        _isAttack = true;
                        // blackboard.AnimatorParameterSetter.Animator.SetBool("IsFire", true);
                        blackboard.AnimatorParameterSetter.Animator.SetTrigger("Fire");
                        blackboard.ApplyCooldown(selectedSkillId, skillCooldown);
                    }
                    break;
                case 4002:
                    if (!_isAttack)
                    {
                        _isAttack = true;
                        StartCoroutine(ChargingNShoot());
                        blackboard.ApplyCooldown(selectedSkillId, skillCooldown);
                    }
                    break;
                case 4003:
                    if (!_isAttack)
                    {
                        _isAttack = true;
                        blackboard.AnimatorParameterSetter.Animator.SetTrigger("Fire");
                        blackboard.ApplyCooldown(selectedSkillId, skillCooldown);
                    }
                    break;
                case 4006:
                    if (!_isAttack)
                    {
                        _isAttack = true;
                        blackboard.AnimatorParameterSetter.Animator.SetTrigger("Smash");
                        blackboard.ApplyCooldown(selectedSkillId, skillCooldown);
                    }
                    break;
                // case 4002, 4003 등 다른 스킬 로직... (기존 코드와 유사하게 작성)
            }
        }

        #endregion

        #region State Management

        // 상태 진입 시 1회 호출되는 초기화 메서드 (기존 코드와 동일)
        private void EnterState(string stateName)
        {
            InitAnimationFlags();
            blackboard.NavMeshAgent.isStopped = false;

            switch (stateName)
            {
                case "Idle":
                    blackboard.NavMeshAgent.isStopped = true;
                    break;
                case "Death":
                    blackboard.AgentCollider.enabled = false;
                    blackboard.AgentRigidbody.isKinematic = true;
                    blackboard.NavMeshAgent.isStopped = true;
                    blackboard.NavMeshAgent.ResetPath();
                    blackboard.AnimatorParameterSetter.Animator.SetBool("IsDeath", true);
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
            
        // 상태 전환 메서드 (기존 코드와 거의 동일)
        private void ChangeState(string stateName)
        {
            if (blackboard.State.GetStates() == stateName) return; // 같은 상태로의 변경 방지

            int mask = DynamicState.GetStateMask(stateName);
            if (mask == 0) return;
            
            blackboard.State.SetState(mask);
            EnterState(stateName);
            // Debug.Log($"State changed to: {stateName}");
        }
        
        #endregion

        #region Helper & Event Methods
        
        private void Init()
        {
            blackboard.Init();
            isInit = true;
        }

        private void InitAnimationFlags()
        {
            List<string> array = blackboard.AnimatorParameterSetter.BoolParameterNames;
            Animator animator = blackboard.AnimatorParameterSetter.Animator;
            foreach (string param in array)
            {
                animator.SetBool(param, false);
            }
        }
        
        public void ApplyDamage(float inDamage)
        {
            OnHit(inDamage);
        }
        
        public void OnHit(float damage)
        {
            if (blackboard is null) return;
            blackboard.CurrentHealth -= damage;
            // 피격 시 추적 상태로 즉시 전환하는 로직 추가 가능
            // if (blackboard.CurrentHealth > 0) ChangeState("Chase");
        }
        
        private void FireBullet(int bulletType = 0)
        {
            Vector3 startPos = blackboard.AttackInfo.firePoint.position;
            Vector3 targetPos = blackboard.Target.transform.position + Vector3.up * 1.5f;
            Vector3 direction = (targetPos - startPos).normalized;
            
            blackboard.AttackInfo.Fire(bulletType, blackboard.Agent, blackboard.AttackInfo.firePoint.position, Vector3.zero, direction, _skillRowData.GetStat(EStatType.Damage));
        }
        
        private IEnumerator ChargingNShoot()
        {
            yield return new WaitForSeconds(2f); // 2초 대기
            blackboard.AnimatorParameterSetter.Animator.SetTrigger("Fire");
        }
        
        public void AnimationEvent_Fire()
        {
            if ((int)_skillRowData.GetStat(EStatType.ID) == 4003)
                FireBullet(1);
            else
                FireBullet();
        }
        
        public void AnimationEvent_Melee()
        {
            if (blackboard.Target == null || _skillRowData == null) return;

            float damage = _skillRowData.GetStat(EStatType.Damage);
            // blackboard.DealDamage(blackboard.Target, damage);
            Debug.Log($"");
        }
        
        public void OnAttackAnimationEnd()
        {
            _isAttack = false;
            _skillRowData = null; // 스킬 사용 완료
            ChangeState("Idle");
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
        
        private void OnDrawGizmos()
        {
            if (blackboard is null) return;
            // 몬스터의 위치를 기준으로 인식 범위를 원으로 표시
            if (blackboard.Agent is not null && blackboard.MaxDetectionRange > 0f)
            {
                // Gizmos의 색상을 설정
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(blackboard.Agent.transform.position, blackboard.MaxDetectionRange);
            }

            if (blackboard.Agent is not null &&
                blackboard.MinDetectionRange > 0f)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(blackboard.Agent.transform.position, blackboard.MinDetectionRange);
            }
            
            // 몬스터의 스킬 사거리를 시각적으로 표시
            if (blackboard.Skills is not null && blackboard.Skills.Count > 0)
            {
                foreach (var skill in blackboard.Skills)
                {
                    var skillRange = skill.Value.Stats[EStatType.Range].GetValue();
                    if (skillRange <= 0f) continue;

                    Gizmos.color = Color.blue;
                    if (blackboard.Agent is not null)
                        Gizmos.DrawWireSphere(blackboard.Agent.transform.position, skillRange);
                }
            }
        
            // Patrol Waypoints를 시각적으로 표시
            if (blackboard.PatrolInfo.wayPoints is { Length: > 0 })
            {
                Gizmos.color = Color.yellow;
                foreach (var t in blackboard.PatrolInfo.wayPoints)
                {
                    Gizmos.DrawSphere(t, 0.1f);
                }
            }
            
            // 몬스터의 배회 범위를 시각적으로 표시
            if (blackboard.WanderInfo.wanderAreaRadius > 0f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(blackboard.WanderInfo.wanderAreaCenter, blackboard.WanderInfo.wanderAreaRadius);
            }
            
            // 추가 정보는 아래에 추가
        }

        #endregion
    }
}