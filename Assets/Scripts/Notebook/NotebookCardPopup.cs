using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 유물 카드 (수첩 칸) + 카드 팝업 (기획서 3.3) + 도장 줄, 탭 빨간 점
// - 카드 칸: 얻은 카드 = 등급 색 틀 + 유물 이름 / 못 얻은 카드 = 갈색 뒷면 + "???"
// - 팝업: 큰 카드가 뒤집히며(가로로 접혔다 펴짐) 나오고, 오른쪽 "발굴 기록" 종이 판. 좌우 화살표로 같은 챕터 넘기기
// - 카드 일러스트(Common_Card_Ill_업적ID)가 오면 Resources 등으로 연결 예정 [확인 필요]
// - [수정] 등급마다 카드 틀이 다름 (NotebookCardFrame: 파피루스 / 청동 / 황금 + 뒷면). 황금은 반짝임이 지나감
public class NotebookCardPopup : MonoBehaviour
{
    private static readonly Color Back = NotebookBuild.Hex("#6E5232");
    private static readonly Color Ink = new Color(0.29f, 0.2f, 0.14f);

    private RectTransform bigCard;
    private Image bigFrame, bigInner;
    private Text bigName, bigGrade, infoTitle, infoBody;
    private Vector2 bigNamePos;
    private List<AchievementDef> list;
    private int index;
    private Action onClosed;

    public bool IsOpen => gameObject.activeSelf;
    public int ClosedFrame { get; private set; } = -1;

    // ================= 칸 카드 =================
    public static void MakeCard(RectTransform parent, AchievementDef def, Vector2 pos, Vector2 size, Action onClick)
    {
        bool got = Got(def);
        Image frame = NotebookBuild.Solid(parent, def.id, pos, size, Color.white, true);
        frame.sprite = got ? NotebookCardFrame.For(def.grade) : NotebookCardFrame.Back(); // [수정] 등급별 카드 틀
        frame.rectTransform.localRotation = Quaternion.Euler(0f, 0f, ((def.id.GetHashCode() & 7) - 3) * 0.8f); // 테이프로 붙인 듯 살짝 기울게
        Button b = frame.gameObject.AddComponent<Button>();
        b.targetGraphic = frame;
        b.onClick.AddListener(() => onClick());
        if (got) CardFace(frame, def, size);
        else UIBuild.Text(frame.rectTransform, "Mark", PuzzleUITheme.GetBodyFont(), 40, new Vector2(0f, -size.y * 0.3f), new Vector2(size.x, 60f), NotebookBuild.PageL).text = "???";
        if (got && def.grade == AchGrade.Gold) NotebookCardFrame.AddShine(frame);
        if (got && AchievementSystem.Instance.IsNew(def.id)) Dot(frame.rectTransform, new Vector2(size.x * 0.5f - 12f, size.y * 0.5f - 12f));
    }

    // [추가] 얻은 카드 앞면: 창(그림) + 이름판 + 위쪽 등급 글자
    private static void CardFace(Image frame, AchievementDef def, Vector2 size)
    {
        Font font = PuzzleUITheme.GetBodyFont();
        NotebookCardFrame.Window(size, out Vector2 wc, out Vector2 ws);
        Image ill = NotebookBuild.Solid(frame.rectTransform, "Ill", wc, ws, Color.clear, false);
        Sprite art = NotebookCardArt.Of(def.id);
        if (art != null) { ill.sprite = art; ill.color = Color.white; ill.preserveAspect = false; }
        else UIBuild.Text(ill.rectTransform, "Mark", font, 60, Vector2.zero, ws, NotebookCardFrame.NameColor(AchGrade.Gold)).text = def.relic.Substring(0, 1);
        NotebookCardFrame.Plaque(size, out Vector2 pc, out Vector2 ps);
        Text name = UIBuild.Text(frame.rectTransform, "Name", font, 18, pc, ps - new Vector2(10f, 4f), NotebookCardFrame.NameColor(def.grade));
        name.text = def.relic;
        name.resizeTextForBestFit = true;
        name.resizeTextMinSize = 12;
        name.resizeTextMaxSize = 18;
        Text grade = UIBuild.Text(frame.rectTransform, "Grade", font, 13, NotebookCardFrame.Band(size), new Vector2(size.x - 30f, 20f), NotebookCardFrame.LabelColor(def.grade));
        grade.text = NotebookCardFrame.Label(def.grade);
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
        if (Input.GetKeyDown(KeyCode.E)) Close(); // [수정] ESC는 설정 창 키 -> 닫기는 E로 통일
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
        bigFrame.color = Color.white;
        bigFrame.sprite = got ? NotebookCardFrame.For(def.grade) : NotebookCardFrame.Back(); // [수정] 등급별 카드 틀
        Sprite art = got ? NotebookCardArt.Of(def.id) : null;
        bigInner.sprite = art;
        bigInner.color = art != null ? Color.white : Color.clear;
        bigName.text = got ? def.relic : "???";
        bigName.color = got ? NotebookCardFrame.NameColor(def.grade) : NotebookBuild.PageL;
        bigName.rectTransform.anchoredPosition = got ? bigNamePos : new Vector2(0f, -180f);
        bigGrade.text = got ? NotebookCardFrame.Label(def.grade) : "";
        bigGrade.color = NotebookCardFrame.LabelColor(def.grade);
        SetBigShine(got && def.grade == AchGrade.Gold);
        infoTitle.text = got ? def.title : "아직 발굴하지 못했다";
        infoBody.text = got ? $"{def.text}\n\n<size=22><color={NotebookCardFrame.HexOf(def.grade)}><b>{GradeName(def.grade)} 등급</b></color>  |  {def.chapter}</size>" : $"단서: {def.hint}";
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

    // [추가] 큰 카드 반짝임 (황금일 때만 켬)
    private void SetBigShine(bool on)
    {
        Transform s = bigCard.Find("Shine");
        if (on && s == null) NotebookCardFrame.AddShine(bigFrame);
        else if (s != null) s.gameObject.SetActive(on);
    }

    private void Build(RectTransform layer)
    {
        NotebookBuild.Backdrop(layer, Close);
        Vector2 card = new Vector2(400f, 600f);
        bigFrame = NotebookBuild.Solid(layer, "BigCard", new Vector2(-240f, 0f), card, Back, true);
        bigCard = bigFrame.rectTransform;
        NotebookCardFrame.Window(card, out Vector2 wc, out Vector2 ws);
        bigInner = NotebookBuild.Solid(bigCard, "Ill", wc, ws, Ink, false);
        NotebookCardFrame.Plaque(card, out Vector2 pc, out Vector2 ps);
        bigNamePos = pc;
        bigName = UIBuild.Text(bigCard, "Name", PuzzleUITheme.GetBodyFont(), 30, pc, ps - new Vector2(20f, 8f), Ink);
        bigName.resizeTextForBestFit = true;
        bigName.resizeTextMinSize = 18;
        bigName.resizeTextMaxSize = 30;
        bigGrade = UIBuild.Text(bigCard, "Grade", PuzzleUITheme.GetBodyFont(), 24, NotebookCardFrame.Band(card), new Vector2(340f, 36f), Ink);

        Image info = NotebookBuild.Solid(layer, "Info", new Vector2(300f, 0f), new Vector2(560f, 520f), NotebookBuild.Hex("#E8D9B8"), true);
        infoTitle = UIBuild.Text(info.rectTransform, "Title", PuzzleUITheme.GetBodyFont(), 34, new Vector2(0f, 200f), new Vector2(500f, 60f), Ink);
        infoBody = UIBuild.Text(info.rectTransform, "Body", PuzzleUITheme.GetBodyFont(), 26, new Vector2(0f, -40f), new Vector2(500f, 380f), Ink);
        infoBody.alignment = TextAnchor.UpperLeft;
        infoBody.supportRichText = true;
        NotebookBuild.Arrow(layer, "<", new Vector2(-500f, 0f), () => Step(-1));
        NotebookBuild.Arrow(layer, ">", new Vector2(640f, 0f), () => Step(1));
        UIBuild.Text(layer, "Help", PuzzleUITheme.GetBodyFont(), 20, new Vector2(0f, -330f), new Vector2(800f, 30f), NotebookBuild.PageL).text = "좌우: 다른 카드  |  E / 바깥 클릭: 닫기";
    }
}
