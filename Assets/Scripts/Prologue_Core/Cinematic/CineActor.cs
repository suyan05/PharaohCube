using System;
using System.Collections;
using UnityEngine;

// 연출용 캐릭터, 물건 움직이기
// - WalkTo: Rigidbody2D 속도로 걸려서 PlayerSpriteAnimator가 걷기 애니메이션, 방향을 알아서 바꿈
//           (막혀서 못 가면 시간이 지나면 목표 위치로 순간이동, 스킵 때도 순간이동)
// - Fade: 캐릭터 그림 투명도 (텐트에서 나오기 등)
// - Jitter: 제자리 덜덜 (무전기 울림)
// - Sink: 작아지면서 아래로 빨려 들어감 (구멍에 떨어지기)
public static class CineActor
{
    public static Transform Player()
    {
        PlayerController2D player = UnityEngine.Object.FindAnyObjectByType<PlayerController2D>();
        if (player == null) Debug.LogWarning("[CineActor] 플레이어를 못 찾음");
        return player != null ? player.transform : null;
    }

    // ================= 걷기 =================
    public static IEnumerator WalkTo(Transform actor, Vector2 point, float speed = 3f)
    {
        if (actor == null) yield break;

        Rigidbody2D body = actor.GetComponent<Rigidbody2D>();
        float timeout = Vector2.Distance(actor.position, point) / Mathf.Max(0.1f, speed) + 1.5f;

        while (timeout > 0f && !Cine.Skipping)
        {
            Vector2 diff = point - (Vector2)actor.position;
            if (diff.magnitude <= speed * Time.deltaTime * 1.5f) break;

            SetVelocity(actor, body, diff.normalized * speed);
            timeout -= Time.deltaTime;
            yield return null;
        }
        Place(actor, body, point);
    }

    private static void SetVelocity(Transform actor, Rigidbody2D body, Vector2 velocity)
    {
        if (body != null) body.linearVelocity = velocity;
        else actor.position += (Vector3)(velocity * Time.deltaTime);
    }

    public static void Place(Transform actor, Rigidbody2D body, Vector2 point)
    {
        if (actor == null) return;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.position = point;
        }
        actor.position = new Vector3(point.x, point.y, actor.position.z);
    }

    // ================= 그림 =================
    public static void SetAlpha(Transform actor, float alpha)
    {
        if (actor == null) return;
        foreach (SpriteRenderer sr in actor.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color c = sr.color;
            c.a = Mathf.Clamp01(alpha);
            sr.color = c;
        }
    }

    public static IEnumerator Fade(Transform actor, float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            SetAlpha(actor, Mathf.Lerp(from, to, t / seconds));
            yield return null;
        }
        SetAlpha(actor, to);
    }

    public static IEnumerator Jitter(Transform target, float seconds, float strength)
    {
        if (target == null) yield break;

        Vector3 origin = target.localPosition;
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            target.localPosition = origin + (Vector3)(UnityEngine.Random.insideUnitCircle * strength);
            yield return null;
        }
        target.localPosition = origin;
    }

    // 구멍으로 떨어짐: 아래로 내려가며 작아지고 사라짐 (원래 크기는 다음 씬에서 새 플레이어라 복구 안 함)
    public static IEnumerator Sink(Transform actor, float seconds, float depth)
    {
        if (actor == null) yield break;

        Vector3 start = actor.position;
        Vector3 scale = actor.localScale;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / seconds);
            actor.position = start + Vector3.down * depth * p * p;
            actor.localScale = scale * (1f - 0.6f * p);
            SetAlpha(actor, 1f - p);
            yield return null;
        }
    }
}
