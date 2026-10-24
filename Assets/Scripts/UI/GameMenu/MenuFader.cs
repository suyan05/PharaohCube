using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// [추가] 메뉴에서 씬 이동: 화면이 어두워짐 -> 씬 불러오기 -> 다시 밝아짐 (씬이 바뀌어도 남는 임시 오브젝트)
public class MenuFader : MonoBehaviour
{
    private Image black;
    private static bool busy;

    public static bool Busy => busy;

    public static bool LoadScene(string sceneName, float fadeOut = 0.6f, float fadeIn = 0.6f)
    {
        if (busy) return false;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[MenuFader] '{sceneName}' 씬이 Build Profiles의 Scene List에 없음");
            return false;
        }
        busy = true;
        Canvas canvas = MenuUI.NewCanvas("MenuFader", 2000, true);
        var f = canvas.gameObject.AddComponent<MenuFader>();
        f.black = MenuUI.FullScreen("Black", canvas.transform, new Color(0f, 0f, 0f, 0f));
        f.StartCoroutine(f.Run(sceneName, fadeOut, fadeIn));
        return true;
    }

    private IEnumerator Run(string sceneName, float fadeOut, float fadeIn)
    {
        yield return Fade(0f, 1f, fadeOut);
        Time.timeScale = 1f; // 일시 정지에서 나가는 경우
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (op != null && !op.isDone) yield return null;
        yield return null;
        yield return Fade(1f, 0f, fadeIn);
        busy = false;
        Destroy(gameObject);
    }

    private IEnumerator Fade(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            black.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        black.color = new Color(0f, 0f, 0f, to);
    }

    private void OnDestroy()
    {
        if (busy && black == null) busy = false;
    }
}
