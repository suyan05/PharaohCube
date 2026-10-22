using UnityEngine;

// 바스테트 구역 방 영역 표시 (게임 동작 없음, Scene 뷰에서 타일 칠할 자리를 보여 주는 가이드)
// Tools/Bastet/5개 방 배치 만들기 메뉴가 방마다 하나씩 만듦
public class BastetRoomArea : MonoBehaviour
{
    [Tooltip("방 이름 (Scene 뷰 라벨)")]
    [SerializeField] private string roomName = "방";
    [Tooltip("바닥 크기 (칸). 요청서 3장 표 기준")]
    [SerializeField] private Vector2 size = new Vector2(14f, 11f);
    [SerializeField] private Color color = new Color(0.9f, 0.5f, 0.6f, 0.9f);

    public Vector2 Size => size;

    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
        // 벽 앞면 자리 (위쪽 2칸): 3/4 탑다운 벽이 올라갈 곳
        Color wall = color; wall.a *= 0.35f;
        Gizmos.color = wall;
        Gizmos.DrawCube(transform.position + new Vector3(0f, size.y * 0.5f + 1f, 0f), new Vector3(size.x, 2f, 0f));
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.Label(transform.position + new Vector3(-size.x * 0.5f, size.y * 0.5f + 2.6f, 0f), $"{roomName} ({size.x:0} x {size.y:0}칸)");
#endif
    }

#if UNITY_EDITOR
    public void EditorSetup(string n, Vector2 s, Color c) { roomName = n; size = s; color = c; }
#endif
}
