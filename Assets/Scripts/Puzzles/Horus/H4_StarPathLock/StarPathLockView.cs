using System;
using UnityEngine;
using UnityEngine.UI;

// H4 화면 꾸미기: 방위 글자, 벽화 '매의 비행' 4컷, 세트의 봉인 덮개
public class StarPathLockView : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private StarPathLockPuzzle puzzle;
    [SerializeField] private RectTransform boardRoot;
    [SerializeField] private Text titleText;
    [SerializeField] private Text statusText;

    [Header("배치 (오버레이 중심 기준)")]
    [SerializeField] private float compassOffset = 240f;
    [SerializeField] private float muralY = -440f;
    [SerializeField] private Vector2 cutSize = new Vector2(230f, 120f);
    [SerializeField] private float cutSpacing = 250f;

    private static readonly string[] CutTexts =
    {
        "동쪽 지평선에서\n매가 떠오른다",
        "하늘 꼭대기를\n높이 지나",
        "서쪽 사막을\n넘어",
        "강 아래로 내려갔다가\n중심에 내려앉는다"
    };

    private static readonly string[] CutLabels = { "동", "북 (하늘)", "서", "남 (강) -> 중심" };

    private readonly Image[] cutImages = new Image[4];
    private Image sealCover;
    private Font font;
    private bool built;
    private Color cutColor, gold, paper, red, lapis;

    private void Start()
    {
        try
        {
            if (puzzle == null || boardRoot == null)
            {
                Debug.LogError("[별길 봉인 화면] Puzzle / Board Root 연결 필요");
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
            Debug.LogError($"[별길 봉인 화면] Start 오류: {e}");
        }
    }

    private void OnDestroy()
    {
        if (puzzle == null) return;
        puzzle.OnStateChanged -= Refresh;
        puzzle.OnMessage -= ShowStatus;
        puzzle.OnTitleChanged -= ShowTitle;
    }

    private void SetupColors()
    {
        cutColor = HorusUiFactory.Hex("#2E3B57");
        gold = HorusUiFactory.Hex("#E8B23A");
        paper = HorusUiFactory.Hex("#F6F1E7");
        red = new Color(0.70f, 0.23f, 0.23f, 0.85f);
        lapis = HorusUiFactory.Hex("#3E7BD6");
    }

    private void Build()
    {
        BuildCompass();
        BuildMural();
        BuildSeal();
        built = true;
    }

    // 별판 테두리 방위 + 동쪽 해돋이 표시 (기획서 9.11)
    private void BuildCompass()
    {
        Vector2 size = new Vector2(120f, 40f);
        HorusUiFactory.CreateText(boardRoot, font, "동", 32, new Vector2(compassOffset, 0f), size, gold);
        HorusUiFactory.CreateText(boardRoot, font, "(해 뜨는 쪽)", 16, new Vector2(compassOffset, -32f), size, paper);
        HorusUiFactory.CreateText(boardRoot, font, "서", 32, new Vector2(-compassOffset, 0f), size, paper);
        HorusUiFactory.CreateText(boardRoot, font, "북", 32, new Vector2(0f, compassOffset), size, paper);
        HorusUiFactory.CreateText(boardRoot, font, "남", 32, new Vector2(0f, -compassOffset), size, paper);
    }

    private void BuildMural()
    {
        HorusUiFactory.CreateText(boardRoot, font, "남쪽 벽화 - 매의 비행", 22,
            new Vector2(0f, muralY + 95f), new Vector2(600f, 36f), gold);
        for (int i = 0; i < cutImages.Length; i++)
        {
            Vector2 pos = new Vector2((i - 1.5f) * cutSpacing, muralY);
            cutImages[i] = HorusUiFactory.CreateRect(boardRoot, $"MuralCut_{i}", pos, cutSize, cutColor);
            RectTransform rt = cutImages[i].rectTransform;
            HorusUiFactory.CreateText(rt, font, CutTexts[i], 18, new Vector2(0f, 12f),
                cutSize - new Vector2(16f, 40f), paper, true);
            HorusUiFactory.CreateText(rt, font, CutLabels[i], 16, new Vector2(0f, -cutSize.y / 2f + 16f),
                new Vector2(cutSize.x, 24f), lapis);
        }
    }

    // 잠겨 있을 때 별판을 덮는 붉은 봉인 (클릭도 막음)
    private void BuildSeal()
    {
        sealCover = HorusUiFactory.CreateRect(boardRoot, "SetSeal", Vector2.zero, new Vector2(420f, 420f), red);
        sealCover.raycastTarget = true;
        HorusUiFactory.CreateText(sealCover.rectTransform, font, "세트의 붉은 봉인", 30,
            Vector2.zero, new Vector2(400f, 60f), paper);
    }

    private void Refresh()
    {
        if (!built) return;
        sealCover.gameObject.SetActive(!puzzle.IsUnlocked);
        for (int i = 0; i < cutImages.Length; i++)
        {
            cutImages[i].color = puzzle.HintCut == i ? gold : cutColor;
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
}