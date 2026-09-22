using _Project._01._Scripts.Monster.Animator;
using _Test.Skills;
using FIMSpace.FProceduralAnimation;
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
        [SerializeField] private CharacterController characterController;
        [SerializeField] private AnimatorParameterSetter animatorParameterSetter;
        [SerializeField] private GameObject target;
        [SerializeField] private GameObject deathEffect;    // 몬스터 사망 이펙트 프리팹
        [SerializeField] private GameObject[] hitEffects;   // 몬스터 피격 이펙트 프리팹들
        [SerializeField] private RagdollController ragdollController;
        [SerializeField] private MonsterDissolve[] dissolve; // 몬스터 디졸브 스크립트
        
        [SerializeField] private MonsterPatrol patrolInfo; // 몬스터 순찰 스크립트
        [SerializeField] private MonsterWander wanderInfo; // 몬스터 방황 스크립트
        [SerializeField] private MonsterAttack attackInfo; // 몬스터 공격 스크립트

        [Header("오디오 리소스")]
        [SerializeField] private AudioSource audioSource;
        
        [Header("Leg Animator")]
        [SerializeField] private LegsAnimator legAnimator;
        // [SerializeField] private AudioClip[] clips;
        
        // [Header("원거리 몬스터 전용 변수")] 
        // [SerializeField] private Transform firePoint;

        [Header("아몬 2페이즈 전용 변수")]
        [SerializeField] private GameObject amonBody;
        [SerializeField] private GameObject amonShield;
        [SerializeField] private GameObject amonEnergyBall;
        [SerializeField] private GameObject amonDeathModel;
        [Tooltip("아레나 기준 앵커. 스킬의 워프/스폰 좌표를 이 Transform 로컬 기준으로 해석한다. 미할당 시 월드 좌표로 폴백.")]
        [SerializeField] private Transform arenaAnchor;
        
        #endregion
        
        #region Properties
        
        public MonsterState State
        {
            get => state;
            set => state = value;
        }
        
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
        
        public CharacterController CharacterController
        {
            get => characterController;
            set => characterController = value;
        }
        
        public AnimatorParameterSetter AnimatorParameterSetter
        {
            get => animatorParameterSetter;
            set => animatorParameterSetter = value;
        }
        
        public GameObject Target
        {
            get => target;
            private set => target = value;
        }

        /// <summary>
        /// 추적 대상을 임시로 바꾼다.
        /// 플레이어가 스킬로 화면 밖에 숨겨지는 동안(LegsEnhanced의 착지점 조작 등)
        /// 몬스터가 그 좌표를 쫓아 맵 밖으로 나가는 것을 막기 위해 사용한다.
        /// Init()이 호출되면 다시 플레이어로 복구된다.
        /// </summary>
        public void SetTarget(GameObject newTarget)
        {
            Target = newTarget;
        }
        
        public GameObject DeathEffect
        {
            get => deathEffect;
            set => deathEffect = value;
        }
        
        public GameObject[] HitEffects
        {
            get => hitEffects;
            set => hitEffects = value;
        }
        
        public MonsterDissolve[] Dissolve => dissolve;
        
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
        public bool HasUsedSoulOrbAt20Percent { get; set; }
        public bool HasUsedSoulOrbAt50Percent { get; set; }
        public bool HasUsedSoulOrbAt80Percent { get; set; }
        public string CurrentState { get; set; }
        public bool IsAnySkillRunning {get; private set;}
        public GameObject AmonBody => amonBody;
        public AmonShield AmonShield => amonShield.GetComponent<AmonShield>();
        public AmonEnergyBall AmonEnergyBall => amonEnergyBall.GetComponent<AmonEnergyBall>();

        public Dictionary<string, object> Map => _map;
        
        public GameObject AmonDeathModel => amonDeathModel;
        public Transform ArenaAnchor => arenaAnchor;
        public bool HasUsedSoulAbsorptionAt50Percent { get; set; }
        public bool HasUsedSoulAbsorptionAt20Percent { get; set; }

        /// <summary>
        /// 그로기(행동 불능) 상태가 끝나는 시각(Time.time 기준). 현재 시각이 이 값보다 작으면 행동하지 않는다.
        /// 상태이상 시스템이 없어, 패턴 파훼 보상을 "일정 시간 정지"로 대체하기 위한 최소 장치다.
        /// </summary>
        public float GroggyEndTime { get; set; }

        public bool IsGroggy => Time.time < GroggyEndTime;
        public RagdollController RagdollController { get => ragdollController; set => ragdollController = value; }
        public AudioSource AudioSource { get => audioSource; set => audioSource = value; }
        public LegsAnimator LegAnimator { get => legAnimator; set => legAnimator = value; }
        // public AudioClip[] Clips { get => clips; set => clips = value; }

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

        [SerializeField] private SkillData[] skillDatas;

        [SerializeField] public Skill[] Skills;

        #endregion

        #region Methods

        private void InitMonsterStatsByID()
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

            // 체험 플레이(Demo) 전용 스탯 배수.
            // 시트에서 원본 값을 채운 "직후"에 적용하므로, Init()이 여러 번 호출되어도 중첩되지 않는다.
            // (매번 시트에서 원본을 새로 읽기 때문. 이 블록을 Init 밖으로 옮기면 값이 계속 줄어든다.)
            if (DemoModeContext.IsActive)
            {
                float scaledMaxHp = (float)_map["maxHealth"] * DemoModeContext.BossHealthMultiplier;
                _map["maxHealth"] = scaledMaxHp;
                _map["health"] = scaledMaxHp;
                _map["damage"] = (float)_map["damage"] * DemoModeContext.BossDamageMultiplier;
            }
            
            // 몬스터 스킬 초기화
            {
                Skills = new Skill[skillDatas.Length];
                for (int i = 0; i < skillDatas.Length; i++)
                {
                    if (skillDatas[i] is null) continue;
                    Skills[i] = new Skill(skillDatas[i], this);
                    // 각 스킬의 활성화 및 비활성화 이벤트에 핸들러 등록
                    Skills[i].OnCast += HandleSkillActivation;
                    Skills[i].OnDeactivate += HandleSkillDeactivation;
                }
            }
        }

        public bool HasSkill(int skillID)
        {
            foreach (var skillData in skillDatas)
            {
                if (skillData.skillID == skillID)
                    return true;
            }
            return false;
        }

        public SkillData GetSkillData(int skillID)
        {
            foreach (var skillData in skillDatas)
            {
                if (skillData.skillID == skillID)
                    return skillData;
            }
            return null;
        }

        public void Init()
        {
            InitMonsterStatsByID();
            // DeathEffect?.SetActive(false);
            // RagdollController?.DeactivateRagdoll();
            NavMeshAgent.isStopped = false;
            NavMeshAgent.ResetPath();
            CurrentHealth = MaxHealth;
            GroggyEndTime = 0.0f;

            // 스킬 도중 사망해 코루틴이 강제 종료되면 OnDeactivate가 불리지 않아 true로 남는다.
            // 풀에서 재사용된 몬스터가 "스킬 실행 중"으로 굳어 아무 행동도 안 하는 것을 막는다.
            IsAnySkillRunning = false;

            // 체력 임계값(80/50/20%)으로 한 번씩만 발동하는 패턴의 사용 여부.
            // 위에서 체력을 최대치로 되돌렸으므로 이 플래그들도 함께 초기화해야 한다.
            // 초기화하지 않으면 재초기화된 보스가 임계값을 지나쳐도 해당 패턴을 영영 쓰지 않는다.
            HasUsedSoulOrbAt80Percent = false;
            HasUsedSoulOrbAt50Percent = false;
            HasUsedSoulOrbAt20Percent = false;
            HasUsedSoulAbsorptionAt50Percent = false;
            HasUsedSoulAbsorptionAt20Percent = false;

            State.SetState("Spawn");
            foreach (var dissolve in Dissolve)
                dissolve.Init();
            Target = MonsterManager.Instance.Player;
        }

        public void Clear()
        {
            _map.Clear();
        }
        
        public void RealeseObjectToPool()
        {
            RagdollController?.DeactivateRagdoll();
            DeathEffect?.SetActive(false);
            Init();
            
            PoolManager.Instance.ReleaseObject(Agent);
        }
        
        #endregion
        
        // 어떤 스킬이든 Active 상태가 되면 호출될 핸들러
        private void HandleSkillActivation() => IsAnySkillRunning = true;

        // 어떤 스킬이든 Active 상태가 끝나면 호출될 핸들러
        private void HandleSkillDeactivation() => IsAnySkillRunning = false;
    }
}