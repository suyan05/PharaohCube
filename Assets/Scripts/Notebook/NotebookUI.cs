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
            if (Input.GetKeyDown(KeyCode.J)) { Close(); return; } // [수정] ESC는 설정 창 키 -> 수첩은 J로만 여닫음
            // [추가] 좌우 방향키 / A, D 로 쪽 넘기기 (카드, 메모 공통)
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) TurnPage(-1);
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) TurnPage(1);
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
        prevBtn.interactable = page > 0;          // [추가] 첫 쪽/끝 쪽에서는 화살표 흐리게
        nextBtn.interactable = page < pages - 1;
    }

    private void ShowMemo()
    {
        chapterTitle.text = "연구원 메모";
        chapterMemo.text = "신전의 비석과 벽에서 읽어 낸 단서를 적어 둔다.\n밤이 되면 같은 자리의 글이 달라지기도 한다.";
        // [수정] "메모 12 / 7" 처럼 라의 방 개수(7)를 넘던 표시 -> 모은 메모 개수만 표시 (방마다 메모 수가 달라서)
        chapterCount.text = $"모은 메모 {NotebookNotes.Count}개";
        NotebookCardPopup.FillStamps(stampRow, 0, 0); // 도장 줄은 메모 탭에서 비움
        // [수정] 한 쪽에 다 안 들어가면 다음 쪽으로 나눔
        List<string> pages = NotebookTexts.NotesPages(MeasureMemo, MemoMaxHeight);
        page = Mathf.Clamp(page, 0, pages.Count - 1);
        memoText.text = pages[page];
        NotebookNotes.MarkSeen();
        pageText.text = pages.Count > 1 ? $"{page + 1} / {pages.Count}" : "";
        prevBtn.gameObject.SetActive(pages.Count > 1);
        nextBtn.gameObject.SetActive(pages.Count > 1);
        prevBtn.interactable = page > 0;
        nextBtn.interactable = page < pages.Count - 1;
    }

    // [추가] 메모 칸 높이 (아래쪽 쪽 번호와 겹치지 않게 칸 높이보다 조금 작게)
    private const float MemoMaxHeight = 740f;

    // [추가] 이 글을 메모 칸에 넣었을 때 높이 (줄바꿈, 글꼴 크기 그대로 계산)
    private float MeasureMemo(string text)
    {
        TextGenerationSettings set = memoText.GetGenerationSettings(new Vector2(memoText.rectTransform.rect.width, 0f));
        set.verticalOverflow = VerticalWrapMode.Overflow;
        return memoText.cachedTextGeneratorForLayout.GetPreferredHeight(text, set) / memoText.pixelsPerUnit;
    }

    // [추가] 쪽 넘기기 (카드 탭은 카드 쪽, 메모 탭은 메모 쪽). 끝에서는 더 안 넘어감
    private void TurnPage(int dir)
    {
        int before = page;
        page = Mathf.Max(0, page + dir);
        if (chapter == MemoTab) ShowMemo();
        else ShowCards();
        if (page != before) Debug.Log($"[NotebookUI] {chapter} {page + 1}쪽");
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
        UIBuild.Text(leftPage, "Help", body, 20, new Vector2(0f, -400f), new Vector2(660f, 30f), new Color(0.55f, 0.42f, 0.28f)).text = "카드를 누르면 크게 보기  |  좌우 : 쪽 넘기기  |  J : 닫기"; // [수정] 쪽 넘기기 안내
    }

    private void BuildRightPage()
    {
        cardArea = UIBuild.Rect(rightPage, "Cards", Vector2.zero, new Vector2(760f, 860f));
        memoText = UIBuild.Text(rightPage, "Notes", PuzzleUITheme.GetBodyFont(), 24, new Vector2(0f, 0f), new Vector2(680f, 800f), new Color(0.29f, 0.2f, 0.14f));
        memoText.alignment = TextAnchor.UpperLeft;
        memoText.supportRichText = true;
        memoText.verticalOverflow = VerticalWrapMode.Truncate; // [추가] 혹시 넘쳐도 쪽 번호 위로 삐져나오지 않게
        pageText = UIBuild.Text(rightPage, "Page", PuzzleUITheme.GetBodyFont(), 22, new Vector2(0f, -405f), new Vector2(200f, 30f), new Color(0.42f, 0.3f, 0.2f));
        // [수정] 카드 쪽과 메모 쪽을 같은 화살표로 넘김
        prevBtn = NotebookBuild.Arrow(rightPage, "<", new Vector2(-150f, -405f), () => TurnPage(-1));
        nextBtn = NotebookBuild.Arrow(rightPage, ">", new Vector2(150f, -405f), () => TurnPage(1));
    }
}
