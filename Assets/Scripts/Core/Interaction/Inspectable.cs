using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.LowLevel;

// 범용 조사 오브젝트 (공통 상호작용 시스템)
// 일지, 장비 상자, 모래 더미, 배 모형, 석상 같은 "E로 조사하면 무언가 일어나는" 물건에 붙임
//
// 조사 순서: 조건 확인 -> (길게 누르기) -> 그림 바꾸기 -> 아이템 팝업 -> 대사 -> 플래그 -> On Done 이벤트
// - Required Flag: 이 플래그가 켜져야 조사 가능 (예: 밤에만 = F_RA_NIGHT_OPEN)
// - Required Tool: 이 아이템이 있어야 조사 가능 (예: TOOL_BRUSH). 없으면 Missing Tool Lines
// - Hold Seconds > 0: 길게 누르기 (붓질). 손을 떼면 게이지가 천천히 줄어듦 (처음부터 다시 아님)
// - 다 끝나면 F_<Object Id>_DONE 플래그가 켜짐 (씬에 다시 들어와도 끝난 상태 유지)
// 콜라이더가 있어야 InteractionSystem이 찾음 (트리거여도 됨)
public class Inspectable : MonoBehaviour, IInteractable
{
    [Header("이름 (플래그 = F_<Object Id>_DONE)")]
    [Tooltip("예: PRO_OBJ_JOURNAL, RA_OBJ_BRQ")]
    [SerializeField] private string objectId = "";
    [SerializeField] private string promptText = "조사하기";
    [SerializeField] private string holdPromptText = "길게 눌러 털기";

    [Header("조건 (비우면 조건 없음)")]
    [SerializeField] private string requiredFlag = "";
    [SerializeField] private DialogueLine[] lockedLines;
    [SerializeField] private string requiredTool = "";
    [SerializeField] private DialogueLine[] missingToolLines = { new DialogueLine("", "도구가 있어야 할 것 같다.") };

    [Header("길게 누르기 (0이면 E 한 번)")]
    [SerializeField, Min(0f)] private float holdSeconds = 0f;
    [Tooltip("손을 뗐을 때 게이지가 줄어드는 속도 (채우는 속도 대비 배율)")]
    [SerializeField, Min(0f)] private float holdDecayRate = 0.5f;
    [Tooltip("플레이어가 이 거리(칸)보다 멀어지면 누르기 중단")]
    [SerializeField] private float holdRange = 1.6f;

    [Header("그림 상태 (비우면 안 바뀜)")]
    [SerializeField] private SpriteRenderer targetRenderer;
    [Tooltip("길게 누르는 동안 진행도에 따라 순서대로 바뀜 (예: _Big, _Small)")]
    [SerializeField] private Sprite[] holdStageSprites;
    [Tooltip("다 끝난 뒤 그림 (예: _Gone, _Open)")]
    [SerializeField] private Sprite doneSprite;

    [Header("결과")]
    [SerializeField] private string[] grantItems;
    [SerializeField] private DialogueLine[] lines;
    [SerializeField] private string[] setFlags;
    [Tooltip("끄면 몇 번이고 다시 조사 가능 (대사만 있는 물건)")]
    [SerializeField] private bool onceOnly = true;
    [Tooltip("이미 끝난 뒤 다시 조사하면 나올 대사 (비우면 반응 없음)")]
    [SerializeField] private DialogueLine[] afterDoneLines;
    [SerializeField] private UnityEvent onDone;

    private bool done;
    private bool busy;
    private bool holdActive;
    private float holdProgress;
    private Transform player;

    // 밖(예: 모래 더미 묶음)에서 결과를 덮어쓸 때 사용
    public event Action<Inspectable> OnCompleted;

    public string ObjectId => objectId;
    public bool IsDone => done;

    // [추가] 지금 E를 길게 누르고 있는 조사 대상 (플레이어 무릎 꿇기, E 말풍선 숨기기용. 없으면 null)
    public static Inspectable Holding { get; private set; }
    public bool IsHoldType => holdSeconds > 0f && !done;
    public float HoldProgress => holdProgress;
    private string DoneFlag => $"F_{objectId}_DONE";

    // ================= 초기화 =================
    private void Start()
    {
        try
        {
            if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
            if (string.IsNullOrEmpty(objectId)) Debug.LogWarning($"[{name}] Object Id가 비어 있음 -> 끝난 상태가 저장되지 않음");

            done = !string.IsNullOrEmpty(objectId) && HasFlag(DoneFlag);
            if (done && doneSprite != null && targetRenderer != null) targetRenderer.sprite = doneSprite;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 조사 오브젝트 초기화 오류: {e}");
        }
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        if (done && onceOnly && !HasLines(afterDoneLines)) return null;
        bool hold = holdSeconds > 0f && !done && HasTool();
        return hold ? $"E 길게: {holdPromptText}" : $"E: {promptText}";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (busy || DialogueBox.IsOpen || ItemPopup.IsShowing) return;
            if (DialogueBox.ClosedFrame == Time.frameCount) return;
            if (who != null) player = who.transform;

            if (done && onceOnly)
            {
                DialogueBox.Play(afterDoneLines);
                return;
            }
            if (!CheckConditions()) return;

            if (holdSeconds > 0f) holdActive = true;
            else StartCoroutine(Complete());
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 조사 오류: {e}");
        }
    }

    private bool CheckConditions()
    {
        if (!string.IsNullOrEmpty(requiredFlag) && !HasFlag(requiredFlag))
        {
            DialogueBox.Play(lockedLines);
            return false;
        }
        if (!HasTool())
        {
            DialogueBox.Play(missingToolLines);
            return false;
        }
        return true;
    }

    private bool HasTool()
    {
        if (string.IsNullOrEmpty(requiredTool)) return true;
        return ItemInventory.Instance != null && ItemInventory.Instance.Has(requiredTool);
    }

    // ================= 길게 누르기 =================
    private void Update()
    {
        if (holdSeconds <= 0f || done || busy)
        {
            if (Holding == this) Holding = null;
            return;
        }
        if (!holdActive && holdProgress <= 0f) return;

        try
        {
            UpdateHold();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 길게 누르기 오류: {e}");
            holdActive = false;
            if (Holding == this) Holding = null;
            HoldRingUI.Hide();
        }
    }


    private void UpdateHold()
    {
        bool pressing = holdActive && Input.GetKey(KeyCode.E) && PlayerInRange() && !PlayerLock.IsLocked;
        if (!pressing) holdActive = false;
        if (pressing) Holding = this;
        else if (Holding == this) Holding = null;

        float rate = 1f / holdSeconds;
        holdProgress += (pressing ? rate : -rate * holdDecayRate) * Time.deltaTime;
        holdProgress = Mathf.Clamp01(holdProgress);
        if (pressing && requiredTool == "TOOL_BRUSH") GameAudio.PlayThrottled("sfx_brush", 0.24f, 0.55f); // [추가] 붓질 소리

        ApplyHoldStage();
        if (holdProgress > 0f) HoldRingUI.Show(transform.position, holdProgress);
        else HoldRingUI.Hide();

        if (holdProgress >= 1f) StartCoroutine(Complete());
    }

    private bool PlayerInRange()
    {
        if (player == null) return false;
        return Vector2.Distance(player.position, transform.position) <= holdRange;
    }

    // 진행도에 따라 단계 그림 (예: 0~50% = Big, 50~100% = Small)
    private void ApplyHoldStage()
    {
        if (targetRenderer == null || holdStageSprites == null || holdStageSprites.Length == 0) return;

        int stage = Mathf.Min(holdStageSprites.Length - 1, Mathf.FloorToInt(holdProgress * holdStageSprites.Length));
        if (holdStageSprites[stage] != null) targetRenderer.sprite = holdStageSprites[stage];
    }

    // ================= 완료 =================
    private IEnumerator Complete()
    {
        busy = true;
        holdActive = false;
        holdProgress = 0f;
        HoldRingUI.Hide();

        if (doneSprite != null && targetRenderer != null) targetRenderer.sprite = doneSprite;

        yield return GrantItems();
        yield return PlayLines(lines);

        FinishFlags();
        busy = false;
        onDone?.Invoke();
        OnCompleted?.Invoke(this);
    }

    private IEnumerator GrantItems()
    {
        if (grantItems == null) yield break;

        foreach (string id in grantItems)
        {
            if (string.IsNullOrEmpty(id)) continue;
            bool closed = false;
            ItemPopup.Grant(id, () => closed = true);
            while (!closed) yield return null;
        }
    }

    // 밖에서도 쓰는 "대사 끝날 때까지 기다리기"
    public static IEnumerator PlayLines(DialogueLine[] toPlay)
    {
        if (!HasLines(toPlay)) yield break;

        bool closed = false;
        DialogueBox.Play(toPlay, () => closed = true);
        while (!closed && DialogueBox.IsOpen) yield return null;
    }

    private void FinishFlags()
    {
        if (onceOnly) done = true;

        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (onceOnly && !string.IsNullOrEmpty(objectId) && !gm.HasFlag(DoneFlag)) gm.SetFlag(DoneFlag);
        if (setFlags == null) return;

        foreach (string flag in setFlags)
        {
            if (!string.IsNullOrEmpty(flag) && !gm.HasFlag(flag)) gm.SetFlag(flag);
        }
    }

    private void OnDisable()
    {
        holdActive = false;
        if (Holding == this) Holding = null; // [추가]
        if (holdProgress > 0f) HoldRingUI.Hide();
    }

    // ================= 도우미 =================
    private static bool HasLines(DialogueLine[] l) => l != null && l.Length > 0;

    private static bool HasFlag(string flag)
    {
        return GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
    }

    // Scene 창에서 선택하면 길게 누르기 거리가 주황 원으로 보임
    private void OnDrawGizmosSelected()
    {
        if (holdSeconds <= 0f) return;
        Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, holdRange);
    }
}