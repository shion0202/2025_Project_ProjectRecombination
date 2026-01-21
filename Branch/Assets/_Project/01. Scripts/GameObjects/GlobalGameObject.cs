using UnityEngine;
using System;

public class GlobalGameObject : MonoBehaviour
{
    public event Action OnObjectDestroyed;
    public event Action OnObjectDisabled;
    
    private void OnDestroy()
    {
        // 오브젝트가 파괴되었음을 외부에 알림
        Debug.Log(gameObject.name + "is Destroy");

        OnObjectDestroyed?.Invoke();
    }

    private void OnDisable()
    {
        // 오브젝트가 비활성화되었음을 외부에 알림
        Debug.Log(gameObject.name + "is Disable");
        OnObjectDisabled?.Invoke();
    }
}