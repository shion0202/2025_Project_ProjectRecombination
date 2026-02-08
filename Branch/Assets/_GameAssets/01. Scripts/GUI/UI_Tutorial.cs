using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Tutorial : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image exampleImage;
    [SerializeField] private string defaultKey;

    private Dictionary<string, TutorialDataSO> tutorialDict;
    private string currentKey;

    private void Awake()
    {
        LoadTutorialData();
    }

    public void ShowTutorialByKey(string key)
    {
        if (!tutorialDict.TryGetValue(key, out var data))
        {
            Debug.LogWarning($"Tutorial key not found: {key}");
            return;
        }

        ShowTutorial(data);
    }

    private void ShowTutorial(TutorialDataSO data)
    {
        titleText.text = data.title;
        descriptionText.text = data.description;
        exampleImage.sprite = data.exampleImage;
    }

    private void LoadTutorialData()
    {
        TutorialDataSO[] datas = Resources.LoadAll<TutorialDataSO>("Tutorial");
        tutorialDict = new Dictionary<string, TutorialDataSO>();

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
        currentKey = defaultKey;
    }
}
