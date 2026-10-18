using UnityEngine;

// 기둥 뒤로 지나가기 (탑다운 2D)
// - 기둥 그림의 아래쪽 일부(받침)만 막고, 위쪽은 플레이어가 지나갈 수 있게 콜라이더를 줄임
// - 플레이어 발이 받침보다 위(= 기둥 뒤)에 있으면 기둥을 플레이어보다 앞에 그려서 가림
//   아래(= 기둥 앞)에 있으면 플레이어가 기둥 앞에 그려짐
// - 기둥과 플레이어는 같은 Sorting Layer(Objects)를 써야 함 (Awake에서 플레이어 레이어로 맞춤)
// 기둥 오브젝트(SpriteRenderer + BoxCollider2D)에 부착
[RequireComponent(typeof(SpriteRenderer))]
public class PillarOcclusion : MonoBehaviour
{
    [Header("막히는 부분 (기둥 받침)")]
    [Tooltip("기둥 그림 높이 중 아래에서부터 막을 비율 (0.25 = 아래 1/4만 막힘, 위 3/4은 지나감)")]
    [Range(0.05f, 1f)]
    [SerializeField] private float blockHeightRatio = 0.25f;
    [Tooltip("막는 폭 비율 (1 = 그림 폭 그대로)")]
    [Range(0.2f, 1f)]
    [SerializeField] private float blockWidthRatio = 0.9f;
    [Tooltip("켜면 시작할 때 BoxCollider2D를 받침 크기로 자동 조절 (끄면 직접 맞춘 콜라이더 사용)")]
    [SerializeField] private bool fitColliderOnAwake = true;

    [Header("가림 판정")]
    [Tooltip("플레이어 발이 (받침 윗선 + 이 값)보다 위면 기둥 뒤로 판단 (유닛)")]
    [SerializeField] private float behindOffsetY = 0f;

    private SpriteRenderer sr;
    private BoxCollider2D col;
    private SpriteRenderer playerSr;
    private Collider2D playerCol;
    private Transform player;
    private float lineY;        // 이 높이보다 플레이어 발이 위면 기둥 뒤 (월드 y)
    private bool lastBehind;
    private bool hasState;
    private float nextFindTime;

    // ================= 초기화 =================
    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
        if (fitColliderOnAwake) FitCollider();
        CacheLine();
    }

    private void Start()
    {
        FindPlayer();
    }

    // 받침 윗선 (월드 y): 이 선을 기준으로 앞/뒤 판단
    private void CacheLine()
    {
        if (sr == null || sr.sprite == null) { lineY = transform.position.y; return; }
        Bounds b = sr.sprite.bounds; // 그림 기준 로컬 범위
        float localTop = b.min.y + b.size.y * blockHeightRatio;
        lineY = transform.TransformPoint(new Vector3(0f, localTop, 0f)).y + behindOffsetY;
    }

    private void FindPlayer()
    {
        if (player != null) return;
        var pc = FindAnyObjectByType<PlayerController2D>();
        if (pc == null) return;

        player = pc.transform;
        playerCol = pc.GetComponent<Collider2D>();
        playerSr = pc.GetComponentInChildren<SpriteRenderer>();

        // 같은 Sorting Layer여야 순서(Order)로 앞뒤가 바뀜
        if (playerSr != null && sr.sortingLayerID != playerSr.sortingLayerID)
        {
            Debug.Log($"[{name}] Sorting Layer를 플레이어와 같게 맞춤 ({SortingLayer.IDToName(playerSr.sortingLayerID)})", this);
            sr.sortingLayerID = playerSr.sortingLayerID;
        }
    }

    // ================= 매 프레임: 앞/뒤만 비교 (할당 없음) =================
    private void LateUpdate()
    {
        if (player == null)
        {
            if (Time.time >= nextFindTime) { nextFindTime = Time.time + 1f; FindPlayer(); }
            return;
        }
        if (playerSr == null) return;

        float feetY = playerCol != null ? playerCol.bounds.min.y : player.position.y;
        bool behind = feetY > lineY;
        if (hasState && behind == lastBehind) return; // 바뀔 때만 순서 변경

        hasState = true;
        lastBehind = behind;
        sr.sortingOrder = behind ? playerSr.sortingOrder + 1 : playerSr.sortingOrder - 1;
    }

    // ================= 콜라이더 =================
    [ContextMenu("콜라이더를 받침(아래쪽) 크기로 맞추기")]
    private void FitCollider()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null) return;

        if (col == null) col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();

        Bounds b = sr.sprite.bounds;
        float h = b.size.y * blockHeightRatio;
        col.isTrigger = false;
        col.enabled = true;
        col.size = new Vector2(b.size.x * blockWidthRatio, h);
        col.offset = new Vector2(b.center.x, b.min.y + h * 0.5f);
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(col);
#endif
    }

    // Scene 뷰: 빨강 = 막힌 받침, 하늘색 = 지나갈 수 있는(기둥 뒤로 숨는) 부분
    private void OnDrawGizmosSelected()
    {
        var r = GetComponent<SpriteRenderer>();
        if (r == null || r.sprite == null) return;

        Bounds b = r.sprite.bounds;
        float h = b.size.y * blockHeightRatio;
        Vector3 blockCenter = transform.TransformPoint(new Vector3(b.center.x, b.min.y + h * 0.5f, 0f));
        Vector3 blockSize = Vector3.Scale(new Vector3(b.size.x * blockWidthRatio, h, 0f), transform.lossyScale);
        Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(blockCenter, blockSize);

        float passH = b.size.y - h;
        Vector3 passCenter = transform.TransformPoint(new Vector3(b.center.x, b.max.y - passH * 0.5f, 0f));
        Vector3 passSize = Vector3.Scale(new Vector3(b.size.x, passH, 0f), transform.lossyScale);
        Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.9f);
        Gizmos.DrawWireCube(passCenter, passSize);
    }
}
