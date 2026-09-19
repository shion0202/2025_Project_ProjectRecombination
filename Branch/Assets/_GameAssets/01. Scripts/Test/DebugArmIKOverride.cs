#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Managers;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [임시] F9 키로 팔 조준 IK(ArmLAim, ArmRAim) 가중치를 강제로 바꾼다.
/// 사격 버튼을 누른 채로 IK 적용 여부에 따른 팔 모양을 비교하기 위한 개발용 컴포넌트다.
/// 누를 때마다 기본 동작 → 강제 0 → 강제 1 → 기본 동작 순으로 바뀐다.
/// 씬 배치 없이 게임 시작 시 자동 생성되며, 에디터와 Development Build에서만 컴파일된다.
/// </summary>
public class DebugArmIKOverride : MonoBehaviour
{
    private enum EMode
    {
        Default,
        ForceZero,
        ForceOne,
    }

    private EMode _mode = EMode.Default;
    private RigAimController _rigAim;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject go = new GameObject(nameof(DebugArmIKOverride));
        DontDestroyOnLoad(go);
        go.AddComponent<DebugArmIKOverride>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
        {
            _mode = (EMode)(((int)_mode + 1) % 3);
            Debug.Log($"[DebugArmIKOverride] 팔 IK 가중치 모드: {_mode}");
        }

        if (_mode == EMode.Default) return;

        if (_rigAim == null)
        {
            if (!GameManager.IsAliveInstance() || GameManager.Instance.Player == null) return;
            _rigAim = GameManager.Instance.Player.GetComponent<RigAimController>();
            if (_rigAim == null) return;
        }

        // 가중치를 올리고 내리는 코루틴이 끝난 뒤에는 이 값이 그대로 유지된다.
        float weight = _mode == EMode.ForceOne ? 1.0f : 0.0f;
        _rigAim.SetWeight("ArmLAim", weight);
        _rigAim.SetWeight("ArmRAim", weight);
    }

    private void OnGUI()
    {
        if (_mode == EMode.Default) return;

        GUI.Label(new Rect(20.0f, 20.0f, 400.0f, 30.0f), $"[F9] 팔 IK 가중치 강제: {(_mode == EMode.ForceOne ? "1" : "0")}");
    }
}
#endif
