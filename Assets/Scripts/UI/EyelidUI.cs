using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 눈 뜨기 연출 (기획서 4.1: 검은 막 두 개가 위아래로 벌어짐, 두 번 깜빡임)
// - EyelidUI.SetClosed();                       눈 감은 상태로 시작
// - yield return EyelidUI.OpenTo(0.4f, 0.6f);   0 = 감음, 1 = 다 뜸 (초 동안)
// - EyelidUI.Hide();                            끝나면 치우기
// 검은 화면(ScreenFader, 200)보다 위(205), 자막(210)보다 아래
// 정식 엔딩의 눈 뜨기에서도 그대로 다시 씀 (총정리 6장)
public class EyelidUI : MonoBehaviour
{
    private const float HalfHeight = 620f; // 화면 절반보다 넉넉하게 (화면비가 달라도 틈 안 생기게)
    private static EyelidUI instance;

    private GameObject canvasGo;
    private RectTransform top, bottom;
    private float openAmount;

    private static EyelidUI Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<EyelidUI>();
            if (instance == null) instance = new GameObject("EyelidUI").AddComponent<EyelidUI>();
            return instance;
        }
    }

    // ================= 밖에서 부르는 함수 =================
    public static void SetClosed()
    {
        try
        {
            EyelidUI ui = Instance;
            ui.EnsureBuilt();
            ui.Apply(0f);
            ui.canvasGo.SetActive(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[EyelidUI] 눈꺼풀 준비 오류: {e}");
        }
    }

    public static IEnumerator OpenTo(float target, float seconds)
    {
        EyelidUI ui = Instance;
        if (!ui.TryShow()) yield break;

        float start = ui.openAmount;
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.unscaledDeltaTime;
            ui.Apply(Mathf.SmoothStep(start, target, t / seconds));
            yield return null;
        }
        ui.Apply(Cine.Skipping ? 1f : target);
    }

    public static void Hide()
    {
        if (instance != null && instance.canvasGo != null) instance.canvasGo.SetActive(false);
    }

    // ================= 내부 =================
    private bool TryShow()
    {
        try
        {
            EnsureBuilt();
            canvasGo.SetActive(true);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[EyelidUI] 눈꺼풀 표시 오류: {e}");
            return false;
        }
    }

    private void Apply(float amount)
    {
        openAmount = Mathf.Clamp01(amount);
        float h = HalfHeight * (1f - openAmount);
        top.sizeDelta = new Vector2(0f, h);
        bottom.sizeDelta = new Vector2(0f, h);
    }

    private void EnsureBuilt()
    {
        if (canvasGo != null) return;

        RectTransform root = UIBuild.CreateCanvas(transform, "EyelidCanvas", 205, out canvasGo);
        top = CreateLid(root, "Top", 1f);
        bottom = CreateLid(root, "Bottom", 0f);
        Apply(0f);
        canvasGo.SetActive(false);
    }

    // 화면 가로 전체를 덮는 막 (edgeY 1 = 위쪽에서 내려옴, 0 = 아래쪽에서 올라옴)
    private static RectTransform CreateLid(RectTransform root, string name, float edgeY)
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
