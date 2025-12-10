using Monster.AI.FSM;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Managers
{
    [Serializable]
    public struct StageData
    {
        public int stageIndex;
        public string stageName;
    }
    public class DungeonManager : Singleton<DungeonManager>
    {
        [SerializeField] private GameObject miniMapPrefab;
        [SerializeField] private Vector3 startPosition;
        
        // 스테이지 데이터
        [SerializeField] private StageData[] stageDatas;
        
        // 현재 플레이어가 있는 스테이지 인덱스
        public int CurrentPlayerStageIndex { get; set; } = 0;
        
        // 로딩된 스테이지 딕셔너리
        public Dictionary<int, string> LoadedStages { get; private set; } = new ();

        #region Initialization

        public async Task Init()
        {
            try
            {
                // 초기화 작업 수행
                CurrentPlayerStageIndex = 0;
                LoadedStages.Clear();

                // 스테이지 데이터 로드 (플레이어의 현재 위치 + 주변 스테이지 로딩
                foreach (StageData stageData in stageDatas)
                {
                    if (stageData.stageIndex != CurrentPlayerStageIndex - 1 &&
                        stageData.stageIndex != CurrentPlayerStageIndex + 1 &&
                        stageData.stageIndex != CurrentPlayerStageIndex) continue;

                    await LoadStage(stageData);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DungeonManager] 초기화 중 예외 발생: {e}");
            }
        }

        public void SetPlayerStartPosition()
        {
            try
            {
                if (GameManager.Instance.Player is null) return;
                
                GameManager.Instance.Player.transform.position = startPosition;
                
                // Dynamic 씬을 Active 씬으로 설정
                SceneController.Instance.SetActiveScene(LoadedStages[CurrentPlayerStageIndex] + "/Dynamic");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DungeonManager] 플레이어 시작 위치 설정 중 예외 발생: {e}");
            }
        }

        private async Task LoadStage(StageData stageData)
        {
            if (LoadedStages.ContainsKey(stageData.stageIndex)) return;
            
            await SceneController.Instance.LoadSceneAdditive(stageData.stageName + "/Static");
            await SceneController.Instance.LoadSceneAdditive(stageData.stageName + "/Dynamic");
            await SceneController.Instance.LoadSceneAdditive(stageData.stageName + "/Hybrid");
            
            LoadedStages.Add(stageData.stageIndex, stageData.stageName);
        }
        
        private async Task UnloadStage(StageData stageData)
        {
            if (!LoadedStages.ContainsKey(stageData.stageIndex)) return;
            
            await SceneController.Instance.UnloadScene(stageData.stageName + "/Static");
            await SceneController.Instance.UnloadScene(stageData.stageName + "/Dynamic");
            await SceneController.Instance.UnloadScene(stageData.stageName + "/Hybrid");
            
            LoadedStages.Remove(stageData.stageIndex);
        }
        
        public async void UpdatePlayerStageIndex(int newStageIndex)
        {
            try
            {
                if (newStageIndex == CurrentPlayerStageIndex) return;
                
                // 새로운 스테이지 인덱스에 따라 필요한 스테이지 로드/언로드
                if (!LoadedStages.ContainsKey(newStageIndex + 1))
                    await LoadStage(stageDatas[newStageIndex + 1]);
                
                if (LoadedStages.ContainsKey(CurrentPlayerStageIndex - 1))
                    await UnloadStage(stageDatas[CurrentPlayerStageIndex - 1]);
                
                CurrentPlayerStageIndex = newStageIndex;
                
                // 현재 플레이어가 있는 스테이지의 Dynamic 씬을 Active 씬으로 설정
                SceneController.Instance.SetActiveScene(LoadedStages[CurrentPlayerStageIndex] + "/Dynamic");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DungeonManager] 플레이어 스테이지 인덱스 업데이트 중 예외 발생: {e}");
            }
        }

        #endregion

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
