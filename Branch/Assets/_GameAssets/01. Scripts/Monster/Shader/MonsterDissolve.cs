using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterDissolve : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private float dissolveDuration = 2.0f;
    [SerializeField] private float spawnDissolveDuration = 2.0f;
    // [SerializeField] private Material dissolveMaterial;
    // [SerializeField] private Material originalMaterial;

    private static readonly int DissolveAmount = Shader.PropertyToID("_VisibleAmount");
    
    public bool isDissolved;

    public void Init()
    {
        foreach (Material material in skinnedMeshRenderer.materials)
        {
            material.SetFloat(DissolveAmount, 1f);
        }
    }
    
    public void Init(float dissolveValue)
    {
        foreach (Material material in skinnedMeshRenderer.materials)
        {
            material.SetFloat(DissolveAmount, dissolveValue);
        }
    }
    
    // 디졸브 효과 시작
    public void StartDissolve(bool isSpawning)
    {
        StartCoroutine(DissolveCoroutine(isSpawning));
    }

    // 디졸브 = false, 스폰 = true
    private IEnumerator DissolveCoroutine(bool isSpawning)
    {
        // skinnedMeshRenderer.material;
        
        float elapsedTime = 0f;
        float startValue = isSpawning ? 1f : 0f;
        float endValue = isSpawning ? 0f : 1f;
        
        float duration = isSpawning ? spawnDissolveDuration : dissolveDuration;
        
        // 디졸브 머티리얼로 변경
        // if (dissolveMaterial is null) yield break;
        // skinnedMeshRenderer.material = dissolveMaterial;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float dissolveAmount = Mathf.Lerp(startValue, endValue, elapsedTime / duration);
            foreach (Material material in skinnedMeshRenderer.materials)
            {
                material.SetFloat(DissolveAmount, dissolveAmount);
            }
            // skinnedMeshRenderer.material.SetFloat(DissolveAmount, dissolveAmount);
            yield return null;
        }
        
        // 원래 머티리얼로 복원
        // if (originalMaterial is null) yield break;
        // skinnedMeshRenderer.material = originalMaterial;
        isDissolved = !isSpawning;
    }
}
