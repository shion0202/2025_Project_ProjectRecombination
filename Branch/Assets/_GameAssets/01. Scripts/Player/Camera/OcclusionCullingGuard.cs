using Managers;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 카메라가 벽 너머로 넘어간 동안에만 오클루전 컬링을 끈다. Main Camera에 붙인다.
///
/// 오클루전 컬링은 카메라가 있는 위치를 기준으로 보이는 물체를 판정한다.
/// 캐릭터가 벽에 붙으면 시네머신 콜라이더의 최소 거리 때문에 카메라가 벽 너머로 밀려나는데,
/// 그 위치에서는 캐릭터 쪽 공간이 벽에 가려진 것으로 판정되어 오브젝트가 꺼지고 화면이 까매진다.
/// 벽은 평소에 Occluder 역할을 해야 하므로 Occluder 설정은 유지하고,
/// 캐릭터에서 카메라까지가 벽에 가로막힌 동안만 컬링을 끄는 방식으로 대응한다.
/// </summary>
[RequireComponent(typeof(Camera))]
public class OcclusionCullingGuard : MonoBehaviour
{
    [Tooltip("벽으로 판정할 레이어. 오클루전 베이크에서 Occluder로 쓰는 레이어와 맞춘다.")]
    [SerializeField] private LayerMask occluderMask = (1 << 0) | (1 << 18);   // Default, Wall

    [Tooltip("캐릭터 발바닥 기준 검사 시작 높이(m). 머리 근처로 둔다.")]
    [SerializeField] private float originHeight = 1.5f;

    [Tooltip("카메라 위치에서 벽과 겹치는지 검사할 반경(m). 카메라 앞면이 벽에 걸친 경우를 잡는다.")]
    [SerializeField] private float cameraProbeRadius = 0.15f;

    [Tooltip("가로막힘이 풀린 뒤 컬링을 다시 켜기까지의 시간(초). 벽 경계에서 켜졌다 꺼졌다 반복하는 것을 막는다.")]
    [SerializeField] private float reenableDelay = 0.2f;

    private Camera _camera;
    private float _lastBlockedTime = float.NegativeInfinity;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

        // 컴포넌트를 끄면 원래 동작(컬링 사용)으로 돌려놓는다.
        if (_camera != null)
        {
            _camera.useOcclusionCulling = true;
        }
    }

    // 시네머신이 카메라 위치를 확정한 뒤, 컬링이 계산되기 직전에 호출된다.
    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
    {
        if (renderingCamera != _camera) return;   // 미니맵, 월드맵 카메라는 대상이 아니다.

        if (IsCameraBehindOccluder())
        {
            _lastBlockedTime = Time.unscaledTime;
        }

        _camera.useOcclusionCulling = Time.unscaledTime - _lastBlockedTime > reenableDelay;
    }

    private bool IsCameraBehindOccluder()
    {
        PlayerController player = GameManager.IsAliveInstance() ? GameManager.Instance.Player : null;
        if (player == null) return false;

        Vector3 origin = player.transform.position + Vector3.up * originHeight;
        Vector3 cameraPosition = _camera.transform.position;

        // 캐릭터에서 카메라까지 벽에 가로막혔거나, 카메라 자체가 벽에 파묻힌 경우
        return Physics.Linecast(origin, cameraPosition, occluderMask, QueryTriggerInteraction.Ignore)
            || Physics.CheckSphere(cameraPosition, cameraProbeRadius, occluderMask, QueryTriggerInteraction.Ignore);
    }
}
