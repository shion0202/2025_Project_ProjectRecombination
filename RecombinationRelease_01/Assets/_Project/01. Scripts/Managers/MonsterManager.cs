using Monster;
using Monster.AI;
using Monster.AI.Blackboard;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Managers
{
    public class MonsterManager: Singleton<MonsterManager>
    {
        [SerializeField] private List<GameObject> monsters;     // 몬스터가 인스턴스 되면 추가되는 리스트
        [SerializeField] private GameObject player;
        
        public GameObject Player => player;
        
        // public List<GameObject> Monsters => monsters;
        
        public void AddMonster(GameObject monster)
        {
            if (monster is not null && !monsters.Contains(monster))
            {
                monsters.Add(monster);
            }
        }
        
        public void RemoveMonster(GameObject monster)
        {
            if (monster is not null && monsters.Contains(monster))
            {
                monsters.Remove(monster);
            }
        }

        public GameObject[] GetBattleMonsters()
        {
            List<GameObject> battleMonsters = new();
            foreach (GameObject monster in monsters)
            {
                Blackboard blackboard = monster?.GetComponentInChildren<Blackboard>();
                if (blackboard is not null)
                {
                    if (blackboard.State.HasState("Chase") || blackboard.State.HasState("Attack") || blackboard.State.HasState("Hit"))
                    {
                        battleMonsters.Add(monster);
                    }
                }

            }
            return battleMonsters.ToArray();
        }

        public void PauseMonsters()
        {
            // To-do: Monster AI를 정지시키는 로직 (카메라 컷씬 등 필요한 상황에서 Game Manager에 의해 호출되는 함수)
        }

        public void UnpauseMonsters()
        {
            // To-do: Monster AI를 다시 실행시키는 로직
            // 함수 2개 쓰는 게 불편하다면 bool 등으로 함수 하나로 통일하고 Game Manager 쪽만 수정해주시면 됩니다
        }
    }
}
