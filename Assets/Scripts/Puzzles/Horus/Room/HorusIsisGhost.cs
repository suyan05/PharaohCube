using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 이시스의 유령 (입구 전실 연출에서만 잠깐 나타남)
// 신화 반영:
// - 이시스의 눈물이 나일을 넘치게 했다 -> 푸른 눈물방울이 떨어지고 바닥에 물결이 번짐
// - 이시스는 솔개(매 비슷한 새)로 변해 오시리스를 찾아다녔다 -> 깃털이 소용돌이치며 모여 모습이 됨
// - 머리 위 왕좌 상징 + 날개 달린 여신 -> 접힌 날개가 활짝 펴지며 등장
// - 사라질 땐 날개로 몸을 감싸고 깃털이 되어 본당(북쪽)으로 흩어짐 = "길은 저쪽"
// 그림: HO_Isis_Ghost(몸), HO_Isis_Wing(오른쪽 날개, 왼쪽은 뒤집어 씀), HO_Feather, HO_Ripple
public class HorusIsisGhost : MonoBehaviour
{
    private const float BodyWidth = 1.6f;      // 화면에서 몸 너비 (플레이어보다 큼 = 여신의 위엄)
    private const float FoldedAngle = -75f;    // 날개 접힘 (아래로)
    private const float OpenAngle = 12f;       // 날개 펼침 (살짝 위로)
    private const float GhostAlpha = 0.85f;
    private static readonly Color Tear = new Color(0.6f, 0.85f, 1f, 1f);
    private static readonly Color Glow = new Color(0.7f, 0.85f, 1f, 0.55f);
    private static readonly Color ThroneGold = new Color(1f, 0.85f, 0.45f, 0.8f);

    private Sprite feather, ripple;
    private SpriteRenderer body, wingL, wingR;
    private Transform wingPivotL, wingPivotR, bodyRoot;
    private Light2D aura;
    private SpriteRenderer halo;
    private float wingAngle = FoldedAngle;
    private float alpha;
    private float scaleK;
    private float bodyH = 72f;
    private bool idle;

    // ================= 만들기 =================
    public static HorusIsisGhost Create(Vector3 pos, Sprite bodySprite, Sprite wingSprite, Sprite featherSprite, Sprite rippleSprite)
    {
        try
        {
            var go = new GameObject("IsisGhost");
            go.transform.position = pos;
            HorusIsisGhost g = go.AddComponent<HorusIsisGhost>();
            g.feather = featherSprite;
            g.ripple = rippleSprite;
            g.Build(bodySprite, wingSprite);
            return g;
        }
        catch (Exception e)
        {
            Debug.LogError($"[이시스 유령] 만들기 오류: {e}");
            return null;
        }
    }

    private void Build(Sprite bodySprite, Sprite wingSprite)
    {
        bodyRoot = new GameObject("Body").transform;
        bodyRoot.SetParent(transform, false);
        body = Part(bodyRoot, bodySprite, HorusFx.Above - 2);
        scaleK = bodySprite != null ? BodyWidth / Mathf.Max(bodySprite.bounds.size.x, 0.01f) : 1f;
        body.transform.localScale = Vector3.one * scaleK;
        bodyH = bodySprite != null ? bodySprite.rect.height : 72f;
        body.transform.localPosition = Vector3.up * (bodyH * 0.5f / 32f * scaleK); // 발끝(꼬리)이 pos에 오게
        wingPivotR = WingPivot("WingR", 1f, wingSprite, out wingR);
        wingPivotL = WingPivot("WingL", -1f, wingSprite, out wingL);
        aura = RaGlowFx.PointLight(bodyRoot, Vector3.up * 1.4f, new Color(0.7f, 0.85f, 1f), 4.5f, 0f);
        halo = RaGlowFx.Glow(bodyRoot, Vector3.up * 1.6f, 3.2f, Glow, HorusFx.Layer, HorusFx.Above - 4);
        ApplyAlpha(0f);
        ApplyWings();
    }

    private SpriteRenderer Part(Transform parent, Sprite sprite, int order)
    {
        var go = new GameObject("Sprite");
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = HorusFx.Layer;
        sr.sortingOrder = order;
        if (RaDarkness.Unlit != null) sr.sharedMaterial = RaDarkness.Unlit;
        return sr;
    }

    // 어깨 위치에 회전축을 두고, 날개 그림은 축에서 바깥쪽으로 붙임 (축을 돌리면 날개가 접혔다 펴짐)
    // 몸 그림 기준 어깨 = 가운데에서 가로 5px, 맨 위에서 24px / 날개 그림 기준 어깨 = 왼쪽 끝, 위에서 6px
    private Transform WingPivot(string objName, float side, Sprite wingSprite, out SpriteRenderer sr)
    {
        const float ppu = 32f;
        var pivot = new GameObject(objName).transform;
        pivot.SetParent(bodyRoot, false);
        pivot.localPosition = new Vector3(side * 5f, bodyH - 24f, 0f) / ppu * scaleK;
        sr = Part(pivot, wingSprite, HorusFx.Above - 3);
        sr.flipX = side < 0f;
        Vector2 size = wingSprite != null ? wingSprite.rect.size : new Vector2(60f, 34f);
        sr.transform.localScale = Vector3.one * scaleK;
        sr.transform.localPosition = new Vector3(side * size.x * 0.5f, -(size.y * 0.5f - 6f), 0f) / ppu * scaleK;
        return pivot;
    }

    private void ApplyAlpha(float a)
    {
        alpha = a;
        Color c = new Color(1f, 1f, 1f, a);
        if (body != null) body.color = c;
        if (wingL != null) wingL.color = c;
        if (wingR != null) wingR.color = c;
        if (aura != null) aura.intensity = a * 1.1f;
        if (halo != null) halo.color = new Color(Glow.r, Glow.g, Glow.b, Glow.a * a);
    }

    private void ApplyWings()
    {
        if (wingPivotR != null) wingPivotR.localRotation = Quaternion.Euler(0f, 0f, wingAngle);
        if (wingPivotL != null) wingPivotL.localRotation = Quaternion.Euler(0f, 0f, -wingAngle);
    }

    // ================= 등장 =================
    // 눈물 -> 물결 -> 깃털 소용돌이 -> 물결에서 몸이 솟아오름 -> 날개가 활짝 (번쩍)
    public IEnumerator Appear()
    {
        yield return Tears(4);
        yield return FeathersGather(16, 1.3f);
        yield return RiseFromRipple(1.1f);
        yield return UnfoldWings(0.55f);
        idle = true;
    }

    private IEnumerator Tears(int count)
    {
        for (int i = 0; i < count && !Cine.Skipping; i++)
        {
            Vector3 land = transform.position + new Vector3(UnityEngine.Random.Range(-0.6f, 0.6f), UnityEngine.Random.Range(-0.2f, 0.2f), 0f);
            yield return Drop(land, 0.35f);
            StartCoroutine(Ripple(land, 1.4f + i * 0.4f, 0.9f));
            GameAudio.Play("sfx_blip_isis", 0.35f, 0.7f + i * 0.08f);
            yield return Cine.Wait(0.18f);
        }
        StartCoroutine(Ripple(transform.position, 3.2f, 1.4f));
    }

    private IEnumerator Drop(Vector3 land, float seconds)
    {
        SpriteRenderer d = HorusFx.Spawn("Tear", FxSprites.Dot, land + Vector3.up * 3f, 0.12f, Tear, HorusFx.Above);
        Vector3 scale = d.transform.localScale;
        d.transform.localScale = new Vector3(scale.x, scale.y * 2.2f, 1f); // 길쭉한 물방울
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            d.transform.position = Vector3.Lerp(land + Vector3.up * 3f, land, (t / seconds) * (t / seconds));
            yield return null;
        }
        Destroy(d.gameObject);
    }

    // 바닥에 동그란 물결이 퍼지며 옅어짐
    private IEnumerator Ripple(Vector3 at, float size, float seconds)
    {
        if (ripple == null) yield break;
        SpriteRenderer r = HorusFx.Spawn("Ripple", ripple, at, 0.3f, Tear, HorusFx.Floor);
        Vector3 one = r.transform.localScale / 0.3f;
        float t = 0f;
        while (t < seconds && r != null)
        {
            t += Time.deltaTime;
            float k = t / seconds;
            r.transform.localScale = one * Mathf.Lerp(0.3f, size, Mathf.Sqrt(k));
            r.color = new Color(Tear.r, Tear.g, Tear.b, 1f - k);
            yield return null;
        }
        if (r != null) Destroy(r.gameObject);
    }

    // 깃털이 원을 그리며 가운데로 빨려 들어옴 (솔개로 변해 헤맨 이시스)
    private IEnumerator FeathersGather(int count, float seconds)
    {
        GameAudio.Play("sfx_wind_gust", 0.5f, 0.8f);
        Vector3 center = transform.position + Vector3.up * 1.6f;
        var list = new SpriteRenderer[count];
        for (int i = 0; i < count; i++) list[i] = HorusFx.Spawn("Feather", feather, center, 0.28f, Color.white, HorusFx.Above);
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            for (int i = 0; i < count; i++) SpinFeather(list[i], center, i, count, k, t);
            yield return null;
        }
        foreach (SpriteRenderer f in list) if (f != null) Destroy(f.gameObject);
        HorusFx.StarBurst(center, new Vector2(0.5f, 0.8f), 14, Tear);
    }

    private static void SpinFeather(SpriteRenderer f, Vector3 center, int i, int count, float k, float t)
    {
        if (f == null) return;
        float a = (i / (float)count) * Mathf.PI * 2f + k * Mathf.PI * 3f;
        float radius = Mathf.Lerp(3.2f, 0.1f, k);
        f.transform.position = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.55f, 0f) * radius;
        f.transform.rotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + t * 200f);
        f.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.2f, 1f, Mathf.Min(1f, k * 3f)));
    }

    // 물결 위에서 아래부터 솟아오르며 투명 -> 반투명
    private IEnumerator RiseFromRipple(float seconds)
    {
        GameAudio.Play("sfx_dream_shimmer", 0.8f, 0.85f);
        StartCoroutine(HorusFx.Pulse(transform.position + Vector3.up * 3.1f, 1.2f, ThroneGold, 2.2f)); // 머리 위 왕좌 상징이 빛남
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            bodyRoot.localScale = new Vector3(Mathf.Lerp(0.7f, 1f, k), Mathf.Lerp(0.15f, 1f, k), 1f);
            bodyRoot.localPosition = Vector3.up * Mathf.Lerp(-0.4f, 0f, k);
            ApplyAlpha(GhostAlpha * k);
            yield return null;
        }
        bodyRoot.localScale = Vector3.one;
        bodyRoot.localPosition = Vector3.zero;
        ApplyAlpha(GhostAlpha);
    }

    // 접혀 있던 날개가 활짝 (화면 번쩍 + 바람 + 살짝 흔들림)
    private IEnumerator UnfoldWings(float seconds)
    {
        GameAudio.Play("sfx_wind_gust", 0.7f, 1.1f);
        CineCamera.Shake(0.35f, 0.04f);
        StartCoroutine(ScreenTintUI.Flash(new Color(0.75f, 0.88f, 1f, 0.45f), 0.04f, 0.08f, 0.6f));
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            float k = t / seconds;
            float overshoot = Mathf.Sin(k * Mathf.PI) * 12f; // 확 펴졌다가 살짝 되돌아옴
            wingAngle = Mathf.Lerp(FoldedAngle, OpenAngle, Mathf.SmoothStep(0f, 1f, k)) + overshoot;
            ApplyWings();
            yield return null;
        }
        wingAngle = OpenAngle;
        ApplyWings();
        HorusFx.StarBurst(transform.position + Vector3.up * 2.2f, new Vector2(2.2f, 0.8f), 20, Tear);
    }

    // ================= 떠 있는 동안: 둥실 + 날개 숨쉬기 + 가끔 깃털 한 장 =================
    private float featherTimer = 0.8f;

    private void Update()
    {
        if (!idle || bodyRoot == null) return;
        bodyRoot.localPosition = Vector3.up * Mathf.Sin(Time.time * 1.6f) * 0.08f;
        wingAngle = OpenAngle + Mathf.Sin(Time.time * 2.1f) * 4f;
        ApplyWings();
        ApplyAlpha(GhostAlpha - 0.08f + Mathf.Sin(Time.time * 3.3f) * 0.08f);
        featherTimer -= Time.deltaTime;
        if (featherTimer > 0f) return;
        featherTimer = UnityEngine.Random.Range(0.5f, 1.1f);
        StartCoroutine(FallingFeather(transform.position + new Vector3(UnityEngine.Random.Range(-1.8f, 1.8f), 2.2f, 0f)));
    }

    private IEnumerator FallingFeather(Vector3 from)
    {
        SpriteRenderer f = HorusFx.Spawn("Feather", feather, from, 0.22f, Color.white, HorusFx.Above);
        float t = 0f;
        while (t < 2f && f != null)
        {
            t += Time.deltaTime;
            f.transform.position = from + new Vector3(Mathf.Sin(t * 3f) * 0.25f, -t * 0.7f, 0f);
            f.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 3f) * 35f);
            f.color = new Color(1f, 1f, 1f, 1f - t / 2f);
            yield return null;
        }
        if (f != null) Destroy(f.gameObject);
    }

    // ================= 퇴장: 날개로 몸을 감싸고 -> 깃털이 되어 towards 쪽으로 흩어짐 =================
    public IEnumerator Vanish(Vector3 towards)
    {
        idle = false;
        yield return FoldAround(0.45f);
        GameAudio.Play("sfx_wind_gust", 0.6f, 1.3f);
        Vector3 center = transform.position + Vector3.up * 1.6f;
        for (int i = 0; i < 22; i++) StartCoroutine(ScatterFeather(center, towards, i));
        yield return FadeOut(0.6f);
        yield return Cine.Wait(0.9f);
        Destroy(gameObject, 2f);
    }

    private IEnumerator FoldAround(float seconds)
    {
        float from = wingAngle, t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            wingAngle = Mathf.Lerp(from, -110f, Mathf.SmoothStep(0f, 1f, t / seconds));
            ApplyWings();
            yield return null;
        }
    }

    private IEnumerator FadeOut(float seconds)
    {
        float from = alpha, t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            ApplyAlpha(Mathf.Lerp(from, 0f, t / seconds));
            bodyRoot.localScale = new Vector3(Mathf.Lerp(1f, 0.4f, t / seconds), Mathf.Lerp(1f, 1.3f, t / seconds), 1f);
            yield return null;
        }
        ApplyAlpha(0f);
    }

    private IEnumerator ScatterFeather(Vector3 center, Vector3 towards, int i)
    {
        Vector3 start = center + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.6f);
        Vector3 dir = (towards - center).normalized;
        Vector3 end = start + dir * UnityEngine.Random.Range(3f, 6f) + (Vector3)(UnityEngine.Random.insideUnitCircle * 1.5f);
        SpriteRenderer f = HorusFx.Spawn("Feather", feather, start, 0.26f, Color.white, HorusFx.Above);
        float seconds = UnityEngine.Random.Range(1.1f, 1.7f), t = 0f;
        while (t < seconds && f != null)
        {
            t += Time.deltaTime;
            float k = t / seconds;
            f.transform.position = Vector3.Lerp(start, end, 1f - (1f - k) * (1f - k)) + Vector3.right * Mathf.Sin(t * 6f + i) * 0.15f;
            f.transform.rotation = Quaternion.Euler(0f, 0f, t * 240f + i * 20f);
            f.color = new Color(1f, 1f, 1f, 1f - k);
            yield return null;
        }
        if (f != null) Destroy(f.gameObject);
    }
}
