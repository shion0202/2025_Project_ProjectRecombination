using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Managers
{
    public class GameManager : Singleton<GameManager>
    {
        public enum GameState
        {
            Loading,
            Title,
            Prologue,
            Epilogue,
            Playing,
            Paused,
            GameOver,
            Credit
        }

        [SerializeField] private bool isHardMode;
        public bool IsHardMode { get => isHardMode; set => isHardMode = value; }
        public PlayerController Player { get; set; }
        // public GameObject MainCamera { get; set; }
        public GameObject FollowCamera { get; set; }
        public GameObject MinimapObject { get; set; }
        private Coroutine _rebirthRoutine;

        public bool IsLoad { get; private set; }
        public GameState CurrentState { get; private set; } = GameState.Loading;

        private void Update()
        {
            switch (CurrentState)
            {
                case GameState.Playing:
                    PlayingProcess();
                    break;
                case GameState.GameOver:
                    GameOverProcess();
                    break;
            }
        }

        // 모든 매니저들이 로드되었음을 수신
        public void SceneLoaded()
        {
            IsLoad = true;
        }

        private void PlayingProcess()
        {
            if (_rebirthRoutine != null) return;

            float hp = Player.Stats.CurrentHealth;
            if (hp <= 0f) CurrentState = GameState.GameOver;
        }

        private void GameOverProcess()
        {
            // 게임 오버 처리
            Debug.Log("[GameManager] 게임 오버 처리 중...");

            if (_rebirthRoutine != null) return;    // 이미 부활 코루틴이 실행 중이면 무시
            // CurrentState = GameState.GameOver;
            
            GUIManager.Instance.GameUIController.OnGameOverPanel();

            // 부활 코루틴 시작
            _rebirthRoutine = StartCoroutine(RebirthGame());
        }

        // 플레이어 부활 코루틴 (5초 대기 후 부활)
        private IEnumerator RebirthGame()
        {
            yield return new WaitForSeconds(5.0f);
            GUIManager.Instance.GameUIController.CloseGameOverPanel();

            if (IsHardMode)
            {
                Debug.Log("[GameManager] 하드 모드 부활 처리 중...");
                // 1. 몬스터 풀 리셋
                MonsterManager.Instance.ReleaseAllMonsters();
                // PoolManager.Instance.ClearPools();
                // PoolManager.Instance.Init();
                // 2. 플레이어가 위치한 현재 스테이지 리셋
                DungeonManager.Instance.ResetCurrentStage();
            }
            
            Player.Stats.CurrentHealth = Player.Stats.MaxHealth;
            Player.Spawn();

            _rebirthRoutine = null;
            
            CurrentState = GameState.Playing;
            
            Debug.Log("[GameManager] 플레이어 부활 완료!");
        }

        #region Pause Objects

        // 플레이어, 카메라, 몬스터 등 일부 오브젝트들을 정지시켜야할 때 사용
        public void PauseObjects()
        {
            // 플레이어 캐릭터와 카메라 Pause
            Player.FollowCamera.SetCameraRotatable(false);
            Player.SetMovable(false);
            Player.SetPlayerState(EPlayerState.Cutscene, true);

            // 현재 존재하는 모든 몬스터 Pause
            MonsterManager.Instance.PauseMonsters();
            
            // 게임 상태를 Paused로 변경
            CurrentState = GameState.Paused;
        }

        public void UnpauseObjects()
        {
            Player.FollowCamera.SetCameraRotatable(true);
            Player.SetMovable(true);
            Player.SetPlayerState(EPlayerState.Cutscene, false);

            MonsterManager.Instance.UnpauseMonsters();
            
            CurrentState = GameState.Playing;
        }

        #endregion

        public async void EnterTitle()
        {
            try
            {
                Debug.Log("[GameManager] 타이틀 씬 로드 중...");
                
                await GUIManager.LoadGUI();
                await SoundManager.Instance.Init();
                
                CurrentState = GameState.Title;
                
                Debug.Log("[GameManager] 타이틀 씬 로드 완료!");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameManager] 타이틀 씬 로드 중 예외 발생: {e}");
            }
        }
        
        public async void EnterPrologue()
        {
            try
            {
                Debug.Log("[GameManager] 게임 실행 준비 중...");
                // 프롤로그 실행
                CurrentState = GameState.Prologue;
            
                // 프롤로그 재생하는 동안 플레이어 씬과 게임 씬 로드
                await DungeonManager.Instance.Init();
                await PoolManager.Instance.Init();
                await LoadPlayerScene();
                
                DungeonManager.Instance.SetPlayerStartPosition();
                
                Debug.Log("[GameManager] 게임 실행 준비 완료!");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameManager] 게임 실행 준비 중 예외 발생: {e}");
            }
        }
        
        public void StartGame()
        {
            // 게임 시작
            CurrentState = GameState.Playing;
            
            // 플레이어 오브젝트 참조 설정
            Player = FindObjectOfType<PlayerController>();
        }

        public async void ExitGame()
        {
            try
            {
                Debug.Log("[GameManager] 게임 종료 중...");
                
                // 게임 리소스 씬 언로드
                await DungeonManager.Instance.UnloadAllStage();
                
                // 필수 씬 언로드
                await UnloadPlayerScene();
                await GUIManager.Instance.UnloadGUI();
                // await SceneController.Instance.UnloadScene("Scene_Persistent");
                
                // 풀 매니저 리셋
                // PoolManager.Instance.ClearPools();   // ?? 왜 풀 리셋하면 오류가 나지?

                Debug.Log("[GameManager] 게임 종료 완료!");
                
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;    // 에디터에서는 플레이 중단
#else
                Application.Quit();                                 // 빌드에서는 프로그램 종료
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameManager] 게임 종료 중 예외 발생: {e}");
            }
        }
        
        public async void EnterEpilogue()
        {
            try
            {
                Debug.Log("[GameManager] 에필로그 씬 로드 중...");
                
                CurrentState = GameState.Epilogue;
                
                // 게임 리소스 씬 언로드
                await DungeonManager.Instance.UnloadAllStage();
                await UnloadPlayerScene();
                
                Debug.Log("[GameManager] 에필로그 씬 로드 완료!\n> 리소스 메모리 정리 완료!");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameManager] 에필로그 씬 로드 중 예외 발생: {e}");
            }
        }

        public void EnterCredit()
        {
            try
            {
                CurrentState = GameState.Credit;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameManager] 크레딧 씬 진입 중 예외 발생: {e}");
            }
        }

        private static Task LoadPlayerScene() => SceneController.Instance.LoadSceneAdditive("Scene_Player");
        
        private async Task UnloadPlayerScene()
        {
            try
            {
                // FollowCamera, MinimapObject의 FollowAudioListener 언로드 (별도의 MonoBehaviour이므로 Update 등에서 참조가 남아 있음)
                SoundManager.Instance.AudioListener.GetComponent<FollowAudioListener>()?.Unload();
                MinimapObject.GetComponent<FollowAudioListener>()?.Unload();
                
                // 플레이어 씬 언로드
                await SceneController.Instance.UnloadScene("Scene_Player");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameManager] 플레이어 씬 언로드 중 예외 발생: {e}");
            }
        }
    }
}
