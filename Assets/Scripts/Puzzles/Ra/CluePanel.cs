using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 비석 단서 화면 (씬에 1개만, RaCanvas 아래 화면 전체 크기 빈 오브젝트에 붙임)
// - 처음 열 때 UI를 코드로 만들고, 모든 비석이 이 화면 하나를 같이 씀
// - 삽화가 있으면 위에 크게 + 글은 아래, 삽화가 없으면 글만 가운데 크게
// - E(비석에서) 또는 Esc로 닫기
[RequireComponent(typeof(RectTransform))]
public class CluePanel : MonoBehaviour
{
    private static readonly Vector2 FrameSize = new Vector2(1280f, 800f);
    private static readonly Vector2 ImageSize = new Vector2(1060f, 440f);
    private static readonly Vector2 ImagePos = new Vector2(0f, 40f);

    [Header("아트 (비우면 코드로 그린 틀)")]
    [Tooltip("UI_Journal_Frame (9-slice로 잘려 있으면 늘려도 안 깨짐)")]
    [SerializeField] private Sprite frameSprite;
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.8f);

    [Header("열려 있는 동안 투명하게 숨길 UI (목표 문구 등)")]
    [SerializeField] private GameObject[] hideWhileOpen;

    private readonly Dictionary<CanvasGroup, float> hiddenGroups = new Dictionary<CanvasGroup, float>();
    private GameObject root;
    private Text titleText;
    private Text bodyText;
    private Image illustration;
    private Font font;
    private Color paper, darkBrown, sand, sandDark;

    private ClueStele owner;
    private PlayerController2D playerController;
    private Rigidbody2D playerRb;

    public bool IsOpen => root != null && root.activeSelf;
    public bool IsOpenFor(ClueStele stele) => IsOpen && owner == stele;

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Hide();
    }

    private void OnDisable()
    {
        if (IsOpen) Hide();
    }

    // ================= 열기 / 닫기 =================
    public void Show(ClueData.ClueContent content, ClueStele from, GameObject player)
    {
        try
        {
            EnsureBuilt();
            owner = from;
            Fill(content);

            LockPlayer(player);
            SetOtherUIHidden(true);
            transform.SetAsLastSibling(); // 다른 퍼즐 화면보다 위에
            root.SetActive(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 단서 화면 열기 오류: {e}");
        }
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
        owner = null;
        SetOtherUIHidden(false);
        if (playerController != null) playerController.enabled = true;
    }

    private void LockPlayer(GameObject player)
    {
        if (player == null) return;

        playerController = player.GetComponent<PlayerController2D>();
        playerRb = player.GetComponent<Rigidbody2D>();
        if (playerController != null) playerController.enabled = false;
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;
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

    // ================= 내용 채우기 =================
    private void Fill(ClueData.ClueContent content)
    {
        if (content == null) content = new ClueData.ClueContent();

        titleText.text = content.title;
        bodyText.text = content.body;

        bool hasImage = content.illustration != null;
        illustration.enabled = hasImage;
        illustration.sprite = content.illustration;
        illustration.color = content.illustrationTint;

        // 삽화가 있으면 글은 아래쪽 작게, 없으면 가운데 크게
        RectTransform rt = bodyText.rectTransform;
        rt.anchoredPosition = hasImage ? new Vector2(0f, -270f) : new Vector2(0f, -20f);
        rt.sizeDelta = hasImage ? new Vector2(1060f, 130f) : new Vector2(1000f, 520f);
        bodyText.fontSize = hasImage ? 28 : 34;
    }

    // ================= 화면 만들기 (처음 한 번) =================
    private void EnsureBuilt()
    {
        if (root != null) return;

        SetupColors();
        font = PuzzleUITheme.GetBodyFont();
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        root = CreateStretch("ClueRoot", (RectTransform)transform);
        RectTransform rootRt = (RectTransform)root.transform;

        BuildBackdrop(rootRt);
        BuildFrame(rootRt);
        BuildContents(rootRt);
        root.SetActive(false);
    }

    private void SetupColors()
    {
        paper = Hex("#F6F1E7");
        darkBrown = Hex("#4A3423");
        sand = Hex("#E8D9B8");
        sandDark = Hex("#C9A66B");
    }

    // 화면 전체 어둡게 + 뒤쪽 클릭 막기
    private void BuildBackdrop(RectTransform parent)
    {
        GameObject bg = CreateStretch("Backdrop", parent);
        Image img = bg.AddComponent<Image>();
        img.color = backdropColor;
        img.raycastTarget = true;
    }

    private void BuildFrame(RectTransform parent)
    {
        if (frameSprite != null)
        {
            Image frame = CreateImage(parent, "Frame", Vector2.zero, FrameSize, Color.white);
            frame.sprite = frameSprite;
            frame.type = frameSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            return;
        }

        // 그림이 없으면: 사암 테두리 + 파피루스 안쪽
        CreateImage(parent, "Frame_Border", Vector2.zero, FrameSize, sandDark);
        CreateImage(parent, "Frame_Inner", Vector2.zero, FrameSize - new Vector2(40f, 40f), paper);
    }

    private void BuildContents(RectTransform parent)
    {
        titleText = CreateText(parent, "Title", 40, new Vector2(0f, 330f), new Vector2(1100f, 60f), darkBrown);
        titleText.fontStyle = FontStyle.Bold;

        illustration = CreateImage(parent, "Illustration", ImagePos, ImageSize, Color.white);
        illustration.preserveAspect = true;
        illustration.enabled = false;

        bodyText = CreateText(parent, "Body", 28, new Vector2(0f, -270f), new Vector2(1060f, 130f), darkBrown);
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        bodyText.lineSpacing = 1.25f;

        CreateText(parent, "Help", 22, new Vector2(0f, -450f), new Vector2(800f, 36f), sand).text = "E / Esc : 닫기";
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