using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// H5 화면: 제단 별 6개, 이어진 선, 성좌 카드 / 모은 단서 패널, 뒤로 버튼
public class SkyAlignView : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private SkyAlignPuzzle puzzle;
    [SerializeField] private RectTransform boardRoot;

    [Header("배치 (오버레이 중심 기준)")]
    [SerializeField] private Vector2 altarCenter = new Vector2(0f, 30f);
    [SerializeField] private float starSize = 60f;

    private Image[] starImages;
    private RectTransform[] starRects;
    private Vector2[] starPos;
    private RectTransform lineLayer;
    private readonly List<GameObject> lines = new List<GameObject>();
    private Text titleText, statusText, cardText, noteText;
    private Button backButton;
    private Font font;
    private bool built;

    private Color gold, red, white, dim, silver, paper, panel, frame;

    private void Start()
    {
        try
        {
            if (puzzle == null || boardRoot == null)
            {
                Debug.LogError("[하늘 화면] Puzzle / Board Root 연결 필요");
                return;
            }
            SetupColors();
            font = HorusUiFactory.LegacyFont();
            Build();
            puzzle.OnStateChanged += Refresh;
            puzzle.OnMessage += ShowStatus;
            puzzle.OnTitleChanged += ShowTitle;
            ShowTitle(puzzle.LastTitle);
            ShowStatus(puzzle.LastMessage);
            Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError($"[하늘 화면] Start 오류: {e}");
        }
    }

    private void OnDestroy()
    {
        if (backButton != null) backButton.onClick.RemoveAllListeners();
        if (puzzle == null) return;
        puzzle.OnStateChanged -= Refresh;
        puzzle.OnMessage -= ShowStatus;
        puzzle.OnTitleChanged -= ShowTitle;
    }

    private void SetupColors()
    {
        gold = HorusUiFactory.Hex("#E8B23A");
        red = HorusUiFactory.Hex("#B33A3A");
        white = HorusUiFactory.Hex("#F6F1E7");
        dim = HorusUiFactory.Hex("#3A4050");
        silver = HorusUiFactory.Hex("#C9D3E0");
        paper = HorusUiFactory.Hex("#E8D9B8");
        panel = HorusUiFactory.Hex("#1E2A44");
        frame = HorusUiFactory.Hex("#2E3B57");
    }

    // ================= 화면 만들기 =================
    private void Build()
    {
        titleText = HorusUiFactory.CreateText(boardRoot, font, "", 40, new Vector2(0f, 420f), new Vector2(900f, 60f), gold);
        BuildAltar();
        BuildPanels();
        statusText = HorusUiFactory.CreateText(boardRoot, font, "", 26, new Vector2(0f, -330f), new Vector2(1500f, 50f), white);
        HorusUiFactory.CreateText(boardRoot, font, "별을 순서대로 클릭  |  뒤로 : 마지막 입력 취소  |  E / Esc : 닫기", 20,
            new Vector2(0f, -380f), new Vector2(1400f, 40f), paper);
        BuildBackButton();
        built = true;
    }

    private void BuildAltar()
    {
        HorusUiFactory.CreateRect(boardRoot, "AltarFrame", altarCenter, new Vector2(600f, 560f), frame);
        var layerGo = new GameObject("Lines", typeof(RectTransform));
        lineLayer = layerGo.GetComponent<RectTransform>();
        lineLayer.SetParent(boardRoot, false);
        lineLayer.anchorMin = lineLayer.anchorMax = new Vector2(0.5f, 0.5f);
        lineLayer.anchoredPosition = Vector2.zero;

        int count = puzzle.Stars.Count;
        starImages = new Image[count];
        starRects = new RectTransform[count];
        starPos = new Vector2[count];
        for (int i = 0; i < count; i++) CreateStar(i);
    }

    private void CreateStar(int index)
    {
        SkyStar star = puzzle.Stars[index];
        float rad = star.angle * Mathf.Deg2Rad;
        Vector2 pos = altarCenter + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * star.radius;
        Image img = HorusUiFactory.CreateRect(boardRoot, $"SkyStar_{index}", pos, new Vector2(starSize, starSize), gold);
        img.raycastTarget = true;
        Button btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        int captured = index;
        btn.onClick.AddListener(() => puzzle.SelectStar(captured));
        HorusUiFactory.CreateText(boardRoot, font, star.label, 22, pos + new Vector2(0f, -50f), new Vector2(200f, 30f), white);
        starImages[index] = img;
        starRects[index] = img.rectTransform;
        starPos[index] = pos;
    }

    private void BuildPanels()
    {
        Image cardPanel = HorusUiFactory.CreateRect(boardRoot, "CardPanel", new Vector2(-640f, 60f), new Vector2(440f, 360f), panel);
        HorusUiFactory.CreateText(cardPanel.rectTransform, font, "성좌 카드", 26, new Vector2(0f, 150f), new Vector2(400f, 40f), gold);
        cardText = HorusUiFactory.CreateText(cardPanel.rectTransform, font, "", 20, new Vector2(0f, -20f), new Vector2(400f, 280f), white, true);

        Image notePanel = HorusUiFactory.CreateRect(boardRoot, "NotePanel", new Vector2(640f, 60f), new Vector2(500f, 380f), panel);
        HorusUiFactory.CreateText(notePanel.rectTransform, font, "모은 단서", 26, new Vector2(0f, 160f), new Vector2(460f, 40f), gold);
        noteText = HorusUiFactory.CreateText(notePanel.rectTransform, font, "", 19, new Vector2(0f, -20f), new Vector2(460f, 300f), white, true);
    }

    private void BuildBackButton()
    {
        Image img = HorusUiFactory.CreateRect(boardRoot, "BackButton", new Vector2(0f, -450f), new Vector2(180f, 56f), HorusUiFactory.Hex("#4A3423"));
        img.raycastTarget = true;
        backButton = img.gameObject.AddComponent<Button>();
        backButton.targetGraphic = img;
        backButton.onClick.AddListener(puzzle.Back);
        HorusUiFactory.CreateText(img.rectTransform, font, "뒤로", 24, Vector2.zero, new Vector2(180f, 56f), white);
    }

    // ================= 화면 갱신 =================
    private void Refresh()
    {
        if (!built) return;
        RefreshStars();
        RebuildLines();
        cardText.text = string.Join("\n\n", puzzle.GetCardLines());
        noteText.text = string.Join("\n\n", puzzle.GetNoteLines());
    }

    private void RefreshStars()
    {
        for (int i = 0; i < starImages.Length; i++)
        {
            bool selected = Contains(puzzle.Input, i);
            if (!puzzle.IsAwake) starImages[i].color = dim;
            else if (selected) starImages[i].color = white;
            else starImages[i].color = puzzle.Stars[i].isTrap ? red : gold;
        }
    }

    private void RebuildLines()
    {
        foreach (GameObject go in lines) if (go != null) Destroy(go);
        lines.Clear();
        Color color = puzzle.WrongFlash ? red : silver;
        for (int k = 1; k < puzzle.Input.Count; k++)
        {
            lines.Add(CreateLine(starPos[puzzle.Input[k - 1]], starPos[puzzle.Input[k]], color));
        }
    }

    private GameObject CreateLine(Vector2 from, Vector2 to, Color color)
    {
        Image img = HorusUiFactory.CreateRect(lineLayer, "Line", from, new Vector2(10f, 8f), color);
        RectTransform rt = img.rectTransform;
        rt.pivot = new Vector2(0f, 0.5f);
        Vector2 diff = to - from;
        rt.anchoredPosition = from;
        rt.sizeDelta = new Vector2(diff.magnitude, 8f);
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
        return img.gameObject;
    }

    // 붉은 별 흔들림(힌트 2), 다음 별 깜빡임(힌트 3)
    private void Update()
    {
        if (!built) return;
        float t = Time.unscaledTime;
        for (int i = 0; i < starRects.Length; i++)
        {
            bool shake = puzzle.TrapHint && puzzle.Stars[i].isTrap;
            float dx = shake ? Mathf.Sin(t * 30f) * 5f : 0f;
            starRects[i].anchoredPosition = starPos[i] + new Vector2(dx, 0f);
            float scale = puzzle.NextHintStar == i ? 1f + 0.18f * (0.5f + 0.5f * Mathf.Sin(t * 5f)) : 1f;
            starRects[i].localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void ShowStatus(string text)
    {
        if (statusText != null) statusText.text = text;
    }

    private void ShowTitle(string text)
    {
        if (titleText != null) titleText.text = text;
    }

    private static bool Contains(IReadOnlyList<int> list, int value)
    {
        for (int i = 0; i < list.Count; i++) if (list[i] == value) return true;
        return false;
    }
}