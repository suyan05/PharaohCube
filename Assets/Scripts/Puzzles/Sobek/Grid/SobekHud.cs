using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 소베크 판 안내 패널 (화면 왼쪽 위에 계속 표시)
// - 판에 들어가면 SobekBoard가 Show, 나가면 Hide (씬에 미리 둘 필요 없음: 처음 부를 때 자동 생성)
// - 내용: 판 이름 / 조작(Z 되돌리기, R 처음으로) / 걸음 수 / 수위 / 남은 시간
public class SobekHud : MonoBehaviour
{
    private static SobekHud instance;

    private TextMeshProUGUI text;
    private GameObject panel;

    public static SobekHud Get(TMP_FontAsset font)
    {
        if (instance != null) return instance;
        var go = new GameObject("SobekHud");
        instance = go.AddComponent<SobekHud>();
        instance.Build(font);
        return instance;
    }

    private void Build(TMP_FontAsset font)
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 해상도 대응: 1920x1080 기준
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panel = new GameObject("Panel");
        panel.transform.SetParent(transform, false);
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.04f, 0.72f);
        bg.raycastTarget = false;
        var rt = (RectTransform)panel.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // 왼쪽 위 고정
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(24f, -24f);
        rt.sizeDelta = new Vector2(620f, 150f);

        var tgo = new GameObject("Text");
        tgo.transform.SetParent(panel.transform, false);
        text = tgo.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = 26f;
        text.color = new Color(1f, 0.95f, 0.82f);
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.TopLeft;
        var trt = (RectTransform)tgo.transform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(18f, 12f);
        trt.offsetMax = new Vector2(-18f, -12f);
        panel.SetActive(false);
    }

    public void Show(string content)
    {
        if (panel == null) return;
        if (!panel.activeSelf) panel.SetActive(true);
        text.text = content;
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
