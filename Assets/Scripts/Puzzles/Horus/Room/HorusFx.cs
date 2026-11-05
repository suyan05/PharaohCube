using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 호루스의 방 연출 효과 모음 (HorusCutscenes, HorusHiddenGallery가 씀)
// - 모두 어둠 속에서도 보이게 Unlit 재질 (RaDarkness.Unlit)
// - 정렬: Above = 모든 물건 위 (날아가는 매, 떠오르는 형판), Floor = 바닥 바로 위 (매 그림자)
public static class HorusFx
{
    public const int Above = 32000;
    public const int Floor = -6;

    public static string Layer = "Default";

    // 플레이어와 같은 정렬 레이어 사용 (연출 시작 때 한 번)
    public static void MatchPlayer()
    {
        Transform p = CineActor.Player();
        SpriteRenderer sr = p != null ? p.GetComponentInChildren<SpriteRenderer>() : null;
        if (sr != null) Layer = sr.sortingLayerName;
    }

    public static SpriteRenderer Spawn(string objName, Sprite sprite, Vector3 pos, float size, Color color, int order)
    {
        var go = new GameObject(objName);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingLayerName = Layer;
        sr.sortingOrder = order;
        if (RaDarkness.Unlit != null) sr.sharedMaterial = RaDarkness.Unlit;
        if (sprite != null) go.transform.localScale = Vector3.one * (size / Mathf.Max(sprite.bounds.size.x, 0.01f));
        return sr;
    }

    // 매가 날개를 퍼덕이며 포물선으로 날아감 (from -> to). 그림은 도착하면 지움
    public static IEnumerator Fly(Sprite falcon, Vector3 from, Vector3 to, float seconds, float size, Color color, int order, float arc, bool trail = true)
    {
        SpriteRenderer sr = Spawn("FlyingFalcon", falcon, from, size, color, order);
        Vector3 baseScale = sr.transform.localScale;
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            sr.transform.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * arc;
            float flap = 1f + 0.18f * Mathf.Sin(t * 22f);
            sr.transform.localScale = new Vector3(baseScale.x * flap, baseScale.y * (2f - flap), 1f);
            if (trail && Random.value < 0.25f) Spark(sr.transform.position, new Color(1f, 0.85f, 0.45f));
            yield return null;
        }
        Object.Destroy(sr.gameObject);
    }

    // 금빛 불똥 (위로 떠오르며 사라짐)
    public static void Spark(Vector3 pos, Color color)
    {
        RaGlowFx.Spark(pos, color, Layer, Above);
    }

    // 범위 안 여기저기 별이 반짝 (count개)
    public static void StarBurst(Vector3 center, Vector2 halfSize, int count, Color color)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 p = center + new Vector3(Random.Range(-halfSize.x, halfSize.x), Random.Range(-halfSize.y, halfSize.y), 0f);
            Spark(p, color);
        }
    }

    // 빛 덩어리 하나를 seconds 동안 켰다가 꺼짐 (석상 눈, 이시스의 빛)
    public static IEnumerator Pulse(Vector3 pos, float size, Color color, float seconds)
    {
        SpriteRenderer glow = RaGlowFx.Glow(null, pos, size, color, Layer, Above - 1);
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            float a = Mathf.Sin(Mathf.Clamp01(t / seconds) * Mathf.PI);
            glow.color = new Color(color.r, color.g, color.b, color.a * a);
            yield return null;
        }
        Object.Destroy(glow.gameObject);
    }

    // 머리 위 말풍선 (!) 이 톡 튀어나왔다 사라짐 (만화 연출)
    public static IEnumerator Emote(Transform who, Sprite bubble, float seconds)
    {
        if (who == null || bubble == null) yield break;
        SpriteRenderer sr = Spawn("Emote", bubble, who.position + Vector3.up * 1.6f, 0.55f, Color.white, Above);
        Vector3 full = sr.transform.localScale;
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            float pop = t < 0.15f ? Mathf.Lerp(0f, 1.25f, t / 0.15f) : Mathf.Lerp(1.25f, 1f, (t - 0.15f) / 0.1f);
            sr.transform.localScale = full * Mathf.Max(pop, 0f);
            sr.transform.position = who.position + Vector3.up * (1.6f + Mathf.Sin(t * 8f) * 0.05f);
            yield return null;
        }
        Object.Destroy(sr.gameObject);
    }

    // 방 전체 밝기 바꾸기 (하늘을 되찾으면 밝아짐)
    public static IEnumerator LightTo(float intensity, Color color, float seconds)
    {
        Light2D global = GlobalLight();
        if (global == null) yield break;
        float from = global.intensity;
        Color fromColor = global.color;
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            float k = t / seconds;
            global.intensity = Mathf.Lerp(from, intensity, k);
            global.color = Color.Lerp(fromColor, color, k);
            yield return null;
        }
        global.intensity = intensity;
        global.color = color;
    }

    public static Light2D GlobalLight()
    {
        foreach (Light2D l in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (l.lightType == Light2D.LightType.Global) return l;
        }
        return null;
    }

    // 그림이 target 쪽으로 빨려 들어가며 작아지고 사라짐 (형판이 내 손에)
    public static IEnumerator MoveInto(Transform t, Transform target, float seconds)
    {
        if (t == null) yield break;
        Vector3 start = t.position;
        Vector3 full = t.localScale;
        float time = 0f;
        while (time < seconds && target != null && !Cine.Skipping)
        {
            time += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, time / seconds);
            t.position = Vector3.Lerp(start, target.position + Vector3.up * 0.8f, k);
            t.localScale = full * Mathf.Lerp(1f, 0.15f, k);
            yield return null;
        }
        Object.Destroy(t.gameObject);
    }

    // 그림이 아래에서 위로 떠오름 (형판)
    public static IEnumerator Rise(SpriteRenderer sr, float height, float seconds)
    {
        Vector3 start = sr.transform.position;
        Vector3 full = sr.transform.localScale;
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            sr.transform.position = start + Vector3.up * height * k;
            sr.transform.localScale = full * Mathf.Lerp(0.2f, 1f, k);
            yield return null;
        }
        sr.transform.position = start + Vector3.up * height;
        sr.transform.localScale = full;
    }
}
