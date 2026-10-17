using UnityEngine;

// 아누비스 방 비석 단서: E -> 단서 화면(CluePanel, 라 방과 같은 화면) 열기 / 닫기
// 라 방 ClueStele와 다른 점
// - [추가] 해금 아이템: 앞 시험의 보상을 얻어야 "!"가 뜸 (A1 클리어 -> A2 비석 "!" -> ...)
// - [추가] 메인 저울 실패 처벌(IAnubisRetryable) 때 다시 안 읽은 상태로 -> "!"가 다시 뜸
// - 읽음 기록은 이 오브젝트가 들고 있음 (GameManager 플래그는 끌 수 없어서)
public class AnubisClueStele : MonoBehaviour, IInteractable, IAnubisRetryable
{
    [Header("연결")]
    [SerializeField] private ClueData data;
    [Tooltip("비우면 씬에서 자동으로 찾음 (AnubisStoryCanvas/Clue_Overlay)")]
    [SerializeField] private CluePanel panel;

    [Header("\"!\" 표시 조건")]
    [Tooltip("이 아이템을 가져야 \"!\"가 뜸 (앞 시험 보상). 비우면 처음부터 뜸\n" +
             "예: A2 비석 = ITEM_ANUBIS_GOLD_JAR (A1 보상)")]
    [SerializeField] private string unlockItemId = "";
    [Tooltip("켜면 해금 전에는 읽을 수도 없음 (안내 문구만)")]
    [SerializeField] private bool blockReadWhileLocked = true;
    [TextArea(1, 2)]
    [SerializeField] private string lockedMessage = "글자가 아직 흐릿하다. 앞의 시험을 먼저 통과하자.";

    [Header("\"!\" 말풍선 (UI_Prompt_New를 넣은 자식 SpriteRenderer)")]
    [SerializeField] private SpriteRenderer newBubble;
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 3f;
    [Tooltip("\"!\"가 톡 커지며 나타나는 시간 (초)")]
    [SerializeField] private float popTime = 0.2f;

    [Header("읽으면 켤 플래그 (목표 안내용, 비우면 F_CLUE_<단서ID>_READ)")]
    [SerializeField] private string readFlag = "";

    private bool read;          // 이번 도전에서 읽었는지 (처벌되면 false)
    private Vector3 bubbleBasePos;
    private Vector3 bubbleBaseScale = Vector3.one;
    private float popTimer;

    public bool IsUnlocked => string.IsNullOrEmpty(unlockItemId)
                              || (ItemInventory.Instance != null && ItemInventory.Instance.Has(unlockItemId));

    private string ReadFlag => !string.IsNullOrEmpty(readFlag) ? readFlag
                             : (data != null ? $"F_CLUE_{data.ClueId}_READ" : "");

    // ================= 초기화 =================
    private void Start()
    {
        if (panel == null) panel = FindFirstObjectByType<CluePanel>(FindObjectsInactive.Include);
        if (data == null) Debug.LogError($"[{name}] Data 칸이 비어 있음 -> Clue_ANUBIS_*.asset 넣기", this);
        if (panel == null) Debug.LogError($"[{name}] 씬에 CluePanel이 없음 -> Tools/Anubis 메뉴 '라 방 기능 적용' 실행", this);

        if (newBubble != null)
        {
            bubbleBasePos = newBubble.transform.localPosition;
            bubbleBaseScale = newBubble.transform.localScale;
            newBubble.enabled = false;
        }
    }

    // ================= "!" 말풍선 =================
    private void Update()
    {
        if (newBubble == null) return;

        bool show = !read && IsUnlocked; // Has()는 HashSet 조회라 매 프레임 써도 가벼움
        if (!show)
        {
            if (newBubble.enabled) newBubble.enabled = false;
            popTimer = 0f;
            return;
        }

        if (!newBubble.enabled) newBubble.enabled = true;
        popTimer = Mathf.Min(popTimer + Time.deltaTime, Mathf.Max(0.01f, popTime));
        float pop = popTimer / Mathf.Max(0.01f, popTime);

        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        newBubble.transform.localPosition = bubbleBasePos + new Vector3(0f, y, 0f);
        newBubble.transform.localScale = bubbleBaseScale * Mathf.SmoothStep(0f, 1f, pop);
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        if (panel != null && panel.IsOpen) return "E: 닫기";
        return "E: 비석 읽기";
    }

    public void Interact(GameObject who)
    {
        if (data == null || panel == null) return;

        if (panel.IsOpen)
        {
            panel.Hide();
            return;
        }

        if (blockReadWhileLocked && !IsUnlocked)
        {
            AnubisToast.Show(lockedMessage, 2.5f);
            return;
        }

        panel.Show(data.Get(false), null, who); // 아누비스 방은 낮/밤 없음
        MarkRead();
    }

    private void MarkRead()
    {
        read = true;

        GameManager gm = GameManager.Instance;
        string flag = ReadFlag;
        if (gm != null && !string.IsNullOrEmpty(flag) && !gm.HasFlag(flag)) gm.SetFlag(flag);
        Debug.Log($"[{name}] 비석 읽음 ({flag})", this);
    }

    // ================= 메인 저울 실패 처벌 =================
    public void ResetForRetry()
    {
        read = false;
        popTimer = 0f;
        if (panel != null && panel.IsOpen) panel.Hide();
    }

    // ================= 테스트 =================
    [ContextMenu("테스트: 안 읽은 상태로 (\"!\" 다시 표시)")]
    private void TestUnread() => ResetForRetry();

    [ContextMenu("테스트: 지금 상태 출력")]
    private void TestPrint() => Debug.Log($"[{name}] 읽음={read}, 해금={IsUnlocked} (조건: '{unlockItemId}')", this);

    // Scene 뷰: 해금 조건을 글자로 표시
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 1.2f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.4f,
            string.IsNullOrEmpty(unlockItemId) ? "처음부터 \"!\"" : $"\"!\" 조건: {unlockItemId}");
#endif
    }
}
