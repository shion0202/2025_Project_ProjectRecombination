using _Test.Skills;
using Managers;
using Monster.AI.Blackboard;
using Monster.AI.Command;
using Script.CustomCollections;
using System;
using UnityEngine;

namespace Monster.AI
{
    public readonly struct NodeContext
    {
        public readonly Blackboard.Blackboard Blackboard;
        public readonly Action<AICommand, int> Enqueue;
        
        public NodeContext(Blackboard.Blackboard blackboard, Action<AICommand, int> enqueue)
        {
            Blackboard = blackboard;
            Enqueue = enqueue;
        }
    }
    
    [Serializable]
    public struct BTData
    {
        // public EState state;
        public string stateName;
        public BehaviorTree.BehaviorTree behaviorTree;
    }
    
    public sealed class AIController : MonoBehaviour, IDamagable
    {
        [SerializeField] private Blackboard.Blackboard blackboard;
        [SerializeField] private BehaviorTree.BehaviorTree globalBehaviorTree; // GlobalBehaviorTree;
        [SerializeField] private BTData[] data;
        
        /// <summary>
        /// <para>AICommandQueue를 구현하여 AI 명령어를 큐에 저장하고 처리</para>
        /// 우선순위 큐를 활용해 우선순위에 따라 명령어를 처리할 수 있도록 개선
        /// </summary>
        private PriorityQueue<AICommand> _queue;
        
        private int _currentPriority = -999; // 현재 실행 중인 명령어의 우선순위(초기값은 매우 낮게 설정)
        // private Coroutine _currentCoroutine;
        private AICommand _currentCommand;
        
        private bool _isInit;
        
        public Blackboard.Blackboard Blackboard => blackboard;

        #region queue methods

        public void EnqueueCommand(AICommand command, int priority = 0)
        {
            if (command == null) return;
            _queue?.Enqueue(command, priority);
        }
        
        public (AICommand item, int priority) DequeueCommand()
        {
            return _queue.Count == 0 ? (null, 0) : _queue.DequeueRow();
        }

        #endregion
        
        #region Unity Methods

        private void Awake()
        {
            // 초기화
            Init();
            // MonsterManager.Instance.AddMonster(gameObject);
        }

        private void OnEnable()
        {
            // 폴리싱을 위해 AIController가 활성화될 때 초기화 작업을 수행
            Init();
            MonsterManager.Instance.AddMonster(gameObject);
        }
        
        // TODO: PoolManager에 릴리즈 후 다시 활성화 될 때 몬스터가 동작하지 않는 문제 발생 (이전에 몬스터 사망 처리 후 재활성화 되는 경우)
        private void OnDisable()
        {
            _isInit = false;
        }
        
        // private void Start() => tree?.Init();
        
        // Blackboard의 상태를 매 프레임마다 업데이트할 필요가 있을 경우 사용
        private void Update()
        {
            // Debug.Log($"{blackboard.CurrentHealth}/{blackboard.MaxHealth}");
            Sense();
            Think();
        }
        
        // LateUpdate는 매 프레임 후에 호출되므로, AI 명령어 큐를 처리하는 데 적합
        private void LateUpdate()
        {
            Act();
        }
        
        // Gizmo를 사용하여 몬스터의 인식 범위를 시각적으로 표시
        // private void OnDrawGizmosSelected()
        private void OnDrawGizmos()
        {
            if (blackboard is null) return;
            // #if UNITY_EDITOR
            // blackboard.InitMonsterStatsByID();
            // #endif
            // 몬스터의 위치를 기준으로 인식 범위를 원으로 표시
            if (blackboard.Agent is not null && blackboard.TryGet(new BBKey<float>("maxDetectionRange"), out var detectionRange) && detectionRange > 0f)
            {
                // Gizmos의 색상을 설정
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(blackboard.Agent.transform.position, detectionRange);
            }

            if (blackboard.Agent is not null &&
                blackboard.TryGet(new BBKey<float>("minDetectionRange"), out var detectionRange2) &&
                detectionRange2 > 0f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(blackboard.Agent.transform.position, detectionRange2);
            }
            
            // 몬스터의 스킬 사거리를 시각적으로 표시
            if (blackboard.Skills is not null && blackboard.Skills.Length > 0)
            {
                foreach (Skill skill in blackboard.Skills)
                {
                    var skillRange = skill.skillData.range;
                    if (!(skillRange > 0f)) continue;

                    Gizmos.color = Color.red;
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

        #region AI Methods

        /// <summary>
        /// <para>AIController의 초기화 작업을 수행</para>
        /// Awake, OnEnable 등에서 호출되어 블랙보드와 큐를 초기화
        /// </summary>
        private void Init()
        {
            if (_isInit) return;
            if (blackboard == null)
            {
                blackboard = GetComponent<Blackboard.Blackboard>();
            }
            if (blackboard == null)
            {
                Debug.LogError("Blackboard component is missing on the AIController.");
                return;
            }
            
            blackboard.Init();
            
            _queue?.Clear();
            _queue = PriorityQueue<AICommand>.CreateMinPriorityQueue();
            _isInit = true;
            // Debug.Log($"{this.ToString()}");
        }
        
        /// <summary>
        /// <para>AI의 상태를 감지하고, 필요한 정보를 업데이트하는 로직</para>
        /// 예: 타겟의 위치, 쿨타임 업데이트 등
        /// </summary>
        private void Sense()
        {
            // Debug.Log(blackboard?.ToStringCooldown());
            // 블랙보드에서 관리되는 쿨타임 목록을 업데이트
            // blackboard?.UpdateCooldownList();
        }

        /// <summary>
        /// <para>AI의 상태를 업데이트하고, 행동을 결정하는 로직</para>
        /// 예: 타겟을 감지하고, 공격 여부를 결정하는 등
        /// </summary>
        private void Think()
        {
            // 블랙보드의 상태를 기반으로 행동 트리를 실행
            globalBehaviorTree?.Tick(new NodeContext(Blackboard, EnqueueCommand));

            foreach (BTData btData in data)
            {
                if (!blackboard.State.HasState(btData.stateName)) continue;
                btData.behaviorTree?.Tick(new NodeContext(Blackboard, EnqueueCommand));
            }
        }

        /// <summary>
        /// <para>AI 행동을 처리하는 로직</para>
        /// 예: 공격, 이동, 대기 등
        /// </summary>
        // private void Act()
        // {
        //     // 큐가 비어있으면 아무 것도 하지 않음
        //     if (_queue.Count == 0) return;
        //     
        //     // if (blackboard.IsCoroutineRunning()) return;
        //     
        //     // 큐에 명령어가 있는 경우, 첫 번째 명령어를 처리
        //     (AICommand command, int priority) = DequeueCommand();
        //     // Debug.Log(command + $"{priority}");
        //
        //     // 현재 실행할 명령어가 DeathCommand인 경우
        //     if (command is DeathCommand)
        //     {
        //         // if (blackboard.State is MonsterState.Death) return;
        //         
        //         // // 즉시 DeathCommand 실행
        //         // _currentCoroutine = StartCoroutine(command.Execute(blackboard, () =>
        //         // {
        //         //     // Debug.Log("DeathCommand completed. Releasing to PoolManager.");
        //         //     _currentPriority = -999;
        //         //     
        //         //     // 비활성화될 때 몬스터 매니저에서 제거
        //         //     MonsterManager.Instance.RemoveMonster(gameObject);
        //         //     
        //         //     // 사망 애니메이션이 끝난 후 오브젝트 풀에 반환
        //         //     PoolManager.Instance.ReleaseObject(gameObject);
        //         // }));
        //         
        //         // 즉시 DeathCommand 실행
        //         command.Execute(blackboard, () =>
        //         {
        //             // Debug.Log("DeathCommand completed. Releasing to PoolManager.");
        //             _currentPriority = -999;
        //             
        //             // 비활성화될 때 몬스터 매니저에서 제거
        //             MonsterManager.Instance.RemoveMonster(gameObject);
        //             
        //             // 사망 애니메이션이 끝난 후 오브젝트 풀에 반환
        //             PoolManager.Instance.ReleaseObject(gameObject);
        //         });
        //         
        //         // 명령어 실행 후 큐 청소
        //         _queue.CleanUp();
        //         return;
        //     }
        //     
        //     if (_currentPriority < priority)
        //     {
        //         _currentPriority = priority;    // 현재 우선순위를 업데이트(다른 명령어가 더 높은 우선순위를 가질 때만 업데이트)
        //         
        //         // // 현재 실행 중인 코루틴이 있으면 중지
        //         // if (_currentCoroutine != null)
        //         // {
        //         //     StopCoroutine(_currentCoroutine);
        //         //     _currentCoroutine = null;
        //         // }
        //         
        //         // 명령어를 실행
        //         // _currentCoroutine = StartCoroutine(command?.Execute(blackboard, () =>
        //         // {
        //         //     _currentPriority = -999;
        //         // }));
        //         command?.Execute(blackboard, () =>
        //         {
        //             _currentPriority = -999;
        //         });
        //     }
        //     // 명령어 실행 후 큐 청소
        //     _queue.CleanUp();
        // }

        private void Act()
        {
            if (_queue.Count == 0)
            {
                ResetCommandState();
                return;
            }
        
            (AICommand command, int priority) = DequeueCommand();
            _queue.CleanUp();

            if (_currentCommand is not null)
            {
                if (_currentPriority < priority)
                {
                    _currentCommand.OnExit(blackboard);
                    _currentPriority = priority;
                    _currentCommand = command;
                    _currentCommand.OnEnter(blackboard, ResetCommandState);
                }
                else
                {
                    _currentCommand.Execute(blackboard, ResetCommandState);
                }
            }
            else
            {
                _currentPriority = priority;
                _currentCommand = command;
                _currentCommand.OnEnter(blackboard, ResetCommandState);
            }
            
            return;

            void ResetCommandState()
            {
                _currentCommand = null;
                _currentPriority = -999;
            }
        }

        #endregion
        
        #region Public Methods
        public void ApplyDamage(float inDamage, LayerMask targetMask = default, float unitOfTime = 1.0f, float defenceIgnoreRate = 0.0f)
        {
            OnHit(inDamage);
        }

        // 피격 이벤트 처리
        public void OnHit(float damage, AICommand command = null, int priority = 0)
        {
            if (blackboard == null) return;
            blackboard.CurrentHealth -= damage;
            
            // 피격 처리 (예: 피격 애니메이션 재생)
            if (command != null)
                EnqueueCommand(command, priority);
            else
                EnqueueCommand(new HitCommand(), 100); // 피격 명령어를 높은 우선순위로 큐에 추가
        }

        public override string ToString()
        {
            string result = "AIController State:\n";
            result += $"- Name: {gameObject.name}\n";
            result += $"- Current Health: {blackboard?.CurrentHealth}/{blackboard?.MaxHealth}\n";
            result += $"- Current State: {blackboard?.State}\n";
            result += $"- Queue Count: {_queue?.Count}\n";
            result += $"- Current Priority: {_currentPriority}\n";
            result += $"- Blackboard: {blackboard}\n";
            result += $"- Queue: {_queue}\n";
            return result;
        }
        
        #endregion
    }
}