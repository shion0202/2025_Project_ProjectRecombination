using Managers;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    // 튜토리얼 강조 연출을 켜는 Output. SetNoticeMessage와 나란히 붙여 쓰는 것을 전제로 한다.
    // 끄는 것은 ClearHighlight 노드가 담당한다.
    public class HighlightTarget : ProcessBase
    {
        public enum EHighlightMode
        {
            // HUD 요소. TutorialHighlightController의 uiTargets 목록에 등록한 이름으로 찾는다.
            // HUD는 Persistent 씬의 프리팹이라 씬 노드에서 직접 참조할 수 없어 이름을 쓴다.
            UI,
            // 같은 씬의 월드 오브젝트. 화면 밖에 있으면 화살표가 방향을 가리킨다.
            WorldObject,
        }

        [SerializeField] private EHighlightMode mode = EHighlightMode.WorldObject;

        [Tooltip("UI 모드에서 사용할 대상 이름")]
        [SerializeField] private string uiTargetKey;

        [Tooltip("UI 모드에서 함께 강조할 대상 이름. 여러 곳을 동시에 가리킬 때만 채운다.")]
        [SerializeField] private List<string> extraUITargetKeys = new();

        [Tooltip("WorldObject 모드에서 강조할 오브젝트")]
        [SerializeField] private Transform worldTarget;

        [Tooltip("WorldObject 모드에서 함께 강조할 오브젝트. 여러 개를 동시에 가리킬 때만 채운다.")]
        [SerializeField] private List<Transform> extraWorldTargets = new();

        [Tooltip("대상을 뺀 나머지 화면을 어둡게 덮을지 여부")]
        [SerializeField] private bool dimScreen = true;

        [Tooltip("자동으로 해제할 시간(초). 0이면 ClearHighlight 노드를 실행할 때까지 유지한다.")]
        [SerializeField] private float autoHideTime = 0.0f;

        // 실행할 때마다 새 리스트를 만들지 않도록 재사용한다.
        private readonly List<string> _uiKeys = new();
        private readonly List<Transform> _worldTargets = new();

        public override void Execute()
        {
            if (IsOn) return;

            TutorialHighlightController highlight = GUIManager.Instance.GameUIController.TutorialHighlight;
            if (mode == EHighlightMode.UI)
            {
                _uiKeys.Clear();
                if (!string.IsNullOrEmpty(uiTargetKey)) _uiKeys.Add(uiTargetKey);
                _uiKeys.AddRange(extraUITargetKeys);

                highlight.ShowUI(_uiKeys, dimScreen, autoHideTime);
            }
            else
            {
                _worldTargets.Clear();
                if (worldTarget != null) _worldTargets.Add(worldTarget);
                _worldTargets.AddRange(extraWorldTargets);

                highlight.ShowWorld(_worldTargets, dimScreen, autoHideTime);
            }

            IsOn = true;
        }

        public override string ToString()
        {
            string targetName = mode == EHighlightMode.UI
                ? uiTargetKey
                : (worldTarget != null ? worldTarget.name : "None");

            return $"[{gameObject.name} ({GetType().Name})] IsOn: {IsOn}, Mode: {mode}, Target: {targetName}";
        }
    }
}
