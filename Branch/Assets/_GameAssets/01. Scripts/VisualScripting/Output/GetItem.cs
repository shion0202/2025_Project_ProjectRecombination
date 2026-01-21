using _Project.Scripts.VisualScripting;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 파츠 아이템 획득을 위한 Output
public class GetItem : ProcessBase
{
    [SerializeField] private int unlockIndex = -1;
    [SerializeField] private List<string> partNames;
    private List<PartBase> _partList = new();
    private Inventory _inven;

    private bool _isInit;

    private void Update()
    {
        if (_isInit) return;
        if (!Managers.MonsterManager.Instance.Player) return;
        
        PlayerController player = Managers.MonsterManager.Instance.Player.GetComponent<PlayerController>();
        if (!player) return;
        
        _inven = player.Inven;
        foreach (string s in partNames)
        {
            _partList.Add(_inven.Parts[s]);
        }
        _isInit = true;
    }

    public override void Execute()
    {
        if (IsOn) return;
        if (partNames.Count <= 0) return;

        GetPartItems();
    }

    public void GetPartItems()
    {
        foreach (PartBase part in _partList)
        {
            _inven.GetItem(part);
        }

        if (unlockIndex > -1)
        {
            Managers.GUIManager.Instance.GameUIController.UnlockParts(unlockIndex);
            Managers.GUIManager.Instance.GameUIController.ActivateRadialMessage(false);
        }
    }
}
