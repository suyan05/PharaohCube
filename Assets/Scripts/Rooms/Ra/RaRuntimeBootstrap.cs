using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// 라의 방 공용 시스템 자동 붙이기 (씬 저장을 깜빡해도 동작하게)
// - 씬이 열릴 때 벽 횃불(WallTorch)이 있는 씬이면 = 라의 방
// - 아래 컴포넌트가 씬에 하나도 없으면 "RaSystems (자동)" 오브젝트를 만들어 붙임
//   RaDarkness(어둠), RaHudDirector(글자 정리), RaTorchTimer(횃불 시간), RaTorchGaugeUI(게이지), RaDayToNight(낮 -> 밤 자동)
// - 메뉴(라의 방 고치기)로 이미 씬에 붙어 있으면 아무것도 안 함
public static class RaRuntimeBootstrap
{
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
            if (UnityEngine.Object.FindFirstObjectByType<WallTorch>(FindObjectsInactive.Include) == null) return;

            GameObject host = null;
            Ensure<RaDarkness>(ref host);
            Ensure<RaHudDirector>(ref host);
            Ensure<RaTorchTimer>(ref host);
            Ensure<RaTorchGaugeUI>(ref host);
            Ensure<RaDayToNight>(ref host);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RaRuntimeBootstrap] 자동 붙이기 오류: {e}");
        }
    }

    private static void Ensure<T>(ref GameObject host) where T : Component
    {
        if (UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include) != null) return;
        if (host == null) host = new GameObject("RaSystems (자동)");
        host.AddComponent<T>();
        Debug.Log($"[RaRuntimeBootstrap] 씬에 {typeof(T).Name}가 없어서 자동으로 붙임 (메뉴로 붙이고 저장하면 이 로그 사라짐)");
    }
}
