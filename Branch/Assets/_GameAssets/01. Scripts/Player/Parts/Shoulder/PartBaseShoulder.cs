using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartBaseShoulder : PartBase
{
    [SerializeField] protected LayerMask ignoreMask = 0;
    [SerializeField] protected float skillCooldown = 0.0f;

    protected override void Awake()
    {
        base.Awake();
        
        if (ignoreMask == 0)
        {
            ignoreMask = ~0;
            ignoreMask &= ~(1 << LayerMask.NameToLayer("TransparentFX"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Water"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("UI"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Face"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Hair"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Outline"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Player"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("PlayerMesh"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Bullet"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Minimap"));
        }
    }

    public override void FinishActionForced()
    {
        // 파생 파츠들이 여기서 쿨타임 UI를 "사용 가능"으로 되돌리므로, 실제 값도 같이 비워야 한다.
        // 값만 남으면 UI는 깨끗한데 스킬이 조용히 막히고, 그 상태로 파츠를 교체하면
        // PreserveCurrentCooldown이 남은 값을 넘겨 쓰지도 않은 쿨타임이 새 파츠에서 돌아간다.
        // 파츠 교체 경로는 Inventory가 이 호출 전에 Preserve를 끝내므로 인계에는 영향이 없다.
        bool hadCooldown = _cooldownRoutine != null || _currentCooldown > 0.0f;

        if (_cooldownRoutine != null)
        {
            StopCoroutine(_cooldownRoutine);
            _cooldownRoutine = null;
        }

        _currentCooldown = 0.0f;

        // 실제로 쿨타임이 돌고 있었을 때만 남은 초 표시를 지운다.
        // ShoulderBasic은 이 메서드를 재정의하지 않아, 일반 세트로 컷씬에 들어가면
        // 숫자가 멈춘 채 화면에 남기 때문에 여기서 정리해야 한다.
        //
        // 쿨타임 오버레이(SetBackSkillIcon = backSkillCooldownImage)는 건드리지 않는다.
        // 등 스킬이 없는 기본 파츠가 이 오버레이를 '사용 불가' 표시로 상시 켜두므로,
        // 여기서 함께 끄면 스킬이 있는 것처럼 보인다. 필요한 파츠는 각자 끄고 있다.
        if (hadCooldown && GUIManager.IsAliveInstance())
        {
            GUIManager.Instance.GameUIController.SetBackSkillCooldown(0.0f);
            GUIManager.Instance.GameUIController.SetBackSkillCooldown(false);
        }
    }

    public override void UseAbility()
    {
        
    }

    public override void UseCancleAbility()
    {
        
    }

    public override void PreserveCurrentCooldown(EPartType currentPartType)
    {
        if (!_owner) return;
        if (_cooldownRoutine != null)
        {
            StopCoroutine(_cooldownRoutine);
            _cooldownRoutine = null;
        }

        // 남은 쿨타임을 '초' 그대로 저장한다. 비율로 저장하면 교체한 파츠의 최대 쿨타임에 비례해
        // 늘어나므로(남은 3초를 최대 28초 파츠로 바꾸면 17초가 되는 식) 남은 시간이 이어지지 않는다.
        _owner.CooldownDictionary[currentPartType] = Mathf.Max(0.0f, _currentCooldown);
    }

    public override void SetCurrentCooldown(EPartType currentPartType)
    {
        if (!_owner) return;

        // 남은 초를 그대로 이어받는다. 새 파츠의 최대 쿨타임으로 자르지 않는다.
        _currentCooldown = Mathf.Max(0.0f, _owner.CooldownDictionary[currentPartType]);

        if (_currentCooldown > 0.0f)
        {
            _cooldownRoutine = StartCoroutine(CoStartCooldown());
        }
    }

    public virtual IEnumerator CoStartCooldown()
    {
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
}
