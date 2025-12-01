using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
public class TriggerExit : ProcessBase
{
    [SerializeField] private string selectedTag = "";

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(selectedTag))
            Execute();
    }

    public override void Execute()
    {
        IsOn = true;
    }
}
    
}