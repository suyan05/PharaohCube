using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 유물 카드 (수첩 칸) + 카드 팝업 (기획서 3.3) + 도장 줄, 탭 빨간 점
// - 카드 칸: 얻은 카드 = 등급 색 틀 + 유물 이름 / 못 얻은 카드 = 갈색 뒷면 + "???"
// - 팝업: 큰 카드가 뒤집히며(가로로 접혔다 펴짐) 나오고, 오른쪽 "발굴 기록" 종이 판. 좌우 화살표로 같은 챕터 넘기기
// - 카드 일러스트(Common_Card_Ill_업적ID)가 오면 Resources 등으로 연결 예정 [확인 필요]
public class NotebookCardPopup : MonoBehaviour
{
    private static readonly Color Back = NotebookBuild.Hex("#6E5232");
    private static readonly Color Ink = new Color(0.29f, 0.2f, 0.14f);

    private RectTransform bigCard;
    private Image bigFrame, bigInner;
    private Text bigName, infoTitle, infoBody;
    private List<AchievementDef> list;
    private int index;
    private Action onClosed;

    public bool IsOpen => gameObject.activeSelf;
    public int ClosedFrame { get; private set; } = -1;

    // ================= 칸 카드 =================
    public static void MakeCard(RectTransform parent, AchievementDef def, Vector2 pos, Vector2 size, Action onClick)
    {
        bool got = Got(def);
        Image frame = NotebookBuild.Solid(parent, def.id, pos, size, got ? NotebookUI.GradeColor(def.grade) : Back, true);
        frame.rectTransform.localRotation = Quaternion.Euler(0f, 0f, ((def.id.GetHashCode() & 7) - 3) * 0.8f); // 테이프로 붙인 듯 살짝 기울게
        Button b = frame.gameObject.AddComponent<Button>();
        b.targetGraphic = frame;
        b.onClick.AddListener(() => onClick());

        Color inner = got ? new Color(0.16f, 0.12f, 0.09f) : new Color(0.29f, 0.2f, 0.14f);
        Image ill = NotebookBuild.Solid(frame.rectTransform, "Ill", new Vector2(0f, 30f), new Vector2(size.x - 24f, size.x - 24f), inner, false);
        Text mark = UIBuild.Text(ill.rectTransform, "Mark", PuzzleUITheme.GetBodyFont(), 60, Vector2.zero, ill.rectTransform.sizeDelta, got ? NotebookUI.GradeColor(def.grade) : NotebookBuild.Hex("#C9A66B"));
        mark.text = got ? def.relic.Substring(0, 1) : "?";
        Text name = UIBuild.Text(frame.rectTransform, "Name", PuzzleUITheme.GetBodyFont(), 20, new Vector2(0f, -size.y * 0.5f + 40f), new Vector2(size.x - 16f, 56f), got ? Ink : NotebookBuild.PageL);
        name.text = got ? def.relic : "???";
        if (got && AchievementSystem.Instance.IsNew(def.id)) Dot(frame.rectTransform, new Vector2(size.x * 0.5f - 12f, size.y * 0.5f - 12f));
    }

    public static void FillStamps(RectTransform row, int got, int total)
    {
        foreach (Transform c in row) Destroy(c.gameObject);
        float step = Mathf.Min(80f, 640f / Mathf.Max(1, total));
        for (int i = 0; i < total; i++)
        {
            bool on = i < got;
            Image s = UIBuild.Image(row, $"Stamp_{i}", new Vector2((i - (total - 1) * 0.5f) * step, 0f), Vector2.one * Mathf.Min(56f, step - 6f), null, on ? NotebookBuild.Hex("#A33A26") : NotebookBuild.Hex("#C9B48A"));
            s.sprite = UIBuild.Circle();
            if (on) UIBuild.Text(s.rectTransform, "L", PuzzleUITheme.GetBodyFont(), 16, Vector2.zero, new Vector2(56f, 56f), NotebookBuild.PageL).text = "발굴";
        }
    }

    public static void SetTabDot(Transform tab, bool on)
    {
        Transform dot = tab.Find("NewDot");
        if (dot == null && on) dot = Dot((RectTransform)tab, new Vector2(-50f, 28f)).transform;
        if (dot != null) dot.gameObject.SetActive(on);
    }

    private static Image Dot(RectTransform parent, Vector2 pos)
    {
        Image d = UIBuild.Image(parent, "NewDot", pos, new Vector2(18f, 18f), null, new Color(0.85f, 0.2f, 0.15f));
        d.sprite = UIBuild.Circle();
        return d;
    }

    private static bool Got(AchievementDef def)
    {
        return AchievementSystem.Instance != null && AchievementSystem.Instance.IsUnlocked(def.id);
    }

    // ================= 팝업 =================
    public static NotebookCardPopup Create(RectTransform root, Action closed)
    {
        RectTransform layer = UIBuild.Stretch(root, "CardPopup");
        NotebookCardPopup p = layer.gameObject.AddComponent<NotebookCardPopup>();
        p.onClosed = closed;
        p.Build(layer);
        layer.gameObject.SetActive(false);
        return p;
    }

    public void Open(List<AchievementDef> cards, int at)
    {
        list = cards;
        index = Mathf.Clamp(at, 0, cards.Count - 1);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Show();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Step(-1);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Step(1);
    }

    private void Step(int dir)
    {
        if (list == null || list.Count == 0) return;
        index = (index + dir + list.Count) % list.Count;
        Show();
    }

    private void Close()
    {
        ClosedFrame = Time.frameCount;
        gameObject.SetActive(false);
        onClosed?.Invoke();
    }

    private void Show()
    {
        AchievementDef def = list[index];
        bool got = Got(def);
        bigFrame.color = got ? NotebookUI.GradeColor(def.grade) : Back;
        bigInner.color = got ? new Color(0.16f, 0.12f, 0.09f) : new Color(0.29f, 0.2f, 0.14f);
        bigName.text = got ? def.relic : "???";
        infoTitle.text = got ? def.title : "아직 발굴하지 못했다";
        infoBody.text = got ? $"{def.text}\n\n<size=20>{GradeName(def.grade)} 등급  |  {def.chapter}</size>" : $"단서: {def.hint}";
        if (got) AchievementSystem.Instance.MarkSeen(def.id);
        StopAllCoroutines();
        StartCoroutine(Flip());
    }

    // 가로로 접혔다가 펴지며 등장 (뒤집히는 느낌)
    private IEnumerator Flip()
    {
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.4f;
            bigCard.localScale = new Vector3(Mathf.Abs(Mathf.Cos(k * Mathf.PI)), 1f, 1f) * Mathf.Lerp(0.85f, 1f, k);
            yield return null;
        }
        bigCard.localScale = Vector3.one;
    }

    private static string GradeName(AchGrade g)
    {
        return g == AchGrade.Gold ? "황금" : (g == AchGrade.Bronze ? "청동" : "파피루스");
    }

    private void Build(RectTransform layer)
    {
        NotebookBuild.Backdrop(layer, Close);
        bigFrame = NotebookBuild.Solid(layer, "BigCard", new Vector2(-240f, 0f), new Vector2(400f, 600f), Back, true);
        bigCard = bigFrame.rectTransform;
        bigInner = NotebookBuild.Solid(bigCard, "Ill", new Vector2(0f, 60f), new Vector2(350f, 350f), Ink, false);
        bigName = UIBuild.Text(bigCard, "Name", PuzzleUITheme.GetBodyFont(), 34, new Vector2(0f, -210f), new Vector2(370f, 90f), Ink);

        Image info = NotebookBuild.Solid(layer, "Info", new Vector2(300f, 0f), new Vector2(560f, 520f), NotebookBuild.Hex("#E8D9B8"), true);
        infoTitle = UIBuild.Text(info.rectTransform, "Title", PuzzleUITheme.GetBodyFont(), 34, new Vector2(0f, 200f), new Vector2(500f, 60f), Ink);
        infoBody = UIBuild.Text(info.rectTransform, "Body", PuzzleUITheme.GetBodyFont(), 26, new Vector2(0f, -40f), new Vector2(500f, 380f), Ink);
        infoBody.alignment = TextAnchor.UpperLeft;
        infoBody.supportRichText = true;
        NotebookBuild.Arrow(layer, "<", new Vector2(-500f, 0f), () => Step(-1));
        NotebookBuild.Arrow(layer, ">", new Vector2(640f, 0f), () => Step(1));
        UIBuild.Text(layer, "Help", PuzzleUITheme.GetBodyFont(), 20, new Vector2(0f, -330f), new Vector2(800f, 30f), NotebookBuild.PageL).text = "좌우: 다른 카드  |  Esc / 바깥 클릭: 닫기";
    }
}
