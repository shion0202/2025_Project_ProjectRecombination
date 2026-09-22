using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialData", menuName = "Scriptable Object/Tutorial Data", order = 21)]
public class TutorialDataSO : ScriptableObject
{
    public string key;
    public string title;
    [TextArea(3, 10)] public string[] descriptions;
    public string enTitle;
    [TextArea(3, 10)] public string[] enDescriptions;
    public Sprite exampleImage;

    [Tooltip("StreamingAssets 안의 영상 파일 이름(예: Pattern_SoulAbsorb.mp4). " +
             "비워두면 exampleImage를 그대로 쓴다. GIF는 Unity가 디코딩하지 못하므로 mp4/webm으로 변환해서 넣는다.")]
    public string videoName;
}
