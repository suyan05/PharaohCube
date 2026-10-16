using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 라 대사 한 줄에 쓸 초상화 표정
public enum RaPortrait { Default, Glow, Night }

// 대사 한 줄 (Inspector에서 편집)
[Serializable]
public class RaDialogueLine
{
    public RaPortrait portrait = RaPortrait.Default;
    [TextArea(2, 4)] public string text = "";

    public RaDialogueLine() { }

    public RaDialogueLine(RaPortrait portrait, string text)
    {
        this.portrait = portrait;
        this.text = text;
    }
}

// 라 대사창 (씬에 1개, RaCanvas 아래 화면 전체 크기 빈 오브젝트에 붙임)
// - Play(대사 배열, 끝났을 때 할 일)로 재생
// - 글자가 한 글자씩 나옴 -> E / Space / Enter / 클릭: 다 나왔으면 다음 줄, 나오는 중이면 한 번에 표시
// - 대화 중에는 이동(PlayerController2D)과 E키 조사(InteractionSystem)를 막음
[RequireComponent(typeof(RectTransform))]
public class RaDialogue : MonoBehaviour
{
    private static readonly Vector2 BoxSize = new Vector2(1400f, 240f);
    private static readonly Vector2 BoxPos = new Vector2(120f, -400f);
    private static readonly Vector2 PortraitSize = new Vector2(300f, 300f);
    private static readonly Vector2 PortraitPos = new Vector2(-720f, -370f);

    [Header("아트 (아트 요청서 E)")]
    [SerializeField] private Sprite portraitDefault; // UI_Portrait_Ra_Default
    [SerializeField] private Sprite portraitGlow;    // UI_Portrait_Ra_Glow
    [Tooltip("비우면 기본 초상화 사용")]
    [SerializeField] private Sprite portraitNight;   // UI_Portrait_Ra_Night
    [SerializeField] private Sprite dialogBoxSprite; // UI_Dialog_Box (9-slice)
    [SerializeField] private Sprite nextSprite;      // UI_Dialog_Next

    [Header("글자")]
    [SerializeField] private string speakerName = "라";
    [SerializeField, Min(1f)] private float charsPerSecond = 28f;

    [Header("화면")]
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.35f);
    [Tooltip("대화 중 투명하게 숨길 UI (목표 문구 등)")]
    [SerializeField] private GameObject[] hideWhileOpen;

    private readonly Dictionary<CanvasGroup, float> hiddenGroups = new Dictionary<CanvasGroup, float>();
    private GameObject root;
    private Image portraitImage;
    private Image nextImage;
    private Text bodyText;
    private Font font;
    private Color paper, darkBrown, gold, orange, lapis;

    private RaDialogueLine[] lines;
    private int index;
    private string fullText = "";
    private float visibleChars;
    private int openedFrame;
    private Action onFinished;

    private PlayerController2D playerController;
    private InteractionSystem playerInteraction;
    private Rigidbody2D playerRb;

    public bool IsPlaying => root != null && root.activeSelf;

    // 대화가 닫힌 프레임 (닫을 때 누른 E로 바로 다시 말을 거는 것 방지용)
    public int ClosedFrame { get; private set; } = -1;

    // ================= 재생 =================
    public void Play(RaDialogueLine[] newLines, Action finished = null)
    {
        try
        {
            if (newLines == null || newLines.Length == 0)
            {
                finished?.Invoke();
                return;
            }

            EnsureBuilt();
            lines = newLines;
            index = 0;
            onFinished = finished;
            openedFrame = Time.frameCount;

            SetPlayerLocked(true);
            SetOtherUIHidden(true);
            transform.SetAsLastSibling();
            root.SetActive(true);
            ShowLine();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 대사 재생 오류: {e}");
        }
    }

    private void Update()
    {
        if (!IsPlaying) return;

        try
        {
            TypeText();
            if (Time.frameCount == openedFrame) return; // 대화를 연 E키가 바로 넘어가지 않게
            if (PressedNext()) Advance();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 대사 진행 오류: {e}");
            Close(false);
        }
    }

    private void OnDisable()
    {
        if (IsPlaying) Close(false);
    }

    private static bool PressedNext()
    {
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetMouseButtonDown(0);
    }

    // 글자가 다 안 나왔으면 한 번에 표시, 다 나왔으면 다음 줄
    private void Advance()
    {
        if (visibleChars < fullText.Length)
        {
            visibleChars = fullText.Length;
            bodyText.text = fullText;
            return;
        }

        index++;
        if (index >= lines.Length) Close(true);
        else ShowLine();
    }

    private void ShowLine()
    {
        RaDialogueLine line = lines[index] ?? new RaDialogueLine();
        fullText = line.text ?? "";
        visibleChars = 0f;
        bodyText.text = "";
        nextImage.enabled = false;

        Sprite sprite = PortraitOf(line.portrait);
        portraitImage.sprite = sprite;
        portraitImage.color = sprite != null ? Color.white : lapis; // 그림이 없으면 라피스 블루 네모
    }

    private void TypeText()
    {
        if (visibleChars >= fullText.Length)
        {
            nextImage.enabled = true;
            float bob = Mathf.Sin(Time.unscaledTime * 5f) * 4f;
            nextImage.rectTransform.anchoredPosition = NextPos() + new Vector2(0f, bob);
            return;
        }

        visibleChars += charsPerSecond * Time.unscaledDeltaTime;
        int count = Mathf.Min(fullText.Length, Mathf.FloorToInt(visibleChars));
        bodyText.text = fullText.Substring(0, count);
    }

    private void Close(bool invokeFinished)
    {
        if (root != null) root.SetActive(false);
        ClosedFrame = Time.frameCount;
        SetOtherUIHidden(false);
        SetPlayerLocked(false);

        Action callback = onFinished;
        onFinished = null;
        lines = null;
        if (invokeFinished) callback?.Invoke();
    }

    private Sprite PortraitOf(RaPortrait portrait)
    {
        switch (portrait)
        {
            case RaPortrait.Glow: return portraitGlow != null ? portraitGlow : portraitDefault;
            case RaPortrait.Night: return portraitNight != null ? portraitNight : portraitDefault;
            default: return portraitDefault;
        }
    }

    // ================= 플레이어 잠금 / UI 숨기기 =================
    private void SetPlayerLocked(bool locked)
    {
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController2D>();
            if (playerController == null) return;
            playerInteraction = playerController.GetComponent<InteractionSystem>();
            playerRb = playerController.GetComponent<Rigidbody2D>();
        }

        playerController.enabled = !locked;
        if (playerInteraction != null) playerInteraction.enabled = !locked;
        if (locked && playerRb != null) playerRb.linearVelocity = Vector2.zero;
    }

    // 다른 UI는 끄지 않고 투명하게만 (그쪽 스크립트가 멈추지 않게)
    private void SetOtherUIHidden(bool hide)
    {
        if (hideWhileOpen == null) return;

        foreach (GameObject go in hideWhileOpen)
        {
            if (go == null) continue;

            CanvasGroup group = go.GetComponent<CanvasGroup>();
            if (group == null) group = go.AddComponent<CanvasGroup>();

            if (hide)
            {
                if (!hiddenGroups.ContainsKey(group)) hiddenGroups[group] = group.alpha;
                group.alpha = 0f;
            }
            else if (hiddenGroups.TryGetValue(group, out float prev))
            {
                group.alpha = prev;
            }
        }
        if (!hide) hiddenGroups.Clear();
    }

    // ================= 화면 만들기 (처음 한 번) =================
    private void EnsureBuilt()
    {
        if (root != null) return;

        SetupColors();
        font = PuzzleUITheme.GetBodyFont();
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        root = CreateStretch("DialogueRoot", (RectTransform)transform);
        RectTransform rt = (RectTransform)root.transform;

        Image backdrop = CreateStretch("Backdrop", rt).AddComponent<Image>();
        backdrop.color = backdropColor;
        backdrop.raycastTarget = true;

        BuildBox(rt);
        BuildPortrait(rt);
        root.SetActive(false);
    }

    private void SetupColors()
    {
        paper = Hex("#F6F1E7");
        darkBrown = Hex("#4A3423");
        gold = Hex("#E8B23A");
        orange = Hex("#E07A3F");
        lapis = Hex("#1E3A5F");
    }

    private void BuildBox(RectTransform parent)
    {
        if (dialogBoxSprite != null)
        {
            Image box = CreateImage(parent, "Box", BoxPos, BoxSize, Color.white);
            box.sprite = dialogBoxSprite;
            box.type = dialogBoxSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        }
        else
        {
            CreateImage(parent, "Box_Border", BoxPos, BoxSize, gold);
            CreateImage(parent, "Box_Inner", BoxPos, BoxSize - new Vector2(12f, 12f), paper);
        }

        float left = BoxPos.x - BoxSize.x * 0.5f;
        Text nameText = CreateText(parent, "Name", 32, new Vector2(left + 260f, BoxPos.y + 78f), new Vector2(400f, 44f), orange);
        nameText.alignment = TextAnchor.MiddleLeft;
        nameText.fontStyle = FontStyle.Bold;
        nameText.text = speakerName;

        bodyText = CreateText(parent, "Body", 30, BoxPos + new Vector2(30f, -20f), BoxSize - new Vector2(180f, 90f), darkBrown);
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.lineSpacing = 1.2f;

        nextImage = CreateImage(parent, "Next", NextPos(), new Vector2(24f, 24f), nextSprite != null ? Color.white : orange);
        nextImage.sprite = nextSprite;
    }

    private void BuildPortrait(RectTransform parent)
    {
        CreateImage(parent, "Portrait_Frame", PortraitPos, PortraitSize + new Vector2(12f, 12f), gold);
        portraitImage = CreateImage(parent, "Portrait", PortraitPos, PortraitSize, lapis);
        portraitImage.preserveAspect = true;
    }

    private static Vector2 NextPos()
    {
        return BoxPos + new Vector2(BoxSize.x * 0.5f - 44f, -BoxSize.y * 0.5f + 34f);
    }

    // ================= 도우미 함수 =================
    private static GameObject CreateStretch(string objName, RectTransform parent)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    private static Image CreateImage(RectTransform parent, string objName, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private Text CreateText(RectTransform parent, string objName, int fontSize, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Text));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}