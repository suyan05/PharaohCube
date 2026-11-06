using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 제단 둘레를 도는 별 하나 (HorusSkyRing이 만들고 움직임)
// - 지평선 위(북쪽 절반): 또렷 + E로 띄우기 / 지평선 아래(남쪽 절반): 흐릿 + 띄울 수 없음
// - 띄운 별: 금빛 후광 + 작은 빛 / 붉은 별 힌트: 부르르 떨림 / 다음 별 힌트: 금빛으로 깜빡
public class HorusSkyStar : MonoBehaviour, IInteractable
{
    private static readonly Color Gold = new Color(1f, 0.85f, 0.45f, 1f);
    private static readonly Color Pale = new Color(0.65f, 0.8f, 1f, 1f);

    private HorusSkyRing ring;
    private int index;
    private string label;
    private bool trap;
    private SpriteRenderer body, halo;
    private Light2D glow;
    private Vector3 baseScale;
    private bool risen, lit;

    public static HorusSkyStar Create(HorusSkyRing ring, int index, Sprite sprite, float size, string label, bool trap)
    {
        // 이름에 Floor가 없어야 플레이어와 앞뒤 정렬됨 (떠 있는 별이라 Above 구간으로 감)
        SpriteRenderer body = HorusFx.Spawn($"H5_SkyStar_{index}", sprite != null ? sprite : FxSprites.Soft, ring.transform.position, size, Color.white, HorusFx.Above - 30);
        HorusSkyStar s = body.gameObject.AddComponent<HorusSkyStar>();
        s.ring = ring;
        s.index = index;
        s.label = label;
        s.trap = trap;
        s.body = body;
        s.baseScale = body.transform.localScale;
        s.Build();
        return s;
    }

    private void Build()
    {
        halo = RaGlowFx.Glow(null, transform.position, 1.6f, new Color(1f, 1f, 1f, 0f), HorusFx.Layer, HorusFx.Above - 31);
        halo.gameObject.name = $"H5_SkyStarHalo_{index}";
        glow = RaGlowFx.PointLight(null, transform.position, trap ? new Color(1f, 0.35f, 0.3f) : Gold, 2.2f, 0f);
        glow.gameObject.name = $"H5_SkyStarLight_{index}";
        CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.55f / Mathf.Max(baseScale.x, 0.001f); // 월드 기준 반지름 0.55
    }

    private void OnDisable()
    {
        if (halo != null) halo.enabled = false;
        if (glow != null) glow.enabled = false;
    }

    private void OnEnable()
    {
        if (halo != null) halo.enabled = true;
        if (glow != null) glow.enabled = true;
    }

    // HorusSkyRing이 매 프레임 부름
    public void SetState(Vector3 pos, bool isRisen, bool isLit, bool trapHint, bool nextHint)
    {
        risen = isRisen;
        lit = isLit;
        Vector3 shake = trapHint ? (Vector3)(UnityEngine.Random.insideUnitCircle * 0.04f) : Vector3.zero;
        transform.position = pos + Vector3.up * Mathf.Sin(Time.time * 2f + index) * 0.06f + shake;
        float k = isRisen ? 1f : 0.75f;
        transform.localScale = baseScale * k;
        body.color = new Color(1f, 1f, 1f, isRisen || isLit ? 1f : 0.3f);
        float pulse = nextHint ? 0.5f + 0.5f * Mathf.Sin(Time.time * 6f) : 0f;
        Color hc = isLit ? Gold : Pale;
        float ha = isLit ? 0.75f : (isRisen ? 0.25f : 0.05f);
        halo.transform.position = transform.position;
        halo.color = new Color(hc.r, hc.g, hc.b, Mathf.Max(ha, pulse * 0.8f));
        glow.transform.position = transform.position;
        glow.intensity = isLit ? 0.9f : pulse * 0.6f;
    }

    public string GetPrompt()
    {
        if (lit) return $"{label} - 이미 떠오른 별";
        if (!risen) return $"{label} - 지평선 아래로 진 별";
        return ring.Busy ? label : $"E: {label} 띄우기";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (lit || DialogueBox.IsOpen || Cine.Running) return;
            ring.TryRaise(index, risen);
        }
        catch (Exception e)
        {
            Debug.LogError($"[하늘 별] Interact 오류: {e}");
        }
    }
}
