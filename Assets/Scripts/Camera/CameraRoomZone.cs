using UnityEngine;

// [추가] 방 카메라 영역: 플레이어가 이 영역(방)에 들어오면
//  1) 카메라가 방 밖(빈 파란 화면, 옆 방)을 보여 주지 않게 방 경계 안에 가둠
//  2) 방이 화면보다 크면 방 전체가 보이도록 살짝 줌 아웃 (fitWholeRoom)
//  방에서 나가면(통로) 원래 줌·맵 경계로 돌아감
// 붙이는 곳: 방마다 빈 오브젝트 + BoxCollider2D(Is Trigger) = 방 바닥 크기 (Tools/Bastet 메뉴가 자동으로 만듦)
[RequireComponent(typeof(BoxCollider2D))]
public class CameraRoomZone : MonoBehaviour
{
    [Header("보여 줄 범위 (트리거 크기 + 여백)")]
    [Tooltip("트리거 바깥으로 더 보여 줄 칸 (왼, 오른, 아래, 위). 위는 3/4 탑다운 벽 앞면이 보이게 넉넉히")]
    [SerializeField] private Vector4 margin = new Vector4(1f, 1f, 1f, 2.5f);

    [Header("줌")]
    [Tooltip("방이 화면보다 크면 방 전체가 보이게 줌 아웃")]
    [SerializeField] private bool fitWholeRoom = true;
    [Tooltip("이 크기보다 더 멀리 줌 아웃하지 않음 (글자·캐릭터가 너무 작아지는 것 방지)")]
    [SerializeField] private float maxSize = 7.5f;
    [SerializeField] private string playerTag = "Player";

    private static CameraRoomZone active;
    private BoxCollider2D box;
    private CameraFollow2D cam;

    public Bounds ViewBounds
    {
        get
        {
            if (box == null) box = GetComponent<BoxCollider2D>();
            Bounds b = box.bounds;
            Vector3 min = b.min - new Vector3(margin.x, margin.z, 0f);
            Vector3 max = b.max + new Vector3(margin.y, margin.w, 0f);
            b.SetMinMax(min, max);
            return b;
        }
    }

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        if (!box.isTrigger) { box.isTrigger = true; Debug.LogWarning($"[CameraRoomZone] {name}: Is Trigger를 켰음", this); }
    }

    private bool IsPlayer(Collider2D c) =>
        c.CompareTag(playerTag) || (c.attachedRigidbody != null && c.attachedRigidbody.CompareTag(playerTag));

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other)) Enter();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 방 두 개가 맞닿은 곳에서 하나를 나갔을 때 남은 방으로 다시 잡기
        if (active == null && IsPlayer(other)) Enter();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other) || active != this) return;
        active = null;
        if (FindCam() == null) return;
        cam.ClearRoomBounds();
        cam.ResetZoom();
        Debug.Log($"[CameraRoomZone] {name} 나감 -> 원래 카메라");
    }

    private void Enter()
    {
        if (active == this || FindCam() == null) return;
        active = this;
        Bounds b = ViewBounds;
        cam.SetRoomBounds(b);
        if (fitWholeRoom)
        {
            Camera c = cam.GetComponent<Camera>();
            float fit = Mathf.Max(b.extents.y, b.extents.x / Mathf.Max(0.01f, c.aspect));
            cam.SetZoom(Mathf.Clamp(fit, cam.BaseSize, Mathf.Max(cam.BaseSize, maxSize)));
        }
        Debug.Log($"[CameraRoomZone] {name} 들어감 -> 방 크기 {b.size.x:0.#} x {b.size.y:0.#}칸에 맞춤");
    }

    private CameraFollow2D FindCam()
    {
        if (cam == null && Camera.main != null) cam = Camera.main.GetComponent<CameraFollow2D>();
        return cam;
    }

    private void OnDisable()
    {
        if (active == this) active = null;
    }

#if UNITY_EDITOR
    public void EditorSetup(Vector2 size, Vector4 m) { GetComponent<BoxCollider2D>().size = size; margin = m; }
#endif

    // Scene 뷰: 카메라가 보여 줄 범위 (하늘색)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
        Bounds b = ViewBounds;
        Gizmos.DrawWireCube(b.center, b.size);
    }
}
