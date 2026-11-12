using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 발굴 현장(Prologue_Dig) 미장센 - 씬이 열리면 자동으로 깔림 (씬 파일을 손으로 고치지 않아도 됨)
// 1. 바닥 장식: 발굴 격자 줄, 쓰러진 기둥, 토기 조각, 뼈, 측량 깃발, 삽, 모래 물결, 자갈
// 2. 복선: 모래 위에 똬리 튼 뱀이 기어간 자국(DG_Track) + 뱀과 금 간 태양이 새겨진 석판(DG_Slab) -> E로 조사
// 3. 살아있는 것: 모래바람 줄기가 흐르고, 마른 풀이 굴러가고, 맴도는 새 그림자가 바닥을 지나감
// - 전부 바닥에 붙은 납작한 그림이라 길을 막지 않음 (충돌 없음). 그림: Resources/Horus/PixelFx/DG_*
public class DigAtmosphere : MonoBehaviour
{
    private const string SceneKey = "Prologue_Dig";
    private const string Art = "Horus/PixelFx/DG_";
    public const string TrackFlag = "F_PRO_OMEN_TRACK";
    public static readonly Vector2 TrackPos = new Vector2(14.6f, 3.3f);
    private static readonly Vector2 SlabPos = new Vector2(15.6f, 10.2f);

    private string layer = "Default";
    private int ground;                     // 바닥 장식 정렬 순서 (플레이어보다 한 칸 뒤 = 모래 더미와 같음)
    private Material lit;                   // 플레이어와 같은 재질 (씬 조명 색을 똑같이 받게)
    private Transform[] birds;
    private Sprite[] birdFrames, flagFrames;
    private SpriteRenderer[] flags;
    private float clock;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreate(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryCreate(scene);

    private static void TryCreate(Scene scene)
    {
        try
        {
            if (!scene.name.Contains(SceneKey)) return;
            if (FindFirstObjectByType<DigAtmosphere>() != null) return;
            new GameObject("DigAtmosphere").AddComponent<DigAtmosphere>();
        }
        catch (Exception e)
        {
            Debug.LogError($"[발굴 현장 미장센] 만들기 오류: {e.Message}");
        }
    }

    private void Start()
    {
        try
        {
            ReadPlayerSorting();
            PlaceDecor();
            PlaceOmens();
            PlaceFlags();
            MakeBirds();
            StartCoroutine(WindLoop());
            StartCoroutine(WeedLoop());
        }
        catch (Exception e)
        {
            Debug.LogError($"[발굴 현장 미장센] 준비 오류: {e}");
        }
    }

    private void ReadPlayerSorting()
    {
        Transform player = CineActor.Player();
        SpriteRenderer sr = player != null ? player.GetComponentInChildren<SpriteRenderer>() : null;
        if (sr == null) return;
        layer = sr.sortingLayerName;
        ground = sr.sortingOrder - 1;
        lit = sr.sharedMaterial;
    }

    // ================= 바닥 장식 =================
    private void PlaceDecor()
    {
        Decal("Grid", 20.4f, 5.6f, false);
        Decal("Grid", 23.6f, 3.9f, true);
        Decal("Column", 10.8f, 10.5f, false);
        Decal("Column", 16.9f, 1.6f, true);
        Decal("Bones", 8.6f, 2.0f, false);
        Decal("Tools", 14.0f, 8.3f, false);
        Decal("Shards", 13.3f, 6.2f, false);
        Decal("Shards", 20.3f, 9.4f, true);
        Decal("Shards", 6.9f, 3.0f, true);
        Decal("Shards", 24.2f, 8.8f, false);
        var rng = new System.Random(47); // 발굴 47일째
        for (int i = 0; i < 9; i++) Scatter("Ripple", rng);
        for (int i = 0; i < 7; i++) Scatter(i % 2 == 0 ? "Pebbles_A" : "Pebbles_B", rng);
    }

    private void Scatter(string art, System.Random rng)
    {
        float x = 1.5f + (float)rng.NextDouble() * 25f;
        float y = 1.2f + (float)rng.NextDouble() * 9.5f;
        Decal(art, x, y, rng.Next(2) == 0);
    }

    private SpriteRenderer Decal(string art, float x, float y, bool flip)
    {
        Sprite s = Resources.Load<Sprite>(Art + art);
        if (s == null)
        {
            Debug.LogWarning($"[발굴 현장 미장센] 그림 없음: {Art}{art}");
            return null;
        }
        var go = new GameObject("DG_" + art);
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(x, y, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.flipX = flip;
        sr.sortingLayerName = layer;
        sr.sortingOrder = ground;
        if (lit != null) sr.sharedMaterial = lit;
        return sr;
    }

    // ================= 복선 =================
    private void PlaceOmens()
    {
        SpriteRenderer track = Decal("Track", TrackPos.x, TrackPos.y, false);
        if (track != null)
        {
            track.gameObject.AddComponent<DigOmen>().Setup("E: 모래 위 자국 살펴보기", TrackFlag, new[]
            {
                new DialogueLine("나", "모래에 뭔가 기어간 자국이 있다. 몇 번이나 제 몸을 넘어가며 똬리를 틀었어."),
                new DialogueLine("나", "폭이 내 팔뚝보다 굵은데... 이 사막에 이런 뱀이 있을 리가."),
                new DialogueLine("나", "...어젯밤 모래폭풍이 만든 무늬겠지. 그렇겠지.")
            }, new Vector2(3.2f, 2.2f));
        }
        SpriteRenderer slab = Decal("Slab", SlabPos.x, SlabPos.y, false);
        if (slab == null) return;
        slab.gameObject.AddComponent<DigOmen>().Setup("E: 묻힌 석판 살펴보기", "F_PRO_OMEN_SLAB", new[]
        {
            new DialogueLine("나", "모래에 반쯤 묻힌 석판이다. 뭔가 새겨져 있어."),
            new DialogueLine("나", "똬리를 튼 뱀, 그리고... 금이 간 태양?"),
            new DialogueLine("나", "이 근처 유적에선 본 적 없는 그림인데. 일단 수첩에 옮겨 두자.")
        }, new Vector2(1.3f, 1.1f));
    }

    // ================= 깃발 (2프레임 펄럭임) =================
    private void PlaceFlags()
    {
        flagFrames = new[] { Resources.Load<Sprite>(Art + "Flag_0"), Resources.Load<Sprite>(Art + "Flag_1") };
        Vector2[] spots = { new Vector2(18.0f, 8.2f), new Vector2(26.3f, 1.8f), new Vector2(17.8f, 1.9f), new Vector2(26.2f, 8.1f) };
        flags = new SpriteRenderer[spots.Length];
        for (int i = 0; i < spots.Length; i++) flags[i] = Decal("Flag_0", spots[i].x, spots[i].y, false);
    }

    // ================= 맴도는 새 그림자 =================
    private void MakeBirds()
    {
        birdFrames = new[] { Resources.Load<Sprite>(Art + "Bird_0"), Resources.Load<Sprite>(Art + "Bird_1") };
        birds = new Transform[2];
        for (int i = 0; i < birds.Length; i++)
        {
            SpriteRenderer sr = Decal("Bird_0", 0f, 0f, false);
            birds[i] = sr != null ? sr.transform : null;
        }
    }

    private void Update()
    {
        clock += Time.deltaTime;
        try
        {
            MoveBird(0, new Vector2(20.5f, 5.5f), 5.2f, 15f, 0f);   // B구역 위를 맴돎 (그 밑에 뭔가 있다는 듯이)
            MoveBird(1, new Vector2(9f, 6.5f), 4f, -19f, 2.4f);
            Flutter();
        }
        catch (Exception e)
        {
            Debug.LogError($"[발굴 현장 미장센] {e.Message}");
            enabled = false;
        }
    }

    private void MoveBird(int i, Vector2 center, float radius, float period, float phase)
    {
        if (birds == null || birds[i] == null) return;
        float a = clock / period * Mathf.PI * 2f + phase;
        birds[i].position = new Vector3(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius * 0.6f, 0f);
        float dirSign = Mathf.Sign(period);
        float heading = Mathf.Atan2(Mathf.Cos(a) * 0.6f * dirSign, -Mathf.Sin(a) * dirSign) * Mathf.Rad2Deg;
        birds[i].rotation = Quaternion.Euler(0f, 0f, Mathf.Round((heading - 90f) / 45f) * 45f); // 그림은 위를 봄. 45도 단위로만 (도트 유지)
        SpriteRenderer sr = birds[i].GetComponent<SpriteRenderer>();
        if (sr != null && birdFrames[0] != null && birdFrames[1] != null) sr.sprite = birdFrames[(int)(clock * 1.6f + i) % 6 == 0 ? 1 : 0];
    }

    private void Flutter()
    {
        if (flags == null || flagFrames == null || flagFrames[0] == null || flagFrames[1] == null) return;
        for (int i = 0; i < flags.Length; i++)
            if (flags[i] != null) flags[i].sprite = flagFrames[(int)(clock * 5f + i * 0.7f) % 2];
    }

    // ================= 모래바람 줄기 / 마른 풀 =================
    private IEnumerator WindLoop()
    {
        Sprite streak = Resources.Load<Sprite>(Art + "Streak");
        while (streak != null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                float h = cam.orthographicSize, w = h * cam.aspect;
                Vector3 c = cam.transform.position;
                var pos = new Vector3(c.x + UnityEngine.Random.Range(-w, w * 0.6f), c.y + UnityEngine.Random.Range(-h, h), 0f);
                Lit(DigDrifter.Spawn(streak, pos, new Vector2(UnityEngine.Random.Range(5f, 8f), -0.3f), 0.9f, layer, ground + 3, 0.7f));
            }
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.12f, 0.4f));
        }
    }

    private IEnumerator WeedLoop()
    {
        Sprite weed = Resources.Load<Sprite>(Art + "Weed");
        while (weed != null)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(7f, 14f));
            var pos = new Vector3(-0.5f, UnityEngine.Random.Range(1.5f, 10f), 0f);
            Lit(DigDrifter.Spawn(weed, pos, new Vector2(UnityEngine.Random.Range(3f, 4.2f), 0f), 9f, layer, ground + 2, 1f, 0.13f));
        }
    }

    private void Lit(DigDrifter d)
    {
        SpriteRenderer sr = d != null ? d.GetComponent<SpriteRenderer>() : null;
        if (sr != null && lit != null) sr.sharedMaterial = lit;
    }

    // 오프닝 연출용: 새 그림자 하나가 from -> to 로 휙 지나감
    public static void FlyOver(Vector2 from, Vector2 to, float seconds)
    {
        try
        {
            DigAtmosphere a = FindFirstObjectByType<DigAtmosphere>();
            Sprite bird = Resources.Load<Sprite>(Art + "Bird_0");
            if (a == null || bird == null) return;
            Vector2 v = (to - from) / Mathf.Max(0.1f, seconds);
            DigDrifter d = DigDrifter.Spawn(bird, from, v, seconds, a.layer, a.ground + 1, 1f);
            a.Lit(d);
            if (d != null) d.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Round((Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - 90f) / 45f) * 45f);
        }
        catch (Exception e)
        {
            Debug.LogError($"[발굴 현장 미장센] 새 그림자 오류: {e.Message}");
        }
    }
}
