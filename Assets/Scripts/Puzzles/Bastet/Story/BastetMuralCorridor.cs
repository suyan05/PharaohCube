using UnityEngine;

// [추가] 바스테트 입구 벽화 복도 (호루스 입구 복도처럼)
// - 같은 오브젝트의 CameraRoomZone(공용)이 복도 안에서 카메라를 복도 높이에 맞춰 내려 줌
//   (복도가 맵 아래쪽 끝에 붙어 있으면 카메라가 맵 경계에 걸려 위쪽 빈 공간을 보여 주던 문제 해결)
//   복도가 화면보다 낮으므로 위아래는 복도 가운데에 고정, 좌우는 플레이어를 따라감
// - [ContextMenu] '벽화 4장을 윗벽에 나란히 걸기': BoxCollider2D(=복도 바닥) 윗변에 벽화를 같은 간격으로 배치
// 붙이는 곳: Bastet_Story/MuralCorridor (Tools/Bastet/★ 벽화 복도 메뉴가 만듦)
// Scene 뷰: 초록 상자 = 복도 바닥, 노란 칸 = 벽화 자리. 상자 크기는 BoxCollider2D의 Edit Collider로 조절
[RequireComponent(typeof(BoxCollider2D))]
public class BastetMuralCorridor : MonoBehaviour
{
    [Header("벽화 (번호 순서대로 왼쪽부터 걸림)")]
    [SerializeField] private BastetMural[] murals = new BastetMural[4];

    [Header("걸기")]
    [Tooltip("복도 바닥 윗변 기준 벽화 아래끝 높이 (0 = 바닥과 벽이 만나는 곳)")]
    [SerializeField] private float wallOffset = 0f;
    [Tooltip("복도 양 끝에서 비워 둘 칸")]
    [SerializeField] private float sidePadding = 1.5f;

    private BoxCollider2D box;

    private Bounds Floor
    {
        get
        {
            if (box == null) box = GetComponent<BoxCollider2D>();
            return box.bounds;
        }
    }

    // i번째(0부터) 벽화가 걸릴 자리 (벽화 아래끝 가운데)
    public Vector3 Slot(int i)
    {
        Bounds b = Floor;
        int n = Mathf.Max(1, murals.Length);
        float x0 = b.min.x + sidePadding, x1 = b.max.x - sidePadding;
        float x = Mathf.Lerp(x0, x1, (i + 0.5f) / n);
        return new Vector3(x, b.max.y + wallOffset, 0f);
    }

    [ContextMenu("벽화 4장을 윗벽에 나란히 걸기")]
    public void Arrange()
    {
        for (int i = 0; i < murals.Length; i++)
        {
            if (murals[i] == null) continue;
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(murals[i].transform, "벽화 걸기");
#endif
            murals[i].transform.position = Slot(i);
        }
        Debug.Log($"[BastetMuralCorridor] 벽화 {murals.Length}장을 복도 윗벽에 걸었음", this);
    }

    private void OnDrawGizmos()
    {
        Bounds b = Floor;
        Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireCube(b.center, b.size);
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.8f);
        for (int i = 0; i < murals.Length; i++)
        {
            Vector3 p = Slot(i);
            Gizmos.DrawWireCube(p + Vector3.up, new Vector3(4f, 2f, 0f)); // 도트 벽화 4x2칸
        }
#if UNITY_EDITOR
        UnityEditor.Handles.Label(new Vector3(b.min.x, b.max.y + 2.6f, 0f), "벽화 복도 (카메라가 이 높이에 맞춰 내려옴)");
#endif
    }

#if UNITY_EDITOR
    public void EditorSetup(BastetMural[] m) { murals = m; }
#endif
}
