using _Project.Scripts.VisualScripting;
using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 캐릭터의 체력을 회복시키는 Output
public class HealCharacter : ProcessBase
{
    [SerializeField] private float healAmount = 0.0f;
    [SerializeField] private EHealType healType = EHealType.Flat;
    private string debuggingString = string.Empty;

    public override void Execute()
    {
        IsOn = true;

        // 플레이어를 찾아서 Hp를 회복
        // To-do: 임시로 Monster Manager를 통해 Player를 탐색하며, 추후 Manager가 정리될 경우 역할에 맞는 Manager로 수정 필요
        PlayerController player = MonsterManager.Instance.Player.GetComponent<PlayerController>();
        if (player != null)
        {
            player.HealHp(healAmount, healType);

            debuggingString = healType == EHealType.Flat ? "" : "%";
            Debug.Log($"Heal player's hp: +{healAmount}{debuggingString}");
            
        }
        else
        {
            Debug.Log("Can't find player by monster manager.");
        }
    }

    public override string ToString()
    {
        string objectName = gameObject.name;
        string log = $"[{objectName} ({GetType().Name})] IsOn: {IsOn}, HealAmount: {healAmount}, HealType: {healType.ToString()}, HealRange: {healType.ToString()}";
        return log;
    }
}
