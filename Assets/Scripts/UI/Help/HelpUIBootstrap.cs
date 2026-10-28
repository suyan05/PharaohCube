using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// 도움 UI 자동 붙이기 (씬에 아무것도 안 놓아도 됨)
// - 오브젝트 위 E 말풍선(WorldPromptUI): 발굴 현장, 스핑크스의 꿈, 중앙 홀
// - 왼쪽 아래 조작키 안내(ControlsHintUI): 발굴 현장 (게임 맨 처음)
// 씬 이름에 아래 글자가 들어 있으면 적용
public static class HelpUIBootstrap
{
    private static readonly string[] PromptScenes = { "Prologue_Dig", "Prologue_Sphinx", "CentralRoom", "CentralHall" };
    private static readonly string[] ControlsScenes = { "Prologue_Dig" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        try
        {
            if (Matches(scene.name, PromptScenes)) Create<WorldPromptUI>("WorldPromptUI (자동)");
            if (Matches(scene.name, ControlsScenes)) Create<ControlsHintUI>("ControlsHintUI (자동)");
        }
        catch (Exception e)
        {
            Debug.LogError($"[HelpUIBootstrap] 도움 UI 붙이기 오류: {e}");
        }
    }

    private static bool Matches(string sceneName, string[] list)
    {
        foreach (string s in list)
        {
            if (sceneName.Contains(s)) return true;
        }
        return false;
    }

    private static void Create<T>(string name) where T : Component
    {
        if (UnityEngine.Object.FindFirstObjectByType<T>() != null) return;
        new GameObject(name).AddComponent<T>();
    }
}
