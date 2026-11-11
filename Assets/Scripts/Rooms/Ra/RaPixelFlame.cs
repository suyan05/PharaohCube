using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// 라의 방 벽 횃불(WallTorch)에 움직이는 작은 픽셀 불꽃을 얹음 (호루스 횃대의 HorusPixelFlame과 같은 생각)
// - 켜진 그림(Light_on)일 때만 불꽃이 보임. 꺼지면 같이 사라짐
// - 씬이 열릴 때 자동으로 붙음. 호루스 횃대(HO_Sconce 그림)에는 HorusPixelFlame이 따로 붙으므로 여기선 아무것도 안 함
// - 그림: Resources/Horus/PixelFx/RA_Flame_0~7 (10x12 도트)
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
            foreach (WallTorch t in FindObjectsByType<WallTorch>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.GetComponent<RaPixelFlame>() == null) t.gameObject.AddComponent<RaPixelFlame>();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[라 픽셀 불꽃] 자동 붙이기 실패: {e.Message}");
        }
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
