using Monster.AI.FSM;
using UnityEngine;
using System;
using System.Threading.Tasks;

namespace Managers
{
    [Serializable]
    public struct StageData
    {
        public int stageIndex;
        public string stageName;
        // public string staticPath;
        // public string hybridPath;
        // public string dynamicPath;
    }
    public class DungeonManager : Singleton<DungeonManager>
    {
        [SerializeField] private GameObject miniMapPrefab;
        
        // 스테이지 데이터
        [SerializeField] private StageData[] stageDatas;

        /// <summary>
        /// 테스트용 모든 스테이지 로드
        /// </summary>
        public async Task LoadAllStage()
        {
            try
            {
                Debug.Log("[DungeonManager] 모든 스테이지 로드 시작...");
                
                foreach (StageData stageData in stageDatas)
                {
                    await SceneController.Instance.LoadSceneAdditive(stageData.stageName + "/Static");
                    await SceneController.Instance.LoadSceneAdditive(stageData.stageName + "/Dynamic");
                    await SceneController.Instance.LoadSceneAdditive(stageData.stageName + "/Hybrid");
                }
                
                LoadMiniMap();
                
                Debug.Log("[DungeonManager] 모든 스테이지 로드 완료!");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DungeonManager] 모든 스테이지 로드 중 예외 발생: {e}");
            }
        }
        
        private void LoadMiniMap()
        {
            if (miniMapPrefab != null)
            {
                Instantiate(miniMapPrefab);
            }
            else
            {
                Debug.LogWarning("[DungeonManager] 미니맵 프리팹이 할당되지 않았습니다.");
            }
        }
        
        #region 아몬 페이즈 설정

        [Header("아몬 1페이즈 설정")]
        [SerializeField] private FSM amonFirstPhase;
        
        [Header("아몬 2페이즈 설정")]
        [SerializeField] private FSM amonSecondPhasePrefab;
        // [SerializeField] private Transform amonSpawnPoint;
        [SerializeField] private Transform playerTeleportPoint;
        [SerializeField] private Transform playerRespawnPoint;

        #endregion
        
        #region 아몬 페이즈 관리

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

        #endregion
    }
}
