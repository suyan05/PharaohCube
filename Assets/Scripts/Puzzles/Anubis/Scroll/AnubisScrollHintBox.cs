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
            collection.OnCollectionReset += Refresh; // [추가] 처벌로 조각을 잃으면 다시 "조각 0 / n"
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (collection == null) return;
        collection.OnPieceCollected -= HandlePiece;
        collection.OnScrollCompleted -= HandleComplete;
        collection.OnCollectionReset -= Refresh;
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

        // [수정] 단계 공개: 모은 조각 수만큼 글이 드러남
        int got = collection.GetCount(scroll);
        string revealed = scroll.GetRevealedText(got, out bool hasMore, out int nextNeed);

        if (!hasMore)
        {
            text.text = $"<b><color=#7A4A10>[{scroll.Title}]</color></b>\n\n{revealed}";
        }
        else if (!string.IsNullOrEmpty(revealed))
        {
            text.text = $"<b><color=#7A4A10>[{scroll.Title}]</color></b>  <size=80%>조각 {got} / {scroll.PieceCount}</size>\n\n{revealed}" +
                        $"\n\n<size=85%><color=#8A6A40>조각을 {nextNeed}개 모으면 다음 글이 드러납니다.</color></size>";
        }
        else
        {
            text.text = $"<b>[{scroll.Title}]</b>\n\n양피지 조각  {got} / {scroll.PieceCount}\n\n<size=85%>방 바닥에서 <b>빛나는 양피지 조각</b>을 찾아 <b>E</b>로 주우세요.\n조각을 {nextNeed}개 모으면 이 시험을 푸는 방법이 드러나기 시작합니다.</size>";
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