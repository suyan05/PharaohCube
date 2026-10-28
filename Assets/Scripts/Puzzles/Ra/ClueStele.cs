using System;
using System.Collections;
using UnityEngine;

// 파란 비석: E -> 단서 패널 열기 / 닫기 (기존 NoteStone 대체)
// - 아직 안 읽은 단서 + 플레이어가 이 방 안에 있을 때만 머리 위에 "!" 말풍선 (UI_Prompt_New)
// - 방 범위는 Room Center + Room Size로 정함 (Scene 창에 초록 상자로 보임, 물리 충돌 없음)
//   Room Size가 0이면 범위 체크 없이 항상 표시
// - 읽음 플래그는 낮/밤 따로 -> 밤이 되어 단서가 바뀌면 "!"가 다시 뜸
//   낮: F_CLUE_<id>_READ / 밤: F_CLUE_<id>_NIGHT_READ (나중에 저널이 이 플래그로 목록을 만듦)
// [추가] 횃불 신전
// - 빛이 있어야 읽힘: 근처 벽 횃불(또는 성소 화로)이 켜져 있어야 "!"가 뜨고 글이 보임. 어두우면 "너무 어두워..." 혼잣말
//   불이 들어오는 순간 비석에서 불똥이 피어오르며 "!"가 나타남
// - 밤: 손상된 상형문자 -> 렌즈로 비춰 해독(SteleDecodeUI) -> 몇 글자는 "?"로 남은 글이 단서 패널에 뜸 (RaNightClueText)
//   한 번 해독하면 F_CLUE_<id>_NIGHT_DECODED -> 다음부터 바로 패널
public class ClueStele : MonoBehaviour, IInteractable
{
    [Header("연결")]
    [SerializeField] private ClueData data;

    // [추가] 다른 화면이 이 비석의 그림을 빌려 쓸 때 (P4 밤 힌트: 낮 벽화)
    public ClueData Data => data;
    [Tooltip("비우면 씬에서 자동으로 찾음")]
    [SerializeField] private CluePanel panel;

    [Header("새 단서 말풍선 (UI_Prompt_New를 넣은 자식 SpriteRenderer)")]
    [SerializeField] private SpriteRenderer newBubble;
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 3f;
    [Tooltip("방에 들어왔을 때 말풍선이 커지며 나타나는 시간 (초)")]
    [SerializeField] private float popTime = 0.2f;

    [Header("방 범위 (플레이어가 이 안에 있을 때만 \"!\" 표시)")]
    [Tooltip("방 가운데 빈 오브젝트. 비우면 비석 위치 기준")]
    [SerializeField] private Transform roomCenter;
    [Tooltip("방 가로 x 세로 (유닛). 0이면 범위 체크 안 함")]
    [SerializeField] private Vector2 roomSize = Vector2.zero;

    [Header("읽을 때 추가로 켤 플래그 (기존 NoteStone의 flagOnRead, 비우면 없음)")]
    [SerializeField] private string flagOnRead = "";

    [Header("[추가] 빛이 있어야 읽힘 (횃불 신전)")]
    [SerializeField] private bool needLight = true;
    [Tooltip("이 비석이 있는 방의 벽 횃불 (하나라도 켜지면 읽힘). 플레이어 손 횃불은 안 셈. 배치 메뉴가 채움")]
    [SerializeField] private WallTorch[] roomTorches;
    [Tooltip("Room Torches가 비었을 때만: 이 반경 안의 벽 횃불이 켜져 있으면 밝음")]
    [SerializeField] private float lightRadius = 6.5f;
    [SerializeField] private DialogueLine[] darkLines = { new DialogueLine("나", "너무 어두워서 글씨가 보이지 않는다. 이 방 벽 횃불부터 밝히자.") };

    [Header("[추가] 밤: 손상된 글을 렌즈로 해독")]
    [SerializeField] private bool lensDecodeAtNight = true;

    private bool wasLit;
    private bool decoding;

    private Vector3 bubbleBasePos;
    private Vector3 bubbleBaseScale = Vector3.one;
    private float popTimer;
    private bool valid;
    private Transform player;

    private string ReadFlag => $"F_CLUE_{data.ClueId}{(RaPhaseState.IsNight ? "_NIGHT" : "")}_READ";
    private string DecodedFlag => $"F_CLUE_{data.ClueId}_NIGHT_DECODED";

    // [추가] 근처 벽 횃불이나 성소 화로가 켜져 있으면 밝음
    private bool IsLitHere()
    {
        if (!needLight) return true;
        if (roomTorches != null && roomTorches.Length > 0)
        {
            foreach (WallTorch t in roomTorches) if (t != null && t.IsLit) return true;
            return false; // 규칙: 그 방 벽 횃불을 밝혀야 읽힘 (손에 든 횃불, 옆 방 불빛으로는 안 됨)
        }
        return WallTorch.AnyLitNear(transform.position, lightRadius);
    }

    // ================= 초기화 =================
    private void Start()
    {
        try
        {
            if (panel == null) panel = FindFirstObjectByType<CluePanel>(FindObjectsInactive.Include);

            valid = ValidateRefs();
            CacheBubble();
            FindPlayer();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 비석 초기화 오류: {e}");
        }
    }

    // 비어 있는 칸을 하나씩 정확히 알려줌
    private bool ValidateRefs()
    {
        if (data == null)
        {
            Debug.LogError($"[{name}] ClueStele의 Data 칸이 비어 있음 -> Clue Data 에셋(Clue_RA_...)을 끌어다 넣기");
            return false;
        }
        if (string.IsNullOrEmpty(data.ClueId))
        {
            Debug.LogError($"[{name}] Clue Data '{data.name}'의 Clue Id가 비어 있음 -> 에셋 클릭 후 Clue Id 입력 (예: RA_P4)");
            return false;
        }
        if (panel == null)
        {
            Debug.LogError($"[{name}] 씬에 CluePanel이 없음 -> RaCanvas 아래 Clue_Overlay 오브젝트에 CluePanel 붙이기");
            return false;
        }
        return true;
    }

    private void CacheBubble()
    {
        if (newBubble == null) return;

        bubbleBasePos = newBubble.transform.localPosition;
        bubbleBaseScale = newBubble.transform.localScale;
        newBubble.enabled = false;
    }

    private void FindPlayer()
    {
        if (player != null) return;

        PlayerController2D pc = FindAnyObjectByType<PlayerController2D>();
        if (pc != null) player = pc.transform;
    }

    // ================= 말풍선 =================
    private void Update()
    {
        try
        {
            UpdateBubble();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 말풍선 갱신 오류: {e}");
            enabled = false; // 매 프레임 같은 오류 도배 방지
        }
    }

    // 안 읽었고 + 방 안에 있으면 "!"가 톡 커지며 나타나고 위아래로 둥실둥실
    private void UpdateBubble()
    {
        if (newBubble == null) return;

        bool lit = IsLitHere();
        if (lit && !wasLit) RevealSparkle();
        wasLit = lit;

        bool show = valid && lit && !IsRead() && IsPlayerInRoom();
        if (!show)
        {
            newBubble.enabled = false;
            popTimer = 0f;
            return;
        }

        newBubble.enabled = true;
        popTimer = Mathf.Min(popTimer + Time.deltaTime, Mathf.Max(0.01f, popTime));
        float pop = popTimer / Mathf.Max(0.01f, popTime);

        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        newBubble.transform.localPosition = bubbleBasePos + new Vector3(0f, y, 0f);
        newBubble.transform.localScale = bubbleBaseScale * Mathf.SmoothStep(0f, 1f, pop);
    }

    // [추가] 불이 들어와 글씨가 드러나는 순간
    private void RevealSparkle()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;
        for (int i = 0; i < 8; i++)
        {
            RaGlowFx.Spark(transform.position + new Vector3(0f, 0.5f + i * 0.08f, 0f), new Color(0.55f, 0.75f, 1f), sr.sortingLayerName, sr.sortingOrder + 1);
        }
    }

    private bool IsRead()
    {
        return GameManager.Instance != null && GameManager.Instance.HasFlag(ReadFlag);
    }

    private bool IsPlayerInRoom()
    {
        if (roomSize.x <= 0f || roomSize.y <= 0f) return true; // 범위 안 정했으면 항상

        FindPlayer();
        if (player == null) return false;

        Rect room = GetRoomRect();
        return room.Contains((Vector2)player.position);
    }

    private Rect GetRoomRect()
    {
        Vector2 center = roomCenter != null ? (Vector2)roomCenter.position : (Vector2)transform.position;
        return new Rect(center - roomSize * 0.5f, roomSize);
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        if (decoding) return null;
        if (valid && panel.IsOpenFor(this)) return "E: 닫기";
        if (!IsLitHere()) return "E: 살펴보기";
        return "E: 비석 읽기";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (!valid || decoding) return;

            if (panel.IsOpenFor(this))
            {
                panel.Hide();
                return;
            }

            // [추가] 어두우면 못 읽음
            if (!IsLitHere())
            {
                if (!DialogueBox.IsOpen && DialogueBox.ClosedFrame != Time.frameCount) DialogueBox.Play(darkLines);
                return;
            }

            // [추가] 밤: 처음 한 번은 렌즈로 해독
            if (RaPhaseState.IsNight && lensDecodeAtNight && !HasFlag(DecodedFlag))
            {
                StartCoroutine(DecodeThenShow(who));
                return;
            }

            panel.Show(CurrentContent(), this, who);
            MarkRead();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 비석 상호작용 오류: {e}");
        }
    }

    // [추가] 밤에는 손상된 글(몇 글자 "?")로 보여줌. 그림은 그대로
    private ClueData.ClueContent CurrentContent()
    {
        ClueData.ClueContent c = data.Get(RaPhaseState.IsNight);
        if (!RaPhaseState.IsNight || !lensDecodeAtNight) return c;

        return new ClueData.ClueContent
        {
            title = c.title,
            body = RaNightClueText.Get(data.ClueId, c.body),
            illustration = c.illustration,
            illustrationTint = c.illustrationTint
        };
    }

    // [추가] 상형문자 판 -> 렌즈로 비춰 해독 -> 단서 패널
    private IEnumerator DecodeThenShow(GameObject who)
    {
        decoding = true;
        PlayerLock.Lock(this);
        ClueData.ClueContent c = CurrentContent();
        yield return SteleDecodeUI.Decode(c.title, RaNightClueText.ToLines(c.body, 12));

        GameManager gm = GameManager.Instance;
        if (gm != null && !gm.HasFlag(DecodedFlag)) gm.SetFlag(DecodedFlag);
        PlayerLock.Unlock(this);
        decoding = false;

        yield return null; // 판을 닫은 E키로 패널이 바로 닫히지 않게
        panel.Show(c, this, who);
        MarkRead();
    }

    private static bool HasFlag(string flag)
    {
        return !string.IsNullOrEmpty(flag) && GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
    }

    // 이미 켜진 플래그를 또 켜면 GameManager가 로그를 남기니까 확인 후 켬
    private void MarkRead()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning($"[{name}] GameManager가 없어서 읽음 표시를 못 함");
            return;
        }

        if (!gm.HasFlag(ReadFlag)) gm.SetFlag(ReadFlag);
        if (!string.IsNullOrEmpty(flagOnRead) && !gm.HasFlag(flagOnRead)) gm.SetFlag(flagOnRead);
    }

    // ================= 에디터: 방 범위를 Scene 창에 초록 상자로 표시 =================
    private void OnDrawGizmosSelected()
    {
        if (roomSize.x <= 0f || roomSize.y <= 0f) return;

        Rect room = GetRoomRect();
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireCube(room.center, room.size);
    }
}