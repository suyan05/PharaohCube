using System;
using UnityEngine;
using UnityEngine.UI;

// 화면 왼쪽 위 횃불 게이지 (총정리 문서 4.1 UI: 작은 불꽃 게이지 + 불씨가 있으면 밑에 금색 불씨 아이콘)
// - 불꽃 동그라미 + 막대 (남은 시간). 밝은 곳에서는 막대가 금빛 테두리 + "멈춤"
// - 15초 아래로 남으면 막대가 붉게 깜빡. 꺼지면 "꺼짐"
// - 불씨: 아이콘 + "라의 불씨" (시간이 안 줄어서 막대가 늘 가득)
// - 연출, 대사 중에는 숨김 (RaHudDirector.Busy)
// - 지금은 도형으로 그림. 아트가 오면 flameSprite / emberSprite 칸에 넣기
public class RaTorchGaugeUI : MonoBehaviour
{
    private const float BarWidth = 240f;

    [SerializeField] private Sprite flameSprite;
    [SerializeField] private Sprite emberSprite;
    [SerializeField] private int sortingOrder = 70;
    [SerializeField] private float lowSeconds = 15f;
    [SerializeField] private float burnSecondsForLow = 60f;

    private PlayerTorchTemp torch;
    private CanvasGroup group;
    private Image flame, barFill, barFrame, emberIcon;
    private Text stateText, emberText;
    private Color fillColor = new Color(1f, 0.62f, 0.2f);
    private Color lowColor = new Color(0.9f, 0.25f, 0.15f);
    private Color gold = new Color(0.91f, 0.7f, 0.23f);

    private void Start()
    {
        try
        {
            Build();
        }
        catch (Exception e)
        {
            Debug.LogError($"[RaTorchGaugeUI] 만들기 오류: {e}");
            enabled = false;
        }
    }

    private void Update()
    {
        if (group == null) return;
        if (torch == null) torch = FindFirstObjectByType<PlayerTorchTemp>();

        bool show = torch != null && !RaHudDirector.Busy;
        group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, Time.unscaledDeltaTime * 6f);
        if (torch == null) return;

        RefreshBar();
        RefreshEmber();
    }

    private void RefreshBar()
    {
        float ratio = torch.IsLit ? torch.BurnRatio : 0f;
        barFill.rectTransform.sizeDelta = new Vector2(BarWidth * ratio, 14f);

        bool low = torch.IsLit && !torch.HasEmber && ratio * burnSecondsForLow < lowSeconds;
        float blink = low ? 0.5f + 0.5f * Mathf.Sin(Time.time * 10f) : 0f;
        barFill.color = Color.Lerp(fillColor, lowColor, low ? 0.6f + 0.4f * blink : 0f);

        bool paused = RaTorchTimer.InBrightPlace && torch.IsLit && !torch.HasEmber;
        barFrame.color = paused ? gold : new Color(0.1f, 0.07f, 0.04f, 0.85f);
        flame.color = torch.IsLit ? Color.Lerp(fillColor, Color.white, 0.2f * Mathf.PerlinNoise(Time.time * 4f, 1f)) : new Color(0.3f, 0.3f, 0.3f);
        stateText.text = !torch.IsLit ? "꺼짐 - 불을 찾자" : (torch.HasEmber ? "꺼지지 않음" : (paused ? "밝은 곳 - 멈춤" : ""));
    }

    private void RefreshEmber()
    {
        bool has = torch.HasEmber;
        emberIcon.enabled = has;
        emberText.enabled = has;
        if (has) emberIcon.color = Color.Lerp(gold, Color.white, 0.25f + 0.25f * Mathf.Sin(Time.time * 3f));
    }

    // ================= 화면 만들기 =================
    private void Build()
    {
        RectTransform root = UIBuild.CreateCanvas(transform, "TorchGaugeCanvas", sortingOrder, out GameObject canvasGo);
        group = canvasGo.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.alpha = 0f;

        RectTransform panel = UIBuild.Rect(root, "Panel", Vector2.zero, new Vector2(330f, 110f));
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 1f);
        panel.anchoredPosition = new Vector2(30f, -30f);

        BuildFlame(panel);
        BuildBar(panel);
        BuildEmber(panel);
    }

    private void BuildFlame(RectTransform panel)
    {
        flame = UIBuild.Image(panel, "Flame", new Vector2(-130f, 20f), new Vector2(56f, 56f), flameSprite, fillColor);
        if (flameSprite == null) flame.sprite = UIBuild.Circle();
        Font font = PuzzleUITheme.GetBodyFont();
        UIBuild.Text(flame.rectTransform, "Label", font, 16, Vector2.zero, new Vector2(56f, 56f), new Color(0.2f, 0.1f, 0.03f)).text = "불";
    }

    private void BuildBar(RectTransform panel)
    {
        barFrame = UIBuild.Image(panel, "BarFrame", new Vector2(30f, 28f), new Vector2(BarWidth + 8f, 22f), null, new Color(0.1f, 0.07f, 0.04f, 0.85f));
        RectTransform back = UIBuild.Image(barFrame.rectTransform, "BarBack", Vector2.zero, new Vector2(BarWidth, 14f), null, new Color(0.25f, 0.18f, 0.12f)).rectTransform;
        barFill = UIBuild.Image(back, "BarFill", Vector2.zero, new Vector2(BarWidth, 14f), null, fillColor);
        RectTransform fill = barFill.rectTransform;
        fill.anchorMin = fill.anchorMax = fill.pivot = new Vector2(0f, 0.5f);
        fill.anchoredPosition = Vector2.zero;

        stateText = UIBuild.Text(panel, "State", PuzzleUITheme.GetBodyFont(), 18, new Vector2(30f, 2f), new Vector2(BarWidth, 26f), new Color(0.96f, 0.94f, 0.9f));
        stateText.alignment = TextAnchor.MiddleLeft;
    }

    private void BuildEmber(RectTransform panel)
    {
        // [수정] 아이콘(왼쪽 끝 -145 ~ -115)과 글자(-95부터)가 겹치지 않게 20px 띄움
        emberIcon = UIBuild.Image(panel, "Ember", new Vector2(-130f, -32f), new Vector2(30f, 30f), emberSprite, gold);
        if (emberSprite == null) emberIcon.sprite = UIBuild.Circle();
        emberText = UIBuild.Text(panel, "EmberText", PuzzleUITheme.GetBodyFont(), 18, new Vector2(-10f, -32f), new Vector2(170f, 26f), gold);
        emberText.horizontalOverflow = HorizontalWrapMode.Overflow;
        emberText.alignment = TextAnchor.MiddleLeft;
        emberText.text = "라의 불씨";
    }
}
