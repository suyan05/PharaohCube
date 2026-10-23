using System;
using UnityEngine;

// 고고학 수첩 + 업적을 게임 시작 때 자동으로 만들어 둠 (씬에 아무것도 안 놓아도 됨)
// "Notebook" 오브젝트 하나 (씬이 바뀌어도 유지): AchievementSystem, AchievementToastUI, NotebookNotes, NotebookUI
public static class NotebookBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        try
        {
            if (UnityEngine.Object.FindFirstObjectByType<AchievementSystem>() != null) return;

            var go = new GameObject("Notebook");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<AchievementSystem>();
            go.AddComponent<NotebookNotes>();
            go.AddComponent<AchievementToastUI>();
            go.AddComponent<NotebookUI>();
        }
        catch (Exception e)
        {
            Debug.LogError($"[NotebookBootstrap] 수첩 만들기 오류: {e}");
        }
    }
}
