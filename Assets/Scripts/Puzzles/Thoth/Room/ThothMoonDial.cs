using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 달의 시계 (홀 가운데, 천창 달빛 아래)
// - 서기관 책상을 바로잡기 전엔 잠김 (무엇을 가리킬지 모름)
// - 화면: 구멍 8개 원판. 맨 위(0번)엔 그믐(빛 없는 달)이 이미 끼워져 있음
//   [수정] 오른쪽 달 그림을 누르면 바로 원판의 다음 빈 구멍(위에서 시계 방향)에 쏙 들어감. 끼운 달을 누르면 빠짐
//   (예전: 달을 고르고 구멍을 또 눌러야 해서 들어갔는지 알기 어려웠음)
//   단서(비석): 달은 오른쪽부터 차서, 둥글어진 뒤, 오른쪽부터 닳는다 / 시계 방향으로 자란다
//   정답(위에서 시계 방향): 그믐5 -> 초승6 -> 상현7 -> 차오름8 -> 보름1 -> 기욺2 -> 하현3 -> 그믐달4
// - 다 끼우면 바늘(< >)로 '내기의 밤' = 왼쪽 반달(하현, 6번 구멍)을 가리키고 '맞추기'
// - 틀리면 "n/7개가 제자리" 알려줌. 맞으면 F_TH_DIAL_DONE -> ThothDirector가 달빛 연출
public class ThothMoonDial : ThothPanel, IInteractable
{
    private const float Radius = 204f;
    private const int NeedleAnswer = 6;
    private static readonly int[] Answer = { 5, 6, 7, 8, 1, 2, 3, 4 };
    private static readonly int[] TrayOrder = { 3, 8, 1, 6, 4, 7, 2 };
    private static readonly Vector2 Center = new Vector2(-220f, -20f);

    [SerializeField] private SpriteRenderer art;   // 월드 그림 (잠김 / 깨어남)

    private readonly int[] holes = new int[8];
    private readonly Image[] holeIcons = new Image[8];
    private readonly Image[] trayIcons = new Image[7];
    private Image plate, needle;
    private Text message;
    private int needleStep;
    private bool solving, busy;

    protected override bool CanClose => !solving;

    private void Start()
    {
        holes[0] = Answer[0];
        RefreshWorld();
    }

    public void RefreshWorld()
    {
        if (art == null) return;
        Sprite s = ThothState.Art(ThothState.Has(ThothIds.DialDone) ? "World/TH_Dial_Active" : "World/TH_Dial_Locked");
        if (s != null) art.sprite = s;
    }

    public string GetPrompt() => ThothState.Has(ThothIds.DialDone) ? "E: 달 시계 보기" : "E: 달 시계 맞추기";

    public void Interact(GameObject who)
    {
        try
        {
            if (busy || IsOpen || ThothState.Busy) return;
            if (ThothState.Has(ThothIds.DialDone)) Cine.Run(Talk(ThothLines.DialAgain));
            else if (!ThothState.Has(ThothIds.DeskDone)) Cine.Run(Talk(ThothLines.DialLocked));
            else OpenPanel();
        }
        catch (Exception e)
        {
            Debug.LogError($"[달 시계] Interact 오류: {e}");
        }
    }

    private IEnumerator Talk(DialogueLine[] lines)
    {
        busy = true;
        Cine.Begin(false);
        yield return Cine.Say(lines);
        Cine.End();
        busy = false;
    }

    // ================= 화면 만들기 =================
    protected override void Build()
    {
        ThothUI.Label(root, "Title", font, 46, new Vector2(0f, 480f), new Vector2(1600f, 60f), ThothUI.Gold, ThothLines.DialTitle);
        plate = ThothUI.Pic(root, "Plate", "UI/UI_Dial_Locked", Center, new Vector2(625f, 625f));
        for (int i = 0; i < 8; i++) BuildHole(i);
        needle = ThothUI.Pic(root, "Needle", "UI/UI_Needle", Center, new Vector2(70f, 220f));
        needle.rectTransform.pivot = new Vector2(0.5f, 0.12f);
        needle.rectTransform.anchoredPosition = Center;
        for (int j = 0; j < 7; j++) BuildTray(j);
        ThothUI.ButtonText(root, "Left", font, "<", Center + new Vector2(-120f, -380f), new Vector2(140f, 64f), () => TurnNeedle(-1));
        ThothUI.ButtonText(root, "Right", font, ">", Center + new Vector2(120f, -380f), new Vector2(140f, 64f), () => TurnNeedle(1));
        ThothUI.ButtonText(root, "Check", font, "맞추기", new Vector2(520f, -330f), new Vector2(260f, 84f), Check);
        message = ThothUI.Label(root, "Message", font, 30, new Vector2(0f, -470f), new Vector2(1700f, 50f), Color.white, "");
    }

    private static Vector2 HolePos(int i)
    {
        float a = i * 45f * Mathf.Deg2Rad;
        return Center + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * Radius;
    }

    private void BuildHole(int i)
    {
        Button b = ThothUI.ButtonPic(root, "Hole" + i, "", HolePos(i), new Vector2(100f, 100f), () => ClickHole(i));
        ((Image)b.targetGraphic).color = new Color(1f, 1f, 1f, 0f); // 안 보이는 클릭 자리 (회색 네모가 보이던 것 없앰)
        b.transition = Selectable.Transition.None;
        holeIcons[i] = ThothUI.Pic((RectTransform)b.transform, "Moon", "", Vector2.zero, new Vector2(92f, 92f));
    }

    private void BuildTray(int j)
    {
        Vector2 pos = new Vector2(j % 2 == 0 ? 400f : 560f, 300f - (j / 2) * 140f);
        Button b = ThothUI.ButtonPic(root, "Tray" + j, $"UI/UI_Moon_{TrayOrder[j]}", pos, new Vector2(110f, 110f), () => ClickTray(j));
        trayIcons[j] = (Image)b.targetGraphic;
    }

    protected override void OnOpened()
    {
        solving = false;
        message.text = ThothLines.DialHelp;
        Refresh();
    }

    // ================= 조작 =================
    // 달 그림을 누르면 다음 빈 구멍에 바로 끼움
    private void ClickTray(int j)
    {
        if (solving) return;
        int hole = NextEmptyHole();
        if (hole < 0)
        {
            Fail("구멍이 다 찼어. 끼운 달을 누르면 다시 빠져.");
            return;
        }
        holes[hole] = TrayOrder[j];
        GameAudio.Play("sfx_th_snap", 0.8f, 1f + hole * 0.04f);
        message.text = ThothLines.DialHelp;
        Refresh();
        StartCoroutine(Pop(holeIcons[hole].rectTransform));
    }

    private int NextEmptyHole()
    {
        for (int i = 1; i < 8; i++) if (holes[i] == 0) return i;
        return -1;
    }

    // 끼운 달을 누르면 빠져서 오른쪽으로 돌아감 (0번 그믐은 고정)
    private void ClickHole(int i)
    {
        if (solving || i == 0 || holes[i] == 0) return;
        holes[i] = 0;
        GameAudio.Play("sfx_th_paper", 0.4f, 1.3f);
        Refresh();
    }

    // 쏙 들어가는 느낌 (크게 -> 제자리)
    private IEnumerator Pop(RectTransform rt)
    {
        for (float t = 0f; t < 0.18f; t += Time.unscaledDeltaTime)
        {
            rt.localScale = Vector3.one * Mathf.Lerp(1.45f, 1f, t / 0.18f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private void TurnNeedle(int dir)
    {
        if (solving) return;
        needleStep = ((needleStep + dir) % 8 + 8) % 8;
        GameAudio.Play("sfx_th_stone", 0.35f, 2f);
        Refresh();
    }

    private bool Placed(int moon)
    {
        foreach (int h in holes) if (h == moon) return true;
        return false;
    }

    private void Refresh()
    {
        for (int i = 0; i < 8; i++)
        {
            holeIcons[i].sprite = holes[i] != 0 ? ThothState.Art($"UI/UI_Moon_{holes[i]}") : null;
            holeIcons[i].enabled = holes[i] != 0;
            holeIcons[i].color = Color.white;
        }
        for (int j = 0; j < 7; j++) trayIcons[j].gameObject.SetActive(!Placed(TrayOrder[j]));
        needle.rectTransform.localEulerAngles = new Vector3(0f, 0f, -45f * needleStep);
    }

    // ================= 확인 =================
    private void Check()
    {
        if (solving) return;
        int right = 0, empty = 0;
        for (int i = 1; i < 8; i++)
        {
            if (holes[i] == 0) empty++;
            else if (holes[i] == Answer[i]) right++;
        }
        if (empty > 0) Fail(ThothLines.DialNotFull);
        else if (right < 7) Fail(ThothLines.DialWrong(right));
        else if (needleStep != NeedleAnswer) Fail(ThothLines.DialNeedle);
        else StartCoroutine(Solve());
    }

    private void Fail(string text)
    {
        message.text = text;
        GameAudio.Play("sfx_th_wrong", 0.7f);
    }

    private IEnumerator Solve()
    {
        solving = true;
        message.text = "";
        Sprite active = ThothState.Art("UI/UI_Dial_Active");
        if (active != null) plate.sprite = active;
        GameAudio.Play("sfx_th_chime", 0.9f);
        ThothState.Set(ThothIds.DialDone);
        RefreshWorld();
        PuzzleSolvedFx.Play();
        yield return new WaitForSecondsRealtime(PuzzleSolvedFx.HoldSeconds);
        solving = false;
        ClosePanel();
    }
}
