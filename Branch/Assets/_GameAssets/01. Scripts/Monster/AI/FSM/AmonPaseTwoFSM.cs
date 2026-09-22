using _Test.Skills;
using Managers;
using Monster.AI.FSM;
using System.Collections;
using UnityEngine;

/// <summary>
/// 1: 데시
/// 2: 날개 휘두르기
/// 3: 소울 스피어 소환
/// 4: 소울 오브 발사
/// 5: 영혼 흡수
/// 6: 락다운
/// 7: 텔레포트
/// </summary>

public class AmonPaseTwoFSM : FSM
{
    private static readonly int IsMoving = Animator.StringToHash("isMoving");
    
    [SerializeField] private GameObject spawnModel;
    [SerializeField] private GameObject deathModel;
    [SerializeField] private GameObject amonBody;
    
    // 연출대기 시간
    [Header("연출 대기 시간")]
    [SerializeField] private float spawnWaitTime = 5f;
    [SerializeField] private float deathWaitTime = 5f;

    private bool _isSpawned;

    // 사망 처리를 한 번만 실행하기 위한 플래그.
    // Act()는 매 프레임 돌기 때문에 없으면 사망 연출 코루틴이 프레임마다 쌓인다.
    private bool _isDying;
    // [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip walkClip;

    private void OnDisable()
    {
        InterruptRunningSkills();
    }

    /// <summary>
    /// 시전/실행 중인 스킬을 강제로 정리한다.
    ///
    /// 스킬이 진행 중인 상태에서 보스가 파괴/비활성화되거나 사망하면
    /// Activate가 실행되지 못해 이펙트/스폰물/안전지대/플레이어 무적이 잔존한다.
    /// Skill.Interrupt가 OnInterrupt로 잔여물을 치운 뒤 코루틴을 멈추고 쿨타임으로 넘긴다.
    /// (스킬 코루틴은 Blackboard가 시작하므로 이 FSM에서 StopCoroutine을 호출하면 멈추지 않는다)
    /// </summary>
    private void InterruptRunningSkills()
    {
        if (blackboard?.Skills == null) return;

        foreach (var skill in blackboard.Skills)
        {
            try
            {
                skill?.Interrupt(blackboard);
            }
            catch (System.Exception e)
            {
                // 한 스킬의 정리 실패가 나머지 스킬 정리를 막지 않도록 로그만 남긴다.
                Debug.LogException(e);
            }
        }
    }
    
    protected override void Think()
    {
        if (!isEnabled || !_isSpawned || blackboard?.Target is null) return; // FSM이 활성화되지 않은 경우 아무 작업도 수행하지 않음(매니저에 의해 활성화 됨)

        // 매 프레임 호출되는 위치라 로그 비용이 그대로 프레임에 실린다. 디버깅할 때만 켤 것.
        // Debug.Log($"{blackboard.CurrentHealth} / {blackboard.MaxHealth}");

        if (blackboard.State.GetStates() == "Death")
        {
            // 사망 후 매 프레임 반복되는 위치. 디버깅할 때만 켤 것.
            // Debug.Log("State is Death");
            return;
        }

        if (blackboard.CurrentHealth <= 0)
        {
            ChangeState("Death");
            return;
        }

        if (blackboard.IsAnySkillRunning)
        {
            // 매 프레임 경로. 디버깅할 때만 켤 것.
            // Debug.Log(blackboard.IsAnySkillRunning);
            return; // 스킬이 실행 중이면 상태 전환을 하지 않음
        }

        // 패턴이 파훼되어 그로기 상태라면 그 시간 동안 아무 패턴도 고르지 않는다.
        // (스킬 실행 중 검사 뒤에 두어, 진행 중인 스킬의 마무리는 방해하지 않는다)
        if (blackboard.IsGroggy)
        {
            ChangeState("Idle");
            return;
        }

        if (blackboard.CurrentHealth <= blackboard.MaxHealth * 0.5f && !blackboard.HasUsedSoulAbsorptionAt50Percent)
        {
            blackboard.HasUsedSoulAbsorptionAt50Percent = true;
            ChangeState("UsingSkill5"); // 영혼 흡수
            return;
        }
        if (blackboard.CurrentHealth <= blackboard.MaxHealth * 0.2f && !blackboard.HasUsedSoulAbsorptionAt20Percent)
        {
            blackboard.HasUsedSoulAbsorptionAt20Percent = true;
            ChangeState("UsingSkill5"); // 영혼 흡수
            return;
        }
        
        // 체력이 50% 이하로 떨어지면 락다운 스킬을 쿨타임 마다 사용
        if (blackboard.CurrentHealth <= blackboard.MaxHealth * 0.5f && blackboard.Skills[5].CurrentState == Skill.SkillState.isReady)
        {
            ChangeState("UsingSkill6"); // 락다운
            return;
        }
        
        // 2페이즈 보스는 플레이어를 직접 추적하며 근접 공격 스킬과 원거리 공격 스킬을 번갈아 사용
        float distanceToTarget = Vector3.Distance(blackboard.transform.position, blackboard.Target.transform.position);

        if (distanceToTarget > blackboard.Skills[6].skillData.range && blackboard.Skills[6].CurrentState == Skill.SkillState.isReady)
        {
            ChangeState("UsingSkill7"); // 텔레포트
        }
        else if (distanceToTarget <= blackboard.Skills[0].skillData.range && blackboard.Skills[0].CurrentState == Skill.SkillState.isReady)
        {
            ChangeState("UsingSkill1"); // 질주 공격
        }
        else if (distanceToTarget <= blackboard.Skills[1].skillData.range && blackboard.Skills[1].CurrentState == Skill.SkillState.isReady)
        {
            ChangeState("UsingSkill2"); // 날개 공격
        }
        else if (distanceToTarget <= blackboard.Skills[3].skillData.range && blackboard.Skills[3].CurrentState == Skill.SkillState.isReady)
        {
            ChangeState("UsingSkill4"); // 소울 오브
        }
        else if (blackboard.Skills[2].CurrentState == Skill.SkillState.isReady)
        {
            ChangeState("UsingSkill3");
        }
        else if (distanceToTarget > blackboard.MinDetectionRange)
        {
            ChangeState("Chase");
        }
        else
        {
            ChangeState("Idle");
        }
    }

    protected override void Act()
    {
        if (!isEnabled || blackboard?.Target is null) return;
        
        string state = blackboard.State.GetStates();
        if (state is null) return;

        // 사망은 스킬 실행 여부와 무관하게 최우선으로 처리한다.
        // 이 검사가 아래 IsAnySkillRunning 아래에 있으면, 패턴 시전 중 죽었을 때
        // Think()가 상태를 Death로 바꿔놓아도 실행이 막혀 패턴이 끝날 때까지 사망 연출이 미뤄진다.
        if (state == "Death")
        {
            if (_isDying) return;
            _isDying = true;

            // 진행 중이던 패턴의 이펙트/스폰물/안전지대를 먼저 치운다.
            // 그냥 두면 보스가 죽은 뒤에도 남아 플레이어를 공격한다.
            InterruptRunningSkills();
            ActDeath();
            return;
        }

        if (blackboard.IsAnySkillRunning)
        {
            // 매 프레임 경로. 디버깅할 때만 켤 것.
            // Debug.Log(blackboard.IsAnySkillRunning);
            return; // 스킬이 실행 중이면 상태 전환을 하지 않음
        }

        switch (state)
        {
            case "Idle":
                // 대기 상태에서 특별한 행동이 필요하지 않음
                break;
            case "Chase":
                ActChase();
                break;
            case "UsingSkill1":
                // 질주 공격
                blackboard.Skills[0].Execute(blackboard);
                break;
            case "UsingSkill2":
                // 날개 공격
                blackboard.Skills[1].Execute(blackboard);
                break;
            case "UsingSkill3":
                // 소울 스피어
                blackboard.Skills[2].Execute(blackboard);
                break;
            case "UsingSkill4":
                // 소울 오브
                blackboard.Skills[3].Execute(blackboard);
                break;
            case "UsingSkill5":
                // 영혼 흡수
                blackboard.Skills[4].Execute(blackboard);
                break;
            case "UsingSkill6":
                // 락다운
                blackboard.Skills[5].Execute(blackboard);
                break;
            case "UsingSkill7":
                // 텔레포트
                blackboard.Skills[6].Execute(blackboard);
                break;
            case "Spawn":
                ActSpawn();
                break;
            // "Death"는 위에서 IsAnySkillRunning보다 먼저 처리하므로 여기까지 오지 않는다.
        }
    }

    private void ActSpawn()
    {
        Debug.Log("Amon Pase Two has spawned.");
        
        spawnModel.SetActive(true);
        amonBody.SetActive(false);
        StartCoroutine(WaitAmonSpawnAnimation());
    }

    private IEnumerator WaitAmonSpawnAnimation()
    {
        yield return new WaitForSeconds(spawnWaitTime);
        
        spawnModel.SetActive(false);
        amonBody.SetActive(true);
        _isSpawned = true;
        ChangeState("Idle");
    }

    private void ActDeath()
    {
        // 사망 처리 로직 구현
        Debug.Log("Amon Pase Two has died.");
        
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
            Destroy(meleeCollision.gameObject);
        
        // 예: 애니메이션 재생, 콜라이더 비활성화, 아이템 드랍 등
        amonBody.SetActive(false);
        deathModel.SetActive(true);
        StartCoroutine(WaitAmonDeathAnimation());
    }
    
    private IEnumerator WaitAmonDeathAnimation()
    {
        // 사망 애니메이션이 재생되는 동안 대기
        yield return new WaitForSeconds(deathWaitTime); // 예: 3초 대기
        
        DungeonManager.Instance.AmonEndPhase();
        Destroy(this);
    }

    private void ActChase()
    {
        if (blackboard.Target is null) return;

        var direction = (blackboard.Target.transform.position - blackboard.transform.position).normalized;
        var chasePosition = blackboard.transform.position + direction * (blackboard.RunSpeed * Time.deltaTime);

        // NavMeshAgent를 사용하여 이동
        if (blackboard.NavMeshAgent is not null)
        {
            blackboard.NavMeshAgent.isStopped = false;
            blackboard.NavMeshAgent.SetDestination(chasePosition);
        }

        // 애니메이션 설정
        blackboard.AnimatorParameterSetter.Animator.SetBool(IsMoving, true);
        
        // 이동 사운드 재생
        AnimationEvent_WalkSound();
    }

    protected override void EnterState(string stateName)
    {
        switch (stateName)
        {
            case "Idle":
                blackboard.AnimatorParameterSetter.Animator.SetBool(IsMoving, false);
                if (blackboard.NavMeshAgent != null)
                    blackboard.NavMeshAgent.isStopped = true;
                break;
            case "Chase":
                // 추적 상태 진입 시 추가 로직이 필요하면 여기에 작성
                break;
            case "UsingSkill1":
            case "UsingSkill2":
            case "UsingSkill3":
            case "UsingSkill4":
            case "UsingSkill5":
            case "UsingSkill6":
            case "UsingSkill7":
                if (blackboard.NavMeshAgent != null)
                    blackboard.NavMeshAgent.isStopped = true;
                break;
        }
    }

    protected override void Init()
    {
        base.Init();
        
        ChangeState("Spawn");

        _isSpawned = false;
        _isDying = false;
    }
    
    public override void ApplyDamage(float inDamage, LayerMask targetMask = default, float unitOfTime = 1, float defenceIgnoreRate = 0)
    {
        if (blackboard.State.GetStates() == "UsingSkill5") // 영혼 흡수 스킬 실행 중일 때는 데미지 절반으로
        {
            base.ApplyDamage(inDamage / 2f, targetMask, unitOfTime, defenceIgnoreRate);
        }
        else
        {
            base.ApplyDamage(inDamage, targetMask, unitOfTime, defenceIgnoreRate);
        }
        
        GUIManager.Instance.GameUIController.UpdateBossHpBar(LocalizationManager.IsKorean ? "해방된 아몬" : "Amon Unbound", blackboard.CurrentHealth, blackboard.MaxHealth);
    }

    // 현재는 애니메이션 이벤트가 아니라 ActChase()가 매 프레임 호출한다.
    // 가드가 없으면 2.8초짜리 이동 클립이 프레임마다 겹쳐 쌓여 소리가 뭉개진다.
    // 걷기 애니메이션에 이벤트를 걸면 ExcutionerFSM.AnimationEvent_PlayWalkSound()처럼
    // ActChase()의 호출을 지우고 이 가드도 뺄 수 있다.
    private void AnimationEvent_WalkSound()
    {
        if (blackboard.AudioSource == null || walkClip == null) return;
        if (blackboard.AudioSource.isPlaying) return;

        blackboard.AudioSource.PlayOneShot(walkClip);
    }
}
