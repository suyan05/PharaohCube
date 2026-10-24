using UnityEngine;
using UnityEngine.SceneManagement;

// [추가] 게임이 켜질 때 자동 실행 (씬에 아무것도 두지 않아도 됨)
// 1) 저장된 설정(음량·화면·언어) 적용  2) 소리 매니저 준비
// 3) 씬이 열릴 때마다: 이어하기 복원 + 자동 저장 + 게임 씬이면 Esc 일시 정지 메뉴 준비
//    예전 PauseMenuUI(Settings 씬으로 이동하던 방식)가 씬에 있으면 꺼서 Esc가 두 번 먹지 않게 함
public static class GameMenuBootstrap
{
    // 메뉴 씬 (일시 정지·자동 저장 안 함)
    private static readonly string[] MenuScenes = { "Title", "MainMenu", "Settings" };

    public static bool IsMenuScene(string sceneName)
    {
        foreach (string s in MenuScenes) if (s == sceneName) return true;
        return sceneName == MenuTheme.Current.titleScene;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        GameSettings.ApplyAll();
        SoundManager.Ensure();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.quitting -= OnQuit;
        Application.quitting += OnQuit;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single); // 처음 연 씬
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        SaveSystem.OnSceneLoaded(scene);
        if (IsMenuScene(scene.name)) return;

        PauseMenu.EnsureInScene();
        foreach (PauseMenuUI old in Object.FindObjectsByType<PauseMenuUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!old.enabled) continue;
            old.enabled = false;
            Debug.LogWarning($"[GameMenuBootstrap] 예전 일시 정지(PauseMenuUI '{old.name}')를 끔 -> 새 PauseMenu 사용", old);
        }
    }

    private static void OnQuit()
    {
        if (!IsMenuScene(SceneManager.GetActiveScene().name)) SaveSystem.SaveNow();
    }
}
