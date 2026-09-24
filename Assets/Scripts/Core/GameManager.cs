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
            // 자식이어도 동작하도록, 최상위 부모(Managers)를 통째로 유지
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            // 다른 씬에 또 있는 중복 Managers는 제거
            Destroy(transform.root.gameObject);
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