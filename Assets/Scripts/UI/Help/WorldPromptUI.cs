using System;
using UnityEngine;
using UnityEngine.UI;

// 상호작용 안내를 화면 아래 대신 "그 오브젝트 바로 위"에 노란 E 말풍선으로 (발굴 일지, 서랍, 스핑크스 발 모래 등)
// - 플레이어 시선이 화면 아래까지 안 내려가서 안내를 못 본다는 피드백 반영
// - 키 모양 네모(E) + 노란 글자, 살짝 위아래로 흔들림, 대상이 바뀌면 톡 튀어나옴
// - 연출, 대사, 아이템 팝업, 퍼즐 화면 중에는 숨김
// - [수정] 길게 누르는 중(게이지 링이 보이는 동안)에도 숨김 (링과 겹치지 않게)
// - [수정] "E 길게: 길게 눌러 털기" -> 키 네모 "E" + 글자 "길게 눌러 털기" (길게가 두 번 나오던 문제)
// - 이게 켜져 있으면 GameMessageUI의 아래쪽 안내는 꺼짐 (둘이 겹치지 않게)
// - HelpUIBootstrap이 정해진 씬에 자동으로 붙임
public class WorldPromptUI : MonoBehaviour
{
    private const int SortingOrder = 60;          // 목표 문구(60)와 같은 층, 대사창(100)보다 아래
    private const float HeadRoom = 0.25f;         // 오브젝트 그림 위로 띄우는 높이 (월드 단위)
    private const float BobHeight = 5f;
    private const float PopTime = 0.15f;

    private static readonly Color KeyColor = new Color(1f, 0.82f, 0.25f, 1f);    // 노란 키
    private static readonly Color KeyEdge = new Color(0.25f, 0.15f, 0.05f, 1f);
    private static readonly Color LabelColor = new Color(1f, 0.88f, 0.35f, 1f);

    private RectTransform canvasRect, bubble;
    private CanvasGroup group;
    private Text keyText, label;
    private InteractionSystem interaction;
    private PlayerController2D player;
    private Transform lastTarget;
    private float popTimer;

    private void Awake()
    {
        try
        {
            Build();
            GameMessageUI.SuppressBottomPrompt = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[WorldPromptUI] 만들기 오류: {e}");
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        GameMessageUI.SuppressBottomPrompt = false;
    }

    // ================= 만들기 =================
    private void Build()
    {
        canvasRect = UIBuild.CreateCanvas(transform, "WorldPromptCanvas", SortingOrder, out GameObject canvasGo);
        canvasGo.GetComponent<GraphicRaycaster>().enabled = false; // 클릭을 막지 않게

        bubble = UIBuild.Rect(canvasRect, "Bubble", Vector2.zero, new Vector2(300f, 48f));
        group = bubble.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        Font font = PuzzleUITheme.GetBodyFont();
        UIBuild.Image(bubble, "KeyEdge", new Vector2(-40f, 0f), new Vector2(44f, 44f), null, KeyEdge);
        UIBuild.Image(bubble, "Key", new Vector2(-40f, 2f), new Vector2(38f, 38f), null, KeyColor);
        keyText = UIBuild.Text(bubble, "KeyText", PuzzleUITheme.GetTitleFont(), 26, new Vector2(-40f, 2f), new Vector2(38f, 38f), KeyEdge);
        keyText.text = "E";

        label = UIBuild.Text(bubble, "Label", font, 24, new Vector2(-12f, 0f), new Vector2(400f, 40f), LabelColor);
        label.alignment = TextAnchor.MiddleLeft;
        label.rectTransform.pivot = new Vector2(0f, 0.5f);
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        Outline outline = label.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    // ================= 매 프레임 =================
    private void LateUpdate()
    {
        try
        {
            Transform target = CurrentTarget(out string prompt);
            bool show = target != null && !Busy() && !HoldRingVisible(target);
            group.alpha = show ? 1f : 0f;
            if (!show)
            {
                lastTarget = null;
                return;
            }

            if (target != lastTarget) StartPop(target, prompt);
            Place(target);
        }
        catch (Exception e)
        {
            Debug.LogError($"[WorldPromptUI] 표시 오류: {e}");
            enabled = false;
        }
    }

    private Transform CurrentTarget(out string prompt)
    {
        prompt = null;
        if (interaction == null) interaction = FindFirstObjectByType<InteractionSystem>();
        if (interaction == null) return null;

        prompt = interaction.CurrentPrompt;
        if (string.IsNullOrEmpty(prompt)) return null;
        return interaction.CurrentTargetTransform;
    }

    // 연출, 대사, 팝업, 퍼즐 화면, 조작 잠금 중에는 숨김
    private bool Busy()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController2D>();
        bool locked = player != null && !player.enabled;
        return locked || Cine.Running || DialogueBox.IsOpen || ItemPopup.IsShowing || PuzzleOverlayFocus.IsActive;
    }

    // 길게 누르기 게이지가 보이는 동안 (누르는 중이거나 게이지가 남아 있음)
    private static bool HoldRingVisible(Transform target)
    {
        if (Inspectable.Holding != null) return true;
        Inspectable inspect = target.GetComponent<Inspectable>();
        return inspect != null && inspect.HoldProgress > 0f;
    }

    private void StartPop(Transform target, string prompt)
    {
        lastTarget = target;
        popTimer = 0f;
        label.text = StripKey(prompt);
    }

    // "E: 발굴 일지 읽기" -> "발굴 일지 읽기" / "E 길게: 털기" -> "길게 눌러 털기" (키는 네모에 따로 그림)
    private static string StripKey(string prompt)
    {
        int colon = prompt.IndexOf(':');
        if (!prompt.StartsWith("E") || colon < 0) return prompt;

        string head = prompt.Substring(0, colon);
        string body = prompt.Substring(colon + 1).Trim();
        if (head.Contains("길게") && !body.Contains("길게")) body = "길게 눌러 " + body;
        return body;
    }

    // 오브젝트 그림의 윗부분 위에 말풍선 (가운데 정렬은 키+글자 전체 폭 기준)
    private void Place(Transform target)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 world = TopOf(target) + Vector3.up * HeadRoom;
        Vector2 screen = cam.WorldToScreenPoint(world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);

        float width = 56f + label.preferredWidth;
        float bob = Mathf.Sin(Time.unscaledTime * 3f) * BobHeight;
        bubble.anchoredPosition = local + new Vector2(-width * 0.5f + 62f, 24f + bob);

        popTimer += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(popTimer / PopTime);
        bubble.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, 1f - (1f - k) * (1f - k));
    }

    private static Vector3 TopOf(Transform target)
    {
        Renderer r = target.GetComponentInChildren<SpriteRenderer>();
        if (r != null && r.enabled) return new Vector3(r.bounds.center.x, r.bounds.max.y, 0f);
        Collider2D c = target.GetComponent<Collider2D>();
        if (c != null) return new Vector3(c.bounds.center.x, c.bounds.max.y, 0f);
        return target.position + Vector3.up * 0.5f;
    }
}
