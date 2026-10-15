using System;
using UnityEngine;

// 파란 비석: E -> 단서 패널 열기 / 닫기 (기존 NoteStone 대체)
// - 아직 안 읽은 단서 + 플레이어가 이 방 안에 있을 때만 머리 위에 "!" 말풍선 (UI_Prompt_New)
// - 방 범위는 Room Center + Room Size로 정함 (Scene 창에 초록 상자로 보임, 물리 충돌 없음)
//   Room Size가 0이면 범위 체크 없이 항상 표시
// - 읽음 플래그는 낮/밤 따로 -> 밤이 되어 단서가 바뀌면 "!"가 다시 뜸
//   낮: F_CLUE_<id>_READ / 밤: F_CLUE_<id>_NIGHT_READ (나중에 저널이 이 플래그로 목록을 만듦)
public class ClueStele : MonoBehaviour, IInteractable
{
    [Header("연결")]
    [SerializeField] private ClueData data;
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

    private Vector3 bubbleBasePos;
    private Vector3 bubbleBaseScale = Vector3.one;
    private float popTimer;
    private bool valid;
    private Transform player;

    private string ReadFlag => $"F_CLUE_{data.ClueId}{(RaPhaseState.IsNight ? "_NIGHT" : "")}_READ";

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

        bool show = valid && !IsRead() && IsPlayerInRoom();
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
        if (valid && panel.IsOpenFor(this)) return "E: 닫기";
        return "E: 비석 읽기";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (!valid) return;

            if (panel.IsOpenFor(this))
            {
                panel.Hide();
                return;
            }

            panel.Show(data.Get(RaPhaseState.IsNight), this, who);
            MarkRead();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 비석 상호작용 오류: {e}");
        }
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