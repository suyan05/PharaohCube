using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 라의 불씨가 배에서 떠올라 플레이어 횃불로 날아가는 연출 (RaSunBoat가 부름)
// 1. 배 안에서 천천히 떠오름 (위아래로 살랑)
// 2. 둥근 호를 그리며 플레이어 머리 위로 날아감 (불똥 꼬리)
// 3. 작아지며 횃불에 스며듦
public static class RaEmberFlight
{
    public static IEnumerator Fly(Vector3 from, Transform target, Color color, SpriteRenderer sortRef)
    {
        string layer = sortRef != null ? sortRef.sortingLayerName : "Default";
        int order = sortRef != null ? sortRef.sortingOrder + 5 : 50;

        var orb = new GameObject("RaEmber_Orb");
        orb.transform.position = from;
        SpriteRenderer glow = RaGlowFx.Glow(orb.transform, Vector3.zero, 0.9f, color, layer, order);
        RaGlowFx.Glow(orb.transform, Vector3.zero, 0.35f, new Color(1f, 0.95f, 0.8f), layer, order + 1);
        Light2D light = RaGlowFx.PointLight(orb.transform, Vector3.zero, color, 2.4f, 1.6f);

        yield return Rise(orb.transform, color, layer, order);
        yield return Arc(orb.transform, target, color, layer, order);
        yield return Absorb(orb.transform, glow, light);
        Object.Destroy(orb);
    }

    private static IEnumerator Rise(Transform orb, Color color, string layer, int order)
    {
        Vector3 start = orb.position;
        float t = 0f;
        while (t < 0.9f)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / 0.9f);
            orb.position = start + new Vector3(Mathf.Sin(t * 9f) * 0.05f, k * 0.8f, 0f);
            if (Random.value < 0.4f) RaGlowFx.Spark(orb.position, color, layer, order - 1);
            yield return null;
        }
    }

    // 2차 베지어 곡선 (시작 -> 위로 볼록 -> 플레이어 머리 위)
    private static IEnumerator Arc(Transform orb, Transform target, Color color, string layer, int order)
    {
        Vector3 a = orb.position;
        float t = 0f;
        const float seconds = 1.1f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            Vector3 b = TargetPoint(target, a);
            Vector3 mid = (a + b) * 0.5f + Vector3.up * 1.6f;
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            orb.position = Vector3.Lerp(Vector3.Lerp(a, mid, k), Vector3.Lerp(mid, b, k), k);
            RaGlowFx.Spark(orb.position, color, layer, order - 1);
            yield return null;
        }
    }

    private static IEnumerator Absorb(Transform orb, SpriteRenderer glow, Light2D light)
    {
        Vector3 scale = orb.localScale;
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float k = 1f - t / 0.3f;
            orb.localScale = scale * Mathf.Max(0.05f, k);
            if (light != null) light.intensity = 1.6f + (1f - k) * 2.5f;
            if (glow != null) glow.color = new Color(glow.color.r, glow.color.g, glow.color.b, k);
            yield return null;
        }
    }

    private static Vector3 TargetPoint(Transform target, Vector3 fallback)
    {
        return target != null ? target.position + Vector3.up * 0.8f : fallback + Vector3.up * 2f;
    }
}
