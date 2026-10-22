using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 장소, 시간 자막 (예: "기자 고원 남쪽, 발굴 캠프" / "발굴 47일째")
// - yield return TitleCardUI.Show(제목, 작은 글, 보여줄 초);
// - 화면 가운데에 글자가 떠올랐다가 사라짐. 검은 화면(ScreenFader, 정렬 200)보다 위(210)에 그림
public class TitleCardUI : MonoBehaviour
{
    private const float FadeSeconds = 0.6f;
    private static TitleCardUI instance;

    private GameObject canvasGo;
    private Text titleText, subText;

    private static TitleCardUI Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<TitleCardUI>();
            if (instance == null) instance = new GameObject("TitleCardUI").AddComponent<TitleCardUI>();
            return instance;
        }
    }

    public static IEnumerator Show(string title, string subtitle, float holdSeconds)
    {
        if (Cine.Skipping) yield break;
        if (!Instance.TryPrepare(title, subtitle)) yield break;

        yield return Instance.FadeTo(1f);
        yield return Cine.Wait(holdSeconds);
        yield return Instance.FadeTo(0f);
        Instance.canvasGo.SetActive(false);
    }

    private bool TryPrepare(string title, string subtitle)
    {
        try
        {
            EnsureBuilt();
            titleText.text = title ?? "";
            subText.text = subtitle ?? "";
            SetAlpha(0f);
            canvasGo.SetActive(true);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[TitleCardUI] 자막 준비 오류: {e}");
            return false;
        }
    }

    private IEnumerator FadeTo(float target)
    {
        float start = titleText.color.a;
        float t = 0f;
        while (t < FadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(start, target, t / FadeSeconds));
            yield return null;
        }
        SetAlpha(target);
    }

    private void SetAlpha(float a)
    {
        titleText.color = new Color(0.96f, 0.94f, 0.9f, a);
        subText.color = new Color(0.91f, 0.7f, 0.23f, a);
    }

    private void EnsureBuilt()
    {
        if (canvasGo != null) return;

        Font font = PuzzleUITheme.GetBodyFont();
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform root = UIBuild.CreateCanvas(transform, "TitleCardCanvas", 210, out canvasGo);
        titleText = UIBuild.Text(root, "Title", font, 52, new Vector2(0f, 30f), new Vector2(1400f, 80f), Color.white);
        subText = UIBuild.Text(root, "Sub", font, 30, new Vector2(0f, -40f), new Vector2(1400f, 50f), Color.white);
        canvasGo.SetActive(false);
    }
}
