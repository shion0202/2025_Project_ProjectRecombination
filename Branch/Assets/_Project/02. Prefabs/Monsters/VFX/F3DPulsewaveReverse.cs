using UnityEngine;

namespace FORGE3D
{
    public class F3DPulsewaveReverse : MonoBehaviour
    {
        public float FadeOutDelay = 0.5f; // 페이드 시작까지 딜레이(초)
        public float FadeOutTime = 2f;    // 컬러 복원 속도
        public float ScaleTime = 2f;      // 스케일 축소 속도
        public Color defaultColor = Color.white; // 복원할 컬러

        private MeshRenderer meshRenderer;
        private int tintColorRef;
        private Color color;
        private bool isFadeOut;
        private bool isEnabled;
        private float fadeOutTimer;

        void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            tintColorRef = Shader.PropertyToID("_Color");
            defaultColor = meshRenderer.material.GetColor(tintColorRef);
        }

        void OnEnable()
        {
            // 오브젝트가 활성화될 때 초기화
            transform.localScale = Vector3.one; // 시작 스케일은 1
            color = meshRenderer.material.GetColor(tintColorRef);
            isFadeOut = false;
            isEnabled = true;
            fadeOutTimer = 0f;
        }

        void Update()
        {
            if (!isEnabled) return;

            // 스케일을 점점 줄임
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * ScaleTime);

            // FadeOutDelay 후 페이드 시작
            if (!isFadeOut)
            {
                fadeOutTimer += Time.deltaTime;
                if (fadeOutTimer >= FadeOutDelay)
                {
                    isFadeOut = true;
                }
            }
            else
            {
                // 컬러를 defaultColor로 복원
                color = Color.Lerp(color, defaultColor, Time.deltaTime * FadeOutTime);
                meshRenderer.material.SetColor(tintColorRef, color);
            }

            // 종료 조건: 스케일과 컬러가 충분히 복원되었는지 체크
            bool scaleClose = (transform.localScale - Vector3.zero).sqrMagnitude < 0.0001f;
            bool colorClose = Mathf.Abs(color.r - defaultColor.r) < 0.01f &&
                              Mathf.Abs(color.g - defaultColor.g) < 0.01f &&
                              Mathf.Abs(color.b - defaultColor.b) < 0.01f &&
                              Mathf.Abs(color.a - defaultColor.a) < 0.01f;
            if (scaleClose && colorClose)
            {
                isEnabled = false;
                // 필요시 오브젝트 비활성화 또는 풀 반환 등 추가
                // gameObject.SetActive(false);
            }
        }
    }
}

