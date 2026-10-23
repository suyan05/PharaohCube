using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 고고학 수첩 (J키) - 기획서 3.2
// - 펼친 가죽 수첩: 왼쪽 가장자리 챕터 탭 (프롤로그 ~ 특별 + 메모), 왼쪽 페이지 = 챕터 제목, 연구원 메모, 발굴 도장
//   오른쪽 페이지 = 유물 카드 3x2 (못 얻은 카드는 뒷면 + ???). 카드를 누르면 카드 팝업 (NotebookCardPopup)
// - "메모" 탭 = 라의 방 [노트 1~7] 단서 글 (NotebookNotes)
// - 여는 동안 게임 멈춤 (Time.timeScale = 0). J / Esc로 닫기
// - 지금은 도형과 색으로 그림. 아트(Common_UI_Book_*)가 오면 칸에 넣으면 그림으로 바뀌게 만들 예정 [확인 필요]
public class NotebookUI : MonoBehaviour
{
    public const string MemoTab = "메모";
    private const int PerPage = 6;

    [SerializeField] private int sortingOrder = 150;

    private static NotebookUI instance;
    private readonly List<Button> tabButtons = new List<Button>();
    private readonly List<string> tabNames = new List<string>();
    private GameObject canvasGo;
    private RectTransform leftPage, rightPage, cardArea;
    private Text chapterTitle, chapterMemo, chapterCount, pageText, memoText;
    private RectTransform stampRow;
    private Button prevBtn, nextBtn;
    private NotebookCardPopup popup;
    private string chapter = "라";
    private int page;
    private float savedTimeScale = 1f;

    public static bool IsOpen => instance != null && instance.canvasGo != null && instance.canvasGo.activeSelf;

    // 연출, 대사, 팝업, 퍼즐 화면, 묶여 있을 때, 플레이어가 없는 화면(메뉴)에서는 못 염
    public static bool Blocked
    {
        get
        {
            if (Cine.Running || DialogueBox.IsOpen || ItemPopup.IsShowing || PuzzleOverlayFocus.IsActive || PlayerLock.IsLocked) return true;
            PlayerController2D pc = FindFirstObjectByType<PlayerController2D>();
            return pc == null || !pc.enabled;
        }
    }

    public static Color GradeColor(AchGrade g)
    {
        switch (g)
        {
            case AchGrade.Gold: return new Color(0.91f, 0.7f, 0.23f);
            case AchGrade.Bronze: return new Color(0.6f, 0.42f, 0.23f);
            default: return new Color(0.79f, 0.65f, 0.42f);
        }
    }

    private void Awake() => instance = this;

    private void Update()
    {
        try
        {
            if (!IsOpen)
            {
                if (Input.GetKeyDown(KeyCode.J) && !Blocked) Open();
                return;
            }
            if (popup != null && (popup.IsOpen || popup.ClosedFrame == Time.frameCount)) return; // 팝업이 Esc를 먼저 받음
            if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Escape)) Close();
        }
        catch (Exception e)
        {
            Debug.LogError($"[NotebookUI] 오류: {e}");
        }
    }

    // ================= 열기 / 닫기 =================
    private void Open()
    {
        if (canvasGo == null) Build();
        savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        PlayerLock.Lock(this);
        canvasGo.SetActive(true);
        ShowChapter(chapter);
    }

    private void Close()
    {
        canvasGo.SetActive(false);
        Time.timeScale = savedTimeScale <= 0f ? 1f : savedTimeScale;
        PlayerLock.Unlock(this);
    }

    private void OnDisable()
    {
        if (IsOpen) Close();
    }

    // ================= 챕터 보여주기 =================
    private void ShowChapter(string name)
    {
        chapter = name;
        page = 0;
        RefreshTabs();
        bool memo = name == MemoTab;
        cardArea.gameObject.SetActive(!memo);
        memoText.gameObject.SetActive(memo);
        if (memo) ShowMemo();
        else ShowCards();
    }

    private void ShowCards()
    {
        List<AchievementDef> list = ChapterList(chapter);
        int got = list.FindAll(a => AchievementSystem.Instance != null && AchievementSystem.Instance.IsUnlocked(a.id)).Count;
        chapterTitle.text = chapter;
        chapterMemo.text = NotebookTexts.Memo(chapter);
        chapterCount.text = $"발굴 {got} / {list.Count}";
        NotebookCardPopup.FillStamps(stampRow, got, list.Count);
        DrawPage(list);
    }

    private void DrawPage(List<AchievementDef> list)
    {
        int pages = Mathf.Max(1, (list.Count + PerPage - 1) / PerPage);
        page = Mathf.Clamp(page, 0, pages - 1);
        foreach (Transform c in cardArea) Destroy(c.gameObject);
        for (int i = 0; i < PerPage; i++)
        {
            int index = page * PerPage + i;
            if (index >= list.Count) break;
            Vector2 pos = new Vector2(-240f + (i % 3) * 240f, i < 3 ? 175f : -175f);
            AchievementDef def = list[index];
            NotebookCardPopup.MakeCard(cardArea, def, pos, new Vector2(200f, 300f), () => popup.Open(list, list.IndexOf(def)));
        }
        pageText.text = pages > 1 ? $"{page + 1} / {pages}" : "";
        prevBtn.gameObject.SetActive(pages > 1);
        nextBtn.gameObject.SetActive(pages > 1);
    }

    private void ShowMemo()
    {
        chapterTitle.text = "연구원 메모";
        chapterMemo.text = "신전의 비석과 벽에서 읽어 낸 단서를 적어 둔다.\n밤이 되면 같은 자리의 글이 달라지기도 한다.";
        chapterCount.text = $"메모 {NotebookNotes.Count} / {NotebookNotes.RaNoteCount}";
        NotebookCardPopup.FillStamps(stampRow, NotebookNotes.Count, NotebookNotes.RaNoteCount);
        memoText.text = NotebookTexts.NotesText();
        NotebookNotes.MarkSeen();
        pageText.text = "";
        prevBtn.gameObject.SetActive(false);
        nextBtn.gameObject.SetActive(false);
    }

    private static List<AchievementDef> ChapterList(string name)
    {
        return AchievementData.All.FindAll(a => a.chapter == name);
    }

    private void RefreshTabs()
    {
        for (int i = 0; i < tabButtons.Count; i++)
        {
            bool sel = tabNames[i] == chapter;
            var rt = (RectTransform)tabButtons[i].transform;
            rt.anchoredPosition = new Vector2(sel ? -884f : -870f, rt.anchoredPosition.y);
            tabButtons[i].targetGraphic.color = sel ? new Color(0.91f, 0.7f, 0.23f) : new Color(0.85f, 0.75f, 0.56f);
            NotebookCardPopup.SetTabDot(tabButtons[i].transform, TabHasNew(tabNames[i]));
        }
    }

    private static bool TabHasNew(string tab)
    {
        if (tab == MemoTab) return NotebookNotes.HasNew;
        return AchievementSystem.Instance != null && AchievementSystem.Instance.ChapterHasNew(tab);
    }

    // ================= 화면 만들기 =================
    private void Build()
    {
        RectTransform root = UIBuild.CreateCanvas(transform, "NotebookCanvas", sortingOrder, out canvasGo);
        NotebookBuild.Backdrop(root, Close);
        RectTransform book = NotebookBuild.Book(root, out leftPage, out rightPage);
        BuildTabs(book);
        BuildLeftPage();
        BuildRightPage();
        popup = NotebookCardPopup.Create(root, () => { if (chapter != MemoTab) ShowCards(); RefreshTabs(); });
        canvasGo.SetActive(false);
    }

    private void BuildTabs(RectTransform book)
    {
        var names = new List<string>(AchievementData.Chapters) { MemoTab };
        for (int i = 0; i < names.Count; i++)
        {
            string n = names[i];
            Button b = NotebookBuild.Tab(book, n, new Vector2(-870f, 380f - i * 92f), () => ShowChapter(n));
            tabButtons.Add(b);
            tabNames.Add(n);
        }
    }

    private void BuildLeftPage()
    {
        Font title = PuzzleUITheme.GetTitleFont() != null ? PuzzleUITheme.GetTitleFont() : PuzzleUITheme.GetBodyFont();
        Font body = PuzzleUITheme.GetBodyFont();
        Color ink = new Color(0.29f, 0.2f, 0.14f);
        chapterTitle = UIBuild.Text(leftPage, "Title", title, 54, new Vector2(0f, 330f), new Vector2(680f, 80f), ink);
        chapterMemo = UIBuild.Text(leftPage, "Memo", body, 26, new Vector2(0f, 150f), new Vector2(660f, 220f), new Color(0.42f, 0.3f, 0.2f));
        chapterMemo.alignment = TextAnchor.UpperLeft;
        chapterCount = UIBuild.Text(leftPage, "Count", body, 30, new Vector2(0f, -150f), new Vector2(660f, 40f), ink);
        stampRow = UIBuild.Rect(leftPage, "Stamps", new Vector2(0f, -240f), new Vector2(660f, 120f));
        UIBuild.Text(leftPage, "Help", body, 20, new Vector2(0f, -400f), new Vector2(660f, 30f), new Color(0.55f, 0.42f, 0.28f)).text = "카드를 누르면 크게 보기  |  J / Esc : 닫기";
    }

    private void BuildRightPage()
    {
        cardArea = UIBuild.Rect(rightPage, "Cards", Vector2.zero, new Vector2(760f, 860f));
        memoText = UIBuild.Text(rightPage, "Notes", PuzzleUITheme.GetBodyFont(), 24, new Vector2(0f, 0f), new Vector2(680f, 800f), new Color(0.29f, 0.2f, 0.14f));
        memoText.alignment = TextAnchor.UpperLeft;
        pageText = UIBuild.Text(rightPage, "Page", PuzzleUITheme.GetBodyFont(), 22, new Vector2(0f, -405f), new Vector2(200f, 30f), new Color(0.42f, 0.3f, 0.2f));
        prevBtn = NotebookBuild.Arrow(rightPage, "<", new Vector2(-150f, -405f), () => { page--; ShowCards(); });
        nextBtn = NotebookBuild.Arrow(rightPage, ">", new Vector2(150f, -405f), () => { page++; ShowCards(); });
    }
}
