using System;
using UnityEngine;

// 아직 열리지 않은 구역 입구
// - AnubisZoneGuide의 Turn Off 칸에 넣어서 씀: 구역 불이 켜지면 이 오브젝트가 꺼져서 길이 열림
// - 플레이어가 부딪히면 AnubisToast로 안내
[RequireComponent(typeof(BoxCollider2D))]
public class AnubisLockedPassage : MonoBehaviour
{
    [Tooltip("부딪혔을 때 화면에 띄울 문구")]
    [SerializeField, TextArea] private string message = "안쪽이 너무 어두워 들어갈 수 없다.\n앞선 시험을 먼저 통과하자.";

    [Tooltip("같은 문구를 다시 띄우기까지 기다리는 시간 (초). 벽에 계속 부딪힐 때 도배 방지")]
    [SerializeField, Range(0.5f, 10f)] private float cooldown = 3f;

    private float nextShowTime;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        try
        {
            if (!IsPlayer(collision.collider)) return;
            if (Time.time < nextShowTime) return;

            nextShowTime = Time.time + cooldown;
            AnubisToast.Show(message);
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 잠긴 입구 처리 오류: {e}");
        }
    }

    // 플레이어 콜라이더가 자식에 붙어 있어도 찾도록 부모까지 확인
    private static bool IsPlayer(Collider2D other)
    {
        return other != null && other.GetComponentInParent<PlayerController2D>() != null;
    }

    // Scene 뷰: 막힌 입구를 빨간 상자로 표시
    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.6f);
        Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.size);
    }
}