using System;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
public enum LogicType
{
    And,
    Or,
}

public class Logic : ProcessBase
{
    [SerializeField] private ProcessData processes1;
    [SerializeField] private LogicType logicType;
    [SerializeField] private ProcessData processes2;

    private void Update()
    {
        Execute();
    }

    public override void Execute()
    {
        var process1IsOn = CheckInputProcessStatus(processes1);
        var process2IsOn = CheckInputProcessStatus(processes2);

        switch (logicType)
        {
            case LogicType.And:
                IsOn = process1IsOn && process2IsOn;
                break;
            case LogicType.Or:
                IsOn = process1IsOn || process2IsOn;
                break;
        }
    }
}
    
}