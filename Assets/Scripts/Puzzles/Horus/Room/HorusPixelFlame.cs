using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 호루스 횃대 위의 움직이는 픽셀 불꽃 + 위로 튀는 불씨 + 계단식 빛 깜빡임
// - HorusPixelFlameAutoAttach 가 씬 시작 때 자동으로 붙임
// - 같은 오브젝트에 WallTorch(별 횃불)가 있으면: 켜졌을 때만 불꽃이 보이고, 빛 깜빡임은 WallTorch 것을 그대로 씀
// - 장식 횃대(WallTorch 없음)는 항상 켜져 있고, 빛 깜빡임을 여기서 함
// - 원래 그림에 박힌 정지 불꽃은 불꽃 없는 횃대 그림(HO_Sconce_Base)으로 바꿔서 숨김
public class HorusPixelFlame : MonoBehaviour
{
    private const float Ppu = 32f;          // 32px = 1칸
    private const int RimFromTop = 8;       // 횃대 그림 맨 위에서 테두리까지 8px
    private const int EmberCount = 4;

    [SerializeField] private Sprite[] frames;
    [SerializeField] private Sprite baseSprite;
    [SerializeField, Min(1f)] private float fps = 10f;
    [SerializeField, Min(1f)] private float flickerSteps = 12f; // 빛이 1초에 바뀌는 횟수 (끊기는 도트 느낌)

    private SpriteRenderer host;
    private SpriteRenderer flame;
    private WallTorch torch;
    private Light2D glow;
    private float glowBase;
    private Vector2 rim;                    // 불꽃 밑동 위치 (횃대 기준)
    private float clock;
    private float flickerTimer;
    private float emberTimer;
    private bool ready;

    private SpriteRenderer[] embers;
    private Vector2[] emberPos;
    private Vector2[] emberVel;
    private float[] emberAge;
    private float[] emberLife;

    private static Sprite dot;

    public void Setup(Sprite[] flameFrames, Sprite unlitSconce)
    {
        frames = flameFrames;
        baseSprite = unlitSconce;
    }

    private void Start()
    {
        try
        {
            host = GetComponent<SpriteRenderer>();
            if (host == null || frames == null || frames.Length == 0) { enabled = false; return; }
            torch = GetComponent<WallTorch>();
            glow = GetComponentInChildren<Light2D>(true);
            glowBase = glow != null ? Mathf.Max(glow.intensity, 0.6f) : 0f;
            rim = FindRim();
            clock = UnityEngine.Random.value * 10f; // 횃불마다 박자가 다르게
            flame = MakeRenderer("PixelFlame", 1);
            flame.transform.localPosition = rim;
            BuildEmbers();
            ready = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[픽셀 불꽃] {name} 준비 실패: {e.Message}");
            enabled = false;
        }
    }

    // 횃대 그림 맨 위에서 8px 아래 = 테두리 위 (그림 기준점이 어디든 bounds로 계산)
    private Vector2 FindRim()
    {
        Bounds b = host.sprite != null ? host.sprite.bounds : new Bounds(Vector3.zero, Vector3.one);
        return new Vector2(b.center.x, b.max.y - RimFromTop / Ppu);
    }

    private SpriteRenderer MakeRenderer(string objName, int orderOffset)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(transform, false); // 부모 크기를 따라가서 픽셀 크기가 횃대와 같음
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerID = host.sortingLayerID;
        sr.sortingOrder = host.sortingOrder + orderOffset;
        return sr;
    }

    private void LateUpdate()
    {
        if (!ready) return;
        try
        {
            bool lit = torch == null || torch.IsLit;
            flame.enabled = lit;
            if (lit && baseSprite != null && host.sprite != baseSprite) host.sprite = baseSprite;
            if (!lit) { HideEmbers(); return; }
            clock += Time.deltaTime;
            flame.sprite = frames[(int)(clock * fps) % frames.Length];
            if (torch == null) StepFlicker();
            UpdateEmbers(Time.deltaTime);
        }
        catch (Exception e)
        {
            Debug.LogError($"[픽셀 불꽃] {name}: {e.Message}");
            enabled = false;
        }
    }

    // 장식 횃대 빛: 부드럽게 말고 1/12초마다 뚝뚝 바뀌게
    private void StepFlicker()
    {
        if (glow == null || !glow.gameObject.activeInHierarchy) return;
        flickerTimer -= Time.deltaTime;
        if (flickerTimer > 0f) return;
        flickerTimer = 1f / flickerSteps;
        float n = Mathf.PerlinNoise(clock * 2.3f, transform.position.x * 0.37f);
        glow.intensity = glowBase * (0.82f + n * 0.32f);
    }

    // ================= 불씨: 1px 점이 위로 떠오르며 노랑 -> 주황 -> 빨강으로 식음 =================
    private void BuildEmbers()
    {
        embers = new SpriteRenderer[EmberCount];
        emberPos = new Vector2[EmberCount];
        emberVel = new Vector2[EmberCount];
        emberAge = new float[EmberCount];
        emberLife = new float[EmberCount];
        for (int i = 0; i < EmberCount; i++)
        {
            embers[i] = MakeRenderer("PixelEmber", 2);
            embers[i].sprite = Dot();
            embers[i].enabled = false;
        }
        emberTimer = UnityEngine.Random.Range(0.2f, 0.8f);
    }

    private static Sprite Dot()
    {
        if (dot != null) return dot;
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        dot = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), Ppu);
        return dot;
    }

    private void UpdateEmbers(float dt)
    {
        emberTimer -= dt;
        if (emberTimer <= 0f)
        {
            SpawnEmber();
            emberTimer = UnityEngine.Random.Range(0.25f, 0.7f);
        }
        for (int i = 0; i < EmberCount; i++)
        {
            if (!embers[i].enabled) continue;
            emberAge[i] += dt;
            if (emberAge[i] >= emberLife[i]) { embers[i].enabled = false; continue; }
            emberVel[i].x += Mathf.Sin((clock + i) * 7f) * 30f * dt; // 살랑살랑 흔들림 (px/초)
            emberPos[i] += emberVel[i] * dt;
            embers[i].transform.localPosition = Snap(emberPos[i]);
            embers[i].color = EmberColor(emberAge[i] / emberLife[i]);
        }
    }

    private void SpawnEmber()
    {
        for (int i = 0; i < EmberCount; i++)
        {
            if (embers[i].enabled) continue;
            emberPos[i] = new Vector2(UnityEngine.Random.Range(-3, 4), UnityEngine.Random.Range(5, 10)); // px, 불꽃 밑동 기준
            emberVel[i] = new Vector2(UnityEngine.Random.Range(-4f, 4f), UnityEngine.Random.Range(14f, 24f));
            emberAge[i] = 0f;
            emberLife[i] = UnityEngine.Random.Range(0.6f, 1.1f);
            embers[i].transform.localPosition = Snap(emberPos[i]);
            embers[i].enabled = true;
            return;
        }
    }

    // px 위치를 1px 칸에 딱 맞춰서 (반 픽셀에 걸리면 번져 보임)
    private Vector3 Snap(Vector2 px)
    {
        return new Vector3(rim.x + Mathf.Round(px.x) / Ppu, rim.y + Mathf.Round(px.y) / Ppu, 0f);
    }

    private static Color EmberColor(float t)
    {
        Color yellow = new Color32(255, 214, 90, 255);
        Color orange = new Color32(240, 120, 30, 255);
        Color red = new Color32(170, 50, 20, 255);
        Color c = t < 0.4f ? Color.Lerp(yellow, orange, t / 0.4f) : Color.Lerp(orange, red, (t - 0.4f) / 0.6f);
        c.a = t < 0.7f ? 1f : (t < 0.85f ? 0.6f : 0.3f); // 투명도도 3단계로 끊어서
        return c;
    }

    private void HideEmbers()
    {
        if (embers == null) return;
        foreach (SpriteRenderer e in embers) if (e != null) e.enabled = false;
    }
}
