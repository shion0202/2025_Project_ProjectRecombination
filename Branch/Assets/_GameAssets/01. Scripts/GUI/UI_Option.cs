using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class UI_Option : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private TextMeshProUGUI bgmVolumeText;
    [SerializeField] private TextMeshProUGUI seVolumeText;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;

    [Header("HDR")]
    [SerializeField] private TextMeshProUGUI TxtStatus;
    [SerializeField] private Button HDRLeftArrow;
    [SerializeField] private Button HDRRightArrow;

    private bool isHDREnabled = false;

    private void Start()
    {
        if (HDRLeftArrow != null) HDRLeftArrow.onClick.AddListener(OnClickHDRLeft);
        if (HDRRightArrow != null) HDRRightArrow.onClick.AddListener(OnClickHDRRight);

        // HDR 자동 사양 체크 및 불러오기
        AutoDetectHardwarePerformance();
        SetHDR(isHDREnabled); // 실제 렌더 파이프라인에 적용
        UpdateHDRUI();        // UI 화살표 및 텍스트 갱신

        float bgmVolume = PlayerPrefs.GetFloat("BGMParam", 0.8f);
        float seVolume = PlayerPrefs.GetFloat("SEParam", 0.8f);

        if (bgmSlider != null) bgmSlider.value = bgmVolume;
        if (seSlider != null) seSlider.value = seVolume;

        SetBGMVolume(bgmVolume);
        SetSEVolume(seVolume);

        UpdateHDRUI();
    }

    public void SetBGMVolume(float value)
    {
        bgmVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";

        value = Mathf.Clamp(value, 0.0001f, 1.0f);
        audioMixer.SetFloat("BGMParam", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("BGMParam", value);
    }

    public void SetSEVolume(float value)
    {
        seVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";

        value = Mathf.Clamp(value, 0.0001f, 1.0f);
        audioMixer.SetFloat("SEParam", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("SEParam", value);
    }

    public void OnClickHDRLeft()
    {
        if (isHDREnabled)
        {
            isHDREnabled = false;
            ApplyHDRChange();
        }
    }

    public void OnClickHDRRight()
    {
        if (!isHDREnabled)
        {
            isHDREnabled = true;
            ApplyHDRChange();
        }
    }

    public void SetHDR(bool enable)
    {
        // 현재 사용 중인 Render Pipeline Asset을 가져옵니다.
        UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        if (urpAsset != null)
        {
            // HDR 설정을 변경합니다.
            urpAsset.supportsHDR = enable;
            Debug.Log($"HDR이 {(enable ? "활성화" : "비활성화")}되었습니다.");
        }
    }

    private void ApplyHDRChange()
    {
        SetHDR(isHDREnabled);
        UpdateHDRUI();
    }

    private void UpdateHDRUI()
    {
        if (isHDREnabled)
        {
            TxtStatus.text = "활성화";
            HDRLeftArrow.interactable = true;
            HDRRightArrow.interactable = false;
        }
        else
        {
            TxtStatus.text = "비활성화";
            HDRLeftArrow.interactable = false;
            HDRRightArrow.interactable = true;
        }
    }

    private void AutoDetectHardwarePerformance()
    {
        // 이미 사용자가 설정을 변경한 적이 있는지 확인 (저장된 값이 있으면 자동 설정 건너뜀)
        if (PlayerPrefs.HasKey("HDREnabled"))
        {
            isHDREnabled = PlayerPrefs.GetInt("HDREnabled") == 1;
            return;
        }

        // 하드웨어 정보 읽기
        int vram = SystemInfo.graphicsMemorySize; // GPU 메모리 (MB 단위)
        int cpuCount = SystemInfo.processorCount; // CPU 코어 수

        // 고사양 기준 설정 (예: VRAM 4GB 초과 및 8코어 이상)
        // 최신 플래그십 기기들을 타겟으로 한다면 6GB(6144MB) 이상을 추천합니다.
        if (vram > 6144 && cpuCount >= 8)
        {
            isHDREnabled = true; // 고사양 기기: HDR 기본 활성화
        }
        else
        {
            isHDREnabled = false; // 중저사양 기기: HDR 기본 비활성화
        }

        // 결정된 기본값 저장
        PlayerPrefs.SetInt("HDREnabled", isHDREnabled ? 1 : 0);
    }
}
