using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FPSCheck : MonoBehaviour
{
    public bool isRealtime = false;
    private TextMeshProUGUI fpsText;

    public float updateInterval = 0.5f;  // 평균 FPS 갱신 간격 (초)
    private float accumulatedTime = 0f;  // 경과 시간 누적
    private int frames = 0;               // 누적된 프레임 수

    void Awake()
    {
        fpsText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (isRealtime)
        {
            float fps = 1.0f / Time.deltaTime;
            fpsText.text = $"Current FPS: {Mathf.RoundToInt(fps).ToString()}";
        }
        else
        {
            accumulatedTime += Time.deltaTime;
            frames++;

            if (accumulatedTime >= updateInterval)
            {
                float averageFPS = frames / accumulatedTime;
                fpsText.text = $"Average FPS: {Mathf.RoundToInt(averageFPS).ToString()}";

                // 초기화
                accumulatedTime = 0f;
                frames = 0;
            }
        }
    }
}
