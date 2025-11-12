using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Monster;
using Monster.AI.Blackboard;
using Monster.AI;
using Managers;

public class ShoulderLaser : PartBaseShoulder
{
    [SerializeField] protected GameObject beamLineRendererPrefab;
    [SerializeField] protected GameObject beamStartPrefab;
    [SerializeField] protected GameObject beamEndPrefab;
    [SerializeField] protected GameObject bulletPrefab;

    [SerializeField] protected float beamDuration = 2.0f;
    [SerializeField] protected float beamCooldown = 5.0f;

    private GameObject beamStart;
    private GameObject beamEnd;
    private GameObject beam;
    private LineRenderer line;
    private bool _isShooting = false;
    private Coroutine _skillCoroutine = null;

    private Vector3 _targetPoint = Vector3.zero;

    protected void Update()
    {
        if (_isShooting)
        {
            ShootBeamInDir(transform.position + (Vector3.up * 1.0f), _targetPoint);
        }
    }

    public override void UseAbility()
    {
        ShootLaser();
    }

    protected void ShootLaser()
    {
        if (_isShooting || _skillCoroutine != null) return;

        // 캐릭터가 카메라 방향을 바라보고, 특정 위치에서 레이저가 시작하며, N초간 지속되도록 설정
        LookCameraDirection();
        _owner.FollowCamera.SetCameraRotatable(false);
        _owner.SetMovable(false);
        _isShooting = true;

        _skillCoroutine = StartCoroutine(CoStopAndCooldown());
    }

    protected void LookCameraDirection()
    {
        Camera cam = Camera.main;
        Vector3 lookDirection = cam.transform.forward;
        lookDirection.y = 0; // 수평 방향으로만 회전
        if (lookDirection != Vector3.zero)
            _owner.transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    protected Vector3 GetTargetPoint(out RaycastHit[] hits)
    {
        Camera cam = Camera.main;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint = Vector3.zero;

        targetPoint = transform.position + (Vector3.up * 1.0f) + ray.direction * 10.0f;
        hits = Physics.CapsuleCastAll(transform.position + (Vector3.up * 1.0f), targetPoint, 1.0f, ray.direction, 100.0f);

        foreach (RaycastHit hit in hits)
        {
            IDamagable monster = hit.transform.GetComponent<IDamagable>();
            if (monster != null)
            {
                monster.ApplyDamage(100.0f);
            }
            else
            {
                monster = hit.transform.GetComponentInParent<IDamagable>();
                if (monster != null)
                {
                    monster.ApplyDamage(100.0f);
                }
            }

            Destroy(Instantiate(bulletPrefab, hit.point, Quaternion.identity), 0.1f);
        }

        return targetPoint;
    }

    void ShootBeamInDir(Vector3 start, Vector3 end)
    {
#if UNITY_5_5_OR_NEWER
        line.positionCount = 2;
#else
		line.SetVertexCount(2); 
#endif
        line.SetPosition(0, start);
        beamStart.transform.position = start;

        beamEnd.transform.position = end;
        line.SetPosition(1, end);

        beamStart.transform.LookAt(beamEnd.transform.position);
        beamEnd.transform.LookAt(beamStart.transform.position);

        float distance = Vector3.Distance(start, end);
        line.sharedMaterial.mainTextureScale = new Vector2(distance / 3.0f, 1);
        line.sharedMaterial.mainTextureOffset -= new Vector2(Time.deltaTime * 375.0f, 0);
    }

    private IEnumerator CoStopAndCooldown()
    {
        GUIManager.Instance.SetBackSkillIcon(true);

        _owner.PlayerAnimator.SetBool("isPlayBackLaserAnim", true);
        yield return new WaitForSeconds(0.5f);

        _owner.PlayerAnimator.SetBool("isPlayBackShootAnim", true);
        yield return new WaitForSeconds(0.5f);

        beamStart = Instantiate(beamStartPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        beamEnd = Instantiate(beamEndPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        beam = Instantiate(beamLineRendererPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        line = beam.GetComponent<LineRenderer>();
        line.startWidth = 2.0f;
        line.endWidth = 2.0f;

        RaycastHit[] hits;
        _targetPoint = GetTargetPoint(out hits);

        yield return new WaitForSeconds(beamDuration);

        _isShooting = false;
        Destroy(beamStart);
        Destroy(beamEnd);
        Destroy(beam);
        _owner.FollowCamera.SetCameraRotatable(true);
        _owner.SetMovable(true);

        _owner.PlayerAnimator.SetBool("isPlayBackShootAnim", false);
        _owner.PlayerAnimator.SetBool("isPlayBackLaserAnim", false);

        float time = beamCooldown;
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
}
