using _Project.Scripts.VisualScripting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// AnimCheck 스크립트와 연동하여 오브젝트 애니메이션을 재생하는 Output
public class PlayAnimation : ProcessBase
{
    [SerializeField] private List<AnimCheck> objects = new();

    public override void Execute()
    {
        if (IsOn) return;
        if (CheckNull()) return;

        IsOn = true;

        foreach (AnimCheck obj in objects)
        {
            obj.Play();
        }
    }

    private bool CheckNull()
    {
        return objects.Count <= 0;
    }

    public override string ToString()
    {
        string objectName = gameObject.name;
        string animNames = "";
        foreach (AnimCheck obj in objects)
        {
            if (animNames != "")
            {
                animNames += ", ";
            }
            animNames += (obj != null ? obj.name : "Null");
        }

        string log = $"[{objectName} ({GetType().Name})] IsOn: {IsOn}, Anim Check Objects: ({animNames})";
        return log;
    }
}
