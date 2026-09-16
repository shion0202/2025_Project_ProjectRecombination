using Managers;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [임시] F10 키로 플레이어 사격 관련 상태를 콘솔(빌드에서는 Player.log)에 출력한다.
/// 재현 조건을 모르는 사격 불가 버그를 발생 시점에 기록하기 위한 개발용 컴포넌트다.
/// 씬 배치 없이 게임 시작 시 자동 생성되므로, 원인 확정 후 이 파일만 지우면 된다.
/// </summary>
public class DebugPlayerStateDump : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject go = new GameObject(nameof(DebugPlayerStateDump));
        DontDestroyOnLoad(go);
        go.AddComponent<DebugPlayerStateDump>();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current.f10Key.wasPressedThisFrame) return;

        PlayerController player = GameManager.IsAliveInstance() ? GameManager.Instance.Player : null;
        if (player == null)
        {
            Debug.Log("[ShootDebug] 플레이어가 없어 상태를 출력하지 않음");
            return;
        }

        string report = player.BuildShootDebugReport();

        GameUIController ui = GUIManager.IsAliveInstance() ? GUIManager.Instance.GameUIController : null;
        if (ui != null)
        {
            report += $"UI: Radial={ui.RadialUI.activeSelf}, Pause={ui.PauseUI.activeSelf}, Help={ui.HelpUI.activeSelf}, WorldMap={ui.WorldMap.activeSelf}\n";
        }
        report += $"GameState: {GameManager.Instance.CurrentState}";

        Debug.Log(report);
    }
}
