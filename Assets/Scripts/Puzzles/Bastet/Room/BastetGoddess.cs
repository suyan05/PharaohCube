using System;
using System.Collections;
using UnityEngine;

// [추가] 바스테트 여신의 목소리 (라의 방 "라의 성소"처럼 초상화와 함께 말함)
// 붙이는 곳: 가운데 홀의 바스테트 여신상 (Tools/Bastet/★ 비석·벽화·여신 대사 세팅 메뉴가 만듦)
// 대사 창은 라의 방 대사 창(RaDialogue)을 그대로 씀 (이름 "바스테트", 바스테트 초상화)
// 단계
//  (1) 처음 들어오면 자동으로 한 번 + 여신상 E: 신전 소개   (안 들었으면 "!")
//  (2) 서브 방 4개를 다 풀면(F_BASTET_B4_CLEAR): 메인 퍼즐 안내 (안 들었으면 "!")
//  (3) 메인 퍼즐을 풀면(F_BASTET_MAIN_DONE): 획득 팝업이 닫힌 뒤 자동으로 마무리 대사
//      (B5의 다음 씬 자동 이동은 이 대사가 끝날 때까지 기다림)
//  그 밖에: 지금 가야 할 방 힌트
public class BastetGoddess : MonoBehaviour, IInteractable
{
    public static bool Busy { get; private set; } // 대사 중 (B5 자동 이동이 기다림)

    [Header("연결 (비우면 자동으로 찾음)")]
    [SerializeField] private RaDialogue dialogue;
    [SerializeField] private SpriteRenderer newBubble;
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 3f;

    [Header("처음 들어왔을 때 자동으로 말하기")]
    [SerializeField] private bool autoIntro = true;
    [Range(0f, 5f)] [SerializeField] private float autoIntroDelay = 1.2f;

    [Header("플래그")]
    [SerializeField] private string introFlag = "F_BASTET_GODDESS_INTRO";
    [SerializeField] private string mainOpenFlag = "F_BASTET_B4_CLEAR";
    [SerializeField] private string mainTalkFlag = "F_BASTET_GODDESS_MAIN";
    [SerializeField] private string mainDoneFlag = "F_BASTET_MAIN_DONE";
    [SerializeField] private string endTalkFlag = "F_BASTET_GODDESS_END";

    [Header("대사 (1) 처음")]
    [SerializeField] private RaDialogueLine[] introLines =
    {
        new RaDialogueLine(RaPortrait.Default, "어둠 속을 걸어온 자여. 나는 바스테트, 라의 눈이자 밤을 지키는 고양이다."),
        new RaDialogueLine(RaPortrait.Default, "이 신전의 불은 스스로 켜지지 않는다. 네 손의 불로 벽의 횃불을 깨워라."),
        new RaDialogueLine(RaPortrait.Glow, "네 개의 방에 나의 흔적이 잠들어 있다. 발자국, 눈, 목걸이, 발톱. 모두 찾아오너라.")
    };

    [Header("대사 (2) 서브 방 4개를 모두 풂")]
    [SerializeField] private RaDialogueLine[] mainLines =
    {
        new RaDialogueLine(RaPortrait.Glow, "네 개의 흔적이 모두 돌아왔구나."),
        new RaDialogueLine(RaPortrait.Default, "북쪽 신상의 방으로 가라. 낮에는 해를 따라 걷고, 밤에는 지나온 길을 거슬러 돌아오너라."),
        new RaDialogueLine(RaPortrait.Night, "밤이 오면 나의 눈은 달빛이 된다. 마지막에 빛난 눈을 잊지 마라.")
    };

    [Header("대사 (3) 메인 퍼즐 클리어")]
    [SerializeField] private RaDialogueLine[] endLines =
    {
        new RaDialogueLine(RaPortrait.Glow, "해와 달을 모두 기억하는구나. 너는 나의 시험을 통과했다."),
        new RaDialogueLine(RaPortrait.Default, "그 형판을 가져가라. 큐브의 한 면이 될 것이다."),
        new RaDialogueLine(RaPortrait.Default, "다음은 심판의 신전. 아누비스가 네 마음의 무게를 달아 볼 것이다.")
    };

    [Header("대사 그 밖에 (지금 가야 할 방)")]
    [SerializeField] private string[] progressHints =
    {
        "서쪽 아래, 발자국의 방부터 가거라. 고양이는 지나온 길을 잊지 않는다.",
        "발자국 인장을 서쪽 위 눈의 방 봉인석에 넣어라.",
        "호박 눈을 동쪽 위 목걸이의 방 봉인석에 넣어라.",
        "목걸이 구슬을 동쪽 아래 발톱의 방 봉인석에 넣어라."
    };

    private Vector3 bubbleBasePos, bubbleBaseScale = Vector3.one;
    private bool subscribed;

    // ================= 초기화 =================
    private IEnumerator Start()
    {
        if (dialogue == null) dialogue = FindFirstObjectByType<RaDialogue>(FindObjectsInactive.Include);
        if (dialogue == null) Debug.LogError("[BastetGoddess] 씬에 RaDialogue(대사 창)가 없음 -> Tools/Bastet/★ 비석·벽화·여신 대사 세팅 실행", this);
        if (newBubble != null)
        {
            bubbleBasePos = newBubble.transform.localPosition;
            bubbleBaseScale = newBubble.transform.localScale;
        }
        if (GameManager.Instance != null) { GameManager.Instance.OnFlagChanged += HandleFlag; subscribed = true; }

        // 처음 들어왔을 때 자동 소개 (연출·팝업이 끝난 뒤)
        if (autoIntro && !HasFlag(introFlag))
        {
            yield return new WaitForSeconds(autoIntroDelay);
            while (Cine.Running || DialogueBox.IsOpen || ItemPopup.IsShowing) yield return null;
            if (!HasFlag(introFlag)) Talk(introLines, introFlag);
        }
        // 클리어했는데 마무리 대사를 못 들은 채로 다시 들어온 경우
        else if (HasFlag(mainDoneFlag) && !HasFlag(endTalkFlag)) StartCoroutine(EndAfterPopup());
    }

    private void OnDestroy()
    {
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
        Busy = false;
    }

    private void HandleFlag(string flag)
    {
        if (flag == mainDoneFlag && !HasFlag(endTalkFlag))
        {
            Busy = true; // B5 자동 이동이 바로 기다리게 (같은 프레임)
            StartCoroutine(EndAfterPopup());
        }
    }

    private IEnumerator EndAfterPopup()
    {
        Busy = true;
        yield return null;
        while (ItemPopup.IsShowing || DialogueBox.IsOpen || Cine.Running) yield return null;
        yield return new WaitForSeconds(0.6f);
        Talk(endLines, endTalkFlag);
    }

    // ================= "!" =================
    private void Update()
    {
        if (newBubble == null) return;
        bool show = !Busy && (!HasFlag(introFlag) || (HasFlag(mainOpenFlag) && !HasFlag(mainTalkFlag)) || (HasFlag(mainDoneFlag) && !HasFlag(endTalkFlag)));
        newBubble.enabled = show;
        if (show) newBubble.transform.localPosition = bubbleBasePos + new Vector3(0f, Mathf.Sin(Time.time * bobSpeed) * bobHeight, 0f);
    }

    // ================= IInteractable =================
    public string GetPrompt() => dialogue != null && dialogue.IsPlaying ? null : "E: 바스테트 여신상";

    public void Interact(GameObject who)
    {
        try
        {
            if (dialogue == null || dialogue.IsPlaying || Busy) return;
            if (dialogue.ClosedFrame == Time.frameCount || DialogueBox.IsOpen) return;

            if (HasFlag(mainDoneFlag)) Talk(endLines, endTalkFlag);
            else if (HasFlag(mainOpenFlag)) Talk(mainLines, mainTalkFlag);
            else if (!HasFlag(introFlag)) Talk(introLines, introFlag);
            else Talk(new[] { new RaDialogueLine(RaPortrait.Default, CurrentHint()) }, "");
        }
        catch (Exception e)
        {
            Debug.LogError($"[BastetGoddess] 대사 오류: {e}", this);
        }
    }

    private string CurrentHint()
    {
        int cleared = 0;
        for (int i = 1; i <= 4; i++) if (HasFlag(BastetItemIds.ClearFlag(i))) cleared = i;
        return cleared < progressHints.Length ? progressHints[cleared] : progressHints[progressHints.Length - 1];
    }

    private void Talk(RaDialogueLine[] lines, string flagAfter)
    {
        if (dialogue == null) { Busy = false; return; }
        Busy = true;
        Debug.Log($"[BastetGoddess] 대사 {lines.Length}줄 ({(string.IsNullOrEmpty(flagAfter) ? "힌트" : flagAfter)})", this);
        dialogue.Play(lines, () =>
        {
            if (!string.IsNullOrEmpty(flagAfter) && GameManager.Instance != null && !HasFlag(flagAfter)) GameManager.Instance.SetFlag(flagAfter);
            Busy = false;
        });
    }

    private static bool HasFlag(string f) => !string.IsNullOrEmpty(f) && GameManager.Instance != null && GameManager.Instance.HasFlag(f);

    // ================= 테스트 =================
    [ContextMenu("테스트: 소개 대사")] private void TestIntro() { if (Application.isPlaying) Talk(introLines, ""); }
    [ContextMenu("테스트: 메인 안내 대사")] private void TestMain() { if (Application.isPlaying) Talk(mainLines, ""); }
    [ContextMenu("테스트: 마무리 대사")] private void TestEnd() { if (Application.isPlaying) Talk(endLines, ""); }

#if UNITY_EDITOR
    public void EditorSetup(RaDialogue d, SpriteRenderer bubble) { dialogue = d; newBubble = bubble; }
#endif
}
