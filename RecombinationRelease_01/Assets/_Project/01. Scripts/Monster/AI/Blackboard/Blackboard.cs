using _Project._01._Scripts.Monster.Animator;
using Managers;
using Monster.AI.Command;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Monster.AI.Blackboard
{
    public class Blackboard : MonoBehaviour
    {
        #region SerializeFields
        
        [Tooltip("몬스터의 현재 상태를 나타냅니다.")]
        [SerializeField] private MonsterState state;
        // [SerializeField] private MonsterAction action; // 현재 행동 상태 (공격, 스킬 사용 등)
        [SerializeField] private int id;
        
        [Header("Dependency")] // 의존성 주입
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private GameObject agent;          // AI 에이전트 (몬스터)
        [SerializeField] private Collider agentCollider;
        [SerializeField] private Rigidbody agentRigidbody;
        [SerializeField] private AnimatorParameterSetter animatorParameterSetter;
        [SerializeField] private GameObject target;
        [SerializeField] private GameObject deathEffect;    // 몬스터 사망 이펙트 프리팹
        // [SerializeField] private LayerMask obstacleLayer;
        
        // [Header("Battle Setting")][SerializeField] private MonsterBase baseInfo;   // 몬스터 기본 정보 스크립트
        [SerializeField] private MonsterPatrol patrolInfo; // 몬스터 순찰 스크립트
        [SerializeField] private MonsterWander wanderInfo; // 몬스터 방황 스크립트
        [SerializeField] private MonsterAttack attackInfo; // 몬스터 공격 스크립트

        #endregion
        
        #region private Fields
        
        private List<(int, float)> _cooldownList = new();    // 스킬의 쿨타입을 적용해 사용 가능 여부 관리
        
        #endregion
        
        #region Properties
        
        public MonsterState State
        {
            get => state;
            set => state = value;
        }
        
        // public MonsterAction Action
        // {
        //     get => action;
        //     set => action = value;
        // }
        
        public NavMeshAgent NavMeshAgent
        {
            get => navMeshAgent;
            set => navMeshAgent = value;
        }
        
        public GameObject Agent
        {
            get => agent;
            set => agent = value;
        }
        
        public Collider AgentCollider
        {
            get => agentCollider;
            set => agentCollider = value;
        }
        
        public Rigidbody AgentRigidbody
        {
            get => agentRigidbody;
            set => agentRigidbody = value;
        }
        
        public AnimatorParameterSetter AnimatorParameterSetter
        {
            get => animatorParameterSetter;
            set => animatorParameterSetter = value;
        }
        
        public GameObject Target
        {
            get => target;
            set => target = value;
        }
        
        public GameObject DeathEffect
        {
            get => deathEffect;
            set => deathEffect = value;
        }
        
        public MonsterPatrol PatrolInfo
        {
            get => patrolInfo;
            set => patrolInfo = value;
        }
        
        public MonsterWander WanderInfo
        {
            get => wanderInfo;
            set => wanderInfo = value;
        }
        
        public MonsterAttack AttackInfo
        {
            get => attackInfo;
            set => attackInfo = value;
        }
        
        public float CurrentHealth
        {
            get => TryGet(new BBKey<float>("health"), out float health) ? health : 0f;
            set => Set(new BBKey<float>("health"), value);
        }
        
        public float MaxHealth => TryGet(new BBKey<float>("maxHealth"), out float maxHealth) ? maxHealth : 0f;
        
        public float RunSpeed => TryGet(new BBKey<float>("runSpeed"), out float runSpeed) ? runSpeed : 0f;
        public float WalkSpeed => TryGet(new BBKey<float>("walkSpeed"), out float walkSpeed) ? walkSpeed : 0f;
        public float MinDetectionRange => TryGet(new BBKey<float>("minDetectionRange"), out float minDetectionRange) ? minDetectionRange : 0f;
        public float MaxDetectionRange => TryGet(new BBKey<float>("maxDetectionRange"), out float maxDetectionRange) ? maxDetectionRange : 0f;

        #endregion

        #region Dictionary

        private readonly Dictionary<string, object> _map = new();
        
        public void Set<T>(BBKey<T> key, T value) => _map[key.Name] = value;

        public bool TryGet<T>(BBKey<T> key, out T value)
        {
            if (_map.TryGetValue(key.Name, out var obj) && obj is T t) 
            { 
                value = t;
                return true;
            }

            value = default!;
            return false;
        }

        public Dictionary<int, RowData> Skills { get; } = new();

        #endregion

        #region Methods

        public void InitMonsterStatsByID()
        {
            // 몬스터의 인덱스 번호 확인
            if (id < 1000 || id > 1999) return;

            RowData defaultStats = DataManager.Instance.GetRowDataByIndex("CharacterStats", id);
                
            if (defaultStats == null)
            {
                Debug.Log("Default stats not found for index: " + id);
                return;
            }
            // Debug.Log(defaultStats.ToString()); // 디버그용: 기본 스탯 데이터 확인
            
            // 몬스터 스탯 초기화
            {
                _map["health"] = defaultStats.GetStat(EStatType.MaxHp);                  // AI의 현재 체력
                _map["maxHealth"] = defaultStats.GetStat(EStatType.MaxHp);
                _map["runSpeed"] = defaultStats.GetStat(EStatType.WalkSpeed);                // 걷기 이동 속도
                _map["walkSpeed"] = defaultStats.GetStat(EStatType.RunSpeed);                  // 뛰기 이동 속도
                _map["damage"] = defaultStats.GetStat(EStatType.Damage);                   // 공격력
                _map["defence"] = defaultStats.GetStat(EStatType.Defence);                   // 방어력
                _map["minDetectionRange"] = defaultStats.GetStat(EStatType.MinDetectiveRange);          // 타겟 인식 범위 (최소)
                _map["maxDetectionRange"] = defaultStats.GetStat(EStatType.MaxDetectiveRange);           // 타겟 인식 범위
            }
            
            // 몬스터 스킬 초기화
            {
                Skills.Clear();
                RowData skillData = DataManager.Instance.GetRowDataByIndex("Monster2Skill", id);
                // Debug.Log("SkillData: " + skillData.GetStringStat(EStatType.IdArray));
                // 스페이스바로 구분된 스킬 ID 문자열을 가져와서 배열로 변환
                string[] skillIds = skillData.GetStringStat(EStatType.IdArray).Split(' ');
                // 배열에 값이 없는 경우
                
                if (skillIds.Length > 0 && !string.IsNullOrWhiteSpace(skillIds[0]))
                {
                    foreach (string skillId in skillIds)
                    {
                        if (!int.TryParse(skillId, out int id)) continue;

                        // 스킬 ID에 해당하는 RowData를 가져옴
                        RowData skillRow = DataManager.Instance.GetRowDataByIndex("MonsterSkill", id);
                        if (skillRow != null)
                        {
                            Skills[id] = skillRow;
                        }
                    }
                }
                else
                {
                    var value = skillData.GetStat(EStatType.IdArray);
                    if (value > 0f && int.TryParse(value.ToString(), out int id))
                    {
                        RowData skillRow = DataManager.Instance.GetRowDataByIndex("MonsterSkill", id);
                        if (skillRow != null)
                        {
                            Skills[id] = skillRow;
                        }
                    }
                }
            }
        }

        public void Init()
        {
            InitMonsterStatsByID();
            agentCollider.enabled = true;
            agentRigidbody.isKinematic = false;
            NavMeshAgent.isStopped = false;
            NavMeshAgent.ResetPath();
            CurrentHealth = MaxHealth;
            State.SetState("Idle");
            Target = MonsterManager.Instance.Player;
        }

        public void Clear()
        {
            _map.Clear();
        }
        
        #endregion

        #region Cooldown Management

        // 쿨타임 적용
        public void ApplyCooldown(int skillId, float cooldownTime)
        {
            // 이미 쿨타임이 적용된 스킬인지 확인
            for (int i = 0; i < _cooldownList.Count; i++)
            {
                if (_cooldownList[i].Item1 == skillId)
                {
                    // 기존 쿨타임 업데이트
                    _cooldownList[i] = (skillId, cooldownTime);
                    return;
                }
            }
            // 새로운 스킬 추가
            _cooldownList.Add((skillId, cooldownTime));
        }
        
        // 쿨타임이 끝났는지 확인
        public bool IsSkillReady(int skillId)
        {
            // TODO: 쿨타임이 적용된 스킬인지 확인이 안됨
            for (int i = 0; i < _cooldownList.Count; i++)
            {
                // 쿨타임이 적용된 스킬인지 확인
                if (_cooldownList[i].Item1 == skillId)
                {
                    // 쿨타임이 아직 남아있음
                    // Debug.Log($"Skill {skillId} is ready: {_cooldownList[i].Item2} seconds remaining.");
                    return false;
                }
            }
            // 쿨타임이 적용되지 않은 스킬
            return true;
        }
        
        // 쿨타임 목록에서 스킬 제거 (현재 시간 기준으로 쿨타임이 끝난 스킬 제거)
        public void UpdateCooldownList()
        {
            if (_cooldownList.Count == 0) return;
            float currentTime = Time.time;
            for (int i = _cooldownList.Count - 1; i >= 0; i--)
            {
                // 쿨타임이 끝난 스킬 제거
                if (_cooldownList[i].Item2 <= currentTime)
                {
                    _cooldownList.RemoveAt(i);
                }
            }
        }

        public void CleanUpCooldownList()
        {
            // 쿨타임 목록을 초기화
            _cooldownList.Clear();
        }

        public string ToStringCooldown()
        {
            string result = $"Blackboard for {gameObject.name}:\n";
            // result += $"State: {state}\n";
            // result += $"Index: {index}\n";
            // result += $"Target: {target?.name ?? "None"}\n";
            // result += $"NavMeshAgent: {navMeshAgent?.name ?? "None"}\n";
            // result += $"PatrolInfo: {patrolInfo?.name ?? "None"}\n";
            // result += $"WanderInfo: {wanderInfo?.name ?? "None"}\n";
            // result += "Cooldown List:\n";

            foreach (var cooldown in _cooldownList)
            {
                result += $"- Skill ID: {cooldown.Item1}, Remaining Time: {cooldown.Item2}\n";
            }

            return result;
        }

        #endregion
        
    }
}