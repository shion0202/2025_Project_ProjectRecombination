using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShoulderHeavy : PartBaseShoulder
{
    [SerializeField] protected GameObject shootPrefab;
    [SerializeField] protected GameObject orbPrefab;
    private Coroutine _skillCoroutine = null;

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

    protected IEnumerator CoShootOrb()
    {
        GUIManager.Instance.SetBackSkillIcon(true);
        _owner.FollowCamera.SetCameraRotatable(false);
        _owner.SetMovable(false);
        LookCameraDirection();

        // 일정 시간 대기
        // To-do: 이펙트, 애니메이션 등 발사 준비 연출
        _owner.PlayerAnimator.SetBool("isPlayBackHeavyAnim", true);
        yield return new WaitForSeconds(1.0f);

        // 오브 생성 및 발사
        Destroy(Instantiate(shootPrefab, transform.position + _owner.transform.forward + Vector3.up, Quaternion.Euler(_owner.transform.rotation.eulerAngles + new Vector3(0.0f, 180.0f, 0.0f))), 2.0f);
        GameObject orb = Instantiate(orbPrefab, transform.position + _owner.transform.forward + Vector3.up, Quaternion.identity);
        Bullet orbComp = orb.GetComponent<Bullet>();
        if (orbComp != null)
        {
            orbComp.Init(_owner.gameObject, null, transform.position + _owner.transform.forward * 1.0f + Vector3.up, Vector3.zero, _owner.transform.forward, 50.0f);
        }

        _owner.PlayerAnimator.SetBool("isPlayBackHeavyAnim", false);
        _owner.FollowCamera.SetCameraRotatable(true);
        _owner.SetMovable(true);

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

    //private IEnumerator CoPlayMorpher(float targetValue)
    //{
    //    float currentValue = _smr.GetBlendShapeWeight(0);
    //    float elapsed = 0f;
    //    while (elapsed < morphDuration)
    //    {
    //        elapsed += Time.deltaTime;
    //        float newValue = Mathf.Lerp(currentValue, targetValue, elapsed / morphDuration);
    //        _smr.SetBlendShapeWeight(0, newValue);
    //        yield return null;
    //    }
    //    _smr.SetBlendShapeWeight(0, targetValue); // 최종 값 설정
    //}
}
