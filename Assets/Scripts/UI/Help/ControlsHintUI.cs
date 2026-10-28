using System;
using UnityEngine;
using UnityEngine.UI;

// 왼쪽 아래 조작키 안내 (반투명): WASD 이동 / E 상호작용 / J 고고학 수첩 / ESC 길게 연출 건너뛰기
// - 연출, 대사, 팝업, 퍼즐 화면 중에는 스르륵 숨김
// - HelpUIBootstrap이 정해진 씬(발굴 현장)에 자동으로 붙임
public class ControlsHintUI : MonoBehaviour
{
    private const int SortingOrder = 55;
    private const float ShownAlpha = 0.6f;
    private const float FadeSpeed = 4f;
    private const float RowHeight = 40f;

    private static readonly Color PanelColor = new Color(0.08f, 0.05f, 0.03f, 0.75f);
    private static readonly Color KeyColor = new Color(0.91f, 0.70f, 0.23f, 1f);
    private static readonly Color KeyText = new Color(0.2f, 0.12f, 0.05f, 1f);
    private static readonly Color TextColor = new Color(0.96f, 0.93f, 0.85f, 1f);

    // 키 이름, 설명
    private static readonly string[,] Rows =
    {
        { "WASD", "이동" },
        { "E", "조사 / 상호작용" },
        { "J", "고고학 수첩" },
        { "ESC 길게", "연출 건너뛰기" }
    };

    private CanvasGroup group;
    private PlayerController2D player;

    private void Awake()
    {
        try
        {
            Build();
        }
        catch (Exception e)
        {
            Debug.LogError($"[ControlsHintUI] 만들기 오류: {e}");
            enabled = false;
        }
    }

    private void Build()
    {
        RectTransform canvas = UIBuild.CreateCanvas(transform, "ControlsHintCanvas", SortingOrder, out GameObject canvasGo);
        canvasGo.GetComponent<GraphicRaycaster>().enabled = false;

        int count = Rows.GetLength(0);
        Vector2 size = new Vector2(330f, count * RowHeight + 24f);
        // 왼쪽 아래 모서리에 붙임 (화면 비율이 달라도 모서리 기준)
        RectTransform panel = UIBuild.Rect(canvas, "Panel", Vector2.zero, size);
        panel.anchorMin = panel.anchorMax = Vector2.zero;
        panel.anchoredPosition = new Vector2(28f + size.x * 0.5f, 28f + size.y * 0.5f);
        group = panel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        UIBuild.Image(panel, "Bg", Vector2.zero, size, null, PanelColor);

        Font font = PuzzleUITheme.GetBodyFont();
        for (int i = 0; i < count; i++)
        {
            float y = size.y * 0.5f - 12f - RowHeight * (i + 0.5f);
            BuildRow(panel, font, i, y, size.x);
        }
    }

    private static void BuildRow(RectTransform panel, Font font, int i, float y, float width)
    {
        string key = Rows[i, 0];
        float keyWidth = key.Length > 1 ? 18f * key.Length + 18f : 34f;
        float left = -width * 0.5f + 16f;

        UIBuild.Image(panel, $"Key_{i}", new Vector2(left + keyWidth * 0.5f, y), new Vector2(keyWidth, 32f), null, KeyColor);
        Text k = UIBuild.Text(panel, $"KeyText_{i}", font, 20, new Vector2(left + keyWidth * 0.5f, y), new Vector2(keyWidth, 32f), KeyText);
        k.text = key;

        Text d = UIBuild.Text(panel, $"Desc_{i}", font, 22, new Vector2(left + keyWidth + 14f, y), new Vector2(220f, 32f), TextColor);
        d.text = Rows[i, 1];
        d.alignment = TextAnchor.MiddleLeft;
        d.rectTransform.pivot = new Vector2(0f, 0.5f);
    }

    private void Update()
    {
        if (group == null) return;
        if (player == null) player = FindFirstObjectByType<PlayerController2D>();

        bool locked = player != null && !player.enabled;
        bool busy = locked || Cine.Running || DialogueBox.IsOpen || ItemPopup.IsShowing || PuzzleOverlayFocus.IsActive;
        float target = busy ? 0f : ShownAlpha;
        group.alpha = Mathf.MoveTowards(group.alpha, target, FadeSpeed * Time.unscaledDeltaTime);
    }
}
