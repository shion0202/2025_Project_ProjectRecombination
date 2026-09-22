using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 얼굴 SDF 셰이더에 캐릭터의 앞/오른쪽 방향을 넘긴다.
///
/// 머티리얼 에셋이 아니라 얼굴 렌더러에 MaterialPropertyBlock으로 넣는다.
/// 에셋에 넣으면 실제 얼굴 렌더러가 다른 머티리얼을 쓸 때(파츠별 머티리얼 교체 등) 값이 전달되지 않아
/// 얼굴 방향이 월드 정면에 고정되고, 에디터에서도 매 프레임 머티리얼 파일이 수정된다.
/// </summary>
[ExecuteAlways]
public class FaceTowards : MonoBehaviour
{
    private static readonly int FaceForwardId = Shader.PropertyToID("_FaceForwardDirection");
    private static readonly int FaceRightId = Shader.PropertyToID("_FaceRightDirection");

    [SerializeField] private Transform targetTransform;
    [Tooltip("얼굴 방향을 넣을 렌더러. 비우면 targetTransform 아래에서 이름에 SDF가 들어간 머티리얼을 쓰는 렌더러를 찾는다.")]
    [SerializeField] private Renderer faceRenderer;

    private MaterialPropertyBlock _propertyBlock;

    private void Update()
    {
        SetHeadDirection();
    }

    private void SetHeadDirection()
    {
        if (targetTransform == null) return;

        if (faceRenderer == null)
        {
            faceRenderer = FindFaceRenderer();
            if (faceRenderer == null) return;
        }

        _propertyBlock ??= new MaterialPropertyBlock();

        // 다른 값이 들어 있을 수 있으므로 기존 블록을 읽어 방향만 덮어쓴다.
        faceRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetVector(FaceForwardId, targetTransform.forward);
        _propertyBlock.SetVector(FaceRightId, targetTransform.right);
        faceRenderer.SetPropertyBlock(_propertyBlock);
    }

    // 몸 머티리얼도 같은 셰이더라 얼굴 방향 속성을 갖고 있으므로, 이름에 SDF가 들어간 얼굴 머티리얼로 구분한다.
    private Renderer FindFaceRenderer()
    {
        Transform root = targetTransform != null ? targetTransform : transform;
        foreach (Renderer candidate in root.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in candidate.sharedMaterials)
            {
                if (material != null && material.name.Contains("SDF") && material.HasProperty(FaceForwardId))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    public override string ToString()
    {
        string targetName = targetTransform != null ? targetTransform.name : "None";
        string rendererName = faceRenderer != null ? faceRenderer.name : "None";

        string log = $"[{gameObject.name} ({GetType().Name})] Target: {(targetName)}, Renderer: {(rendererName)}";
        return log;
    }
}
