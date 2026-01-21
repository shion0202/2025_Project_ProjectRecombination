using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
public class Activate : ProcessBase
{
    [SerializeField] private GameObject obj;

    public override void Execute()
    {
        IsOn = true;
        obj.SetActive(true);
    }
}
    
}
