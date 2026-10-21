using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 화면 페이드 (검은 막) - 프롤로그 시작/붕괴/눈 뜨기에서 같이 씀
// - yield return ScreenFader.FadeIn(1f);   검정 -> 화면 (코루틴 안에서 기다릴 수 있음)
// - yield return ScreenFader.FadeOut(1f);  화면 -> 검정
// - ScreenFader.SetBlack();                바로 검게 (씬 시작 Awake/Start에서)
// - 씬에 안 놓아도 처음 부를 때 자동으로 생김. 씬이 바뀌면 같이 사라짐
public class ScreenFader : MonoBehaviour
{
    private static ScreenFader instance;

    [SerializeField] private int sortingOrder = 200;

    private GameObject canvasGo;
    private Image black;

    private static ScreenFader Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<ScreenFader>();
            if (instance == null) instance = new GameObject("ScreenFader").AddComponent<ScreenFader>();
            return instance;
        }
    }

    // ================= 밖에서 부르는 함수 =================
    public static IEnumerator FadeIn(float seconds) => Instance.Fade(1f, 0f, seconds);

    public static IEnumerator FadeOut(float seconds) => Instance.Fade(0f, 1f, seconds);

    public static void SetBlack()
    {
        try
        {
            Instance.EnsureBuilt();
            Instance.SetAlpha(1f);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ScreenFader] 검은 화면 오류: {e}");
        }
    }

    // ================= 페이드 =================
    private IEnumerator Fade(float from, float to, float seconds)
    {
        if (!TryBuild()) yield break;

        float t = 0f;
        while (seconds > 0f && t < seconds)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / seconds));
            yield return null;
        }
        SetAlpha(to);
    }

    private bool TryBuild()
    {
        try
        {
            EnsureBuilt();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ScreenFader] 페이드 준비 오류: {e}");
            return false;
        }
    }

    private void SetAlpha(float a)
    {
        black.color = new Color(0f, 0f, 0f, Mathf.Clamp01(a));
        canvasGo.SetActive(a > 0.001f);
    }

    private void EnsureBuilt()
    {
        if (canvasGo != null) return;

        RectTransform root = UIBuild.CreateCanvas(transform, "FaderCanvas", sortingOrder, out canvasGo);
        black = UIBuild.Stretch(root, "Black").gameObject.AddComponent<Image>();
        black.raycastTarget = false;
        SetAlpha(0f);
    }
}
