using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
public class ForLoop : ProcessBase
{
    // [SerializeField] private ProcessData inputData;
    [SerializeField] private List<ProcessData> outputData;
    
    [SerializeField] private int loopCount = 1;
    // [SerializeField] private float delay;

    // private void Update()
    // {
    //     if (CheckInputProcessStatus(inputData))
    //     {
    //         IsOn = true;
    //         Execute();
    //     }
    //     else
    //     {
    //         IsOn = false;
    //     }
    // }

    public override void Execute()
    {
        if (IsOn) return;

        if (outputData is null || outputData.Count == 0) return;
        
        // for (var i = 0; i < loopCount; i++)
        // {
        //     StartCoroutine(RuntimeProcess());
        // }
        for (var i = 0; i < loopCount; i++)
        {
            foreach (var output in outputData)
                output.process.Execute();
        }
    }
    
    // private IEnumerator RuntimeProcess()
    // {
    //     foreach (var output in outputData)
    //         output.process.Execute();
    //     yield return new WaitForSeconds(delay);
    // }
}
    
}