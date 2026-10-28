using UnityEngine;
using UnityEngine.SceneManagement;

// [추가] 맵 바깥(카메라 배경)을 검은색으로 - 바스테트·아누비스 씬 (기본 파란 배경 #314D79 대신)
// 씬에 아무것도 안 놓아도 씬이 열릴 때마다 자동으로 Main Camera 배경을 바꿈 (씬 파일은 안 건드려서 팀 충돌 없음)
// 다른 씬도 검게 하려면 아래 Targets에 씬 이름 일부를 추가
public static class SceneBackgroundColor
{
    // 씬 이름에 이 글자가 들어 있으면 배경을 검게
    private static readonly string[] Targets = { "Bastet", "Anubis" };
    private static readonly Color Black = new Color(0f, 0f, 0f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= HandleScene;
        SceneManager.sceneLoaded += HandleScene;
        HandleScene(SceneManager.GetActiveScene(), LoadSceneMode.Single); // Play로 바로 연 씬
    }

    private static void HandleScene(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || !IsTarget(scene.name)) return;
        Camera cam = Camera.main;
        if (cam == null) { Debug.LogWarning($"[SceneBackgroundColor] {scene.name}: Main Camera 없음 (MainCamera 태그 확인)"); return; }
        cam.clearFlags = CameraClearFlags.SolidColor; // URP: Background Type = Solid Color
        cam.backgroundColor = Black;
        Debug.Log($"[SceneBackgroundColor] {scene.name}: 카메라 배경 검은색");
    }

    private static bool IsTarget(string sceneName)
    {
        foreach (string t in Targets) if (sceneName.Contains(t)) return true;
        return false;
    }
}
