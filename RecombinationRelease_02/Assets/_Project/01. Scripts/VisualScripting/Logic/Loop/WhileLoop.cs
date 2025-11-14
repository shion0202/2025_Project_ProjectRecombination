using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    
public class WhileLoop : ProcessBase
{
    [SerializeField] private ProcessData inputData;
    [SerializeField] private List<ProcessData> outputData;
    
    [SerializeField] private float delay;

    private void Update()
    {
        if (CheckInputProcessStatus(inputData))
        {
            IsOn = true;
            Execute();
        }
        else
        {
            IsOn = false;
        }
    }
    
    public override void Execute()
    {
        StartCoroutine(RuntimeProcess());
    }

    private IEnumerator RuntimeProcess()
    {
        foreach (var output in outputData)
            output.process.Execute();
        yield return new WaitForSeconds(delay);
    }
}
    
}