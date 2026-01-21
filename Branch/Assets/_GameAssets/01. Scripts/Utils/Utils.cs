using FORGE3D;
using Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Utils
{
    // 풀링 오브젝트일 때와 아닐 때를 구분하여 코드를 작성하는 것이 번거롭기에 구현한 범용 Instantiate 함수
    // 추후에 풀링 오브젝트 여부가 바뀌더라도 큰 수정 없이 변경 가능
    public static GameObject Instantiate(GameObject go)
    {
        // 풀링 오브젝트일 경우
        if (PoolManager.Instance.IsPooledObject(go).Item1)
        {
            GameObject pooledObject = PoolManager.Instance.GetObject(go);
            return pooledObject;
        }

        return GameObject.Instantiate(go);
    }

    public static GameObject Instantiate(GameObject go, Transform parent)
    {
        if (PoolManager.Instance.IsPooledObject(go).Item1)
        {
            GameObject pooledObject = PoolManager.Instance.GetObject(go, parent);
            return pooledObject;
        }

        return GameObject.Instantiate(go, parent);
    }

    public static GameObject Instantiate(GameObject go, Vector3 position, Quaternion rotation)
    {
        if (PoolManager.Instance.IsPooledObject(go).Item1)
        {
            GameObject pooledObject = PoolManager.Instance.GetObject(go, position, rotation);
            return pooledObject;
        }

        return GameObject.Instantiate(go, position, rotation);
    }

    public static GameObject Instantiate(GameObject go, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (PoolManager.Instance.IsPooledObject(go).Item1)
        {
            GameObject pooledObject = PoolManager.Instance.GetObject(go, position, rotation, parent);
            return pooledObject;
        }

        return GameObject.Instantiate(go, position, rotation, parent);
    }

    public static void Destroy(GameObject go, float delay = 0.0f)
    {
        if (!go) return;

        if (PoolManager.Instance.IsPooledObject(go).Item1)
        {
            PoolManager.Instance.ReleaseObject(go, delay);
            return;
        }

        GameObject.Destroy(go, delay);
    }

    // 공통으로 사용할 데미지 계산식
    // 도트 데미지 등 필요한 경우가 존재하여 방어 무시율 스탯을 따로 인자로 추가하였으며, 필요에 따라 공격자의 스탯을 인자로 받는 식으로 수정 가능
    public static float GetDamage(float originalDamage, float defenceIgnoreRate, float unitOfTime, StatDictionary stats)
    {
        // 현재 데미지 공식: (원래 데미지 - (피격자의 방어력 * (1 - 공격자의 방어 무시율))) * (1 - 피격자의 데미지 감소율)
        float totalDamage = (originalDamage - ((stats[EStatType.Defence].value + stats[EStatType.AddDefence].value) * (1 - defenceIgnoreRate)) * unitOfTime) * (1 - stats[EStatType.DamageReductionRate].value);

        // 방어력이나 데미지 감소율로 인한 감소 값이 원래 데미지보다 클 경우 최소 데미지를 보장하도록 Clamp하였으나, 기획 의도에 따라 수정될 수 있음
        return Mathf.Clamp(totalDamage, 1.0f * unitOfTime, totalDamage);
    }

    public static float GetDamage(float originalDamage, float defenceIgnoreRate, float unitOfTime, Dictionary<string, object> stats)
    {
        object oDefence, oAddDefence, oDamageReductionRate;
        stats.TryGetValue("Defence", out oDefence);
        stats.TryGetValue("AddDefence", out oAddDefence);
        stats.TryGetValue("DamageReductionRate", out oDamageReductionRate);

        float defence = oDefence != null ? (float)oDefence : 0.0f;
        float addDefence = oAddDefence != null ? (float)oAddDefence : 0.0f;
        float damageReductionRate = oDamageReductionRate != null ? (float)oDamageReductionRate : 0.0f;

        float totalDamage = (originalDamage - (((float)defence + (float)addDefence) * (1 - defenceIgnoreRate)) * unitOfTime) * (1 - (float)damageReductionRate);
        return Mathf.Clamp(totalDamage, 1.0f * unitOfTime, totalDamage);
    }
}
