using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private readonly HashSet<string> activeFlags = new HashSet<string>();

    // 플래그가 켜질 때마다 (플래그 이름) 전달
    public event Action<string> OnFlagChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetFlag(string flagId)
    {
        if (activeFlags.Contains(flagId))
        {
            Debug.Log($"[GameManager] 이미 켜진 플래그: {flagId}");
            return;
        }

        activeFlags.Add(flagId);
        Debug.Log($"[GameManager] 플래그 ON: {flagId}");
        OnFlagChanged?.Invoke(flagId);
    }

    /* HasFlag<string> 플래그는 켜져있나 아닌가 만 중요하고 
     순서는 필요 없으니, 중복 방지 조회가 빠른 HashSet로 지정.*/
    public bool HasFlag(string flagId)
    {
        return activeFlags.Contains(flagId);
    }

    [ContextMenu("테스트: 활성화된 플래그 전체 출력")]
    void TestPrintFlags()
    {
        Debug.Log($"[GameManager] 활성 플래그 수: {activeFlags.Count}");
        foreach (var flag in activeFlags)
        {
            Debug.Log($" - {flag}");
        }
    }
}