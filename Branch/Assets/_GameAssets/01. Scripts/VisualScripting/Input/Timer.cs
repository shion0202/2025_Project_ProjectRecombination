using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
public class Timer : ProcessBase
{
    [SerializeField] private float seconds;
    [Tooltip("상호작용 키로 건너뛸 수 있는지. 튜토리얼 메시지 진행용 타이머만 켠다.")]
    [SerializeField] private bool skippable = true;

    // 대기 중인 타이머들. 상호작용 키를 눌렀을 때 한 번에 끝내기 위해 들고 있는다.
    private static readonly HashSet<Timer> Waiting = new();

    /// <summary>대기 중인 타이머를 즉시 끝낸다. 다음 튜토리얼 메시지로 넘어가는 용도.</summary>
    public static bool SkipWaiting()
    {
        if (Waiting.Count == 0) return false;

        foreach (Timer timer in new List<Timer>(Waiting))
        {
            timer.StopAllCoroutines();
            timer.IsOn = true;
        }
        Waiting.Clear();
        return true;
    }

    public override void Execute()
    {
        StartCoroutine(WaitForSeconds());
    }

    private void OnDisable()
    {
        Waiting.Remove(this);
    }

    private IEnumerator WaitForSeconds()
    {
        if (skippable) Waiting.Add(this);

        yield return new WaitForSeconds(seconds);

        Waiting.Remove(this);
        IsOn = true;
    }
}

    
}