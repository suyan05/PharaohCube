using System;
using System.Collections;
using UnityEngine;

// 프롤로그 2 스핑크스의 꿈 진행 담당 (기획서 3.1)
// 1. 시작 연출 (SphinxWakeCutscene)
// 2. 목표: 앞발 모래 2개를 붓으로 털기 (PawSand Inspectable 2개)
// 3. 둘 다 털면: 땅울림 -> 비석 드러남 -> 스핑크스 수수께끼 (틀리면 힌트 후 다시) -> 볼록렌즈 획득
//    (비석이 드러나면 먼저 상형문자만 보여줌 -> 못 읽음 -> 스핑크스: 읽고 싶으면 답하라)
// 4. 목표: 비석을 렌즈로 읽기 (SteleReader) -> 렌즈로 비춘 만큼 해독 -> 스핑크스 작별 -> 계단 열림
// 5. 계단에 들어가면 DreamStairs가 F_PROLOGUE_CLEAR + 다음 씬
public class SphinxDreamDirector : MonoBehaviour
{
    [Header("연결 (배치 도구가 채움)")]
    [SerializeField] private SphinxWakeCutscene wake;
    [SerializeField] private SphinxHead head;
    [SerializeField] private Inspectable[] pawSands;
    [SerializeField] private SteleReader stele;
    [SerializeField] private SpriteRenderer steleRenderer;
    [Tooltip("Pro_Obj_Stele_Shown")]
    [SerializeField] private Sprite steleShownSprite;
    [SerializeField] private DreamStairs stairs;

    [Header("아이템, 플래그")]
    [SerializeField] private string brushItem = "TOOL_BRUSH";
    [SerializeField] private string lensItem = "TOOL_LENS";
    [SerializeField] private string steleFlag = "F_PRO_STELE_SHOWN";
    [Tooltip("이 씬만 바로 Play할 때 붓이 없으면 막히니까 지급 (프롤로그 1에서 넘어오면 이미 있음)")]
    [SerializeField] private bool grantBrushIfMissing = true;

    [Header("목표 문구")]
    [SerializeField] private string objectiveSand = "스핑크스의 앞발을 덮은 모래를 털어내자";
    [SerializeField] private string objectiveLens = "비석을 렌즈로 읽어보자";
    [SerializeField] private string objectiveStairs = "열린 계단으로 내려가자";

    [Header("효과 (비워도 됨)")]
    [SerializeField] private Sprite[] dustSprites;
    [SerializeField] private Color dustColor = new Color(0.72f, 0.64f, 0.77f, 0.9f);
    [SerializeField] private AudioClip rumbleSfx;

    [Header("대사")]
    [SerializeField] private SphinxDreamLines lines = new SphinxDreamLines();

    private string objective = "";
    private int clearedCount;
    private bool started;
    private bool subscribed;

    // ================= 시작 =================
    private void Awake()
    {
        ScreenFader.SetBlack();
    }

    private IEnumerator Start()
    {
        EnsureBrush();
        if (wake != null) yield return wake.Play();
        else yield return ScreenFader.FadeIn(1f);

        Subscribe();
        started = true;
        SetObjective(objectiveSand);
    }

    private void EnsureBrush()
    {
        ItemInventory inv = ItemInventory.Instance;
        if (!grantBrushIfMissing || inv == null || inv.Has(brushItem)) return;

        Debug.Log("[SphinxDreamDirector] 테스트용으로 붓 지급 (이 씬을 바로 Play함)");
        inv.Grant(brushItem);
    }

    private void Subscribe()
    {
        Cine.Ended += ShowObjective;
        subscribed = true;
        if (pawSands != null)
        {
            foreach (Inspectable sand in pawSands)
            {
                if (sand != null) sand.OnCompleted += HandlePawCleared;
            }
        }
        if (stele != null) stele.AfterDecode = AfterDecode;
    }

    private void OnDestroy()
    {
        if (!subscribed) return;
        Cine.Ended -= ShowObjective;
        if (pawSands != null)
        {
            foreach (Inspectable sand in pawSands)
            {
                if (sand != null) sand.OnCompleted -= HandlePawCleared;
            }
        }
        if (stele != null) stele.AfterDecode = null;
    }

    // ================= 목표 =================
    private void SetObjective(string text)
    {
        objective = text;
        ShowObjective();
    }

    private void ShowObjective()
    {
        if (!started || Cine.Running) return;
        ObjectiveBoxUI.Show(objective);
    }

    // ================= 앞발 모래 =================
    private void HandlePawCleared(Inspectable sand)
    {
        try
        {
            clearedCount++;
            CineFX.Puff(dustSprites, sand.transform.position + Vector3.up * 0.3f, 12, 1.2f, dustColor);
            CineCamera.Shake(0.3f, 0.05f);

            if (clearedCount >= pawSands.Length) StartCoroutine(RevealStele());
            else StartCoroutine(Cine.Say(lines.firstPaw));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SphinxDreamDirector] 앞발 모래 결과 오류: {e}");
        }
    }

    private IEnumerator RevealStele()
    {
        Cine.Begin(false);
        Vector3 spot = stele != null ? stele.transform.position : transform.position;
        CineCamera.Focus(spot + Vector3.up);
        CineCamera.Zoom(4.5f, 1f);
        yield return Cine.Wait(0.6f);

        CineAudio.Play(GameAudio.Or(rumbleSfx, "sfx_rumble"));
        DreamFX.Ripple(1.6f, 0.25f);
        yield return CineCamera.Rumble(1.2f, 0.02f, 0.1f);
        CineFX.Puff(dustSprites, spot + Vector3.up * 0.5f, 20, 1.6f, dustColor);
        ShowStele();
        yield return Cine.Wait(0.8f);
        yield return Cine.Say(lines.steleRevealed);
        if (stele != null) yield return stele.ShowUnreadable();
        yield return HeadSay(lines.askToRead);

        yield return Riddle();
        yield return GrantLens();
        SetObjective(objectiveLens);
        Cine.End();
    }

    private void ShowStele()
    {
        if (steleRenderer != null && steleShownSprite != null) steleRenderer.sprite = steleShownSprite;
        if (GameManager.Instance != null && !GameManager.Instance.HasFlag(steleFlag)) GameManager.Instance.SetFlag(steleFlag);
    }

    // ================= 수수께끼 (틀려도 다시 고를 수 있음) =================
    private IEnumerator Riddle()
    {
        if (head != null) CineCamera.Focus(head.transform.position + Vector3.up * 0.5f);
        CineCamera.Zoom(6f, 1f);
        yield return Cine.Wait(0.8f);

        while (true)
        {
            int picked = -1;
            yield return AskRiddle(i => picked = i);
            if (picked == lines.correctIndex) break;
            yield return HeadSay(lines.wrong);
        }
        yield return HeadSay(lines.right);
    }

    private IEnumerator AskRiddle(Action<int> chosen)
    {
        if (head != null) yield return head.Ask(lines.riddle, lines.options, chosen);
        else
        {
            bool done = false;
            DialogueBox.Ask(lines.riddle, lines.options, i => { done = true; chosen(i); });
            while (!done && DialogueBox.IsOpen) yield return null;
        }
    }

    private IEnumerator GrantLens()
    {
        bool closed = false;
        ItemPopup.Grant(lensItem, () => closed = true);
        while (!closed) yield return null;
        yield return Cine.Say(lines.afterLens);
    }

    // ================= 비석 해독 -> 계단 =================
    // SteleReader가 해독을 끝낸 직후, 같은 연출(Cine) 안에서 이어서 돌림
    private IEnumerator AfterDecode()
    {
        yield return Cine.Say(lines.afterDecode);
        yield return HeadSay(lines.farewell);

        SetObjective(objectiveStairs);
        if (stairs != null) yield return stairs.Open();
        else Debug.LogError("[SphinxDreamDirector] DreamStairs가 연결 안 됨 -> F12로 넘기기");
    }

    private IEnumerator HeadSay(DialogueLine[] toSay)
    {
        if (head != null) yield return head.Say(toSay);
        else yield return Cine.Say(toSay);
    }
}
