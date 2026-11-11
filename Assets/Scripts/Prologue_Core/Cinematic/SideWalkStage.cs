using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 옆에서 본 통로 무대 - 탑다운 게임이 잠깐 "옆모습 2D"로 바뀌어, 실루엣이 된 플레이어가 통로를 직접 걸어감
// - yield return SideWalkStage.Play(장면);  (Cine.Begin ~ Cine.End 사이)  -> 끝나면 검은 화면으로 남음
//   yield return SideWalkStage.Release();   -> 검은 화면을 걷어냄 (사이에 MuralStage 컷을 끼울 수 있게 둘로 나눔)
// - 조작: A / D (또는 화살표) 걷기, E 조사 / 끝의 문으로 들어가기. ESC 길게 = 건너뛰기
// - 영화식 장치: 3겹 패럴랙스(먼 벽 0.5배, 가운데 1배, 앞 기둥 1.7배 속도), 앞을 스쳐 지나가는 검은 기둥,
//   걸을 때 미세한 핸드헬드 흔들림, 시작할 때 천천히 물러나는 돌리 아웃 + 제목, 비네트, 움직이는 횃불과 빛
// - 그림: Resources/Horus/PixelFx/SW_* (도트 320x180을 6배로 보여줌), 횃불은 HO_Flame_0~7 재사용
public class SideWalkStage : MonoBehaviour
{
    private const float K = 6f;             // 도트 1px = 화면 6px
    private const int SortOrder = 94;       // 벽화 무대(95), 대사창(100)보다 아래
    private const string Art = "Horus/PixelFx/";
    private const float FeetY = 141f;       // 발이 닿는 높이 (바닥 윗선 132보다 조금 아래)
    private const float Speed = 44f;        // 걷는 속도 (도트 px/초)
    private const float BarHeight = 96f;
    private static SideWalkStage instance;

    private GameObject canvasGo, frame;
    private RectTransform world, far, mid, fore;
    private Image black, playerImg;
    private Text titleText, subText, promptText;
    private Sprite[] walk, flames;
    private Sprite idle;
    private readonly List<Image> flameImgs = new List<Image>();
    private readonly List<Image> glowImgs = new List<Image>();
    private SideWalkScene scene;
    private float px, camX, clock, walkClock, stepTimer, cooldown;

    private static SideWalkStage Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<SideWalkStage>();
            if (instance == null) instance = new GameObject("SideWalkStage").AddComponent<SideWalkStage>();
            return instance;
        }
    }

    // ================= 재생 =================
    public static IEnumerator Play(SideWalkScene scene)
    {
        if (Cine.Skipping || scene == null) yield break;
        SideWalkStage s = Instance;
        if (!s.TryOpen(scene)) yield break;
        yield return s.Fade(0f, 1f, 0.5f);
        s.frame.SetActive(true);
        yield return s.Intro();
        yield return s.WalkLoop();
        yield return s.Fade(0f, 1f, 0.6f);
        s.frame.SetActive(false);
    }

    public static IEnumerator Release()
    {
        SideWalkStage s = instance;
        if (s == null || s.canvasGo == null || !s.canvasGo.activeSelf) yield break;
        yield return s.Fade(1f, 0f, 0.6f);
        s.canvasGo.SetActive(false);
    }

    private bool TryOpen(SideWalkScene newScene)
    {
        try
        {
            scene = newScene;
            EnsureBuilt();
            if (!LoadSprites()) return false;
            BuildWorld();
            px = 30f;
            camX = 0f;
            cooldown = 0f;
            frame.SetActive(false);
            SetBlack(0f);
            canvasGo.SetActive(true);
            ApplyCamera(0f, false);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[옆모습 통로] 준비 오류: {e}");
            if (canvasGo != null) canvasGo.SetActive(false);
            return false;
        }
    }

    // 돌리 아웃: 살짝 확대된 상태에서 천천히 물러나며 제목이 떴다 사라짐
    private IEnumerator Intro()
    {
        const float seconds = 2.6f;
        float t = 0f;
        while (t < seconds && !Cine.Skipping && !Cine.Hurry)
        {
            t += Time.deltaTime;
            SetBlack(1f - Mathf.Clamp01(t / 0.9f));
            world.localScale = Vector3.one * Mathf.Lerp(1.12f, 1f, Mathf.SmoothStep(0f, 1f, t / seconds));
            SetTitleAlpha(Mathf.Clamp01(Mathf.Min((t - 0.3f) / 0.6f, (seconds - t) / 0.6f)));
            Animate(0f);
            yield return null;
        }
        world.localScale = Vector3.one;
        SetBlack(0f);
        SetTitleAlpha(0f);
        yield return Cine.Say(scene.startLines);
        cooldown = 0.3f;
    }

    private IEnumerator WalkLoop()
    {
        bool done = false;
        while (!done && !Cine.Skipping)
        {
            cooldown -= Time.deltaTime;
            float dir = Input.GetAxisRaw("Horizontal");
            Step(dir);
            Animate(dir);
            SideWalkSpot spot = NearSpot();
            bool atExit = px >= scene.length - 80f;
            promptText.text = spot != null ? spot.prompt : (atExit ? scene.exitPrompt : "A / D: 걷기");
            if (cooldown <= 0f && Input.GetKeyDown(KeyCode.E))
            {
                if (spot != null) yield return Talk(spot);
                else if (atExit) done = true;
            }
            yield return null;
        }
        promptText.text = "";
    }

    private IEnumerator Talk(SideWalkSpot spot)
    {
        promptText.text = "";
        Animate(0f);
        yield return null; // 같은 E 입력이 대사를 바로 넘기지 않게 한 프레임 쉼
        yield return Cine.Say(spot.lines);
        spot.seen = true;
        cooldown = 0.3f;
    }

    private SideWalkSpot NearSpot()
    {
        SideWalkSpot best = null;
        float bestDist = 22f;
        foreach (SideWalkSpot s in scene.spots)
        {
            float d = Mathf.Abs(s.x - px);
            if (d >= bestDist || s.lines == null || s.lines.Length == 0) continue;
            best = s;
            bestDist = d;
        }
        return best;
    }

    // ================= 걷기 / 카메라 =================
    private void Step(float dir)
    {
        px = Mathf.Clamp(px + dir * Speed * Time.deltaTime, 22f, scene.length - 46f);
        if (dir != 0f && playerImg != null) playerImg.rectTransform.localScale = new Vector3(dir > 0f ? 1f : -1f, 1f, 1f);
        float lead = playerImg != null ? playerImg.rectTransform.localScale.x * 34f : 0f; // 가는 쪽을 더 많이 보여줌
        float goal = Mathf.Clamp(px + lead - 160f, 0f, scene.length - 320f);
        camX = Mathf.Lerp(camX, goal, 1f - Mathf.Exp(-3.2f * Time.deltaTime));
        ApplyCamera(dir, true);
    }

    private void ApplyCamera(float dir, bool sway)
    {
        float bob = sway && dir != 0f ? Mathf.Sin(walkClock * 7f) * 3f : 0f; // 걸을 때만 살짝 (핸드헬드)
        far.anchoredPosition = new Vector2(-camX * 0.5f * K, bob * 0.4f);
        mid.anchoredPosition = new Vector2(-camX * K, bob);
        fore.anchoredPosition = new Vector2(-camX * 1.7f * K, bob * 1.6f);
        if (playerImg != null) playerImg.rectTransform.anchoredPosition = P(px, FeetY - 18f);
    }

    private void Animate(float dir)
    {
        clock += Time.deltaTime;
        if (playerImg != null)
        {
            if (dir != 0f)
            {
                walkClock += Time.deltaTime;
                playerImg.sprite = walk[(int)(walkClock * 10f) % walk.Length];
                Footstep();
            }
            else playerImg.sprite = idle;
        }
        for (int i = 0; i < flameImgs.Count; i++)
        {
            if (flameImgs[i] != null) flameImgs[i].sprite = flames[(int)(clock * 10f + i * 3) % flames.Length];
            if (glowImgs[i] == null) continue;
            float n = Mathf.PerlinNoise(Mathf.Floor(clock * 12f) * 0.31f, i * 1.7f); // 1/12초마다 뚝뚝 바뀌는 빛
            glowImgs[i].color = new Color(scene.glowTint.r, scene.glowTint.g, scene.glowTint.b, 0.2f + n * 0.16f);
        }
    }

    private void Footstep()
    {
        stepTimer -= Time.deltaTime;
        if (stepTimer > 0f) return;
        stepTimer = 0.38f;
        GameAudio.PlayRandom("sfx_step_sand_", 3, 0.35f);
    }

    // ================= 통로 깔기 =================
    private void BuildWorld()
    {
        ClearChildren(far);
        ClearChildren(mid);
        ClearChildren(fore);
        flameImgs.Clear();
        glowImgs.Clear();
        Color dim = scene.wallTint * 0.9f;
        dim.a = 1f;
        float farEnd = (scene.length - 320f) * 0.5f + 320f;
        for (float x = 160f; x < farEnd + 160f; x += 320f) Put(far, "SW_Wall", x, 66f, scene.wallTint);
        for (float x = 160f; x < scene.length + 160f; x += 320f) Put(mid, "SW_Floor", x, 156f, dim);
        BuildGates();
        BuildColumns();
        BuildSpots();
        playerImg = Put(mid, "SW_Idle", px, FeetY - 18f, Color.white);
        float foreEnd = (scene.length - 320f) * 1.7f + 320f;
        for (float x = 260f; x < foreEnd; x += 310f) Put(fore, "SW_Fore", x, 90f, Color.white);
        titleText.text = scene.title;
        subText.text = scene.sub;
    }

    private void BuildGates()
    {
        Font font = PuzzleUITheme.GetBodyFont();
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        foreach (SideWalkGate g in scene.gates)
        {
            Put(mid, "SW_Gate", g.x, 70f, g.dark ? new Color(0.32f, 0.26f, 0.36f, 1f) : scene.wallTint);
            if (string.IsNullOrEmpty(g.label)) continue;
            // [수정] 바닥에 두면 대사창에 가려짐 -> 문 안쪽 어두운 윗부분에
            Text label = UIBuild.Text(mid, "GateLabel", font, 22, P(g.x, 46f), new Vector2(200f, 60f), new Color(1f, 0.9f, 0.7f, g.dark ? 0.5f : 0.85f));
            label.text = g.label;
        }
        float doorX = scene.length - 46f;
        Image glow = Put(mid, "SW_Glow", doorX, 80f, new Color(scene.glowTint.r, scene.glowTint.g, scene.glowTint.b, 0.3f));
        if (glow != null) glow.rectTransform.sizeDelta *= 2.4f;
        Put(mid, "SW_Door", doorX, 68f, Color.white);
    }

    // 기둥 사이사이에 횃대 (문 자리는 비움)
    private void BuildColumns()
    {
        for (float x = 70f; x < scene.length - 110f; x += 150f)
        {
            if (!NearGate(x, 50f)) Put(mid, "SW_Column", x, 67f, scene.wallTint);
            float sx = x + 75f;
            if (sx > scene.length - 100f || NearGate(sx, 44f)) continue;
            // [수정] 횃대가 사람만큼 커 보였음 -> 0.6배로 줄임 (불꽃 밑동이 횃대 테두리에 닿게 높이도 같이 맞춤)
            Image glow = Put(mid, "SW_Glow", sx, 70f, scene.glowTint);
            if (glow != null) glow.rectTransform.sizeDelta *= 1.5f;
            Shrink(Put(mid, "HO_Sconce_Base", sx, 80f, Color.white));
            flameImgs.Add(Shrink(Put(mid, "HO_Flame_0", sx, 69.8f, Color.white)));
            glowImgs.Add(glow);
        }
    }

    private static Image Shrink(Image img)
    {
        if (img != null) img.rectTransform.sizeDelta *= 0.6f;
        return img;
    }

    private bool NearGate(float x, float range)
    {
        foreach (SideWalkGate g in scene.gates)
            if (Mathf.Abs(g.x - x) < range) return true;
        return false;
    }

    private void BuildSpots()
    {
        foreach (SideWalkSpot s in scene.spots)
        {
            s.seen = false;
            if (!string.IsNullOrEmpty(s.sprite)) Put(mid, s.sprite, s.x, s.y, Color.white);
        }
    }

    // 도트 좌표(왼쪽 위 0,0 / 아래로 +y)에 그림을 놓음. 그림이 없으면 경고만 하고 넘어감
    private static Image Put(RectTransform parent, string art, float x, float y, Color tint)
    {
        Sprite s = Resources.Load<Sprite>(Art + art);
        if (s == null)
        {
            Debug.LogWarning($"[옆모습 통로] 그림 없음: {Art}{art}");
            return null;
        }
        Image img = UIBuild.Image(parent, art, P(x, y), s.rect.size * K, s, tint);
        img.preserveAspect = false;
        img.color = tint;
        return img;
    }

    private static Vector2 P(float x, float y) => new Vector2((x - 160f) * K, (90f - y) * K);

    private static void ClearChildren(RectTransform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
    }

    private bool LoadSprites()
    {
        if (walk != null && flames != null && idle != null) return true;
        var w = new Sprite[8];
        var f = new Sprite[8];
        for (int i = 0; i < 8; i++)
        {
            w[i] = Resources.Load<Sprite>($"{Art}SW_Walk_{i}");
            f[i] = Resources.Load<Sprite>($"{Art}HO_Flame_{i}");
            if (w[i] != null && f[i] != null) continue;
            Debug.LogWarning($"[옆모습 통로] 그림 없음: {Art}SW_Walk_{i} 또는 HO_Flame_{i}");
            return false;
        }
        idle = Resources.Load<Sprite>(Art + "SW_Idle");
        walk = w;
        flames = f;
        return idle != null;
    }

    // ================= 화면 틀 =================
    private void EnsureBuilt()
    {
        if (canvasGo != null) return;
        Font font = PuzzleUITheme.GetBodyFont();
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform root = UIBuild.CreateCanvas(transform, "SideWalkCanvas", SortOrder, out canvasGo);
        RectTransform group = UIBuild.Stretch(root, "Frame");
        frame = group.gameObject;
        UIBuild.Image(group, "Back", Vector2.zero, new Vector2(6000f, 4000f), null, new Color(0.03f, 0.02f, 0.05f, 1f));
        world = UIBuild.Rect(group, "World", Vector2.zero, new Vector2(1920f, 1080f));
        far = UIBuild.Rect(world, "Far", Vector2.zero, new Vector2(1920f, 1080f));
        mid = UIBuild.Rect(world, "Mid", Vector2.zero, new Vector2(1920f, 1080f));
        fore = UIBuild.Rect(world, "Fore", Vector2.zero, new Vector2(1920f, 1080f));
        Image vig = UIBuild.Image(group, "Vignette", Vector2.zero, new Vector2(1940f, 1100f), Resources.Load<Sprite>(Art + "SW_Vignette"), Color.clear);
        vig.preserveAspect = false;
        Bar(group, true);
        RectTransform bottom = Bar(group, false);
        promptText = UIBuild.Text(bottom, "Prompt", font, 30, new Vector2(0f, 0f), new Vector2(1400f, 60f), new Color(1f, 0.93f, 0.78f, 0.9f));
        titleText = UIBuild.Text(group, "Title", font, 70, new Vector2(0f, 150f), new Vector2(1600f, 100f), new Color(1f, 0.9f, 0.68f, 0f));
        subText = UIBuild.Text(group, "Sub", font, 30, new Vector2(0f, 80f), new Vector2(1600f, 50f), new Color(1f, 1f, 1f, 0f));
        black = UIBuild.Image(root, "Fade", Vector2.zero, new Vector2(6000f, 4000f), null, Color.black);
        canvasGo.SetActive(false);
    }

    private static RectTransform Bar(RectTransform root, bool top)
    {
        RectTransform rt = UIBuild.Rect(root, top ? "BarTop" : "BarBottom", Vector2.zero, Vector2.zero);
        rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rt.sizeDelta = new Vector2(0f, BarHeight);
        rt.anchoredPosition = Vector2.zero;
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        return rt;
    }

    private void SetTitleAlpha(float a)
    {
        Color c = titleText.color;
        titleText.color = new Color(c.r, c.g, c.b, a);
        subText.color = new Color(1f, 1f, 1f, a * 0.75f);
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds && !Cine.Skipping && !Cine.Hurry)
        {
            t += Time.deltaTime;
            SetBlack(Mathf.Lerp(from, to, t / seconds));
            yield return null;
        }
        SetBlack(to);
    }

    private void SetBlack(float a)
    {
        if (black != null) black.color = new Color(0f, 0f, 0f, a);
    }
}
