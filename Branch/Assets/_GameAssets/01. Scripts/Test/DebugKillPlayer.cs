#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Managers;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [임시] F8 키로 플레이어를 즉시 사망시킨다. 사망~부활 연출을 반복 확인하기 위한 개발용 컴포넌트다.
/// 씬 배치 없이 게임 시작 시 자동 생성된다.
/// 행사 빌드에서 실수로 눌리지 않도록 에디터와 Development Build에서만 컴파일된다.
/// </summary>
public class DebugKillPlayer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject go = new GameObject(nameof(DebugKillPlayer));
        DontDestroyOnLoad(go);
        go.AddComponent<DebugKillPlayer>();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current.f8Key.wasPressedThisFrame) return;

        if (!GameManager.IsAliveInstance() || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            Debug.Log("[DebugKillPlayer] 플레이 중이 아니라 무시함");
            return;
        }

        PlayerController player = GameManager.Instance.Player;
        if (player == null || (player.CurrentPlayerState & EPlayerState.Dead) != 0)
        {
            Debug.Log("[DebugKillPlayer] 플레이어가 없거나 이미 사망 상태라 무시함");
            return;
        }

        // TakeDamage는 무적/스폰 중에 무시되므로, 실제 사망 경로(체력 0 + Die)를 직접 태운다.
        // GameManager.PlayingProcess가 체력 0을 감지해 사망 연출과 부활을 진행한다.
        Debug.Log("[DebugKillPlayer] F8 입력 감지, 플레이어 강제 사망");
        player.Stats.CurrentHealth = 0.0f;
        player.Die();
    }
}
#endif
