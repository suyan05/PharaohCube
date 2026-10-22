using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 연출 중 화면 위아래 검은 바 (영화처럼)
// - LetterboxUI.Show(true / false): 0.4초 동안 스르륵 들어오고 나감
// - 대사창(정렬 100)보다 뒤(90)에 그려서 대사는 바 위에 보임
public class LetterboxUI : MonoBehaviour
{
    private const float BarHeight = 110f;
    private const float SlideSeconds = 0.4f;
    private static LetterboxUI instance;

    [SerializeField] private int sortingOrder = 90;

    private GameObject canvasGo;
    private RectTransform top, bottom;
    private float current;
    private Coroutine sliding;

    private static LetterboxUI Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<LetterboxUI>();
            if (instance == null) instance = new GameObject("LetterboxUI").AddComponent<LetterboxUI>();
            return instance;
        }
    }

    public static void Show(bool show)
    {
        try
        {
            LetterboxUI ui = Instance;
            ui.EnsureBuilt();
            if (ui.sliding != null) ui.StopCoroutine(ui.sliding);
            ui.sliding = ui.StartCoroutine(ui.Slide(show ? 1f : 0f));
        }
        catch (Exception e)
        {
            Debug.LogError($"[LetterboxUI] 검은 바 오류: {e}");
        }
    }

    private IEnumerator Slide(float target)
    {
        canvasGo.SetActive(true);
        float start = current;
        float t = 0f;
        while (t < SlideSeconds)
        {
            t += Time.unscaledDeltaTime;
            Apply(Mathf.SmoothStep(start, target, t / SlideSeconds));
            yield return null;
        }
        Apply(target);
        if (target <= 0f) canvasGo.SetActive(false);
        sliding = null;
    }

    private void Apply(float amount)
    {
        current = amount;
        float h = BarHeight * amount;
        top.sizeDelta = new Vector2(0f, h);
        bottom.sizeDelta = new Vector2(0f, h);
    }

    private void EnsureBuilt()
    {
        if (canvasGo != null) return;

        RectTransform root = UIBuild.CreateCanvas(transform, "LetterboxCanvas", sortingOrder, out canvasGo);
        top = CreateBar(root, "Top", 1f);
        bottom = CreateBar(root, "Bottom", 0f);
        Apply(0f);
        canvasGo.SetActive(false);
    }

    // 화면 가로 전체를 덮는 바 (edgeY 1 = 위쪽, 0 = 아래쪽)
    private static RectTransform CreateBar(RectTransform root, string name, float edgeY)
    {
        RectTransform rt = UIBuild.Stretch(root, name);
        rt.anchorMin = new Vector2(0f, edgeY);
        rt.anchorMax = new Vector2(1f, edgeY);
        rt.pivot = new Vector2(0.5f, edgeY);
        rt.anchoredPosition = Vector2.zero;

        Image img = rt.gameObject.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        return rt;
    }
}
