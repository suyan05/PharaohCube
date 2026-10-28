using System;
using System.Collections;
using UnityEngine;

// 연출용 캐릭터, 물건 움직이기
// - WalkTo: Rigidbody2D 속도로 걸려서 PlayerSpriteAnimator가 걷기 애니메이션, 방향을 알아서 바꿈
//           (스킵 때는 목표 위치로 순간이동)
//           [수정] 물건에 막혀서 더 못 가면 제자리걸음 하지 않고 그 자리에서 바로 멈춤 (예: 무전기 앞)
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
        var stuck = new StuckCheck(actor.position);

        while (timeout > 0f && !Cine.Skipping)
        {
            Vector2 diff = point - (Vector2)actor.position;
            if (diff.magnitude <= speed * Time.deltaTime * 1.5f) break;
            if (stuck.Update(actor.position, Time.deltaTime))
            {
                Place(actor, body, actor.position); // 막힘 -> 여기서 멈춤 (순간이동 없이)
                yield break;
            }

            SetVelocity(actor, body, diff.normalized * speed);
            timeout -= Time.deltaTime;
            yield return null;
        }
        Place(actor, body, point);
    }

    // 0.2초 동안 거의 못 움직였으면 막힌 것
    private class StuckCheck
    {
        private const float Window = 0.2f;
        private const float MinMove = 0.03f;
        private Vector2 last;
        private float timer;

        public StuckCheck(Vector2 start)
        {
            last = start;
        }

        public bool Update(Vector2 now, float dt)
        {
            timer += dt;
            if (timer < Window) return false;
            bool stuck = Vector2.Distance(now, last) < MinMove;
            last = now;
            timer = 0f;
            return stuck;
        }
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

    // ================= 두리번거리기 =================
    // 아주 잠깐 그 방향으로 속도를 줘서 PlayerSpriteAnimator가 그쪽을 보게 함 (거의 안 움직임)
    public static IEnumerator Look(Transform actor, Vector2 dir)
    {
        Rigidbody2D body = actor != null ? actor.GetComponent<Rigidbody2D>() : null;
        if (body == null || dir == Vector2.zero) yield break;

        body.linearVelocity = dir.normalized;
        yield return null;
        yield return null;
        if (body != null) body.linearVelocity = Vector2.zero;
    }

    // ================= 눕기 / 일어나기 =================
    // 누운 그림(Player_Lie_Down)이 오기 전까지 캐릭터 그림을 옆으로 90도 눕혀서 대신함
    public static void LieDown(Transform actor, bool lying)
    {
        Transform visual = VisualOf(actor);
        if (visual != null) visual.localRotation = lying ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;
    }

    // 90도 -> 0도로 일으키면서 살짝 들썩
    public static IEnumerator StandUp(Transform actor, float seconds)
    {
        Transform visual = VisualOf(actor);
        if (visual == null) yield break;

        Vector3 origin = visual.localPosition;
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / seconds);
            visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(90f, 0f, p * p));
            visual.localPosition = origin + Vector3.up * Mathf.Sin(p * Mathf.PI) * 0.15f;
            yield return null;
        }
        visual.localRotation = Quaternion.identity;
        visual.localPosition = origin;
    }

    private static Transform VisualOf(Transform actor)
    {
        if (actor == null) return null;
        SpriteRenderer sr = actor.GetComponentInChildren<SpriteRenderer>();
        return sr != null ? sr.transform : actor;
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
