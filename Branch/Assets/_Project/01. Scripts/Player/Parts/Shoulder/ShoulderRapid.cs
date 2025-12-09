using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;
using Cinemachine;

public class ShoulderRapid : PartBaseShoulder
{
    [SerializeField] protected GameObject missilePrefab;
    [SerializeField] protected GameObject targetingPrefab; // 타겟팅 표시용 프리팹
    [SerializeField] protected int maxTargetCount = 12;
    [SerializeField] private float particleStopDelay = 0.9f;  // Inspector에서 조절 가능
    [SerializeField] protected float skillDamage = 100.0f;
    private Coroutine _skillCoroutine = null;
    private List<GameObject> targetingInstances = new List<GameObject>();

    [SerializeField] protected float maxYawAngle = 90f; // 좌우 방향 최대 90도씩 = 180도 범위
    [SerializeField] protected float maxPitchAngle = 10f; // 상하 각도 범위 (조절 가능)
    [SerializeField] protected Vector3 launchOffset = Vector3.zero;

    [SerializeField] protected List<CinemachineVirtualCamera> cutsceneCams = new();
    [SerializeField] protected LayerMask obstacleMask;
    protected CinemachineBrain brain;
    protected CinemachineBlendDefinition defaultBlend;
    protected CinemachineImpulseSource source;

    protected override void Awake()
    {
        base.Awake();

        brain = Camera.main.GetComponent<CinemachineBrain>();
        defaultBlend = brain.m_DefaultBlend;
        source = gameObject.GetComponent<CinemachineImpulseSource>();
    }

    protected void OnEnable()
    {
        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        for (int i = 0; i < targetingInstances.Count; ++i)
        {
            Utils.Destroy(targetingInstances[i]);
        }
        targetingInstances.Clear();

        brain.m_DefaultBlend = defaultBlend;
        _owner.FollowCamera.SetCameraRotatable(true);
        _owner.SetMovable(true);
        _owner.PlayerAnimator.SetBool("isPlayShoulderAnim", false);
        _owner.SetPlayerState(EPlayerState.Skilling, false);

        for (int i = 0; i < cutsceneCams.Count; ++i)
        {
            cutsceneCams[i].m_Priority = 10;
        }

        if (_currentCooldown <= 0.0f)
        {
            StartCoroutine(SetBackSkillIcon());
        }
    }

    protected void OnDisable()
    {
        GUIManager.Instance.GameUIController.SetBackSkillIcon(false);

        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        for (int i = 0; i < targetingInstances.Count; ++i)
        {
            Utils.Destroy(targetingInstances[i]);
        }
        targetingInstances.Clear();

        brain.m_DefaultBlend = defaultBlend;
        _owner.FollowCamera.SetCameraRotatable(true);
        _owner.SetMovable(true);
        _owner.PlayerAnimator.SetBool("isPlayShoulderAnim", false);
        _owner.SetPlayerState(EPlayerState.Skilling, false);

        for (int i = 0; i < cutsceneCams.Count; ++i)
        {
            cutsceneCams[i].m_Priority = 10;
        }

        if (Managers.GUIManager.IsAliveInstance())
        {
            GUIManager.Instance.GameUIController.SetBackSkillIcon(false);
            GUIManager.Instance.GameUIController.SetBackSkillCooldown(0.0f);
            GUIManager.Instance.GameUIController.SetBackSkillCooldown(false);
        }
    }

    public override void UseAbility()
    {
        if (_cooldownRoutine != null) return;
        LaunchTargetMissiles();
    }

    public override void FinishActionForced()
    {
        base.FinishActionForced();

        GUIManager.Instance.GameUIController.SetBackSkillIcon(false);

        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        for (int i = 0; i < targetingInstances.Count; ++i)
        {
            Utils.Destroy(targetingInstances[i]);
        }
        targetingInstances.Clear();

        brain.m_DefaultBlend = defaultBlend;
        _owner.FollowCamera.SetCameraRotatable(true);
        _owner.SetMovable(true);
        _owner.PlayerAnimator.SetBool("isPlayShoulderAnim", false);
        _owner.SetPlayerState(EPlayerState.Skilling, false);

        for (int i = 0; i < cutsceneCams.Count; ++i)
        {
            cutsceneCams[i].m_Priority = 10;
        }

        if (Managers.GUIManager.IsAliveInstance())
        {
            GUIManager.Instance.GameUIController.SetBackSkillIcon(false);
            GUIManager.Instance.GameUIController.SetBackSkillCooldown(0.0f);
            GUIManager.Instance.GameUIController.SetBackSkillCooldown(false);
        }
    }

    public override IEnumerator CoStartCooldown()
    {
        yield return null;
        yield return null;

        GUIManager.Instance.GameUIController.SetBackSkillIcon(true);
        GUIManager.Instance.GameUIController.SetBackSkillCooldown(true);
        GUIManager.Instance.GameUIController.SetBackSkillCooldown(_currentCooldown);

        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            _currentCooldown -= 0.1f;
            GUIManager.Instance.GameUIController.SetBackSkillCooldown(_currentCooldown);
            if (_currentCooldown <= 0.0f)
            {
                _currentCooldown = 0.0f;
                break;
            }
        }

        GUIManager.Instance.GameUIController.SetBackSkillIcon(false);
        GUIManager.Instance.GameUIController.SetBackSkillCooldown(false);
        _cooldownRoutine = null;
    }

    private void LaunchTargetMissiles()
    {
        // 스킬 시전 하는 동안 카메라, 플레이어 이동 불가
        // 캐릭터가 카메라 방향을 바라봄
        // 화면 범위 내의 적을 조준(타겟팅), 최대 수치까지 타겟팅 가능
        // 타겟팅된 적에게 미사일 발사, 최대 수치가 아닐 경우 남은 미사일은 타겟팅된 적들에게 균등 분배
        if (_skillCoroutine != null) return;

        // 1. 스킬 시전 중 플레이어와 카메라 조작 불가
        _owner.FollowCamera.SetCameraRotatable(false);
        _owner.SetMovable(false);
        _owner.SetPlayerState(EPlayerState.Skilling, true);

        // 2. 플레이어가 카메라 방향 바라봄
        LookCameraDirection();

        brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 0.3f);
        cutsceneCams[0].m_Priority = 100;
        GUIManager.Instance.GameUIController.SetBackSkillIcon(true);

        _skillCoroutine = StartCoroutine(CoLaunchTargetMissiles());
    }

    protected void LookCameraDirection()
    {
        Camera cam = Camera.main;
        Vector3 lookDirection = cam.transform.forward;
        lookDirection.y = 0; // 수평 방향으로만 회전
        if (lookDirection != Vector3.zero)
            _owner.transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    List<TargetPoint> FindValidTargets(LayerMask obstacleMask, float maxRange)
    {
        Camera cam = Camera.main;
        List<TargetPoint> result = new List<TargetPoint>();
        TargetPoint[] allTargets = GameObject.FindObjectsOfType<TargetPoint>();

        foreach (var target in allTargets)
        {
            GameObject obj = target.gameObject;

            // 거리 검사
            float distance = Vector3.Distance(cam.transform.position, obj.transform.position);
            if (distance > maxRange)
                continue;

            // 카메라에 보이는지 검사
            Vector3 viewportPos = cam.WorldToViewportPoint(obj.transform.position);
            bool isVisible = viewportPos.z > 0 &&
                             viewportPos.x >= 0 && viewportPos.x <= 1 &&
                             viewportPos.y >= 0 && viewportPos.y <= 1;

            if (!isVisible)
                continue;

            // 방해물에 가려졌는지 검사하는 Raycast 추가
            Vector3 direction = obj.transform.position - cam.transform.position;
            if (Physics.Raycast(cam.transform.position, direction, out RaycastHit hit, distance, obstacleMask))
            {
                // 방해물이 적과 카메라 사이에 있음
                continue;
            }

            // 모든 조건 만족 시 리스트에 추가
            result.Add(target);
        }
        return result;
    }

    protected Vector3 GetRandomDirection(Vector3 forward)
    {
        // 좌우 yaw -maxYaw ~ +maxYawdeg, 상하 pitch -maxPitch ~ +maxPitchdeg
        float roll = Random.Range(-maxPitchAngle, maxPitchAngle);
        float yaw = Random.Range(-maxYawAngle, maxYawAngle);
        float pitch = Random.Range(-maxPitchAngle, maxPitchAngle);
        Quaternion rot = Quaternion.Euler(pitch, yaw, roll);
        return rot * forward;
    }

    private IEnumerator CoLaunchTargetMissiles()
    {
        yield return new WaitForSeconds(0.5f);

        // 3. 화면 내의 적을 감지(카메라 시야각/범위 외 적 제외)
        List<TargetPoint> targets = FindValidTargets(obstacleMask, 50.0f);
        if (targets.Count > maxTargetCount)
        {
            targets = targets.GetRange(0, maxTargetCount);
        }

        // 4. 타겟마다 targetingPrefab 생성(시각적 타겟 표시)
        foreach (var enemy in targets)
        {
            Vector3 targetPoint = enemy.transform.position;

            GameObject targeting = Utils.Instantiate(targetingPrefab, targetPoint, Quaternion.identity, enemy.transform);
            targetingInstances.Add(targeting);

            ParticleSystem ps = targeting.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                StartCoroutine(StopParticleAfterDelay(ps));
            }

            // 타겟팅 후 다음 타겟팅 전까지 잠깐 대기
            yield return new WaitForSeconds(0.2f);
        }

        // 5. 각 타겟에게 유도 미사일 발사
        // Count된 적이 없을 경우 종료
        int targetCount = targets.Count;
        if (targetCount <= 0)
        {
            for (int i = 0; i < targetingInstances.Count; ++i)
            {
                Utils.Destroy(targetingInstances[i]);
            }
            targetingInstances.Clear();

            brain.m_DefaultBlend = defaultBlend;
            _owner.FollowCamera.SetCameraRotatable(true);
            _owner.SetMovable(true);
            _owner.PlayerAnimator.SetBool("isPlayShoulderAnim", false);
            _owner.SetPlayerState(EPlayerState.Skilling, false);

            for (int i = 0; i < cutsceneCams.Count; ++i)
            {
                cutsceneCams[i].m_Priority = 10;
            }

            GUIManager.Instance.GameUIController.SetBackSkillIcon(false);
            GUIManager.Instance.GameUIController.SetBackSkillCooldown(0.0f);
            GUIManager.Instance.GameUIController.SetBackSkillCooldown(false);

            _skillCoroutine = null;
            yield break;
        }

        _owner.PlayerAnimator.SetBool("isPlayShoulderAnim", true);
        yield return new WaitForSeconds(0.4f);

        brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 0.1f);
        cutsceneCams[0].m_Priority = 10;

        yield return new WaitForSeconds(0.3f);

        _owner.FollowCamera.ApplyShake(source);

        // 타겟팅 프리팹 제거 (시각 효과 종료)
        foreach (var inst in targetingInstances)
        {
            Utils.Destroy(inst);
        }

        int missilesPerTarget = maxTargetCount / targetCount; // 기본 분배 수
        int remainder = maxTargetCount % targetCount;         // 나머지 미사일 수

        for (int i = 0; i < targetCount; i++)
        {
            int missilesToFire = missilesPerTarget + (i < remainder ? 1 : 0); // 나머지는 앞 타겟에 1개씩 분배
            TargetPoint enemy = targets[i];

            for (int j = 0; j < missilesToFire; j++)
            {
                Vector3 targetPoint = enemy.transform.position;
                Vector3 camShootDirection = (targetPoint - transform.position).normalized;
                Vector3 randomDir = GetRandomDirection(camShootDirection);

                GameObject missile = Utils.Instantiate(missilePrefab, _owner.transform.position + launchOffset, Quaternion.LookRotation(randomDir));
                var missileComp = missile.GetComponent<Missile>();
                if (missileComp != null)
                {
                    missileComp.Parent = transform;
                    missileComp.Init(_owner.gameObject, enemy.transform, transform.position, targetPoint, randomDir, skillDamage);
                }
            }
        }

        // 6. 플레이어와 카메라의 조작 재개
        targetingInstances.Clear();

        brain.m_DefaultBlend = defaultBlend;
        _owner.FollowCamera.SetCameraRotatable(true);
        _owner.SetMovable(true);
        _owner.PlayerAnimator.SetBool("isPlayShoulderAnim", false);
        _owner.SetPlayerState(EPlayerState.Skilling, false);

        for (int i = 0; i < cutsceneCams.Count; ++i)
        {
            cutsceneCams[i].m_Priority = 10;
        }

        _currentCooldown = skillCooldown - _owner.Stats.TotalStats[EStatType.CooldownReduction].value;
        GUIManager.Instance.GameUIController.SetBackSkillCooldown(true);
        GUIManager.Instance.GameUIController.SetBackSkillCooldown(_currentCooldown);
        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            _currentCooldown -= 0.1f;
            GUIManager.Instance.GameUIController.SetBackSkillCooldown(_currentCooldown);
            if (_currentCooldown <= 0.0f)
            {
                _currentCooldown = 0.0f;
                break;
            }
        }

        GUIManager.Instance.GameUIController.SetBackSkillIcon(false);
        GUIManager.Instance.GameUIController.SetBackSkillCooldown(0.0f);
        GUIManager.Instance.GameUIController.SetBackSkillCooldown(false);
        Debug.Log("쿨타임 종료");
        _skillCoroutine = null;
    }

    private IEnumerator StopParticleAfterDelay(ParticleSystem ps)
    {
        yield return new WaitForSeconds(particleStopDelay);

        if (ps != null)
        {
            ps.Pause();
        }
    }

    private IEnumerator SetBackSkillIcon()
    {
        yield return null;
        GUIManager.Instance.GameUIController.SetBackSkillIcon(false);
    }
}
