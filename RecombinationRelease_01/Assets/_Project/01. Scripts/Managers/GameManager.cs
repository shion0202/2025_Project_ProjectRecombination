using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Managers
{
    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private PlayerController player;
        private Coroutine _rebirthRoutine = null;

        private void Start()
        {
            var hp = player.Stats.CurrentHealth;

            if (hp <= 0f)
                Debug.LogError("Player HP Stats Init Failed");
        }

        private void Update()
        {
            if (_rebirthRoutine != null) return;

            var hp = player.Stats.CurrentHealth;
            if (hp <= 0f)
            {
                // 게임 오버 처리
                Debug.Log("Player is Death");
                GUIManager.Instance.OnGameOverPanel();
                //StartCoroutine(SceneClear());
                _rebirthRoutine = StartCoroutine(RebirthGame());
            }
        }

        private IEnumerator SceneClear()
        {
            yield return new WaitForSeconds(5);
            // GameScene 다시 로드
            SceneManager.LoadScene("GameScene");
        }

        private IEnumerator RebirthGame()
        {
            yield return new WaitForSeconds(5.0f);

            GUIManager.Instance.CloseGameOverPanel();
            player.Stats.CurrentBodyHealth = player.Stats.MaxBodyHealth;
            player.Stats.CurrentPartHealth = player.Stats.MaxPartHealth;
            player.Spawn();

            _rebirthRoutine = null;
        }

        // 플레이어, 카메라, 몬스터 등 일부 오브젝트들을 정지시켜야할 때 사용
        public void PauseObjects()
        {
            // 플레이어 캐릭터와 카메라 Pause
            player.FollowCamera.SetCameraRotatable(false);
            player.SetMovable(false);

            // 현재 존재하는 모든 몬스터 Pause
            MonsterManager.Instance.PauseMonsters();
        }

        public void UnpauseObjects()
        {
            player.FollowCamera.SetCameraRotatable(true);
            player.SetMovable(true);
            MonsterManager.Instance.UnpauseMonsters();
        }
    }
}
