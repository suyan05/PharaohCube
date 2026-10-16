using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 메인 저울(아누비스의 심판) 화면에 '사자의 서' 비문을 바로 보여 주는 판 (UI는 실행 시 생성)
// 피드백 대응
// - "심판 설명이 Tab(사자의 서)에 있는 걸 몰랐다" -> 저울 화면 안에 비문을 그대로 적음
// - "A~H가 자연수라는 걸 알려 줘야 한다" -> 판 위쪽에 보물 기호표 + 자연수 안내
// Panel_MainScale(AnubisPuzzleManager2D가 붙은 패널)에 부착
public class AnubisScaleClueBoard : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("한글 TMP 폰트 (MonoplexKR-Regular SDF)")]
    [SerializeField] private TMP_FontAsset font;

    [Header("위치 (패널 중심 기준 px, 1920 x 1080 기준)")]
    [Tooltip("[수정] 저울 왼쪽 접시, 안내 글과 겹치지 않게 왼쪽 여백 안으로")]
    [SerializeField] private Vector2 position = new Vector2(-665, 20);
    [SerializeField] private Vector2 size = new Vector2(330, 560);

    [Header("문구")]
    [SerializeField] private string title = "사자의 서";
    [TextArea(2, 4)]
    [SerializeField] private string legend = "H 심장 · A 황금 단지 · B 청동 그릇\nC 흙의 단지 · D 흑요석 추";
    [TextArea(1, 3)]
    [SerializeField] private string naturalNumberNote = "※ 모든 무게는 자연수(1, 2, 3 … kg)";
    [TextArea(1, 2)]
    [SerializeField] private string lockedClue = "(아직 해독되지 않음)";
    [TextArea(1, 3)]
    [SerializeField] private string goalText = "깃털과 같은 무게만큼 보물을 올려라.";

    private TextMeshProUGUI text;
    private AnubisNotebookManager notebook;
    private readonly StringBuilder sb = new StringBuilder(512); // 갱신 때마다 재사용

    private void Awake()
    {
        Build();
    }

    private void OnEnable()
    {
        notebook = AnubisNotebookManager.Instance;
        if (notebook != null) notebook.OnClueUnlocked += HandleClue;
        else Debug.LogWarning($"[{name}] AnubisNotebookManager가 없어 비문을 표시할 수 없음", this);
        Refresh();
    }

    private void OnDisable()
    {
        if (notebook != null) notebook.OnClueUnlocked -= HandleClue;
    }

    private void HandleClue(int index, string clue) => Refresh();

    private void Refresh()
    {
        if (text == null) return;

        sb.Clear();
        sb.Append("<b><color=#7A4A10>[").Append(title).Append("]</color></b>\n");
        sb.Append("<size=80%>").Append(legend).Append("</size>\n");
        sb.Append("<size=85%><color=#8A2E1F>").Append(naturalNumberNote).Append("</color></size>\n\n");

        int count = notebook != null ? notebook.ClueCount : 4;
        for (int i = 0; i < count; i++)
        {
            string clue = notebook != null ? notebook.GetClue(i) : null;
            sb.Append("<b>비문 ").Append(i + 1).Append("</b>  ");
            if (string.IsNullOrEmpty(clue)) sb.Append("<color=#9A8A70>").Append(lockedClue).Append("</color>");
            else sb.Append(clue);
            sb.Append('\n');
        }
        sb.Append('\n');

        sb.Append("<size=90%><b>").Append(goalText).Append("</b></size>");
        text.text = sb.ToString();
    }

    private void Build()
    {
        var go = new GameObject("ScaleClueBoard", typeof(RectTransform), typeof(Image));
        go.layer = 5; // UI
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        rt.SetSiblingIndex(Mathf.Min(1, transform.childCount - 1)); // 배경 바로 위, 유물보다 아래

        var bg = go.GetComponent<Image>();
        bg.color = new Color(0.85f, 0.76f, 0.58f, 0.96f); // 양피지 색 (양피지 힌트 상자와 같음)
        bg.raycastTarget = false; // 유물 드래그를 막지 않게

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.layer = 5;
        var trt = (RectTransform)textGo.transform;
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(18, 16);
        trt.offsetMax = new Vector2(-18, -16);

        text = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.color = new Color(0.25f, 0.17f, 0.08f, 1f);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12;
        text.fontSizeMax = 20;
        text.raycastTarget = false;
    }

    [ContextMenu("테스트: 비문 4개 채워 보기 (Play 중)")]
    private void TestFillClues()
    {
        if (!Application.isPlaying || AnubisNotebookManager.Instance == null) return;
        var nb = AnubisNotebookManager.Instance;
        nb.UnlockClue(0, "가장 비천한 흙의 단지(C)에 태양의 황금(A)을 더해야만 심장(H)의 무게를 얻으리라. (C + A = H)");
        nb.UnlockClue(1, "청동의 그릇(B)과 흙(C)의 무게 합은, 밤의 흑요석(D)과 황금(A)의 합보다 1kg 더 무겁도다. ((B + C) - (D + A) = 1)");
        nb.UnlockClue(2, "망자의 심장(H)은 밤의 흑요석 추(D) 두 개의 무게와 정확히 같도다. (H = 2D)");
        nb.UnlockClue(3, "청동의 그릇(B)은 흙의 단지(C) 다섯 개의 무게와 같도다. (B = 5C)");
    }

    // Scene 뷰에서는 실행 전이라 판이 안 보이므로 자리만 표시 (패널 RectTransform 기준)
    private void OnDrawGizmosSelected()
    {
        var parent = transform as RectTransform;
        if (parent == null) return;
        Vector3 center = parent.TransformPoint(position);
        Vector3 s = parent.TransformVector(size);
        Gizmos.color = new Color(0.85f, 0.76f, 0.58f, 0.9f);
        Gizmos.DrawWireCube(center, new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), 0f));
    }
}
