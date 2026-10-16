using UnityEngine;

// 맵에 놓이는 양피지 조각. 가까이 가서 E키로 줍기
// [추가] 피드백 "양피지를 주워야 하는지 모르겠다" 대응
// - 조각 그림이 위아래로 둥실거리고 반짝여서 "주울 수 있는 물건"으로 보이게
// - 플레이어가 처음으로 조각 근처에 오면 한 번만 안내 문구 (씬 전체에서 1번)
// - 처음 주울 때 "조각을 모으면 푸는 방법이 드러난다" 안내 (씬 전체에서 1번)
[RequireComponent(typeof(Collider2D))]
public class AnubisScrollPiece : MonoBehaviour, IInteractable
{
    [Header("어떤 양피지의 몇 번 조각인지")]
    [SerializeField] private AnubisScrollData scroll;
    [Tooltip("조각 번호 (1부터 시작)")]
    [Min(1)]
    [SerializeField] private int pieceNumber = 1;

    [Header("연결 (비우면 자동으로 찾음)")]
    [SerializeField] private AnubisScrollCollection collection;

    [Header("눈에 띄게 [추가]")]
    [Tooltip("움직일 그림 (비우면 이 오브젝트나 자식의 SpriteRenderer)")]
    [SerializeField] private SpriteRenderer visual;
    [Tooltip("위아래로 둥실거리는 높이 (유닛)")]
    [Range(0f, 0.3f)] [SerializeField] private float bobHeight = 0.08f;
    [Range(0.5f, 5f)] [SerializeField] private float bobSpeed = 2.2f;
    [Tooltip("반짝임 색 (기본 색과 이 색 사이를 오감)")]
    [SerializeField] private Color shineColor = new Color(1f, 0.93f, 0.6f, 1f);
    [Range(0.5f, 6f)] [SerializeField] private float shineSpeed = 3f;

    [Header("첫 안내 [추가]")]
    [Tooltip("이 거리 안에 처음 들어오면 안내 (유닛)")]
    [Range(0.5f, 6f)] [SerializeField] private float noticeRadius = 2.5f;
    [TextArea(1, 3)]
    [SerializeField] private string approachMessage = "바닥에 빛나는 양피지 조각이 있다.\nE로 주우면 시험을 푸는 방법이 조금씩 드러난다.";
    [TextArea(1, 3)]
    [SerializeField] private string firstPickupMessage = "양피지 조각을 주웠다! 조각을 모을수록 글이 드러난다.\n[H] 양피지 보기 · 제단 퍼즐 화면에서도 볼 수 있다.";

    // 씬 전체에서 한 번만 안내 (조각마다 뜨면 귀찮으니까)
    private static bool approachShown;
    private static bool pickupShown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        approachShown = false;
        pickupShown = false;
    }

    private Transform visualTr;
    private Vector3 visualBasePos;
    private Color visualBaseColor;
    private Transform player;
    private float noticeRadiusSqr;

    private void Awake()
    {
        if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
        if (visual != null)
        {
            visualTr = visual.transform;
            visualBasePos = visualTr.localPosition;
            visualBaseColor = visual.color;
        }
        noticeRadiusSqr = noticeRadius * noticeRadius;
    }

    private void Start()
    {
        if (collection == null) collection = FindFirstObjectByType<AnubisScrollCollection>();
        if (collection == null) Debug.LogError($"[{name}] AnubisScrollCollection이 씬에 없음 (Managers 확인)", this);
        if (scroll == null) Debug.LogError($"[{name}] 양피지 데이터(Scroll) 미연결", this);

        // 이미 주운 조각이면 숨김
        if (collection != null && collection.Has(scroll, pieceNumber - 1)) gameObject.SetActive(false);

        var pc = FindAnyObjectByType<PlayerController2D>(); // 한 번만 찾음
        if (pc != null) player = pc.transform;
    }

    private void Update()
    {
        Animate();
        CheckApproach();
    }

    // 둥실거림 + 반짝임 (할당 없음)
    private void Animate()
    {
        if (visualTr == null) return;
        float t = Time.time;

        // 그림이 이 오브젝트 자체면 위치는 건드리지 않음 (콜라이더가 같이 움직이지 않게)
        if (visualTr != transform)
            visualTr.localPosition = visualBasePos + Vector3.up * (Mathf.Sin(t * bobSpeed) * bobHeight);

        float k = (Mathf.Sin(t * shineSpeed) + 1f) * 0.5f;
        visual.color = Color.Lerp(visualBaseColor, shineColor, k * 0.6f);
    }

    private void CheckApproach()
    {
        if (approachShown || player == null) return;
        if ((player.position - transform.position).sqrMagnitude > noticeRadiusSqr) return;

        approachShown = true;
        AnubisToast.Show(approachMessage, 5f);
    }

    public string GetPrompt() => "E: 양피지 조각 줍기";

    public void Interact(GameObject player)
    {
        if (collection == null || scroll == null) return;
        collection.Collect(scroll, pieceNumber - 1);

        if (!pickupShown)
        {
            pickupShown = true;
            approachShown = true;
            AnubisToast.Show(firstPickupMessage, 5f);
        }
        gameObject.SetActive(false);
    }

    // Scene 뷰: 조각 위치, 번호, 첫 안내 범위 표시
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.95f, 0.85f, 0.6f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.35f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"{(scroll != null ? scroll.Title : "?")} #{pieceNumber}");
#endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.95f, 0.85f, 0.6f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, noticeRadius);
    }
}
