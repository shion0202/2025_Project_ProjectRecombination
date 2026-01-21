using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class StartAmonFirstPhase : ProcessBase
    {
        public override void Execute()
        {
            if (IsOn) return;

            Managers.DungeonManager.Instance.AmonFirstPhase();

            IsOn = true;
        }
    }
}