using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 서기관의 책상 (동쪽 서고) - 거짓 기록 찾기
// - 처음 E: 서랍에서 벽화 B 조각 하나 (B 조각 3번)
// - 벽화 B를 되살리기 전: "무슨 밤인지 모르겠다"
// - 그 뒤 E: 책상 화면. 위에 벽화 '달과의 내기', 아래 파피루스 기록 3장 -> 벽화와 다른 기록을 클릭
//   맞으면 그 장은 붉은 테(지움), 나머지는 금 테 + F_TH_DESK_DONE -> 달 시계가 깨어남
// - [수정] 난이도: 열 때마다 기록 순서가 섞임 + 틀리면 기록이 흐려지며 창이 닫힘 (찍어서 풀기 어렵게)
public class ThothDesk : ThothPanel, IInteractable
{
    private const int DrawerPiece = 3;
    private const float PanelY = -130f;
    private readonly int[] order = { 0, 2, 1 };   // 화면 왼쪽부터 보여줄 기록 번호 ([수정] 열 때마다 섞음)
    private static readonly float[] SlotX = { -305f, -76f, 159f };

    private readonly Image[] papers = new Image[3];
    private readonly Text[] paperTexts = new Text[3];
    private Text message;
    private bool solving, busy, shaking;

    protected override bool CanClose => !solving;

    public string GetPrompt()
    {
        if (!ThothState.Has(ThothIds.Piece("B", DrawerPiece))) return "E: 서기관의 책상 살펴보기";
        return ThothState.Has(ThothIds.DeskDone) ? "E: 바로잡은 기록 보기" : "E: 그 밤의 기록 읽기";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (busy || IsOpen || ThothState.Busy) return;
            if (!ThothState.Has(ThothIds.Piece("B", DrawerPiece))) Cine.Run(Drawer());
            else if (ThothState.Has(ThothIds.DeskDone)) Cine.Run(Talk(ThothLines.DeskAgain));
            else if (!ThothState.Has(ThothIds.FrescoDone("B"))) Cine.Run(Talk(ThothLines.DeskNeedB));
            else OpenPanel();
        }
        catch (Exception e)
        {
            Debug.LogError($"[서기관 책상] Interact 오류: {e}");
        }
    }

    private IEnumerator Drawer()
    {
        busy = true;
        Cine.Begin(false);
        GameAudio.Play("sfx_th_stone", 0.4f, 1.6f);
        yield return Cine.Say(ThothLines.DeskDrawer);
        ThothState.Set(ThothIds.Piece("B", DrawerPiece));
        yield return ThothFrescoWall.ShowPopup(ThothIds.PieceItem("B"));
        Cine.End();
        busy = false;
    }

    private IEnumerator Talk(DialogueLine[] lines)
    {
        busy = true;
        Cine.Begin(false);
        yield return Cine.Say(lines);
        Cine.End();
        busy = false;
    }

    // ================= 화면 =================
    protected override void Build()
    {
        ThothUI.Label(root, "Title", font, 44, new Vector2(0f, 500f), new Vector2(1600f, 60f), ThothUI.Gold, ThothLines.DeskTitle);
        UIBuild.Image(root, "MuralFrame", new Vector2(0f, 330f), new Vector2(500f, 290f), null, ThothUI.Gold);
        ThothUI.Pic(root, "Mural", "Fresco/ThFB_full", new Vector2(0f, 330f), new Vector2(480f, 270f));
        ThothUI.Pic(root, "Desk", "UI/UI_DeskPanel", new Vector2(0f, PanelY), new Vector2(1080f, 630f));
        for (int i = 0; i < 3; i++) papers[i] = BuildPaper(i);
        ThothUI.Label(root, "Side", font, 24, new Vector2(374f, PanelY), new Vector2(140f, 400f), Color.white, "거짓 기록은\n붉은 줄로\n지운다");
        message = ThothUI.Label(root, "Message", font, 30, new Vector2(0f, -470f), new Vector2(1700f, 50f), Color.white, "");
    }

    private Image BuildPaper(int slot)
    {
        Vector2 pos = new Vector2(SlotX[slot], PanelY - 9f);
        Button b = ThothUI.ButtonPic(root, "Record" + slot, "UI/UI_Papyrus_A", pos, new Vector2(207f, 288f), () => Pick(slot));
        paperTexts[slot] = UIBuild.Text((RectTransform)b.transform, "Text", font, 23, Vector2.zero, new Vector2(170f, 250f), ThothUI.Ink);
        paperTexts[slot].text = ThothLines.Records[order[slot]];
        return (Image)b.targetGraphic;
    }

    protected override void OnOpened()
    {
        solving = false;
        message.text = ThothLines.DeskHelp;
        Shuffle();
        for (int i = 0; i < 3; i++) SetPaper(i, "UI/UI_Papyrus_A");
    }

    // [추가] 열 때마다 기록 순서 섞기
    private void Shuffle()
    {
        for (int i = order.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        for (int i = 0; i < 3; i++) if (paperTexts[i] != null) paperTexts[i].text = ThothLines.Records[order[i]];
    }

    private void SetPaper(int slot, string art)
    {
        Sprite s = ThothState.Art(art);
        if (s != null) papers[slot].sprite = s;
        papers[slot].color = Color.white;
    }

    private void Pick(int slot)
    {
        if (solving) return;
        if (order[slot] == ThothLines.FalseRecord)
        {
            StartCoroutine(Solve(slot));
            return;
        }
        GameAudio.Play("sfx_th_wrong", 0.7f);
        message.text = ThothLines.DeskWrong;
        StartCoroutine(WrongThenClose(slot));
    }

    private IEnumerator Shake(int slot)
    {
        if (shaking) yield break;
        shaking = true;
        RectTransform rt = papers[slot].rectTransform;
        Vector2 home = rt.anchoredPosition;
        papers[slot].color = new Color(1f, 0.6f, 0.55f, 1f);
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            rt.anchoredPosition = home + new Vector2(Mathf.Sin(t * 60f) * 8f, 0f);
            yield return null;
        }
        rt.anchoredPosition = home;
        foreach (Image p in papers) p.color = Color.white;
        shaking = false;
    }

    // [추가] 틀리면 흔들린 뒤 기록이 흐려지며 창이 닫힘 -> 다시 열면 순서가 섞여 있음
    private IEnumerator WrongThenClose(int slot)
    {
        solving = true;
        yield return Shake(slot);
        yield return new WaitForSecondsRealtime(0.9f);
        solving = false;
        ClosePanel();
    }

    private IEnumerator Solve(int slot)
    {
        solving = true;
        for (int i = 0; i < 3; i++) SetPaper(i, i == slot ? "UI/UI_Papyrus_C" : "UI/UI_Papyrus_B");
        message.text = "";
        GameAudio.Play("sfx_th_paper", 0.8f, 0.8f);
        ThothState.Set(ThothIds.DeskDone);
        PuzzleSolvedFx.Play();
        yield return new WaitForSecondsRealtime(PuzzleSolvedFx.HoldSeconds);
        solving = false;
        ClosePanel();
        Cine.Run(Talk(ThothLines.DeskSolved));
    }
}
