using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Managers;

public class UI_Tutorial : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI pageIndicatorText; // "1/3" 처럼 표시할 텍스트
    [SerializeField] private Image exampleImage;
    [SerializeField] private string defaultKey;

    [Header("보스 패턴 설명 모드")]
    [Tooltip("패턴 설명으로 열릴 때 숨길 오브젝트들. 좌측 메뉴 버튼 6개와 우상단 나가기 버튼을 넣는다. " +
             "패턴 설명은 ESC로만 닫으므로 마우스로 누를 것이 없어야 한다.")]
    [SerializeField] private GameObject[] patternModeHidden;

    [Tooltip("패턴 설명으로 열릴 때만 보일 오브젝트들. ESC로 닫는다는 안내 문구 등. " +
             "평소 도움말에서는 나가기 버튼으로 닫으므로 이 안내가 필요 없다.")]
    [SerializeField] private GameObject[] patternModeOnly;

    [Header("예시 영상 (TutorialDataSO.videoName 이 비어 있으면 쓰지 않는다)")]
    [Tooltip("영상이 그려질 RawImage. VideoPlayer의 Target Texture와 같은 RenderTexture를 물려둔다.")]
    [SerializeField] private RawImage exampleVideo;
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Navigation Buttons")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    private Dictionary<string, TutorialDataSO> tutorialDict;
    private TutorialDataSO currentData;
    private int currentPageIndex = 0;
    private bool _isPatternMode;

    // 현재 언어 상태에 맞춰 캐싱할 변수들
    private string currentTitle;
    private string[] currentDescriptions;

    private void Awake()
    {
        // 패턴 설명이 창을 켜기 전에 먼저 부르면 여기 오기 전에 이미 만들어져 있다.
        if (tutorialDict == null) LoadTutorialData();

        // 버튼 이벤트 바인딩
        if (prevButton != null) prevButton.onClick.AddListener(OnClickPrev);
        if (nextButton != null) nextButton.onClick.AddListener(OnClickNext);
    }

    // 설정 창에서 언어가 바뀐 후, 튜토리얼 창이 새로 열릴 때(SetActive(true))마다 실행됩니다.
    private void OnEnable()
    {
        if (currentData == null) return;

        // 변경된 최신 언어 상태를 반영하여 타이틀과 설명 배열을 다시 세팅합니다.
        currentPageIndex = 0;
        SetupCurrentLanguageData();
        UpdateUI();
    }

    public void ShowTutorialByKey(string key)
    {
        // 꺼져 있는 오브젝트에는 Awake가 돌지 않아 사전이 비어 있다.
        // 보스 패턴 설명은 창을 켜기 전에 내용을 먼저 채우므로 이 경로로 들어온다.
        if (tutorialDict == null) LoadTutorialData();

        if (!tutorialDict.TryGetValue(key, out var data))
        {
            Debug.LogWarning($"Tutorial key not found: {key}");
            return;
        }

        currentData = data;
        currentPageIndex = 0; // 튜토리얼을 열 때는 항상 1페이지부터
        _isPatternMode = false; // 평소 도움말 경로로 열렸으므로 패턴 모드를 푼다
        SetupCurrentLanguageData(); // 열리는 순간의 최신 언어 데이터 캐싱
        UpdateUI();
    }

    /// <summary>
    /// 보스 패턴 설명으로 연다. 내용을 채운 뒤 패턴 모드를 켜는 순서여야 한다.
    /// (ShowTutorialByKey가 패턴 모드를 푸는 쪽이라 순서를 바꾸면 메뉴가 그대로 보인다)
    /// </summary>
    public void ShowPatternGuide(string key)
    {
        ShowTutorialByKey(key);
        SetPatternMode(true);
    }

    private void SetupCurrentLanguageData()
    {
        if (currentData == null) return;

        currentTitle = LocalizationManager.IsKorean ? currentData.title : currentData.enTitle;
        currentDescriptions = LocalizationManager.IsKorean ? currentData.descriptions : currentData.enDescriptions;
    }

    public void OnClickNext()
    {
        if (currentData == null || currentData.descriptions == null) return;

        if (currentPageIndex < currentData.descriptions.Length - 1)
        {
            currentPageIndex++;
            UpdateUI();
        }
    }

    public void OnClickPrev()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (currentData == null) return;

        titleText.text = currentTitle;
        UpdateExample();

        if (currentDescriptions.Length > 0)
        {
            descriptionText.text = currentDescriptions[currentPageIndex];

            if (pageIndicatorText != null)
                pageIndicatorText.text = $"{currentPageIndex + 1} / {currentDescriptions.Length}";
        }
        else
        {
            descriptionText.text = string.Empty;
            if (pageIndicatorText != null) pageIndicatorText.text = "0 / 0";
        }

        // 버튼 활성화/비활성화 제어 (첫 페이지면 '이전' 비활성화 등)
        if (prevButton != null) prevButton.interactable = currentPageIndex > 0;
        if (nextButton != null) nextButton.interactable = currentPageIndex < currentData.descriptions.Length - 1;

        ApplyPatternMode();
    }

    /// <summary>
    /// 보스 패턴 설명으로 열지, 평소 도움말로 열지 정한다.
    /// 패턴 설명에서는 좌측 메뉴와 나가기 버튼을 숨긴다. 닫는 것은 ESC가 처리한다.
    /// 평소 도움말 경로(메뉴 버튼, 기본 키)는 ShowTutorialByKey를 거치므로 자동으로 이 모드가 풀린다.
    /// </summary>
    public void SetPatternMode(bool on)
    {
        _isPatternMode = on;
        ApplyPatternMode();
    }

    private void ApplyPatternMode()
    {
        SetActiveAll(patternModeHidden, !_isPatternMode);
        SetActiveAll(patternModeOnly, _isPatternMode);
    }

    private static void SetActiveAll(GameObject[] targets, bool active)
    {
        if (targets == null) return;

        foreach (GameObject target in targets)
        {
            if (target != null) target.SetActive(active);
        }
    }

    /// <summary>
    /// 예시 자료를 영상 또는 스프라이트 중 하나로 띄운다.
    /// videoName이 비어 있으면 기존 동작(exampleImage)을 그대로 쓰므로 기존 도움말 데이터는 손댈 필요가 없다.
    /// </summary>
    private void UpdateExample()
    {
        bool hasVideoName = !string.IsNullOrWhiteSpace(currentData.videoName);
        bool useVideo = hasVideoName && videoPlayer != null && exampleVideo != null;

        // 배선이 빠지면 조용히 이미지로 넘어가 원인을 찾기 어렵다. 어긋난 것을 이름으로 알려준다.
        if (hasVideoName && !useVideo)
        {
            Debug.LogWarning($"[UI_Tutorial] '{currentData.key}'에 videoName이 있지만 " +
                             "exampleVideo/videoPlayer 참조가 비어 있어 예시 이미지로 대체한다.");
        }

        if (exampleImage != null) exampleImage.gameObject.SetActive(!useVideo);
        if (exampleVideo != null) exampleVideo.gameObject.SetActive(useVideo);

        if (!useVideo)
        {
            if (videoPlayer != null) videoPlayer.Stop();
            if (exampleImage != null) exampleImage.sprite = currentData.exampleImage;
            return;
        }

        string url = Path.Combine(Application.streamingAssetsPath, currentData.videoName);

        // 파일이 없으면 VideoPlayer는 예외도 로그도 없이 검은 화면만 보여준다.
        // 안드로이드는 StreamingAssets가 apk 안(jar:file://...)이라 File.Exists로 확인할 수 없으므로 건너뛴다.
        if (!url.Contains("://") && !File.Exists(url))
        {
            Debug.LogWarning($"[UI_Tutorial] 예시 영상을 찾을 수 없다: {url}\n" +
                             "TutorialDataSO.videoName에 확장자(.mp4)가 빠지지 않았는지 확인할 것.");
        }

        if (videoPlayer.url != url)
        {
            videoPlayer.Stop();
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = url;
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.isLooping = true;

            // 패턴 설명은 Time.timeScale = 0 으로 게임을 멈춘 채 보여준다.
            // VideoPlayer가 timeScale을 무시하고 계속 재생되려면 이 모드여야 하므로,
            // 인스펙터 설정이 초기화되어도 영상이 멈추지 않도록 코드에서도 지정한다.
            videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        }

        videoPlayer.Play();
    }

    // 창이 닫혀도 영상이 계속 디코딩되지 않도록 멈춘다.
    private void OnDisable()
    {
        if (videoPlayer != null) videoPlayer.Stop();
    }

    private void LoadTutorialData()
    {
        TutorialDataSO[] datas = Resources.LoadAll<TutorialDataSO>("Tutorial");
        tutorialDict = new Dictionary<string, TutorialDataSO>();

        foreach (TutorialDataSO data in datas)
        {
            if (string.IsNullOrEmpty(data.key)) continue;
            if (tutorialDict.ContainsKey(data.key)) continue;
            tutorialDict.Add(data.key, data);
        }

        foreach (TutorialDataSO data in datas)
        {
            if (string.IsNullOrEmpty(data.key))
            {
                Debug.LogWarning($"{data.name} has empty key");
                continue;
            }

            if (tutorialDict.ContainsKey(data.key))
            {
                Debug.LogWarning($"Duplicate tutorial key: {data.key}");
                continue;
            }

            tutorialDict.Add(data.key, data);
        }

        // 디폴트 표시
        ShowTutorialByKey(defaultKey);
    }
}
