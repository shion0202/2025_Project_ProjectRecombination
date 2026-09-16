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
        //GUIManager.Instance.GameUIController.ResetSkillCooldown();
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

        // 쿨타임이 얼마나 지났는지 백분율(%)로 저장 (1 -> 0)
        _owner.CooldownDictionary[currentPartType] = _currentCooldown / skillCooldown;
    }

    public override void SetCurrentCooldown(EPartType currentPartType)
    {
        if (!_owner) return;

        _currentCooldown = skillCooldown * _owner.CooldownDictionary[currentPartType];

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
