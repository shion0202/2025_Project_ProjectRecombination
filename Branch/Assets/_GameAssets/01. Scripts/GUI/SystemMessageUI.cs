// using Google.GData.Documents;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

namespace _Project.Scripts.GUI
{
    public class SystemMessage
    {
        public string Speaker;
        public string Text;
        // public int Priority; // 0=높음, 10=낮음 등
        // public float AutoCloseSec;

        public override string ToString()
        {
            return $"{Speaker}: {Text}";
        }
    }

    public static class SystemMessageBus
    {
        public static event Action<SystemMessage> OnDialogEvent;

        public static void Publish(SystemMessage e) => OnDialogEvent?.Invoke(e);
    }
    
    public class SystemMessageUI : MonoBehaviour
    {
        private readonly List<SystemMessage> _queue = new();
        
        [SerializeField] private TextMeshProUGUI systemMessageText; // 시스템 메시지 표시용 UI 텍스트

        void OnEnable() => SystemMessageBus.OnDialogEvent += HandleDialogEvent;
        void OnDisable() => SystemMessageBus.OnDialogEvent -= HandleDialogEvent;
        
        private void HandleDialogEvent(SystemMessage e)
        {
            _queue.Add(e);
            
            // 우선순위가 필요 없어서 정렬은 따로 하지 않는다.
            // _queue.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            // 시스템 메시지 로그 출력
            TryShowSystemMessage();
        }

        /// <summary>
        /// 시스템 메시지를 채팅 형식으로 표현
        /// 큐에서 데이터를 가져와서 UI에 표시
        /// 가장 최근에 발생한 이벤트 4개만 표시하고, 그 이전 이벤트는 삭제
        /// </summary>
        private void TryShowSystemMessage()
        {
            if (_queue.Count == 0) return;
            // var dialogEvent = _queue.First();
            
            while (_queue.Count > 4)
                _queue.RemoveAt(0); // 4개 이상이면 가장 오래된 이벤트 제거
            
            systemMessageText.text = string.Empty;  // 기존 텍스트 초기화

            foreach (var e in _queue)
            {
                string[] dialogEvents = e.ToString().Split('\n');
            
                // 대화 이벤트들을 채팅 형식으로 합침
                systemMessageText.text = string.Join(systemMessageText.text, dialogEvents);
            }
        }
    }
}