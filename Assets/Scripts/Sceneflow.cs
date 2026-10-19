using UnityEngine;
using UnityEngine.SceneManagement;

// 씬 순서 이동 공용 기능 (Build Profiles -> Scene List의 번호 순서대로)
// 0 Ra_Tilemap -> 1 Anubis_Tutorial -> 2 Anubis_Tilemap -> 3 Bastet_Tutorial -> 4 Bastet_Tilemap ...
// 다른 방(아누비스, 바스테트 등)도 끝날 때 SceneFlow.LoadNext() 한 줄만 부르면 다음 방으로 감
public static class SceneFlow
{
    // 지금 씬 다음에 체크된 씬이 있는지
    public static bool HasNext
    {
        get
        {
            int next = SceneManager.GetActiveScene().buildIndex + 1;
            return next > 0 && next < SceneManager.sceneCountInBuildSettings;
        }
    }

    // 다음 번호 씬으로 이동. 마지막 씬이면 false
    public static bool LoadNext()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        int next = current + 1;

        if (current < 0)
        {
            Debug.LogWarning("[SceneFlow] 지금 씬이 Build Profiles의 Scene List에 없음 -> 다음 씬을 알 수 없음");
            return false;
        }
        if (next >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log("[SceneFlow] 마지막 씬이라 더 넘어갈 곳이 없음");
            return false;
        }

        Debug.Log($"[SceneFlow] {current}번 -> {next}번 씬으로 이동");
        SceneManager.LoadScene(next);
        return true;
    }

    // 씬 이름으로 이동 (이름이 비었거나 Scene List에 없으면 다음 번호 씬으로)
    public static void LoadByNameOrNext(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.Log($"[SceneFlow] '{sceneName}' 씬으로 이동");
            SceneManager.LoadScene(sceneName);
            return;
        }

        if (!string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning($"[SceneFlow] '{sceneName}' 씬이 Scene List에 없음 -> 다음 번호 씬으로 이동");
        }
        LoadNext();
    }
}