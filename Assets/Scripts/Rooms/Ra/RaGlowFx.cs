using UnityEngine;
using UnityEngine.Rendering.Universal;

// 라의 방 빛 효과 도우미 (어둠 속에서도 보이게 Unlit 재질 사용)
// - Glow: 부드러운 빛 덩어리 그림 (FxSprites.Soft)
// - PointLight: 주변을 비추는 Point Light 2D
// - Spark: 위로 떠오르며 사라지는 작은 불똥 (RaSpark가 알아서 지움)
public static class RaGlowFx
{
    public static SpriteRenderer Glow(Transform parent, Vector3 localPos, float size, Color color, string layer, int order)
    {
        var go = new GameObject("Glow");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * size;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FxSprites.Soft;
        sr.color = color;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        if (RaDarkness.Unlit != null) sr.sharedMaterial = RaDarkness.Unlit;
        return sr;
    }

    public static Light2D PointLight(Transform parent, Vector3 localPos, Color color, float radius, float intensity)
    {
        var go = new GameObject("Light");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        Light2D light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.pointLightInnerRadius = radius * 0.2f;
        light.pointLightOuterRadius = radius;
        light.intensity = intensity;
        return light;
    }

    public static void Spark(Vector3 pos, Color color, string layer, int order)
    {
        var go = new GameObject("Spark");
        go.transform.position = pos + (Vector3)(Random.insideUnitCircle * 0.12f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FxSprites.Dot;
        sr.color = color;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        if (RaDarkness.Unlit != null) sr.sharedMaterial = RaDarkness.Unlit;

        RaSpark spark = go.AddComponent<RaSpark>();
        spark.Init(new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(0.5f, 0.9f)), Random.Range(0.7f, 1.2f));
    }
}

// 불똥 하나 (떠오르며 좌우로 살랑, 점점 투명해지다 사라짐)
public class RaSpark : MonoBehaviour
{
    private SpriteRenderer sr;
    private Vector2 velocity;
    private float life;
    private float age;
    private float seed;

    public void Init(Vector2 startVelocity, float lifeSeconds)
    {
        sr = GetComponent<SpriteRenderer>();
        velocity = startVelocity;
        life = Mathf.Max(0.1f, lifeSeconds);
        seed = Random.value * 10f;
    }

    private void Update()
    {
        age += Time.deltaTime;
        float sway = Mathf.Sin((age + seed) * 7f) * 0.25f;
        transform.position += (Vector3)(velocity + new Vector2(sway, 0f)) * Time.deltaTime;

        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Clamp01(1f - age / life);
            sr.color = c;
        }
        if (age >= life) Destroy(gameObject);
    }
}
