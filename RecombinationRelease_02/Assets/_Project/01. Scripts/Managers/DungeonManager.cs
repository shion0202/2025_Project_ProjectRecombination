using Managers;
using Monster.AI.FSM;
using UnityEngine;

namespace Managers
{
    public class DungeonManager : Singleton<DungeonManager>
    {
        [Header("아몬 1페이즈 설정")]
        [SerializeField] private FSM amonFirstPhase;
        
        [Header("아몬 2페이즈 설정")]
        public FSM amonSecondPhasePrefab;
        // public Transform amonSpawnPoint;
        public Transform playerTeleportPoint;
        public Transform playerRespawnPoint;
        
        public void AmonFirstPhase()
        {
            amonFirstPhase.isEnabled = true;
        }
        
        // 아몬 1페이즈 종료 및 2페이즈 시작
        public void AmonSecondPhase()
        {
            // amonSecondPhasePrefab.SetActive(true);
            amonFirstPhase.isEnabled = false;
            // playerRespawnPoint.position = MonsterManager.Instance.Player.transform.position;
            MonsterManager.Instance.Player.SetActive(false);
            MonsterManager.Instance.Player.transform.position = playerTeleportPoint.position;
            MonsterManager.Instance.Player.SetActive(true);
            amonSecondPhasePrefab.isEnabled = true;
        }
        
        // 아몬 2페이즈 종료
        public void AmonEndPhase()
        {
            amonSecondPhasePrefab.isEnabled = false;
            MonsterManager.Instance.Player.SetActive(false);
            MonsterManager.Instance.Player.transform.position = playerRespawnPoint.position;
            MonsterManager.Instance.Player.SetActive(true);
        }
    }
}
