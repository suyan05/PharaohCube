using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// 라의 방 벽 횃불(WallTorch)에 움직이는 작은 픽셀 불꽃을 얹음 (호루스 횃대의 HorusPixelFlame과 같은 생각)
// - 켜진 그림(Light_on)일 때만 불꽃이 보임. 꺼지면 같이 사라짐
// - 씬이 열릴 때 자동으로 붙음. 호루스 횃대(HO_Sconce 그림)에는 HorusPixelFlame이 따로 붙으므로 여기선 아무것도 안 함
// - 그림: Resources/Horus/PixelFx/RA_Flame_0~7 (10x12 도트)
// - [수정] 테스트용 횃불 그림이면 청동 횃대 + HorusPixelFlame으로 바꿈 (다른 방과 같은 움직이는 횃불)
public class RaPixelFlame : MonoBehaviour
{
    private const string OnSpriteName = "Light_on";
    private const string Art = "Horus/PixelFx/RA_Flame_";
    private const int FrameCount = 8;
    private static Sprite[] frames;

    private SpriteRenderer host, flame;
    private float clock;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttachAll();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AttachAll();

    private static void AttachAll()
    {
        try
        {
            if (!LoadFrames()) return;
            bool sconce = LoadSconce();
            foreach (WallTorch t in FindObjectsByType<WallTorch>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (sconce && IsTestTorch(t)) { ToSconce(t); continue; }
                if (t.GetComponent<RaPixelFlame>() == null && t.GetComponent<HorusPixelFlame>() == null) t.gameObject.AddComponent<RaPixelFlame>();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[라 픽셀 불꽃] 자동 붙이기 실패: {e.Message}");
        }
    }

    // [추가] 테스트용 횃불(Light_off / Light_on: 막대 위 네모)을 호루스 방과 같은 청동 횃대 + 움직이는 불꽃으로
    // - 그림: Resources/Horus/PixelFx/HO_Sconce_Base, HO_Flame_0~7 (HorusPixelFlame이 켜졌을 때 불꽃, 불씨, 빛 깜빡임)
    // [확인 필요] 씬의 횃불 크기(Scale)가 테스트 그림에 맞춰져 있으면 횃대가 커 보일 수 있음
    private static Sprite sconceBase;
    private static Sprite[] sconceFrames;

    private static bool LoadSconce()
    {
        if (sconceBase != null && sconceFrames != null) return true;
        var list = new Sprite[FrameCount];
        for (int i = 0; i < FrameCount; i++)
        {
            list[i] = Resources.Load<Sprite>($"Horus/PixelFx/HO_Flame_{i}");
            if (list[i] == null) return false;
        }
        sconceBase = Resources.Load<Sprite>("Horus/PixelFx/HO_Sconce_Base");
        sconceFrames = list;
        return sconceBase != null;
    }

    private static bool IsTestTorch(WallTorch t)
    {
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        return sr != null && sr.sprite != null && sr.sprite.name.StartsWith("Light_");
    }

    private static void ToSconce(WallTorch t)
    {
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        t.UseSprites(sconceBase, sconceBase);
        sr.sprite = sconceBase;
        if (t.GetComponent<HorusPixelFlame>() == null) t.gameObject.AddComponent<HorusPixelFlame>().Setup(sconceFrames, sconceBase);
        RaPixelFlame old = t.GetComponent<RaPixelFlame>();
        if (old != null) Destroy(old);
    }

    private static bool LoadFrames()
    {
        if (frames != null) return true;
        var list = new Sprite[FrameCount];
        for (int i = 0; i < FrameCount; i++)
        {
            list[i] = Resources.Load<Sprite>(Art + i);
            if (list[i] == null) return false;
        }
        frames = list;
        return true;
    }

    private void Start()
    {
        host = GetComponentInChildren<SpriteRenderer>();
        clock = UnityEngine.Random.value * 5f;
    }

    private void LateUpdate()
    {
        try
        {
            bool lit = host != null && host.sprite != null && host.sprite.name == OnSpriteName && host.enabled;
            if (lit && flame == null) Build();
            if (flame == null) return;
            flame.enabled = lit;
            if (!lit) return;
            clock += Time.deltaTime;
            flame.sprite = frames[(int)(clock * 10f) % frames.Length];
        }
        catch (Exception e)
        {
            Debug.LogError($"[라 픽셀 불꽃] {name}: {e.Message}");
            enabled = false;
        }
    }

    // 횃불 그림 맨 위(불 자리)에 얹음. 횃불이 얼마나 확대/축소돼 있든 불꽃은 도트 크기 그대로
    private void Build()
    {
        var go = new GameObject("PixelFlame");
        go.transform.SetParent(host.transform, false);
        Bounds b = host.sprite.bounds;
        go.transform.localPosition = new Vector3(b.center.x, b.max.y - b.size.y * 0.18f, 0f);
        Vector3 s = host.transform.lossyScale;
        go.transform.localScale = new Vector3(1f / Mathf.Max(0.001f, Mathf.Abs(s.x)), 1f / Mathf.Max(0.001f, Mathf.Abs(s.y)), 1f);
        flame = go.AddComponent<SpriteRenderer>();
        flame.sortingLayerID = host.sortingLayerID;
        flame.sortingOrder = host.sortingOrder + 1;
    }
}
