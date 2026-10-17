using System;
using UnityEngine;

// 아누비스 신상: E로 아누비스와 대화 (라 방 RaShrine의 아누비스 버전, 대사창은 RaDialogue를 같이 씀)
// 단계별 대사 (위에서부터 먼저 맞는 것):
//   (1) 메인 심판 통과 후   : "!" -> 마무리 대사 (한 번 들으면 "!" 사라짐)
//   (2) 처벌 직후           : "!" -> 다시 도전 격려 대사 (처벌될 때마다 다시 "!")
//   (3) 첫 대화 전          : "!" -> 방 소개 대사
//   (4) 보물 4개 모두 모음   : "!" -> 심판 안내 대사 (한 번 들으면 "!" 사라짐)
//   (5) 진행 중             : "!" 없음 -> 진행 안내 대사 + 지금 모은 보물 수
// 신상 오브젝트(statue_0 등, Collider2D 필요)에 부착
public class AnubisShrine : MonoBehaviour, IInteractable
{
    private enum Stage { Intro, Progress, Ready, AfterPenalty, Cleared }

    [Header("연결 (비우면 자동으로 찾음)")]
    [SerializeField] private RaDialogue dialogue;
    [SerializeField] private AnubisJudgmentPenalty penalty;

    [Header("\"!\" 말풍선 (UI_Prompt_New를 넣은 자식 SpriteRenderer)")]
    [SerializeField] private SpriteRenderer newBubble;
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 3f;

    [Header("안내 문구")]
    [SerializeField] private string prompt = "E: 아누비스 신상 조사";

    [Header("플래그")]
    [SerializeField] private string firstTalkedFlag = AnubisObjectiveGuide.FlagShrineFirstTalked;
    [SerializeField] private string mainClearFlag = AnubisObjectiveGuide.FlagMainDone;
    [SerializeField] private string endTalkedFlag = AnubisObjectiveGuide.FlagShrineEndTalked;

    [Header("심판에 필요한 보물 (서브 퍼즐 보상 4개)")]
    [SerializeField]
    private string[] trialItems =
    {
        AnubisItemIds.GoldJar, AnubisItemIds.ClayPot, AnubisItemIds.Obsidian, AnubisItemIds.BronzeBowl
    };

    [Header("대사 (초상화: Default = 기본, Glow = 빛남, Night = 엄격)")]
    [SerializeField]
    private RaDialogueLine[] introLines =
    {
        new RaDialogueLine(RaPortrait.Default, "망자여, 두아트의 심판정에 온 것을 환영한다. 나는 저울을 지키는 자, 아누비스다."),
        new RaDialogueLine(RaPortrait.Night, "네 심장은 진실의 깃털과 함께 저울에 오를 것이다. 그 전에 네 개의 시험을 통과하라."),
        new RaDialogueLine(RaPortrait.Default, "바닥에 흩어진 양피지 조각을 모아라. 조각이 모일수록 시험의 규칙이 드러난다."),
        new RaDialogueLine(RaPortrait.Default, "시험마다 보물 하나와 사자의 서 비문 하나를 얻는다. 비문은 보물의 무게를 말해 줄 것이다.")
    };

    [SerializeField]
    private RaDialogueLine[] progressLines =
    {
        new RaDialogueLine(RaPortrait.Default, "불이 켜진 구역의 제단으로 가라. 시험을 하나 통과할 때마다 다음 길이 열린다."),
        new RaDialogueLine(RaPortrait.Default, "막히면 시험 화면 오른쪽 위의 우제트의 눈을 눌러라. 길을 조금 비춰 줄 것이다.")
    };

    [SerializeField]
    private RaDialogueLine[] readyLines =
    {
        new RaDialogueLine(RaPortrait.Glow, "네 개의 보물을 모두 모았구나."),
        new RaDialogueLine(RaPortrait.Night, "남쪽 심판의 저울로 가라. 깃털의 무게는 일곱. 비문으로 각 보물의 무게를 밝혀라."),
        new RaDialogueLine(RaPortrait.Night, "기회는 한 번뿐이다. 저울이 기울면 암무트가 보물을 삼킬 것이다.")
    };

    [SerializeField]
    private RaDialogueLine[] penaltyLines =
    {
        new RaDialogueLine(RaPortrait.Night, "저울이 기울었고, 암무트가 보물을 삼켰다."),
        new RaDialogueLine(RaPortrait.Default, "허나 사자의 서에 새긴 비문은 사라지지 않았다. 시험의 모습은 바뀌었으니 처음부터 다시 걸어라.")
    };

    [SerializeField]
    private RaDialogueLine[] clearedLines =
    {
        new RaDialogueLine(RaPortrait.Glow, "심장과 깃털이 평형을 이루었다. 너의 영혼은 진실하다."),
        new RaDialogueLine(RaPortrait.Glow, "아알루의 갈대밭으로 가는 길이 열렸다. 가거라, 의로운 자여.")
    };

    [Tooltip("진행 중 대사 끝에 붙는 한 줄 ({0} = 모은 보물 수, {1} = 전체)")]
    [SerializeField] private string progressCountFormat = "지금까지 모은 시험의 보물: {0} / {1}";

    private Vector3 bubbleBasePos;
    private bool penaltyPending;   // 처벌 뒤 아직 신상과 대화 안 함
    private bool readyTalked;      // 보물 4개 상태에서 심판 안내를 들음 (처벌되면 다시 들을 수 있음)
    private bool penaltySubscribed;

    // ================= 초기화 =================
    private void Start()
    {
        if (dialogue == null) dialogue = FindFirstObjectByType<RaDialogue>(FindObjectsInactive.Include);
        if (penalty == null) penalty = FindFirstObjectByType<AnubisJudgmentPenalty>(FindObjectsInactive.Include);
        if (dialogue == null) Debug.LogError($"[{name}] 씬에 RaDialogue가 없음 -> Tools/Anubis 메뉴 '라 방 기능 적용'을 실행", this);

        if (penalty != null)
        {
            penalty.OnPenaltyApplied += HandlePenalty;
            penaltySubscribed = true;
        }

        if (newBubble != null) bubbleBasePos = newBubble.transform.localPosition;
    }

    private void OnDestroy()
    {
        if (penaltySubscribed && penalty != null) penalty.OnPenaltyApplied -= HandlePenalty;
    }

    private void HandlePenalty(int failCount)
    {
        penaltyPending = true;
        readyTalked = false;
    }

    // ================= "!" 말풍선 =================
    private void Update()
    {
        if (newBubble == null) return;

        bool show = ShouldShowBubble();
        if (newBubble.enabled != show) newBubble.enabled = show;
        if (!show) return;

        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        newBubble.transform.localPosition = bubbleBasePos + new Vector3(0f, y, 0f);
    }

    private bool ShouldShowBubble()
    {
        switch (CurrentStage())
        {
            case Stage.Cleared: return !HasFlag(endTalkedFlag);
            case Stage.AfterPenalty: return true;
            case Stage.Intro: return true;
            case Stage.Ready: return !readyTalked;
            default: return false;
        }
    }

    private Stage CurrentStage()
    {
        if (HasFlag(mainClearFlag)) return Stage.Cleared;
        if (penaltyPending) return Stage.AfterPenalty;
        if (!HasFlag(firstTalkedFlag)) return Stage.Intro;
        if (CountTrialItems() >= trialItems.Length) return Stage.Ready;
        return Stage.Progress;
    }

    private int CountTrialItems()
    {
        ItemInventory inv = ItemInventory.Instance;
        if (inv == null || trialItems == null) return 0;

        int count = 0;
        for (int i = 0; i < trialItems.Length; i++)
        {
            if (inv.Has(trialItems[i])) count++;
        }
        return count;
    }

    // ================= IInteractable =================
    public string GetPrompt() => prompt;

    public void Interact(GameObject player)
    {
        if (dialogue == null || dialogue.IsPlaying) return;
        if (dialogue.ClosedFrame == Time.frameCount) return; // 대화를 닫은 E로 다시 열리는 것 방지
        if (PuzzleOverlayFocus.IsActive) return;

        Stage stage = CurrentStage();
        Debug.Log($"[{name}] 아누비스 대화: {stage}", this);

        switch (stage)
        {
            case Stage.Cleared: dialogue.Play(clearedLines, () => SetFlagOnce(endTalkedFlag)); break;
            case Stage.AfterPenalty: dialogue.Play(penaltyLines, () => penaltyPending = false); break;
            case Stage.Intro: dialogue.Play(introLines, () => SetFlagOnce(firstTalkedFlag)); break;
            case Stage.Ready: dialogue.Play(readyLines, () => readyTalked = true); break;
            default: dialogue.Play(BuildProgressLines()); break;
        }
    }

    // 진행 대사 + 보물 수 한 줄 (대화할 때만 만들어서 매 프레임 할당 없음)
    private RaDialogueLine[] BuildProgressLines()
    {
        int baseCount = progressLines != null ? progressLines.Length : 0;
        var lines = new RaDialogueLine[baseCount + 1];
        for (int i = 0; i < baseCount; i++) lines[i] = progressLines[i];
        lines[baseCount] = new RaDialogueLine(RaPortrait.Default,
            string.Format(progressCountFormat, CountTrialItems(), trialItems.Length));
        return lines;
    }

    // ================= 도우미 =================
    private static void SetFlagOnce(string flag)
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && !string.IsNullOrEmpty(flag) && !gm.HasFlag(flag)) gm.SetFlag(flag);
    }

    private static bool HasFlag(string flag)
    {
        return !string.IsNullOrEmpty(flag) && GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
    }

    // ================= 테스트 =================
    [ContextMenu("테스트: 처벌 직후 상태로 (Play 중)")]
    private void TestPenalty() => HandlePenalty(1);

    [ContextMenu("테스트: 지금 대화 단계 출력")]
    private void TestPrintStage() => Debug.Log($"[{name}] 지금 단계 = {CurrentStage()}, 보물 {CountTrialItems()}/{trialItems.Length}", this);

    // Scene 뷰: 상호작용 범위(플레이어 1.2m) 표시
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.7f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 1.2f);
    }
}
