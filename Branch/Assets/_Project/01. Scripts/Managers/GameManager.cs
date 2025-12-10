using System;
using System.Collections;
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
            GameOver
        }

        public PlayerController Player { get; set; }
        public GameObject MainCamera { get; set; }
        public GameObject FollowCamera { get; set; }
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
            SoundManager.Instance.Init();
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
            Debug.Log("Player is Death");
            GUIManager.Instance.GameUIController.OnGameOverPanel();
            
            // 부활 코루틴 시작
            _rebirthRoutine = StartCoroutine(RebirthGame());
        }

        // 플레이어 부활 코루틴 (5초 대기 후 부활)
        private IEnumerator RebirthGame()
        {
            yield return new WaitForSeconds(5.0f);

            GUIManager.Instance.GameUIController.CloseGameOverPanel();
            Player.Stats.CurrentHealth = Player.Stats.MaxHealth;
            Player.Spawn();

            _rebirthRoutine = null;
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
                await SceneController.Instance.LoadSceneAdditive("Scene_UI");
                CurrentState = GameState.Title;
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
                // 프롤로그 실행
                CurrentState = GameState.Prologue;
            
                // 프롤로그 재생하는 동안 플레이어 씬과 게임 씬 로드
                // await SceneController.Instance.LoadSceneAdditive("Scene_Game");
                await DungeonManager.Instance.LoadAllStage();   // 테스트용 모든 스테이지 로드
                await SceneController.Instance.LoadSceneAdditive("Scene_Player");
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

        public void ExitGame()
        {
            // 게임 종료
            // 모든 씬 언로드
            SceneController.Instance.UnloadScene("Scene_Game");
        }
    }
}
