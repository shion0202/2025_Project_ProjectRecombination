using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;
using Cinemachine;
using _Project._01._Scripts.Monster;
using Monster.AI.FSM;

public class ShoulderLaser : PartBaseShoulder
{
    [SerializeField] protected GameObject beamLineRendererPrefab;
    [SerializeField] protected GameObject beamStartPrefab;
    [SerializeField] protected GameObject beamEndPrefab;
    [SerializeField] protected GameObject bulletPrefab;

    [SerializeField] protected float beamDamage = 300.0f;
    [SerializeField] protected Vector3 beamOffset = Vector3.zero;
    [SerializeField] protected float beamDuration = 2.0f;
    [SerializeField] protected float beamMaxDistance = 100.0f;
    [SerializeField] protected float beamRadius = 1.0f;
    [SerializeField] protected LayerMask obstacleMask;

    private CinemachineBasicMultiChannelPerlin noise;
    private readonly Collider[] _hitResults = new Collider[20];
    private float _timer = 0.05f;

    private GameObject beamStart;
    private GameObject beamEnd;
    private GameObject beam;
    private LineRenderer line;
    private bool _isShooting = false;
    private Coroutine _skillCoroutine = null;
    private float _currentTimer = 0.0f;
    
    protected void Start()
    {
        noise = _owner.FollowCamera.VCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }

    protected void OnEnable()
    {
        _currentTimer = 0.0f;
        _owner.PlayerAnimator.SetBool("isPlayBackLaserAnim", false);
        _owner.PlayerAnimator.SetBool("isPlayBackShootAnim", false);
        _isShooting = false;
        _owner.SetPlayerState(EPlayerState.Skilling, false);

        if (beamStart) Utils.Destroy(beamStart);
        if (beamEnd) Utils.Destroy(beamEnd);
        if (beam) Utils.Destroy(beam);
        line = null;

        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        _damagedTargets.Clear();
    }

    protected void OnDisable()
    {
        noise.m_AmplitudeGain = 0.0f;
        _currentTimer = 0.0f;
        _owner.PlayerAnimator.SetBool("isPlayBackLaserAnim", false);
        _owner.PlayerAnimator.SetBool("isPlayBackShootAnim", false);
        _isShooting = false;
        _owner.SetPlayerState(EPlayerState.Skilling, false);

        if (beamStart) Utils.Destroy(beamStart);
        if (beamEnd) Utils.Destroy(beamEnd);
        if (beam) Utils.Destroy(beam);
        line = null;

        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        _damagedTargets.Clear();

        if (Managers.GUIManager.IsAliveInstance())
        {
            GUIManager.Instance.GameUIController.SetBackSkillIcon(false);
            GUIManager.Instance.GameUIController.SetBackSkillCooldown(0.0f);
            GUIManager.Instance.GameUIController.SetBackSkillCooldown(false);
        }
    }

    protected void Update()
    {
        if (!_isShooting) return;

        _currentTimer += Time.deltaTime;
        if (_currentTimer < _timer) return;
        _currentTimer = 0.0f;

        Vector3 origin = transform.position + (_owner.transform.right * beamOffset.x + _owner.transform.up * beamOffset.y + _owner.transform.forward * beamOffset.z);
        Vector3 targetPoint = GetTargetPoint(origin);

        ShootBeamInDir(origin, targetPoint);
    }

    public override void UseAbility()
    {
        if (_cooldownRoutine != null) return;
        ShootLaser();
    }

    public override void FinishActionForced()
    {
        base.FinishActionForced();

        noise.m_AmplitudeGain = 0.0f;
        _currentTimer = 0.0f;
        _owner.PlayerAnimator.SetBool("isPlayBackLaserAnim", false);
        _owner.PlayerAnimator.SetBool("isPlayBackShootAnim", false);
        _isShooting = false;
        _owner.SetPlayerState(EPlayerState.Skilling, false);

        if (beamStart) Utils.Destroy(beamStart);
        if (beamEnd) Utils.Destroy(beamEnd);
        if (beam) Utils.Destroy(beam);
        line = null;

        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        _damagedTargets.Clear();

        if (Managers.GUIManager.IsAliveInstance())
        {
            GUIManager.Instance.GameUIController.SetBackSkillIcon(false);
            GUIManager.Instance.GameUIController.SetBackSkillCooldown(0.0f);
            GUIManager.Instance.GameUIController.SetBackSkillCooldown(false);
        }
    }

    protected void ShootLaser()
    {
        if (_isShooting || _skillCoroutine != null) return;

        // 캐릭터가 카메라 방향을 바라보고, 특정 위치에서 레이저가 시작하며, N초간 지속되도록 설정
        LookCameraDirection();
        _skillCoroutine = StartCoroutine(CoStopAndCooldown());
    }

    protected void LookCameraDirection()
    {
        Camera cam = Camera.main;
        Vector3 lookDirection = cam.transform.forward;
        lookDirection.y = 0; // 수평 방향으로만 회전
        if (lookDirection != Vector3.zero)
        {
            _owner.transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    protected Vector3 GetTargetPoint(Vector3 origin)
    {
        Camera cam = Camera.main;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 startPoint = _owner.FollowCamera.transform.position + _owner.FollowCamera.transform.forward * (Vector3.Distance(_owner.transform.position, _owner.FollowCamera.transform.position));
        float maxDistance = beamMaxDistance;
        Vector3 targetPoint = Vector3.zero;

        // 벽 충돌 위치 찾기 (CapsuleCast에서 첫 충돌)
        if (Physics.Raycast(startPoint, ray.direction, out RaycastHit hitInfo, maxDistance, obstacleMask))
        {
            targetPoint = hitInfo.point;
            maxDistance = hitInfo.distance;  // 벽에 닿는 거리로 범위 제한
        }
        Vector3 targetDirection = targetPoint != Vector3.zero ? (targetPoint - origin).normalized : ray.direction;

        // 적 데미지 처리
        int hitCount = Physics.OverlapCapsuleNonAlloc(
            origin,
            origin + targetDirection * maxDistance,
            beamRadius,
            _hitResults,
            targetMask
        );
        for (int i = 0; i < hitCount; i++)
        {
            var collider = _hitResults[i];
            float hitZoneValue = 1.0f;
            PartialBlow partialBlow = collider.GetComponent<PartialBlow>();
            if (partialBlow)
            {
                hitZoneValue = partialBlow.fValue;
            }

            IDamagable enemy = collider.transform.GetComponent<IDamagable>();
            if (enemy != null)
            {
                Transform otherParent = collider.transform;
                if (_damagedTargets.Contains(otherParent)) continue;
                _damagedTargets.Add(otherParent);
                enemy.ApplyDamage(beamDamage * _timer * hitZoneValue, targetMask, _timer, 0.0f);

                if (_owner.CompareTag("Player"))
                {
                    Managers.GUIManager.Instance.GameUIController.StartHitCrosshair();
                }
            }
            else
            {
                enemy = collider.transform.GetComponentInParent<IDamagable>();
                if (enemy != null)
                {
                    Transform otherParent = collider.transform.GetComponentInParent<FSM>().transform;
                    if (_damagedTargets.Contains(otherParent)) continue;
                    _damagedTargets.Add(otherParent);
                    enemy.ApplyDamage(beamDamage * _timer * hitZoneValue, targetMask, _timer, 0.0f);

                    if (_owner.CompareTag("Player"))
                    {
                        Managers.GUIManager.Instance.GameUIController.StartHitCrosshair();
                    }
                }
            }

            Utils.Destroy(Utils.Instantiate(bulletPrefab, collider.transform.position, Quaternion.identity), 0.1f);
        }

        _damagedTargets.Clear();

        return origin + targetDirection * maxDistance;
    }

    void ShootBeamInDir(Vector3 start, Vector3 end)
    {
        if (!line) return;

        line.positionCount = 2;

        line.SetPosition(0, start);
        beamStart.transform.position = start;

        line.SetPosition(1, end);
        beamEnd.transform.position = end;

        beamStart.transform.LookAt(beamEnd.transform.position);
        beamEnd.transform.LookAt(beamStart.transform.position);

        //float distance = Vector3.Distance(start, end);
        //line.sharedMaterial.mainTextureScale = new Vector2(distance / 3.0f, 1);
        //line.sharedMaterial.mainTextureOffset -= new Vector2(Time.deltaTime * 375.0f, 0);
    }

    protected void ApplyLaserShake(float gain)
    {
        noise.m_AmplitudeGain = gain;
    }

    private IEnumerator CoStopAndCooldown()
    {
        GUIManager.Instance.GameUIController.SetBackSkillIcon(true);

        _owner.PlayerAnimator.SetBool("isPlayBackLaserAnim", true);
        yield return new WaitForSeconds(0.5f);

        _owner.PlayerAnimator.SetBool("isPlayBackShootAnim", true);
        yield return new WaitForSeconds(0.5f);

        _owner.SetPlayerState(EPlayerState.Skilling, true);
        beamStart = Utils.Instantiate(beamStartPrefab, Vector3.zero, Quaternion.identity);
        beamEnd = Utils.Instantiate(beamEndPrefab, Vector3.zero, Quaternion.identity);
        beam = Utils.Instantiate(beamLineRendererPrefab, Vector3.zero, Quaternion.identity);
        line = beam.GetComponent<LineRenderer>();
        _isShooting = true;

        ApplyLaserShake(1.5f);

        yield return new WaitForSeconds(beamDuration);

        _isShooting = false;
        _owner.SetPlayerState(EPlayerState.Skilling, false);
        Utils.Destroy(beamStart);
        Utils.Destroy(beamEnd);
        Destroy(beam);
        line = null;

        ApplyLaserShake(0.0f);

        _owner.PlayerAnimator.SetBool("isPlayBackShootAnim", false);
        _owner.PlayerAnimator.SetBool("isPlayBackLaserAnim", false);

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
        GUIManager.Instance.GameUIController.SetBackSkillCooldown(false);
        Debug.Log("쿨타임 종료");
        _skillCoroutine = null;
    }

    void DrawCapsule(Vector3 point1, Vector3 point2, float radius, Color color, float duration = 0)
    {
        int segments = 16; // 원의 세그먼트 수 (원에 가까울수록 정밀)
        float angleStep = 360f / segments;

        // 캡슐 축선 그리기
        Debug.DrawLine(point1, point2, color, duration);

        // 각 끝점에 원 그리기 (XY 평면 기준 예시)
        for (int i = 0; i < segments; i++)
        {
            float angle1 = Mathf.Deg2Rad * i * angleStep;
            float angle2 = Mathf.Deg2Rad * (i + 1) * angleStep;

            Vector3 offset1 = new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * radius;
            Vector3 offset2 = new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * radius;

            Debug.DrawLine(point1 + offset1, point1 + offset2, color, duration);
            Debug.DrawLine(point2 + offset1, point2 + offset2, color, duration);
        }
    }

}
