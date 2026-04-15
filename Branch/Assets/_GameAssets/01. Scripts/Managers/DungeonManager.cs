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
        [SerializeField] private GameObject startPosition;
        
        // 스테이지 데이터
        [SerializeField] private StageData[] stageDatas;
        
        // 현재 플레이어가 있는 스테이지 인덱스
        [SerializeField] private int currentPlayerStageIndex;
        public int CurrentPlayerStageIndex { get => currentPlayerStageIndex; private set => currentPlayerStageIndex = value; }
        
        public Vector3 RestartPosition { get; set; }
        
        // 로딩된 스테이지 딕셔너리
        private Dictionary<int, string> LoadedStages { get; set; } = new();

        // 스테이지 갱신 중 여부
        private bool _isUpdatingStage = false;

        #region Initialization

        private bool _isInit;
        
        public async Task Init()
        {
            try
            {
                if (_isInit) return;
                
                // 초기화 작업 수행
                CurrentPlayerStageIndex = 0;
                LoadedStages.Clear();

                // 스테이지 데이터 로드 (플레이어의 현재 위치 + 주변 스테이지 로딩
                foreach (StageData stageData in stageDatas)
                {
                    if (stageData.stageIndex != CurrentPlayerStageIndex + 1 &&
                        stageData.stageIndex != CurrentPlayerStageIndex) continue;

                    await LoadStage(stageData);
                }
                
                // 미니맵 로드
                LoadMiniMap();
                
                _isInit = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DungeonManager] 초기화 중 예외 발생: {e}");
            }
        }

        public void SetStartPosition(GameObject obj)
        {
            startPosition = obj;
        } 

        public void SetPlayerStartPosition()
        {
            try
            {
                if (GameManager.Instance.Player is null) return;
                if (startPosition is null) return;
                
                GameManager.Instance.Player.transform.position = startPosition.transform.position;
                
                // Dynamic 씬을 Active 씬으로 설정
                SceneController.Instance.SetActiveScene(LoadedStages[CurrentPlayerStageIndex]);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DungeonManager] 플레이어 시작 위치 설정 중 예외 발생: {e}");
            }
        }

        #endregion

        #region Manage Stage Load/Unload

        private async Task LoadStage(StageData stageData)
        {
            if (LoadedStages.ContainsKey(stageData.stageIndex)) return;

            LoadedStages.Add(stageData.stageIndex, stageData.stageName);

            await SceneController.Instance.LoadSceneAdditive(stageData.stageName);
        }
        
        private async Task UnloadStage(StageData stageData)
        {
            if (!LoadedStages.ContainsKey(stageData.stageIndex)) return;

            LoadedStages.Remove(stageData.stageIndex);

            await SceneController.Instance.UnloadScene(stageData.stageName);
        }
        
        public async void UpdatePlayerStageIndex(int newStageIndex)
        {
            // 이미 스테이지 갱신 작업 중이라면 추가 요청 무시
            if (_isUpdatingStage) return;

            try
            {
                if (newStageIndex == CurrentPlayerStageIndex) return;
                _isUpdatingStage = true;

                // 새로운 스테이지 인덱스에 따라 필요한 스테이지 로드/언로드
                {
                    // newStageIndex 값이 현재 플레이어 스테이지 인덱스보다 작아지는 경우 (뒤로 이동)
                    if (newStageIndex < CurrentPlayerStageIndex)
                    {
                        if (LoadedStages.ContainsKey(CurrentPlayerStageIndex + 1))
                            await UnloadStage(stageDatas[CurrentPlayerStageIndex + 1]);
                        
                        await LoadStage(stageDatas[newStageIndex - 1 < 0 ? 0 : newStageIndex - 1]);
                    }
                    // newStageIndex 값이 현재 플레이어 스테이지 인덱스보다 커지는 경우 (앞으로 이동)
                    else if (newStageIndex > CurrentPlayerStageIndex)
                    {
                        if (LoadedStages.ContainsKey(CurrentPlayerStageIndex - 1))
                            await UnloadStage(stageDatas[CurrentPlayerStageIndex - 1]);

                        await LoadStage(stageDatas[newStageIndex + 1]);
                    }
                }
                
                CurrentPlayerStageIndex = newStageIndex;
                
                // 현재 플레이어가 있는 스테이지의 Dynamic 씬을 Active 씬으로 설정
                SceneController.Instance.SetActiveScene(LoadedStages[CurrentPlayerStageIndex]);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DungeonManager] 플레이어 스테이지 인덱스 업데이트 중 예외 발생: {e}");
            }
            finally
            {
                _isUpdatingStage = false;
            }
        }

        #endregion

        #region Test Load All Stages

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

        #endregion
        
        /// <summary>
        /// 모든 스테이지 언로드
        /// </summary>
        public async Task UnloadAllStage()
        {
            try
            {
                Debug.Log("[DungeonManager] 모든 스테이지 언로드 시작...");
                
                foreach (StageData stageData in stageDatas)
                {
                    await SceneController.Instance.UnloadScene(stageData.stageName);
                }
                
                Debug.Log("[DungeonManager] 모든 스테이지 언로드 완료!");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DungeonManager] 모든 스테이지 언로드 중 예외 발생: {e}");
            }
        }

        /// <summary>
        /// 미니맵 리소스 로드
        /// </summary>
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

        public async void ResetCurrentStage()
        {
            try
            {
                Debug.Log("[DungeonManager] 현재 스테이지 리셋 시작...");
                
                if (!LoadedStages.ContainsKey(CurrentPlayerStageIndex))
                {
                    Debug.LogWarning("[DungeonManager] 현재 스테이지가 로드되어 있지 않습니다.");
                    return;
                }
                
                StageData currentStageData = stageDatas[CurrentPlayerStageIndex];
                
                // 1. 현재 스테이지 언로드
                await SceneController.Instance.UnloadScene(currentStageData.stageName);
                
                // 2. 현재 스테이지 다시 로드
                await SceneController.Instance.LoadSceneAdditive(currentStageData.stageName);
                
                // 3. 플레이어 위치 리스폰 지점으로 이동
                if (GameManager.Instance.Player)
                {
                    GameManager.Instance.Player.enabled = false;
                    GameManager.Instance.Player.transform.position = RestartPosition;
                    GameManager.Instance.Player.enabled = true;
                }

                currentPlayerStageIndex--;
                SceneController.Instance.SetActiveScene(LoadedStages[CurrentPlayerStageIndex]);
                
                Debug.Log("[DungeonManager] 현재 스테이지 리셋 완료!");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DungeonManager] 현재 스테이지 리셋 중 예외 발생: {e}");
            }
        }
    }
}
