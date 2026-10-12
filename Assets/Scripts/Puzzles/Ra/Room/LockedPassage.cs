using System;
using UnityEngine;

// 잠긴 구역 입구 (RaZoneLighting의 Turn Off 칸에 넣어서 씀)
// - 플레이어가 부딪히면 화면 오른쪽 위에 안내 문구 표시
// - 문구는 GameMessageUI가 "[Door] "로 시작하는 로그를 알림으로 띄우는 기능을 그대로 이용
// - 구역 불이 켜지면 RaZoneLighting이 이 오브젝트를 꺼서 길이 열림
[RequireComponent(typeof(BoxCollider2D))]
public class LockedPassage : MonoBehaviour
{
    // GameMessageUI.HandleLog가 이 말머리로 시작하는 로그를 알림으로 띄움
    private const string MessageTag = "[Door] ";

    [Tooltip("부딪혔을 때 화면에 띄울 문구")]
    [SerializeField, TextArea]
    private string message = "안쪽이 너무 어두워 들어갈 수 없다. 회로판에 빛을 먼저 밝혀 보자.";

    [Tooltip("같은 문구를 다시 띄우기까지 기다리는 시간 (초). 벽에 계속 부딪힐 때 도배 방지")]
    [SerializeField] private float cooldown = 3f;

    private float nextShowTime;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        try
        {
            if (!IsPlayer(collision.collider)) return;
            if (Time.time < nextShowTime) return;

            nextShowTime = Time.time + cooldown;
            Debug.Log(MessageTag + message);
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 잠긴 입구 처리 오류: {e}");
        }
    }

    // 플레이어 콜라이더가 자식 오브젝트에 붙어 있어도 찾도록 부모까지 확인
    private static bool IsPlayer(Collider2D other)
    {
        return other != null && other.GetComponentInParent<PlayerController2D>() != null;
    }
}