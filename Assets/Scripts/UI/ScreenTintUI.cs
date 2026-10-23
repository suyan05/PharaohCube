using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 화면 전체 색 번쩍임 (꿈 전환, 유물 발견 등)
// - yield return ScreenTintUI.Flash(색, 올라가는 초, 머무는 초, 내려가는 초);
// - 색의 알파 = 가장 진할 때 진하기. 대사창(100)보다 위, 검은 화면(200)보다 아래(150)
public class ScreenTintUI : MonoBehaviour
{
    private static ScreenTintUI instance;

    private GameObject canvasGo;
    private Image tint;

    private static ScreenTintUI Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<ScreenTintUI>();
            if (instance == null) instance = new GameObject("ScreenTintUI").AddComponent<ScreenTintUI>();
            return instance;
        }
    }

    public static IEnumerator Flash(Color color, float inSeconds, float holdSeconds, float outSeconds)
    {
        ScreenTintUI ui = Instance;
        if (!ui.TryShow()) yield break;

        yield return ui.FadeAlpha(color, 0f, color.a, inSeconds);
        yield return Cine.Wait(holdSeconds);
        yield return ui.FadeAlpha(color, color.a, 0f, outSeconds);
        ui.canvasGo.SetActive(false);
    }

    private IEnumerator FadeAlpha(Color color, float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            tint.color = new Color(color.r, color.g, color.b, Mathf.Lerp(from, to, t / seconds));
            yield return null;
        }
        tint.color = new Color(color.r, color.g, color.b, to);
    }

    private bool TryShow()
    {
        try
        {
            if (canvasGo == null)
            {
                RectTransform root = UIBuild.CreateCanvas(transform, "TintCanvas", 150, out canvasGo);
                tint = UIBuild.Stretch(root, "Tint").gameObject.AddComponent<Image>();
                tint.raycastTarget = false;
            }
            tint.color = Color.clear;
            canvasGo.SetActive(true);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ScreenTintUI] 색 번쩍임 오류: {e}");
            return false;
        }
    }
}
