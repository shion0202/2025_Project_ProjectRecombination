using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialData", menuName = "Scriptable Object/Tutorial Data", order = 21)]
public class TutorialDataSO : ScriptableObject
{
    public string key;
    public string title;
    [TextArea(3, 10)] public string[] descriptions;
    public Sprite exampleImage;
}
