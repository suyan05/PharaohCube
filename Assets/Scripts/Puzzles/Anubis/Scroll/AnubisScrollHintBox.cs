using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 퍼즐 패널 안에 '양피지 힌트' 상자를 표시 (UI는 실행 시 생성)
// 조각을 다 모으기 전: "조각 n / 5" 안내 / 다 모으면: 푸는 방법 글
public class AnubisScrollHintBox : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private AnubisScrollData scroll;
    [Tooltip("비우면 자동으로 찾음")]
    [SerializeField] private AnubisScrollCollection collection;
    [SerializeField] private TMP_FontAsset font;

    [Header("위치 (패널 중심 기준 px)")]
    [SerializeField] private Vector2 position = new Vector2(-560, -20);
    [SerializeField] private Vector2 size = new Vector2(500, 660);

    private TextMeshProUGUI text;

    private void Awake()
    {
        Build();
    }

    private void OnEnable()
    {
        if (collection == null) collection = FindFirstObjectByType<AnubisScrollCollection>();
        if (collection != null)
        {
            collection.OnPieceCollected += HandlePiece;
            collection.OnScrollCompleted += HandleComplete;
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (collection == null) return;
        collection.OnPieceCollected -= HandlePiece;
        collection.OnScrollCompleted -= HandleComplete;
    }

    private void HandlePiece(AnubisScrollData s, int index, int count) { if (s == scroll) Refresh(); }
    private void HandleComplete(AnubisScrollData s) { if (s == scroll) Refresh(); }

    private void Refresh()
    {
        if (text == null) return;
        if (scroll == null || collection == null)
        {
            text.text = "";
            return;
        }

        if (collection.IsComplete(scroll))
        {
            text.text = $"<b><color=#7A4A10>[{scroll.Title}]</color></b>\n\n{scroll.FullText}";
        }
        else
        {
            int got = collection.GetCount(scroll);
            text.text = $"<b>[{scroll.Title}]</b>\n\n양피지 조각  {got} / {scroll.PieceCount}\n\n<size=85%>이 방 어딘가에 흩어진 조각을 모두 모으면\n이 시험을 푸는 방법이 드러납니다.</size>";
        }
    }

    private void Build()
    {
        var go = new GameObject("ScrollHintBox", typeof(RectTransform), typeof(Image));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        rt.SetSiblingIndex(Mathf.Min(1, transform.childCount - 1));

        var bg = go.GetComponent<Image>();
        bg.color = new Color(0.85f, 0.76f, 0.58f, 0.95f);
        bg.raycastTarget = false;

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.layer = 5;
        var trt = (RectTransform)textGo.transform;
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(26, 22);
        trt.offsetMax = new Vector2(-26, -22);

        text = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.color = new Color(0.25f, 0.17f, 0.08f, 1f);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16;
        text.fontSizeMax = 26;
        text.raycastTarget = false;
    }
}