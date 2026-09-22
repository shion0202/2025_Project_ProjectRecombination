using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartBaseLegs : PartBase, ILegsMovement
{
    [Header("다리 파츠 설정")]
    [SerializeField] protected float skillTime = 0.5f;
    [SerializeField] protected float skillDuration = 1.0f;
    [SerializeField] protected float skillCooldown = 3.0f;
    [SerializeField] protected int maxSkillCount = 1;
    [SerializeField] protected float skillRange = 1.0f;
    [SerializeField] protected float skillDamage = 0.0f;
    protected int _currentSkillCount = 0;
    protected Coroutine _skillCoroutine = null;
    protected EAnimationType _legsAnimType = EAnimationType.Base;

    // 이동 루프음 (호버 비행음, 캐터필러 바퀴음, 롤러음)
    // 이동을 시작하면 처음부터 재생하고, 이동하는 동안 반복하며, 멈추면 정지한다.
    // 볼륨을 0/1로 켜고 끄는 방식은 소리가 계속 흘러가고 있어 다시 움직일 때 중간부터 들렸다.
    protected AudioSource _moveLoopSource;
    private int _lastMoveSoundFrame = -1;

    public EAnimationType LegsAnimType => _legsAnimType;

    protected void InitMoveLoopSound(AudioSource source)
    {
        _moveLoopSource = source;
        if (_moveLoopSource == null) return;

        _moveLoopSource.loop = true;
        _moveLoopSource.playOnAwake = false;
    }

    // GetMoveDirection에서 매 프레임 호출한다.
    protected void UpdateMoveLoopSound(bool isMoving)
    {
        if (_moveLoopSource == null) return;

        // 일시정지 등으로 시간이 멈춘 동안에는 이동으로 보지 않는다.
        if (!isMoving || Time.timeScale <= 0.0f)
        {
            StopMoveLoopSound();
            return;
        }

        _lastMoveSoundFrame = Time.frameCount;
        if (!_moveLoopSource.isPlaying)
        {
            _moveLoopSource.Play();
        }
    }

    protected void StopMoveLoopSound()
    {
        if (_moveLoopSource != null && _moveLoopSource.isPlaying)
        {
            _moveLoopSource.Stop();
        }
    }

    // 이동이 막힌 상태(스킬, 컷씬, 라디얼 UI, 사망 등)에서는 PlayerController가 GetMoveDirection을 호출하지 않는다.
    // 이동 중에 막히면 정지 요청이 오지 않아 루프음이 계속 남으므로, 갱신이 끊기면 여기서 멈춘다.
    // (PlayerController.LateUpdate와의 실행 순서에 따라 한 프레임 차이가 날 수 있어 1프레임은 허용한다)
    protected virtual void LateUpdate()
    {
        if (_moveLoopSource == null || !_moveLoopSource.isPlaying) return;

        if (Time.frameCount - _lastMoveSoundFrame > 1)
        {
            StopMoveLoopSound();
        }
    }

    public override void UseAbility()
    {
        // 스킬 입력 시작 시 실행할 로직
    }

    public override void UseCancleAbility()
    {
        // 스킬 입력 종료 시 실행할 로직
        // 버튼을 누르는 동안 차지 후, 버튼을 뗄 때 대시하는 기능 등
    }

    // 장비 교체 등 특수한 상황에서 대시를 강제로 종료해야 할 때 사용
    public override void FinishActionForced()
    {
        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        _currentSkillCount = 0;

        // 강제 종료는 쿨타임 UI를 "사용 가능"으로 되돌리는데, 여기서 _currentCooldown을 같이 비우지 않으면
        // 화면과 실제 상태가 어긋난다. Update()는 UI를 끄기만 하고 다시 켜지 않기 때문에
        // 컷씬이 끝난 뒤 UI는 깨끗한데 스킬만 조용히 막히고, 그 상태로 파츠를 교체하면
        // PreserveCurrentCooldown이 남은 값을 저장해 쓰지도 않은 쿨타임이 새 파츠에서 돌아간다.
        // 파츠 교체 경로는 Inventory가 이 호출 전에 Preserve를 끝내므로 인계에는 영향이 없다.
        _currentCooldown = 0.0f;
    }

    public virtual Vector3 GetMoveDirection(Vector2 moveInput, Transform characterTransform, Transform cameraTransform)
    {
        return Vector3.zero;
    }

    public override void PreserveCurrentCooldown(EPartType currentPartType)
    {
        if (!_owner) return;
        if (_cooldownRoutine != null)
        {
            StopCoroutine(_cooldownRoutine);
            _cooldownRoutine = null;
        }

        // 남은 쿨타임을 '초' 그대로 저장한다.
        // 비율로 저장하면 교체한 파츠의 최대 쿨타임에 비례해 늘어나므로
        // (남은 3초짜리를 최대 28초인 파츠로 바꾸면 17초가 되는 식) 남은 시간이 그대로 이어지지 않는다.
        _owner.CooldownDictionary[currentPartType] = Mathf.Max(0.0f, _currentCooldown);
    }

    public override void SetCurrentCooldown(EPartType currentPartType)
    {
        if (!_owner) return;

        // 남은 초를 그대로 이어받는다. 새 파츠의 최대 쿨타임으로 자르지 않는다.
        // (17초 남은 상태로 최대 3초짜리 파츠에 갈아타도 17초를 그대로 치르게 한다)
        _currentCooldown = Mathf.Max(0.0f, _owner.CooldownDictionary[currentPartType]);

        if (_currentCooldown > 0.0f)
        {
            _cooldownRoutine = StartCoroutine(CoStartCooldown());
        }
    }

    public virtual IEnumerator CoStartCooldown()
    {
        GUIManager.Instance.GameUIController.SetLegsSkillIcon(true);
        GUIManager.Instance.GameUIController.SetLegsSkillCooldown(true);
        GUIManager.Instance.GameUIController.SetLegsSkillCooldown(_currentCooldown);

        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            _currentCooldown -= 0.1f;
            GUIManager.Instance.GameUIController.SetLegsSkillCooldown(_currentCooldown);
            if (_currentCooldown <= 0.0f)
            {
                _currentCooldown = 0.0f;
                break;
            }
        }

        GUIManager.Instance.GameUIController.SetLegsSkillIcon(false);
        GUIManager.Instance.GameUIController.SetLegsSkillCooldown(false);
        _cooldownRoutine = null;
    }
}
