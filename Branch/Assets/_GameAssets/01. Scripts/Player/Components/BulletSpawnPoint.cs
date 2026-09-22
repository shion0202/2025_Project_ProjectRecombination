using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletSpawnPoint : MonoBehaviour
{
    // 팔 파츠들이 이 발사 지점을 공유한다. 파츠마다 총구 위치가 달라 장착 시 보정값을 더하므로,
    // 보정 전 원래 위치를 기억해 두고 항상 이 위치 기준으로 보정한다.
    private Vector3 _defaultLocalPosition;
    private bool _isDefaultSaved;

    private void Awake()
    {
        SaveDefault();
    }

    private void SaveDefault()
    {
        if (_isDefaultSaved) return;

        _defaultLocalPosition = transform.localPosition;
        _isDefaultSaved = true;
    }

    // 원래 위치에 파츠별 보정값(이 오브젝트의 부모 기준 로컬 좌표)을 더한다.
    public void ApplyOffset(Vector3 localOffset)
    {
        SaveDefault();
        transform.localPosition = _defaultLocalPosition + localOffset;
    }
}
