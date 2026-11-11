using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 해의 방(H1) 빛줄기 연출 - 딱딱한 직선 하나였던 것을 "진짜 빛이 거울에 튕기는" 느낌으로
// - 3겹: 넓고 옅은 빛 번짐 + 중간 빛 + 가는 흰 심지. 가장자리는 부드럽게 흐려짐 (코드로 만든 그라데이션)
// - 거울을 돌리면 빛이 해에서부터 다시 "달려 나감" (한 번에 뿅 나타나지 않음). 바뀌지 않은 앞부분은 그대로 유지
// - 꺾이는 곳(거울)과 끝점마다 작은 조명 + 반짝임, 빛줄기를 따라 흐르는 빛 알갱이, 숨 쉬듯 굵기가 일렁임
// - HorusSunMirrors가 Show(꼭짓점들, 색) / Hide()만 부르면 됨. 퍼즐 판정(어디에 닿는지)은 예전 그대로
public class HorusBeamFx : MonoBehaviour
{
    private const float Speed = 16f;        // 빛이 달려 나가는 빠르기 (칸/초)
    private const int MaxLights = 10;
    private const int MoteCount = 12;

    private LineRenderer core, mid, glow;
    private readonly List<Vector3> path = new List<Vector3>();
    private readonly List<float> cum = new List<float>();
    private readonly List<Vector3> shown = new List<Vector3>();
    private Light2D[] lights;
    private SpriteRenderer[] flares, motes;
    private float total, head, clock;
    private Color color = Color.white;
    private static Texture2D softTex;
    private static Sprite dot;

    public static HorusBeamFx Attach(LineRenderer coreLine)
    {
        HorusBeamFx fx = coreLine.gameObject.AddComponent<HorusBeamFx>();
        fx.core = coreLine;
        fx.Build();
        return fx;
    }

    private void Build()
    {
        core.startWidth = core.endWidth = 0.06f;
        core.numCornerVertices = 3;
        mid = MakeLine("BeamMid", core.sortingOrder - 1);
        glow = MakeLine("BeamGlow", core.sortingOrder - 2);
        lights = new Light2D[MaxLights];
        flares = new SpriteRenderer[MaxLights];
        for (int i = 0; i < MaxLights; i++)
        {
            flares[i] = MakeDot("BeamFlare", core.sortingOrder + 1, 0.5f);
            lights[i] = MakeLight(transform); // 반짝임 그림의 크기 변화에 조명 반지름이 휘둘리지 않게 따로 둠
        }
        motes = new SpriteRenderer[MoteCount];
        for (int i = 0; i < MoteCount; i++) motes[i] = MakeDot("BeamMote", core.sortingOrder + 2, 0.09f);
        Hide();
    }

    private LineRenderer MakeLine(string objName, int order)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(transform, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;
        lr.textureMode = LineTextureMode.Stretch;
        lr.sortingLayerID = core.sortingLayerID;
        lr.sortingOrder = order;
        lr.material = new Material(Shader.Find("Sprites/Default")) { mainTexture = SoftTexture() };
        lr.positionCount = 0;
        return lr;
    }

    private SpriteRenderer MakeDot(string objName, int order, float size)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * size;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Dot();
        sr.sortingLayerID = core.sortingLayerID;
        sr.sortingOrder = order;
        sr.sharedMaterial = core.sharedMaterial; // 어둠 속에서도 빛나게 (빛줄기와 같은 재질)
        sr.enabled = false;
        return sr;
    }

    private static Light2D MakeLight(Transform parent)
    {
        var go = new GameObject("BeamLight", typeof(Light2D));
        go.transform.SetParent(parent, false);
        Light2D l = go.GetComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.pointLightOuterRadius = 2.2f;
        l.pointLightInnerRadius = 0.2f;
        l.falloffIntensity = 0.8f;
        l.intensity = 0f;
        return l;
    }

    // ================= 밖에서 부르는 함수 =================
    // 새 길을 받음. 예전 길과 똑같은 앞부분까지는 빛이 그대로 있고, 달라진 곳부터 다시 달려 나감
    public void Show(List<Vector3> points, Color beamColor)
    {
        try
        {
            float keep = CommonLength(points);
            path.Clear();
            path.AddRange(points);
            cum.Clear();
            total = 0f;
            for (int i = 0; i < path.Count; i++)
            {
                if (i > 0) total += Vector3.Distance(path[i - 1], path[i]);
                cum.Add(total);
            }
            head = Mathf.Min(head, keep);
            color = beamColor;
            enabled = path.Count >= 2;
            if (!enabled) Hide();
        }
        catch (Exception e)
        {
            Debug.LogError($"[빛줄기] 길 받기 오류: {e.Message}");
        }
    }

    public void Hide()
    {
        path.Clear();
        head = 0f;
        total = 0f;
        if (core != null) core.positionCount = 0;
        if (mid != null) mid.positionCount = 0;
        if (glow != null) glow.positionCount = 0;
        if (flares != null) for (int i = 0; i < flares.Length; i++) SetFlare(i, false, 0f);
        if (motes != null) foreach (SpriteRenderer m in motes) m.enabled = false;
    }

    // 예전 길과 새 길이 처음부터 얼마나 같은지 (거리)
    private float CommonLength(List<Vector3> next)
    {
        float len = 0f;
        for (int i = 1; i < Mathf.Min(path.Count, next.Count); i++)
        {
            if ((path[i - 1] - next[i - 1]).sqrMagnitude > 0.0001f) break;
            Vector3 a = path[i] - path[i - 1], b = next[i] - next[i - 1];
            if (Vector3.Cross(a.normalized, b.normalized).sqrMagnitude > 0.0001f || Vector3.Dot(a, b) <= 0f) break;
            len += Mathf.Min(a.magnitude, b.magnitude);
            if (Mathf.Abs(a.magnitude - b.magnitude) > 0.01f) break;
        }
        return len;
    }

    // ================= 매 프레임 =================
    private void Update()
    {
        if (path.Count < 2) return;
        try
        {
            clock += Time.deltaTime;
            head = Mathf.Min(total, head + Speed * Time.deltaTime);
            BuildShown();
            float breathe = 1f + 0.12f * Mathf.Sin(clock * 3.1f) + 0.05f * Mathf.Sin(clock * 7.7f);
            Draw(core, 0.06f, new Color(1f, 1f, 0.92f, 0.95f));
            Draw(mid, 0.26f * breathe, new Color(color.r, color.g, color.b, 0.5f));
            Draw(glow, 0.8f * breathe, new Color(color.r, color.g, color.b, 0.22f));
            PlaceFlares();
            PlaceMotes();
        }
        catch (Exception e)
        {
            Debug.LogError($"[빛줄기] {e.Message}");
            enabled = false;
        }
    }

    // 빛이 지금 도달한 곳(head)까지만 잘라낸 꼭짓점
    private void BuildShown()
    {
        shown.Clear();
        shown.Add(path[0]);
        for (int i = 1; i < path.Count; i++)
        {
            if (cum[i] <= head) { shown.Add(path[i]); continue; }
            float seg = Mathf.Max(0.0001f, cum[i] - cum[i - 1]);
            shown.Add(Vector3.Lerp(path[i - 1], path[i], Mathf.Clamp01((head - cum[i - 1]) / seg)));
            break;
        }
    }

    private void Draw(LineRenderer lr, float width, Color c)
    {
        lr.positionCount = shown.Count;
        lr.SetPositions(shown.ToArray());
        lr.startWidth = lr.endWidth = width;
        lr.startColor = lr.endColor = c;
    }

    // 꺾이는 곳과 빛의 머리에 반짝임 + 조명
    private void PlaceFlares()
    {
        for (int i = 0; i < MaxLights; i++)
        {
            int v = i + 1; // 0번(해)은 뺌
            bool on = v < shown.Count;
            if (on) flares[i].transform.position = lights[i].transform.position = shown[v];
            bool isHead = v == shown.Count - 1;
            float pulse = 0.75f + 0.25f * Mathf.Sin(clock * 9f + i * 1.3f);
            SetFlare(i, on, (isHead ? 1.1f : 0.8f) * pulse);
        }
    }

    private void SetFlare(int i, bool on, float power)
    {
        SpriteRenderer f = flares[i];
        if (f == null) return;
        f.enabled = on;
        f.color = new Color(1f, 0.97f, 0.85f, 0.85f * Mathf.Clamp01(power));
        f.transform.localScale = Vector3.one * (0.34f + 0.2f * power);
        Light2D l = lights != null ? lights[i] : null;
        if (l == null) return;
        l.color = color;
        l.intensity = on ? power : 0f;
    }

    // 빛줄기를 따라 흘러가는 빛 알갱이
    private void PlaceMotes()
    {
        for (int i = 0; i < MoteCount; i++)
        {
            float d = Mathf.Repeat(clock * 3.2f + i * (total / MoteCount), Mathf.Max(0.01f, total));
            motes[i].enabled = d <= head;
            if (!motes[i].enabled) continue;
            Vector3 p = PointAt(d);
            float wob = Mathf.Sin(clock * 5f + i * 2.1f) * 0.07f;
            motes[i].transform.position = p + new Vector3(wob, Mathf.Cos(clock * 4f + i) * 0.07f, 0f);
            motes[i].color = new Color(1f, 0.98f, 0.85f, 0.5f + 0.4f * Mathf.Sin(clock * 6f + i));
        }
    }

    private Vector3 PointAt(float d)
    {
        for (int i = 1; i < path.Count; i++)
        {
            if (cum[i] < d) continue;
            float seg = Mathf.Max(0.0001f, cum[i] - cum[i - 1]);
            return Vector3.Lerp(path[i - 1], path[i], (d - cum[i - 1]) / seg);
        }
        return path[path.Count - 1];
    }

    // ================= 그림 만들기 =================
    // 세로로 0 -> 1 -> 0 으로 변하는 부드러운 띠 (빛줄기 가장자리를 흐리게)
    private static Texture2D SoftTexture()
    {
        if (softTex != null) return softTex;
        softTex = new Texture2D(2, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        for (int y = 0; y < 32; y++)
        {
            float k = 1f - Mathf.Abs(y - 15.5f) / 15.5f;
            Color c = new Color(1f, 1f, 1f, k * k);
            softTex.SetPixel(0, y, c);
            softTex.SetPixel(1, y, c);
        }
        softTex.Apply();
        return softTex;
    }

    // 가운데가 밝고 가장자리로 갈수록 사라지는 둥근 점
    private static Sprite Dot()
    {
        if (dot != null) return dot;
        const int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float r = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f)) / (n * 0.5f);
                float a = Mathf.Clamp01(1f - r);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }
        tex.Apply();
        dot = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return dot;
    }
}
