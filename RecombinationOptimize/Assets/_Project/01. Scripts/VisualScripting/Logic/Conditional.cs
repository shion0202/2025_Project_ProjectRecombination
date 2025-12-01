using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class Conditional : ProcessBase
    {
        [SerializeField] private ProcessData inputData;
        [SerializeField] private List<ProcessData> outputData;

        private bool _isRunning;


        // TODO: inputData 가 ture 일때 한번만 실행된다.
        // Input Data에 True 신호가 들어올 때 마다 한번 실행한다.
        private void Update()
        {
            IsOn = CheckInputProcessStatus(inputData);

            if (IsOn)
            {
                if (_isRunning) return;

                Execute();
                _isRunning = true;
            }
            else
            {
                _isRunning = false;
            }
        }

        public override void Execute()
        {
            foreach (var output in outputData)
                output.process.Execute();
        }
    }
}