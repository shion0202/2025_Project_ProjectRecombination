using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class StartBoss : ProcessBase
    {
        [SerializeField] private string bossName;

        public override void Execute()
        {
            if (IsOn) return;

            Managers.GUIManager.Instance.SetBossName(bossName);
            Managers.GUIManager.Instance.ToggleBossHp(true);

            IsOn = true;
        }
    }
}