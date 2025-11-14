using Cinemachine;
using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class ShoulderHeavy : PartBaseShoulder
{
    [SerializeField] protected GameObject shootPrefab;
    [SerializeField] protected GameObject orbPrefab;
    [SerializeField] protected Transform backPart;
    //protected SkinnedMeshRenderer smr;
    private Coroutine _skillCoroutine = null;
    protected Coroutine _morphBlendRoutine = null;

    [SerializeField] protected List<CinemachineVirtualCamera> cutsceneCams = new();
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
        brain.m_DefaultBlend = defaultBlend;
        _owner.FollowCamera.SetCameraRotatable(true);
        _owner.SetMovable(true);
        _owner.PlayerAnimator.SetBool("isPlayBackHeavyAnim", false);
        _owner.SetPlayerState(EPlayerState.Skilling, false);
        SkinnedMeshRenderer smr = backPart.GetComponent<SkinnedMeshRenderer>();
        smr.SetBlendShapeWeight(0, 0.0f);

        for (int i = 0; i < cutsceneCams.Count; ++i)
        {
            cutsceneCams[i].m_Priority = 10;
        }

        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        if (_morphBlendRoutine != null)
        {
            StopCoroutine(_morphBlendRoutine);
            _morphBlendRoutine = null;
        }

        GUIManager.Instance.SetBackSkillIcon(false);
        GUIManager.Instance.SetBackSkillCooldown(0.0f);
        GUIManager.Instance.SetBackSkillCooldown(false);
    }

    protected void OnDisable()
    {
        brain.m_DefaultBlend = defaultBlend;
        _owner.FollowCamera.SetCameraRotatable(true);
        _owner.SetMovable(true);
        _owner.PlayerAnimator.SetBool("isPlayBackHeavyAnim", false);
        _owner.SetPlayerState(EPlayerState.Skilling, false);
        SkinnedMeshRenderer smr = backPart.GetComponent<SkinnedMeshRenderer>();
        smr.SetBlendShapeWeight(0, 0.0f);

        for (int i = 0; i < cutsceneCams.Count; ++i)
        {
            cutsceneCams[i].m_Priority = 10;
        }

        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        if (_morphBlendRoutine != null)
        {
            StopCoroutine(_morphBlendRoutine);
            _morphBlendRoutine = null;
        }

        if (Managers.GUIManager.IsAliveInstance())
        {
            GUIManager.Instance.SetBackSkillIcon(false);
            GUIManager.Instance.SetBackSkillCooldown(0.0f);
            GUIManager.Instance.SetBackSkillCooldown(false);
        }
    }

    public override void UseAbility()
    {
        ShootOrb();
    }

    private void ShootOrb()
    {
        if (_skillCoroutine != null) return;
        _skillCoroutine = StartCoroutine(CoShootOrb());
    }

    protected void LookCameraDirection()
    {
        Camera cam = Camera.main;
        Vector3 lookDirection = cam.transform.forward;
        lookDirection.y = 0; // 수평 방향으로만 회전
        if (lookDirection != Vector3.zero)
            _owner.transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    protected Vector3 GetTargetPoint(out RaycastHit hit)
    {
        Camera cam = Camera.main;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 startPoint = _owner.FollowCamera.transform.position + _owner.FollowCamera.transform.forward * (Vector3.Distance(_owner.transform.position, _owner.FollowCamera.transform.position));
        Vector3 targetPoint = Vector3.zero;

        if (Physics.Raycast(startPoint, ray.direction, out hit, 100.0f, ignoreMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * 100.0f;
        }

        return targetPoint;
    }

    protected IEnumerator CoShootOrb()
    {
        _owner.SetPlayerState(EPlayerState.Skilling, true);
        GUIManager.Instance.SetBackSkillIcon(true);
        _owner.FollowCamera.SetCameraRotatable(false);
        _owner.SetMovable(false);
        LookCameraDirection();

        Vector3 spawnPoint = transform.position + _owner.transform.forward + Vector3.up * 2.5f;
        Vector3 targetPoint = GetTargetPoint(out RaycastHit hit);
        Vector3 camShootDirection = (targetPoint - spawnPoint);
        camShootDirection.y = 0.0f;
        camShootDirection.Normalize();

        brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 0.3f);
        cutsceneCams[0].m_Priority = 100;

        // 일정 시간 대기
        // To-do: 이펙트, 애니메이션 등 발사 준비 연출
        _owner.PlayerAnimator.SetBool("isPlayBackHeavyAnim", true);
        yield return new WaitForSeconds(1.0f);

        SkinnedMeshRenderer smr = backPart.GetComponent<SkinnedMeshRenderer>();
        float min = 0.0f;
        float max = 100.0f;

        float elapsed = min;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;

            float weight = Mathf.Lerp(min, max, elapsed / 0.3f);  // weight는 0~100 범위
            smr.SetBlendShapeWeight(0, weight);
            yield return null;
        }

        // 보정: 정확히 1(max)로 설정
        smr.SetBlendShapeWeight(0, max);

        // 오브 생성 및 발사
        _owner.FollowCamera.ApplyShake(source);
        Utils.Destroy(Utils.Instantiate(shootPrefab, spawnPoint, Quaternion.Euler(_owner.transform.rotation.eulerAngles + new Vector3(0.0f, 180.0f, 0.0f))), 2.0f);
        GameObject orb = Utils.Instantiate(orbPrefab, spawnPoint, Quaternion.identity);
        Bullet orbComp = orb.GetComponent<Bullet>();
        if (orbComp != null)
        {
            orbComp.Init(_owner.gameObject, null, spawnPoint, Vector3.zero, camShootDirection, 200.0f);
        }

        yield return new WaitForSeconds(0.4f);

        _owner.SetPlayerState(EPlayerState.Skilling, false);
        _owner.PlayerAnimator.SetBool("isPlayBackHeavyAnim", false);
        _owner.FollowCamera.SetCameraRotatable(true);
        _owner.SetMovable(true);

        if (_morphBlendRoutine != null)
        {
            StopCoroutine(_morphBlendRoutine);
        }
        _morphBlendRoutine = StartCoroutine(CoMorphBlend());

        brain.m_DefaultBlend = defaultBlend;
        cutsceneCams[0].m_Priority = 10;

        float time = 5.0f;
        GUIManager.Instance.SetBackSkillCooldown(true);
        GUIManager.Instance.SetBackSkillCooldown(time);
        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            time -= 0.1f;
            GUIManager.Instance.SetBackSkillCooldown(time);
            if (time <= 0.0f)
            {
                break;
            }
        }

        GUIManager.Instance.SetBackSkillIcon(false);
        GUIManager.Instance.SetBackSkillCooldown(false);
        Debug.Log("쿨타임 종료");
        _skillCoroutine = null;
    }

    protected IEnumerator CoMorphBlend(float time = 0.5f)
    {
        SkinnedMeshRenderer smr = backPart.GetComponent<SkinnedMeshRenderer>();
        float min = 100.0f;
        float max = 0.0f;

        float elapsed = min;
        while (elapsed < time)
        {
            elapsed -= Time.deltaTime;
            float weight = Mathf.Lerp(min, max, elapsed / time);
            smr.SetBlendShapeWeight(0, weight);
            yield return null;
        }

        smr.SetBlendShapeWeight(0, max);
        _morphBlendRoutine = null;
    }
}
