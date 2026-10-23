using System;
using UnityEngine;
using UnityEngine.UI;

// 퍼즐 화면 오른쪽 위 힌트 버튼 (우제트의 눈, 아트 요청서 D)
// - 퍼즐 오버레이(P1_Overlay 등) 안에 빈 오브젝트를 만들고 붙임 -> 처음 켜질 때 버튼을 코드로 만듦
// - Provider 칸에 IPuzzleHintProvider를 가진 퍼즐을 넣음 (ShadowClockPuzzle, EclipsePuzzle 등)
// - 퍼즐이 "지금 힌트 가능"일 때만 보임 (낮에는 자동 힌트라 안 보임)
// - 누르면 힌트 문구 표시 + 쿨타임 (회색 + 시계 방향으로 줄어드는 게이지)
[RequireComponent(typeof(RectTransform))]
public class PuzzleHintButton : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("힌트를 주는 퍼즐 (IPuzzleHintProvider를 가진 컴포넌트)")]
    [SerializeField] private MonoBehaviour provider;

    [Header("아트 (UI_Hint_*)")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite disabledSprite;

    [Header("튜닝")]
    [Tooltip("힌트를 쓴 뒤 다시 쓸 수 있을 때까지 (초)")]
    [SerializeField, Min(0f)] private float cooldown = 45f;
    [Tooltip("힌트 문구가 떠 있는 시간 (초)")]
    [SerializeField, Min(0.5f)] private float messageTime = 6f;

    [Header("배치 (1920x1080 기준, 화면 가운데가 0,0)")]
    [SerializeField] private Vector2 buttonPos = new Vector2(820f, 420f);
    [SerializeField] private float buttonSize = 96f;
    [SerializeField] private Vector2 messagePos = new Vector2(0f, 395f);

    private IPuzzleHintProvider hint;
    private GameObject buttonRoot;
    private Button button;
    private Image gauge;
    private GameObject messageBox;
    private Text messageText;
    private Font font;
    private float nextReadyTime;
    private float messageHideTime;

    private bool CoolingDown => Time.time < nextReadyTime;

    // ================= 초기화 =================
    // 오버레이가 꺼진 채로 시작하면 Awake가 늦게 불리니까 OnEnable에서 만듦
    private void OnEnable()
    {
        try
        {
            if (!EnsureBuilt()) return;
            messageBox.SetActive(false);
            Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 힌트 버튼 초기화 오류: {e}");
            enabled = false;
        }
    }

    // [추가] 문구 상자는 화면 맨 위 캔버스로 옮겨서, 퍼즐 화면이 닫힐 때 같이 숨겨야 함
    private void OnDisable()
    {
        if (messageBox != null) messageBox.SetActive(false);
    }

    private bool EnsureBuilt()
    {
        if (buttonRoot != null) return true;

        hint = provider as IPuzzleHintProvider;
        if (hint == null)
        {
            Debug.LogError($"[{name}] Provider 칸에 힌트를 줄 수 있는 퍼즐(IPuzzleHintProvider)을 넣어야 함");
            enabled = false;
            return false;
        }

        font = PuzzleUITheme.GetBodyFont();
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildButton();
        BuildMessage();
        return true;
    }

    private void Update()
    {
        try
        {
            Refresh();
            if (messageBox.activeSelf && Time.time >= messageHideTime) messageBox.SetActive(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 힌트 버튼 갱신 오류: {e}");
            enabled = false; // 매 프레임 같은 오류 도배 방지
        }
    }

    // 보일지 / 누를 수 있는지 / 쿨타임 게이지
    private void Refresh()
    {
        bool show = hint.HintAvailable;
        if (buttonRoot.activeSelf != show) buttonRoot.SetActive(show);
        if (!show) return;

        bool cooling = CoolingDown;
        button.interactable = !cooling;
        gauge.enabled = cooling;
        if (cooling) gauge.fillAmount = cooldown > 0f ? (nextReadyTime - Time.time) / cooldown : 0f;
    }

    // ================= 클릭 =================
    private void OnClicked()
    {
        try
        {
            if (CoolingDown || !hint.HintAvailable) return;

            string message = hint.RequestHint();
            if (string.IsNullOrEmpty(message)) return;

            nextReadyTime = Time.time + cooldown;
            ShowMessage(message);
            MarkHintUsed();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 힌트 실행 오류: {e}");
        }
    }

    // [추가] 힌트를 한 번이라도 쓰면 기록 (업적 '스스로 읽은 자' 판정용)
    public const string HintUsedFlag = "F_HINT_USED";

    private static void MarkHintUsed()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && !gm.HasFlag(HintUsedFlag)) gm.SetFlag(HintUsedFlag);
    }

    private void ShowMessage(string message)
    {
        messageText.text = message;
        messageBox.SetActive(true);
        messageBox.transform.SetAsLastSibling();
        messageHideTime = Time.time + messageTime;
    }

    // ================= 화면 만들기 =================
    private void BuildButton()
    {
        Image img = CreateImage((RectTransform)transform, "HintButton", buttonPos, Vector2.one * buttonSize, Color.white);
        img.sprite = normalSprite;
        if (normalSprite == null) img.color = Hex("#E8B23A");
        img.raycastTarget = true;
        buttonRoot = img.gameObject;

        button = buttonRoot.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(OnClicked);
        ApplyTransition();

        gauge = CreateImage(img.rectTransform, "Cooldown", Vector2.zero, Vector2.one * buttonSize, new Color(0f, 0f, 0f, 0.55f));
        gauge.sprite = disabledSprite != null ? disabledSprite : normalSprite;
        gauge.type = Image.Type.Filled;
        gauge.fillMethod = Image.FillMethod.Radial360;
        gauge.fillOrigin = (int)Image.Origin360.Top;
        gauge.fillClockwise = false;
        gauge.enabled = false;

        CreateText(img.rectTransform, "힌트", 20, new Vector2(0f, -buttonSize * 0.5f - 16f), new Vector2(120f, 28f), Hex("#E8D9B8"));
    }

    // 그림이 있으면 마우스 올림/쿨타임 그림으로 바꾸고, 없으면 색으로
    private void ApplyTransition()
    {
        if (normalSprite == null)
        {
            button.transition = Selectable.Transition.ColorTint;
            return;
        }

        button.transition = Selectable.Transition.SpriteSwap;
        SpriteState state = new SpriteState
        {
            highlightedSprite = hoverSprite,
            selectedSprite = normalSprite,
            pressedSprite = hoverSprite,
            disabledSprite = disabledSprite
        };
        button.spriteState = state;
    }

    // [수정] 힌트 문구가 퍼즐 그림 뒤에 가려지던 문제:
    //  문구 상자에 따로 Canvas를 달아 퍼즐 화면보다 항상 위에 그림 (대사창보다는 아래)
    //  + 금색 테두리 + 위치를 퍼즐 제목 바로 아래 가운데로 고정
    private void BuildMessage()
    {
        Image border = CreateImage((RectTransform)transform, "HintMessage", Vector2.zero, new Vector2(1120f, 84f), Hex("#E8B23A"));
        border.preserveAspect = false;
        messageBox = border.gameObject;
        PinToScreenTop(border.rectTransform);

        Canvas canvas = messageBox.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 98;

        Image box = CreateImage(border.rectTransform, "Inner", Vector2.zero, new Vector2(1112f, 76f), new Color(0.1f, 0.07f, 0.04f, 0.96f));
        box.preserveAspect = false;
        messageText = CreateText(box.rectTransform, "", 26, Vector2.zero, new Vector2(1070f, 70f), Hex("#E8B23A"));
        messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    // 버튼이 어디에 붙어 있든 화면 위 가운데 (위에서 150px)
    private void PinToScreenTop(RectTransform rt)
    {
        Canvas root = GetComponentInParent<Canvas>();
        if (root != null) rt.SetParent(root.rootCanvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -150f);
    }

    // ================= 도우미 함수 =================
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
        img.preserveAspect = true;
        return img;
    }

    private Text CreateText(RectTransform parent, string text, int fontSize, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = text;
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