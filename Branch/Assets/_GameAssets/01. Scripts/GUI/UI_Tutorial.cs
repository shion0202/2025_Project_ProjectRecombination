using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UI_Tutorial : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI pageIndicatorText; // "1/3" 처럼 표시할 텍스트
    [SerializeField] private Image exampleImage;
    [SerializeField] private string defaultKey;

    [Header("Navigation Buttons")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    private Dictionary<string, TutorialDataSO> tutorialDict;
    private TutorialDataSO currentData;
    private int currentPageIndex = 0;

    private void Awake()
    {
        LoadTutorialData();

        // 버튼 이벤트 바인딩
        if (prevButton != null) prevButton.onClick.AddListener(OnClickPrev);
        if (nextButton != null) nextButton.onClick.AddListener(OnClickNext);
    }

    public void ShowTutorialByKey(string key)
    {
        if (!tutorialDict.TryGetValue(key, out var data))
        {
            Debug.LogWarning($"Tutorial key not found: {key}");
            return;
        }

        currentData = data;
        currentPageIndex = 0; // 새 튜토리얼을 열 때는 항상 1페이지부터
        UpdateUI();
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

        // 데이터가 리스트/배열이라고 가정 (SO에서 string[] descriptions로 수정 필요)
        titleText.text = currentData.title;
        exampleImage.sprite = currentData.exampleImage;

        // 현재 인덱스에 맞는 설명글 표시
        if (currentData.descriptions != null && currentData.descriptions.Length > 0)
        {
            descriptionText.text = currentData.descriptions[currentPageIndex];

            // 페이지 번호 업데이트 (ex: 1 / 3)
            if (pageIndicatorText != null)
                pageIndicatorText.text = $"{currentPageIndex + 1} / {currentData.descriptions.Length}";
        }

        // 버튼 활성화/비활성화 제어 (첫 페이지면 '이전' 비활성화 등)
        if (prevButton != null) prevButton.interactable = currentPageIndex > 0;
        if (nextButton != null) nextButton.interactable = currentPageIndex < currentData.descriptions.Length - 1;
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
