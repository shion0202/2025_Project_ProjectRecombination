using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugManager : MonoBehaviour
{
    private static DebugManager s_instance;

    public static DebugManager Instance
    {
        get
        {
            Init();
            return s_instance;
        }
    }
    public static DebugManager UnsafeInstance
    {
        get => s_instance;
    }

    [Header("Cheat")]
    [SerializeField] private bool getAllParts = false;

    public bool GetAllParts
    {
        get => getAllParts;
    }

    private void Awake()
    {
        Init();
    }

    private static void Init()
    {
        if (s_instance != null) return;

        GameObject go = GameObject.Find("DebugManager");
        if (go == null)
        {
            go = new GameObject("DebugManager");
            go.AddComponent<DebugManager>();
        }

        DontDestroyOnLoad(go);
        s_instance = go.GetComponent<DebugManager>();
    }
}
