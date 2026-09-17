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

        // 현재 플레이 모드. 행사 출품용 체험 플로우(Demo)와 본편(Normal)을 구분한다.
        // DungeonManager가 어느 스테이지 세트를 쓸지, AmonEndPhase가 어디로 분기할지를 이 값으로 판단한다.
        public EPlayMode PlayMode { get; private set; } = EPlayMode.Normal;

        public PlayerController Player { get; set; }
        // public GameObject MainCamera { get; set; }
        public GameObject FollowCamera { get; set; }
        public GameObject MinimapObject { get; set; }
        private Coroutine _rebirthRoutine;

        [Header("Death / Revive")]
        [SerializeField] private float rebirthDelay = 5.0f;             // 사망부터 부활까지 전체 시간 (기존 값)
        [SerializeField] private float deathUIDelay = 0.4f;             // 사망 후 System Failure 텍스트가 깜빡이며 켜지기 시작하는 시점 (노이즈와 패널이 먼저 들어온다)
        [SerializeField] private float rebootStartTime = 1.0f;          // REBOOTING 표시 및 화면 회복 시작 시점
        [SerializeField] private float rebootRecoverIntensity = 0.0f;   // 회복이 끝났을 때의 노이즈 강도 (0이면 완전히 회복)
        [SerializeField] private float recoveredHoldTime = 0.5f;        // 회복을 마치고 System Restored를 보여준 뒤 부활하기까지의 시간

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

            // 부활 코루틴 시작 (사망 표시도 코루틴이 담당)
            _rebirthRoutine = StartCoroutine(RebirthGame());
        }

        // 플레이어 사망 연출 및 부활 코루틴
        // 사망: 화면 노이즈 최대 + HUD 숨김 + 사망 표시 패널, deathUIDelay 뒤 System Failure가 깜빡이며 켜짐
        //      (노이즈와 텍스트가 같은 프레임에 들어오면 화면 전환이 급작스럽다)
        // 리부팅: REBOOTING 표시, 화면을 rebootRecoverIntensity까지 서서히 회복
        // 복구 완료: 노이즈가 걷히는 순간 System Restored로 바꾸고 알림음, recoveredHoldTime 동안 유지
        //          (곧 부활한다는 신호를 준다. 알림음을 부활 순간에 내면 이펙트 소리에 묻힌다)
        // 부활: 사망 표시를 치우고 HUD 복구, 부활 이펙트
        // 대기와 회복 모두 게임 시간 기준이라 일시정지 중에는 함께 멈춘다.
        private IEnumerator RebirthGame()
        {
            GameUIController ui = GUIManager.Instance.GameUIController;

            // 1. 사망
            ScreenGlitch.SetIntensity(1.0f);
            ui.SetHudLocked(true);
            ui.ShowDeathUI();

            yield return new WaitForSeconds(deathUIDelay);
            ui.ShowSystemFailureText();

            yield return new WaitForSeconds(Mathf.Max(0.0f, rebootStartTime - deathUIDelay));

            // 2. 리부팅
            ui.ShowRebootingText();

            float recoverDuration = Mathf.Max(0.0f, rebirthDelay - rebootStartTime - recoveredHoldTime);
            float elapsed = 0.0f;
            while (elapsed < recoverDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / recoverDuration);
                float easeOut = 1.0f - (1.0f - t) * (1.0f - t);   // 초반에 빨리 걷히고 후반은 천천히
                ScreenGlitch.SetIntensity(Mathf.Lerp(1.0f, rebootRecoverIntensity, easeOut));
                yield return null;
            }
            ScreenGlitch.SetIntensity(rebootRecoverIntensity);

            // 3. 복구 완료
            ui.ShowRestoredText();
            Player.PlayRestoredSound();

            yield return new WaitForSeconds(recoveredHoldTime);

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

            // 4. 부활
            ScreenGlitch.Clear();
            ui.HideDeathUI();
            ui.SetHudLocked(false);
            Player.PlayReviveEffect();

            _rebirthRoutine = null;
            
            CurrentState = GameState.Playing;
            
            Debug.Log("[GameManager] 플레이어 부활 완료!");
        }

        #region Pause Objects

        // 플레이어, 카메라, 몬스터 등 일부 오브젝트들을 정지시켜야할 때 사용
        public void PauseObjects()
        {
            // 누르고 있던 공격·이동 키의 동작이 컷씬 중에도 이어지지 않도록 기본 상태로 되돌린다.
            // 일부 파츠의 강제 종료가 이동·카메라 회전을 다시 켜므로 잠그기 전에 호출한다.
            Player.ResetToIdle();

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

                // 타이틀은 마우스로 조작하는 메뉴이므로 커서가 항상 자유로워야 한다.
                // 인게임에서 PlayerController가 Locked로 잠근 상태 그대로 복귀하면
                // 메뉴 버튼을 클릭할 수 없으므로, 진입 시점에 무조건 해제한다.
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

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
        
        public async void EnterPrologue(EPlayMode mode)
        {
            try
            {
                Debug.Log($"[GameManager] 게임 실행 준비 중... (mode: {mode})");
                CurrentState = GameState.Loading;

                // 이전 판이 사망 연출 도중 끝났을 수 있으므로 화면 노이즈를 확실히 끈다.
                ScreenGlitch.Clear();

                PlayMode = mode;

                if (mode == EPlayMode.Demo)
                {
                    // 하드 모드를 강제로 끈다. 하드 모드 사망 시 RebirthGame()이 ResetCurrentStage()로
                    // 현재 스테이지를 리로드하는데, 튜토리얼은 스테이지가 1개뿐이라
                    // 그것이 곧 보스전 전체 초기화를 의미한다.
                    IsHardMode = false;

                    // 스탯 배수는 여기서 주입한다. 스테이지 씬이 로드되기 "전"이므로
                    // 씬 내 컴포넌트의 Awake 순서와 무관하게 항상 몬스터 초기화보다 앞선다.
                    DemoModeContext.LoadAndApply();
                }
                else
                {
                    DemoModeContext.Reset();
                }

                // 프롤로그 재생하는 동안 플레이어 씬과 게임 씬 로드
                //
                // 순서 주의: 스테이지 씬보다 플레이어 씬을 "먼저" 로드해야 한다.
                // 씬에 배치된 몬스터는 Blackboard.Init()에서 Target = MonsterManager.Instance.Player를
                // 한 번 읽고 굳는다. Player가 아직 없으면 Target이 null로 남아
                // FSM의 Think()/Act()가 첫 줄에서 return해 그 몬스터가 통째로 정지한다.
                // (SceneTestLauncher.LoadInGame()이 이미 이 순서를 쓰고 있다.)
                await PoolManager.Instance.Init();
                await LoadPlayerScene();
                await DungeonManager.Instance.Init();

                DungeonManager.Instance.SetPlayerStartPosition();
                
                Debug.Log("[GameManager] 게임 실행 준비 완료!");
                
                // 프롤로그 실행
                CurrentState = GameState.Prologue;
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
            Player = Instance.Player;
            if (Player)
            {
                Player.PlayIntroSequence(4.0f, () =>
                {
                    Debug.Log("[GameManager] 시퀀스 최종 종료 검증 완료. 이제 완벽한 플레이 상태입니다.");
                });
            }
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
        
        /// <summary>
        /// 게임 월드(스테이지 + 플레이어)를 정리하고 관련 매니저 상태를 되돌린다.
        /// UI 씬은 건드리지 않으므로, 크레딧처럼 UI가 계속 필요한 화면으로 넘어갈 때도 쓸 수 있다.
        ///
        /// 호출 순서에 의존성이 있다.
        ///  1) 풀 오브젝트 회수는 씬 언로드 "전에" (씬과 함께 파괴되면 풀 밖에서 사라진다)
        ///  2) 매니저 상태 리셋은 씬 언로드 "후에" (언로드가 참조를 사용한다)
        /// </summary>
        private async Task CleanupGameWorld()
        {
            // 1. 씬 언로드 "전에" 풀 오브젝트 회수
            MonsterManager.Instance.ResetSession();

            // 2. 진행 중인 부활 코루틴 중단.
            //    부활 대기(5초) 도중에 판이 끝나면 코루틴이 살아남아
            //    다음 판 시작 직후 Player.Spawn()과 상태 전이를 실행해버린다.
            if (_rebirthRoutine != null)
            {
                StopCoroutine(_rebirthRoutine);
                _rebirthRoutine = null;
            }

            // 부활 코루틴을 중간에 멈추면 노이즈 강도(전역 셰이더 값)가 그대로 남는다.
            ScreenGlitch.Clear();

            // 3. 씬 언로드
            await DungeonManager.Instance.UnloadAllStage();
            await UnloadPlayerScene();

            // 4. 매니저 상태 리셋
            DungeonManager.Instance.ResetSession();
            DungeonStateManager.Instance.ClearStates();
            DemoModeContext.Reset();

            // 5. 참조 해제
            Player = null;
            FollowCamera = null;
            MinimapObject = null;
        }

        /// <summary>
        /// 체험 플레이 클리어 → 크레딧으로 넘어간다.
        /// 본편의 EnterEpilogue() 자리에 대응하며, 데모는 에필로그를 건너뛰고 바로 크레딧으로 간다.
        /// (본편: 게임씬 → 에필로그 → 크레딧 → 타이틀 / 데모: 데모씬 → 크레딧 → 타이틀)
        ///
        /// PlayMode는 여기서 되돌리지 않는다. 크레딧이 끝났을 때 UI_Credits가
        /// 본편/데모 중 어느 경로로 복귀할지 판단해야 하기 때문이다.
        /// </summary>
        public async void EnterDemoCredit()
        {
            try
            {
                Debug.Log("[GameManager] 체험 플레이 클리어, 크레딧 진입 중...");

                // 언로드를 await 하는 동안에도 Update()는 계속 돌기 때문에, Playing 상태로 두면
                // PlayingProcess()가 이미 파괴된 Player를 참조해 예외가 나거나
                // 체력을 0으로 읽어 GameOver로 잘못 전이된다.
                CurrentState = GameState.Loading;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                await CleanupGameWorld();

                CurrentState = GameState.Credit;

                Debug.Log("[GameManager] 크레딧 진입 완료!");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameManager] 크레딧 진입 중 예외 발생: {e}");
            }
        }

        /// <summary>
        /// 체험 플레이 1판을 종료하고 타이틀로 복귀한다. 크레딧이 끝나면 UI_Credits가 호출한다.
        /// 행사에서는 게임을 끄지 않고 종일 반복 실행하므로, 이 경로가 세션 상태를 완전히 되돌려야 한다.
        ///
        /// EnterDemoCredit()이 이미 월드를 정리했더라도 CleanupGameWorld()를 다시 호출한다.
        /// 언로드는 이미 언로드된 씬에 대해 no-op이고, 리셋도 멱등이므로 안전하다.
        /// (크레딧을 거치지 않는 경로에서 호출되더라도 상태가 새지 않게 하기 위함이다.)
        /// </summary>
        public async void ReturnToTitleFromDemo()
        {
            try
            {
                Debug.Log("[GameManager] 체험 플레이 종료, 타이틀 복귀 시작...");

                CurrentState = GameState.Loading;

                await CleanupGameWorld();

                // UI 씬도 언로드한다.
                // Scene_UI의 UI 오브젝트들은 InitUI가 한 번만 생성하고 이후에는 SetActive로 토글될 뿐이라,
                // 초기화가 Awake/Start에 있는 스크립트(UI_Prologue, TitleLogoDT, UI_Credits 등)는
                // 2회차에 다시 돌지 않는다. 씬을 통째로 다시 만들어 UI 상태를 확실히 초기화한다.
                // (UnloadGUI가 DOTween.KillAll()로 잔여 트윈까지 정리한다.)
                await GUIManager.Instance.UnloadGUI();

                PlayMode = EPlayMode.Normal;

                Debug.Log("[GameManager] 타이틀 복귀 완료!");

                // 타이틀 진입 (EnterTitle이 Scene_UI를 다시 로드한다)
                EnterTitle();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameManager] 타이틀 복귀 중 예외 발생: {e}");
            }
        }

        public async void EnterEpilogue()
        {
            try
            {
                Debug.Log("[GameManager] 에필로그 씬 로드 중...");
                
                CurrentState = GameState.Epilogue;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

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

        private static async Task LoadPlayerScene()
        {
            await SceneController.Instance.LoadSceneAdditive("Scene_Player");

            // 플레이어 씬은 스테이지 씬보다 먼저 로드되므로 이 시점에는 발밑에 지형이 없다.
            // 그대로 두면 스테이지 로드가 끝날 때까지 계속 낙하해 낙하 속도가 누적되고,
            // 시작 위치로 옮겨도 그 속도 때문에 바닥을 뚫는다.
            // 시작 위치가 확정될 때(DungeonManager.SetPlayerStartPosition)까지 컨트롤러를 꺼둔다.
            PlayerController player = Instance.Player;
            if (player == null) return;

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
        }
        
        private async Task UnloadPlayerScene()
        {
            try
            {
                // FollowCamera, MinimapObject의 FollowAudioListener 언로드 (별도의 MonoBehaviour이므로 Update 등에서 참조가 남아 있음)
                // 반복 플레이 시 이미 정리된 상태로 재진입할 수 있어 null 가드를 둔다.
                if (SoundManager.Instance.AudioListener != null)
                    SoundManager.Instance.AudioListener.GetComponent<FollowAudioListener>()?.Unload();

                if (MinimapObject != null)
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
