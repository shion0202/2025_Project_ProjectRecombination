using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_Indicator : MonoBehaviour
{
    [SerializeField] private RectTransform indicatorUI;   // HUD에 표시될 UI (ex. Image)
    [SerializeField] private RectTransform directionArrow;
    [SerializeField] private Camera mainCamera;           // 주 카메라
    [SerializeField] private Canvas canvas;               // 인디케이터용 Canvas
    [SerializeField] private Transform defaultTarget;
    [SerializeField] private TextMeshProUGUI distanceText;
    private PlayerController player;
    private Transform target;            // 3D 타겟 오브젝트 (월드 기준)
    private bool _isOn;

    [Header("캔버스 마진 (픽셀)")]
    public float edgeMargin = 30f; // HUD 외곽에서 떨어진 거리

    public bool IsOn
    {
        get => _isOn;
        set => _isOn = value;
    }
    
    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void Start()
    {
        target = defaultTarget;
        player = Managers.MonsterManager.Instance.Player.GetComponent<PlayerController>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (!target || !mainCamera)
        {
            indicatorUI.gameObject.SetActive(false);
            return;
        }

        if (_isOn)
        {
            indicatorUI.gameObject.SetActive(true);
        }
        else
        {
            indicatorUI.gameObject.SetActive(false);
            return;
        }

        Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(target.position);

        bool isBehind = screenPos.z < 0;

        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        float minX = edgeMargin;
        float maxX = screenSize.x - edgeMargin;
        float minY = edgeMargin;
        float maxY = screenSize.y - edgeMargin;

        Vector2 clampedScreenPos;

        if (!isBehind && viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1)
        {
            // 화면 내
            clampedScreenPos = new Vector2(screenPos.x, screenPos.y);
        }
        else
        {
            // 뷰포트 기준 중심(0.5, 0.5) → 타겟 뷰포트(0~1) 방향 벡터
            Vector2 fromCenter = new Vector2(viewportPos.x - 0.5f, viewportPos.y - 0.5f);

            // 만약 뒤에 있다면 벡터 반전
            if (isBehind)
            {
                fromCenter = -fromCenter;
            }

            // 방향 벡터에 (0,0)이 들어오지 않도록 보정
            if (fromCenter.sqrMagnitude < 0.0001f)
                fromCenter = Vector2.up; // 임의의 기본 방향

            // 화면 외곽(가장자리)에 위치하도록 정규화 및 스크린 영역 매핑
            fromCenter.Normalize();

            // 외곽 선분 계산: 중심에서 외곽까지 x/y 비율
            float slope = fromCenter.y / fromCenter.x;

            Vector2 screenCenter = screenSize * 0.5f;
            Vector2 edge = screenCenter;

            // 화면의 네 변과 교점 계산
            float borderX = (fromCenter.x > 0) ? maxX : minX;
            float borderY = slope * (borderX - screenCenter.x) + screenCenter.y;

            if (borderY >= minY && borderY <= maxY)
            {
                // 좌/우 측면에서 교점 발생
                edge.x = borderX;
                edge.y = borderY;
            }
            else
            {
                // 상/하 측면에서 교점 발생
                borderY = (fromCenter.y > 0) ? maxY : minY;
                borderX = (borderY - screenCenter.y) / slope + screenCenter.x;
                edge.x = borderX;
                edge.y = borderY;
            }

            clampedScreenPos = new Vector2(
                Mathf.Clamp(edge.x, minX, maxX),
                Mathf.Clamp(edge.y, minY, maxY)
            );
        }

        if (player != null && target != null && distanceText != null)
        {
            float distanceSqr = (player.transform.position - target.position).sqrMagnitude;
            float approxDistance = Mathf.Sqrt(distanceSqr); // 최적화를 위해 특정 조건에서만 실제 거리 계산을 하거나, 필요할 때만 호출하도록 처리 가능
            distanceText.text = $"{approxDistance:F0}m";
        }

        // 스크린 → UI 좌표 변환
        Vector2 uiPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            clampedScreenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
            out uiPos);

        Vector3 dirToTarget = (mainCamera.transform.position - target.position).normalized;

        // 카메라의 Forward, Right 벡터를 각각 추출
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;

        // 방향 벡터를 카메라가 보는 평면으로 투영 (기준 평면을 camForward와 camRight 평면으로 함)
        Vector3 projectedDir = Vector3.ProjectOnPlane(dirToTarget, mainCamera.transform.up).normalized;

        // 카메라 평면 상에서의 방향 좌표 (Right를 x, Forward를 y 축으로)
        float x = Vector3.Dot(projectedDir, camRight);
        float y = Vector3.Dot(projectedDir, camForward);

        // 각도 계산 (atan2) - Unity UI에서는 Z축 회전으로 적용
        float angle = Mathf.Atan2(x, y) * Mathf.Rad2Deg; // x, y 순서 주의

        directionArrow.localRotation = Quaternion.Euler(0, 0, -angle);

        indicatorUI.anchoredPosition = uiPos;
    }
}