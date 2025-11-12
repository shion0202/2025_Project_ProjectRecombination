using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;

public class EventManager : MonoBehaviour
{
    // 이벤트 타입에 따른 리스너(액션) 목록을 저장하는 딕셔너리
    // EEventType: 이벤트 타입
    // Component: 이벤트를 발생시킨 컴포넌트(Sender)
    // object: 이벤트와 함께 전달되는 추가 데이터(Param)
    private Dictionary<EEventType, Action<EEventType, Component, object>> listeners
        = new Dictionary<EEventType, Action<EEventType, Component, object>>();

    private static EventManager _instance;
    private static object _lock = new object();
    private static bool applicationQuitting = false;

    public static EventManager Instance
    {
        get
        {
            if (applicationQuitting)
                return null;

            Init();
            return _instance;
        }
    }

    private void Start()
    {
        Init();
    }

    private void OnDestroy()
    {
        applicationQuitting = true;
    }

    static void Init()
    {
        lock (_lock)
        {
            if (_instance == null)
            {
                GameObject manager = GameObject.Find("@Manager_Event");

                if (manager == null)
                {
                    manager = new GameObject("@Manager_Event");
                    manager.AddComponent<EventManager>();
                }

                DontDestroyOnLoad(manager);
                _instance = manager.GetComponent<EventManager>();
            }
        }
    }

    // 특정 이벤트 타입이 실행될 때 호출할 리스너(액션)을 등록하는 함수
    public void AddListener(EEventType eventType, Action<EEventType, Component, object> action)
    {
        if (!listeners.ContainsKey(eventType))
        {
            listeners.Add(eventType, null);
        }

        // 이벤트 중복 등록 방지
        listeners[eventType] -= action;
        listeners[eventType] += action;

        Debug.Log($"[EventManager] Added Listener for {eventType}. Total Listeners: {listeners[eventType]?.GetInvocationList().Length ?? 0}");
    }

    // 특정 이벤트 타입에 등록된 리스너(액션)를 제거하는 함수
    public void RemoveListener(EEventType eventType, Action<EEventType, Component, object> action)
    {
        if (!listeners.ContainsKey(eventType)) return;

        listeners[eventType] -= action;

        Debug.Log($"[EventManager] Removed Listener for {eventType}. Total Listeners: {listeners[eventType]?.GetInvocationList().Length ?? 0}");
    }

    // 인자로 주어진 이벤트 타입에 해당하는 모든 리스너를 호출하는 함수
    // 추가로 이벤트를 발생시킨 Sender와 추가적인 데이터(Param)을 함께 전달
    public void PostNotification(EEventType eventType, Component sender, object param = null)
    {
        if (!listeners.ContainsKey(eventType)) return;

        listeners[eventType].Invoke(eventType, sender, param);
    }

    // 특정 타입의 이벤트를 초기화하는 함수
    public void RemoveEvent(EEventType eventType)
    {
        listeners.Remove(eventType);
    }

    // 모든 이벤트를 삭제하는 Clear 함수
    public void Clear()
    {
        foreach (EEventType eventType in listeners.Keys)
        {
            RemoveEvent(eventType);
        }
    }

    public override string ToString()
    {
        if (listeners == null || listeners.Count == 0)
        {
            return "[Event Manager] No listeners registered.";
        }

        string log = "[Event Manager] Listeners:\n";
        foreach (var kvp in listeners)
        {
            int count = kvp.Value?.GetInvocationList().Length ?? 0;
            log += $"- Event: {kvp.Key}, Listeners: {count}\n";
        }
        return log;
    }
}