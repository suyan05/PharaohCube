using System;
using System.Collections;
using UnityEngine;

// [추가] 바스테트 비석 (라의 방 비석 방식)
// 1) 방이 어두우면 못 읽음: 이 방 벽 횃불(WallTorch)이 하나라도 켜져 있어야 "!"가 뜨고 읽힘
//    어두우면 "너무 어두워서 글씨가 보이지 않는다. 이 방 벽 횃불부터 밝히자."
// 2) 처음 읽을 때: 상형문자만 보이는 비석 -> 렌즈(돋보기)로 비춘 곳만 한글로 해독 (SteleDecodeUI, 라·프롤로그 공용)
// 3) 해독이 끝나면 벽화 그림 + 글 화면(CluePanel) -> 고고학 수첩 메모에 기록 ([노트 N])
// 4) 두 번째부터는 바로 벽화 화면
// 붙이는 곳: Stele_B1~B5, Slate_Bastet_Hall (Tools/Bastet/★ 비석·벽화·여신 대사 세팅 메뉴가 붙임)
public class BastetClueStele : MonoBehaviour, IInteractable
{
    [Header("내용 (Clue Data: 제목·글·벽화 그림)")]
    [SerializeField] private ClueData data;
    [Tooltip("비우면 씬에서 찾음")]
    [SerializeField] private CluePanel panel;

    [Header("열리는 조건 (앞 방을 클리어해야 글자가 또렷해짐)")]
    [SerializeField] private string unlockFlag = "";
    [SerializeField] private string lockedMessage = "글자가 아직 흐릿하다. 앞의 시험을 먼저 통과하자.";

    [Header("빛이 있어야 읽힘 (라의 방 방식)")]
    [SerializeField] private bool needLight = true;
    [Tooltip("이 방 벽 횃불 (하나라도 켜지면 읽힘). 비우면 같은 방(BastetRoomArea) 안의 벽 횃불을 찾음")]
    [SerializeField] private WallTorch[] roomTorches;
    [Tooltip("방 벽 횃불을 못 찾았을 때: 이 반경 안에 켜진 벽 횃불이 있으면 읽힘")]
    [SerializeField] private float lightRadius = 6.5f;
    [SerializeField] private DialogueLine[] darkLines = { new DialogueLine("나", "너무 어두워서 글씨가 보이지 않는다. 이 방 벽 횃불부터 밝히자.") };

    [Header("렌즈 해독 (처음 한 번)")]
    [SerializeField] private bool lensDecode = true;
    [Range(6, 20)] [SerializeField] private int charsPerLine = 12;

    [Header("고고학 수첩 메모 (해독하면 기록, 0이면 안 함)")]
    [SerializeField] private int noteNumber = 0;
    [TextArea(1, 3)] [SerializeField] private string noteText = "";

    [Header("\"!\" 말풍선 (자식 SpriteRenderer)")]
    [SerializeField] private SpriteRenderer newBubble;
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 3f;

    private Vector3 bubbleBasePos, bubbleBaseScale = Vector3.one;
    private float popTimer;
    private bool decoding, wasLit;
    private Transform player;
    private BastetRoomArea room;

    private string Id => data != null ? data.ClueId : name;
    private string ReadFlag => $"F_CLUE_{Id}_READ";
    private string DecodedFlag => $"F_CLUE_{Id}_DECODED";
    private bool Unlocked => string.IsNullOrEmpty(unlockFlag) || HasFlag(unlockFlag);

    // ================= 초기화 =================
    private void Start()
    {
        if (panel == null) panel = FindFirstObjectByType<CluePanel>(FindObjectsInactive.Include);
        room = GetComponentInParent<BastetRoomArea>();
        if ((roomTorches == null || roomTorches.Length == 0) && room != null)
            roomTorches = room.GetComponentsInChildren<WallTorch>(true);
        if (data == null) Debug.LogError($"[{name}] Data 칸이 비어 있음 -> Clue_BASTET_*.asset 넣기", this);
        if (panel == null) Debug.LogError($"[{name}] 씬에 CluePanel이 없음 -> Tools/Bastet/실전 방 세팅 실행", this);
        if (newBubble != null)
        {
            bubbleBasePos = newBubble.transform.localPosition;
            bubbleBaseScale = newBubble.transform.localScale;
            newBubble.enabled = false;
        }
        PlayerController2D pc = FindAnyObjectByType<PlayerController2D>();
        if (pc != null) player = pc.transform;
    }

    private bool IsLitHere()
    {
        if (!needLight) return true;
        if (roomTorches != null && roomTorches.Length > 0)
        {
            foreach (WallTorch t in roomTorches) if (t != null && t.IsLit) return true;
            return false;
        }
        return WallTorch.AnyLitNear(transform.position, lightRadius);
    }

    private bool PlayerInRoom()
    {
        if (room == null || player == null) return true;
        Vector2 half = room.Size * 0.5f;
        Vector2 d = player.position - room.transform.position;
        return Mathf.Abs(d.x) <= half.x + 0.5f && Mathf.Abs(d.y) <= half.y + 1f;
    }

    // ================= "!" (불이 켜지고, 열렸고, 아직 안 읽었을 때) =================
    private void Update()
    {
        bool lit = IsLitHere();
        if (lit && !wasLit) Sparkle(); // 불이 켜지는 순간 글씨가 드러나는 반짝임
        wasLit = lit;
        if (newBubble == null) return;

        bool show = lit && Unlocked && !HasFlag(ReadFlag) && PlayerInRoom() && !decoding;
        if (!show)
        {
            if (newBubble.enabled) newBubble.enabled = false;
            popTimer = 0f;
            return;
        }
        newBubble.enabled = true;
        popTimer = Mathf.Min(popTimer + Time.deltaTime, 0.2f);
        newBubble.transform.localPosition = bubbleBasePos + new Vector3(0f, Mathf.Sin(Time.time * bobSpeed) * bobHeight, 0f);
        newBubble.transform.localScale = bubbleBaseScale * Mathf.SmoothStep(0f, 1f, popTimer / 0.2f);
    }

    private void Sparkle()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;
        for (int i = 0; i < 8; i++)
            RaGlowFx.Spark(transform.position + new Vector3(0f, 0.5f + i * 0.08f, 0f), new Color(1f, 0.82f, 0.45f), sr.sortingLayerName, sr.sortingOrder + 1);
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        if (decoding) return null;
        if (panel != null && panel.IsOpen) return "E: 닫기";
        if (!IsLitHere()) return "E: 살펴보기";
        return "E: 비석 읽기";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (data == null || panel == null || decoding) return;
            if (panel.IsOpen) { panel.Hide(); return; }
            if (DialogueBox.IsOpen || DialogueBox.ClosedFrame == Time.frameCount) return;

            if (!IsLitHere()) { DialogueBox.Play(darkLines); return; }
            if (!Unlocked) { AnubisToast.Show(lockedMessage, 2.5f); return; }

            if (lensDecode && !HasFlag(DecodedFlag)) { StartCoroutine(DecodeThenShow(who)); return; }
            ShowPanel(who);
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 비석 오류: {e}", this);
        }
    }

    private IEnumerator DecodeThenShow(GameObject who)
    {
        decoding = true;
        PlayerLock.Lock(this);
        ClueData.ClueContent c = data.Get(false);
        yield return SteleDecodeUI.Decode(c.title, RaNightClueText.ToLines(c.body.Replace("\n", " "), charsPerLine));
        SetFlag(DecodedFlag);
        if (noteNumber > 0 && !string.IsNullOrEmpty(noteText))
        {
            Debug.Log($"[노트 {noteNumber}] {noteText}"); // 고고학 수첩 메모 (NotebookNotes가 이 로그를 받아 저장)
            AnubisToast.Show($"[노트 {noteNumber}] 고고학 수첩에 기록되었다 (J)", 2.5f);
        }
        PlayerLock.Unlock(this);
        decoding = false;
        yield return null; // 해독 화면을 닫은 E로 벽화가 바로 닫히지 않게
        ShowPanel(who);
    }

    private void ShowPanel(GameObject who)
    {
        panel.Show(data.Get(false), null, who);
        SetFlag(ReadFlag);
    }

    private void OnDisable()
    {
        if (decoding) PlayerLock.Unlock(this);
        decoding = false;
    }

    private static bool HasFlag(string f) => !string.IsNullOrEmpty(f) && GameManager.Instance != null && GameManager.Instance.HasFlag(f);
    private static void SetFlag(string f) { if (GameManager.Instance != null && !GameManager.Instance.HasFlag(f)) GameManager.Instance.SetFlag(f); }

    // ================= 테스트 =================
    [ContextMenu("테스트: 렌즈 해독 화면 다시 보기")]
    private void TestShowDecode()
    {
        if (Application.isPlaying && !decoding) StartCoroutine(DecodeThenShow(player != null ? player.gameObject : null));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, lightRadius);
    }

#if UNITY_EDITOR
    public void EditorSetup(ClueData d, CluePanel p, SpriteRenderer bubble, string unlock, int note, string noteBody)
    {
        data = d; panel = p; newBubble = bubble; unlockFlag = unlock; noteNumber = note; noteText = noteBody;
    }
#endif
}
