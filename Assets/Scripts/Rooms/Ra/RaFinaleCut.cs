using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 라 엔딩 전체 화면 컷 (RaFinale이 만들고 지움)
// - 일러스트(없으면 도형) + 뒤에서 도는 빛줄기 + 해 + 위로 날리는 불티
// - 큐브 공개 (바스테트 엔딩 BastetEnding과 같은 방식):
//   왼쪽에 큐브, 오른쪽에 여섯 신의 면 칸 -> 라의 면이 큐브에서 날아가 "라" 칸에 박힘 -> 번쩍 + (형판 n / 6)
// - 정렬 94: 검은 바(90)보다 위, 건너뛰기 안내(95), 대사창(100), 검은 화면(200)보다 아래
public class RaFinaleCut : MonoBehaviour
{
    private const int SortingOrder = 94;
    private const float FaceFlyTime = 0.9f;
    private static readonly Vector2 SunPos = new Vector2(0f, 150f);
    private static readonly Vector2 CubePos = new Vector2(-430f, 20f);
    private static readonly Vector2 CubeSize = new Vector2(560f, 560f);
    private static readonly Vector2 SlotSize = new Vector2(190f, 190f);
    private static readonly Color Gold = new Color(1f, 0.78f, 0.3f, 1f);
    private static readonly Color Paper = new Color(0.96f, 0.93f, 0.85f, 1f);
    private static readonly Color SlotEmpty = new Color(0.16f, 0.12f, 0.08f, 0.9f);

    // 바스테트 엔딩과 같은 순서
    private static readonly string[] GodNames = { "라", "바스테트", "토트", "호루스", "소베크", "아누비스" };
    private static readonly string[] PlateIds = { "PLATE_RA", "PLATE_BASTET", "PLATE_THOTH", "PLATE_HORUS", "PLATE_SOBEK", "PLATE_ANUBIS" };

    private class Ember
    {
        public RectTransform rt;
        public Image img;
        public float speed, sway, age, life;
    }

    private readonly List<Ember> embers = new List<Ember>();
    private readonly Image[] slots = new Image[RaFinaleArt.FaceCount];
    private RaFinaleArt art;
    private Font titleFont, bodyFont;
    private RectTransform root, stage, cubeRoot, rays;
    private CanvasGroup titleGroup, cubeGroup;
    private Image flash, cubeGlow, cubeImage, flyingFace;
    private Text captionText, statusText;
    private float emberTimer;
    private bool running;

    public static RaFinaleCut Create(RaFinaleArt art, RaFinaleLines lines)
    {
        var go = new GameObject("RaFinaleCut");
        RaFinaleCut cut = go.AddComponent<RaFinaleCut>();
        try
        {
            cut.Build(art, lines);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RaFinaleCut] 화면 만들기 오류: {e}");
        }
        return cut;
    }

    // ================= 만들기 =================
    private void Build(RaFinaleArt finaleArt, RaFinaleLines lines)
    {
        art = finaleArt;
        titleFont = PuzzleUITheme.GetTitleFont() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bodyFont = PuzzleUITheme.GetBodyFont() ?? titleFont;
        root = UIBuild.CreateCanvas(transform, "RaFinaleCanvas", SortingOrder, out _);

        Image bg = UIBuild.Stretch(root, "Bg").gameObject.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.03f, 0.02f, 1f); // 뒤쪽 클릭 막기 (raycastTarget 기본 켜짐)

        stage = UIBuild.Stretch(root, "Stage");
        bool hasArt = art != null && art.raIllustration != null;
        BuildSun(stage, !hasArt);
        // 일러스트 배경이 투명이면 뒤에서 도는 빛줄기가 보임 (꽉 찬 그림이면 그림이 다 덮음)
        if (hasArt) UIBuild.Image(stage, "Illustration", Vector2.zero, new Vector2(1920f, 1080f), art.raIllustration, Color.white);
        else RaFinaleShapes.Placeholder(stage, bodyFont);

        BuildTitle(lines);
        BuildCube();
        flash = UIBuild.Stretch(root, "Flash").gameObject.AddComponent<Image>();
        flash.color = new Color(1f, 0.95f, 0.82f, 0f);
        flash.raycastTarget = false;
    }

    // 해 원반은 임시 그림일 때만 (일러스트에는 아트가 그린 해가 있음)
    private void BuildSun(RectTransform parent, bool withDisk)
    {
        rays = UIBuild.Image(parent, "Rays", SunPos, new Vector2(2600f, 2600f), RaFinaleShapes.Rays(), Color.white).rectTransform;
        rays.GetComponent<Image>().color = new Color(1f, 0.68f, 0.25f, 0.28f);
        Image glow = UIBuild.Image(parent, "SunGlow", SunPos, new Vector2(1400f, 1400f), FxSprites.Soft, Color.white);
        glow.color = new Color(1f, 0.5f, 0.12f, 0.6f);
        if (!withDisk) return;
        Image disk = UIBuild.Image(parent, "SunDisk", SunPos, new Vector2(430f, 430f), UIBuild.Circle(), Color.white);
        disk.color = new Color(1f, 0.82f, 0.38f, 1f);
    }

    private void BuildTitle(RaFinaleLines lines)
    {
        RectTransform t = UIBuild.Stretch(root, "Title");
        titleGroup = t.gameObject.AddComponent<CanvasGroup>();
        titleGroup.alpha = 0f;
        Text title = UIBuild.Text(t, "Name", titleFont, 46, new Vector2(0f, 470f), new Vector2(1200f, 60f), Gold);
        title.text = lines != null ? lines.title : "";
        Text sub = UIBuild.Text(t, "Sub", bodyFont, 24, new Vector2(0f, 420f), new Vector2(1200f, 36f), new Color(1f, 0.92f, 0.75f, 0.85f));
        sub.text = lines != null ? lines.subtitle : "";
    }

    // 왼쪽 큐브 + 오른쪽 여섯 면 칸 (바스테트 엔딩과 같은 배치)
    private void BuildCube()
    {
        cubeRoot = UIBuild.Stretch(root, "CubeReveal");
        cubeGroup = cubeRoot.gameObject.AddComponent<CanvasGroup>();
        cubeGroup.alpha = 0f;

        Image dim = cubeRoot.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.78f);
        dim.raycastTarget = false;
        cubeGlow = UIBuild.Image(cubeRoot, "Glow", CubePos, new Vector2(1000f, 1000f), FxSprites.Soft, Color.white);
        cubeGlow.color = new Color(1f, 0.7f, 0.25f, 0.5f);
        cubeImage = UIBuild.Image(cubeRoot, "Cube", CubePos, CubeSize, art != null ? art.cubeSprite : null, new Color(0.12f, 0.23f, 0.37f, 1f));
        for (int i = 0; i < slots.Length; i++) BuildSlot(i);

        captionText = UIBuild.Text(cubeRoot, "Caption", titleFont, 44, new Vector2(0f, 445f), new Vector2(1400f, 60f), Gold);
        Text sub = UIBuild.Text(cubeRoot, "Sub", bodyFont, 26, new Vector2(0f, 390f), new Vector2(1200f, 40f), Paper);
        sub.text = "여섯 신의 큐브";
        statusText = UIBuild.Text(cubeRoot, "Status", bodyFont, 32, new Vector2(0f, -380f), new Vector2(1600f, 50f), Paper);

        flyingFace = UIBuild.Image(cubeRoot, "FlyingFace", CubePos, new Vector2(360f, 360f), art != null ? art.Face(RaFinaleArt.RaFace) : null, Gold);
        flyingFace.enabled = false;
    }

    private void BuildSlot(int i)
    {
        int col = i % 3, row = i / 3;
        Vector2 pos = new Vector2(250f + col * 230f, 150f - row * 290f);
        Sprite face = i != RaFinaleArt.RaFace && HasPlate(i) && art != null ? art.Face(i) : null;
        slots[i] = UIBuild.Image(cubeRoot, $"Slot_{GodNames[i]}", pos, SlotSize, face, SlotEmpty);
        Text label = UIBuild.Text(cubeRoot, $"Name_{GodNames[i]}", bodyFont, 28, pos + new Vector2(0f, -125f), new Vector2(220f, 40f), i == RaFinaleArt.RaFace ? Gold : Paper);
        label.text = GodNames[i];
    }

    // ================= 움직임 =================
    private void Update()
    {
        if (!running || rays == null) return;
        rays.Rotate(0f, 0f, -5f * Time.deltaTime);
        emberTimer += Time.deltaTime;
        if (emberTimer > 0.08f)
        {
            emberTimer = 0f;
            SpawnEmber();
        }
        for (int i = embers.Count - 1; i >= 0; i--) MoveEmber(embers[i], i);
    }

    private void SpawnEmber()
    {
        float size = UnityEngine.Random.Range(8f, 22f);
        Vector2 pos = new Vector2(UnityEngine.Random.Range(-920f, 920f), -560f);
        Image img = UIBuild.Image(stage, "Ember", pos, new Vector2(size, size), FxSprites.Soft, Color.white);
        img.color = new Color(1f, UnityEngine.Random.Range(0.45f, 0.8f), 0.2f, 0f);
        embers.Add(new Ember
        {
            rt = img.rectTransform,
            img = img,
            speed = UnityEngine.Random.Range(70f, 170f),
            sway = UnityEngine.Random.Range(0f, 6.28f),
            life = UnityEngine.Random.Range(4f, 7f)
        });
    }

    private void MoveEmber(Ember e, int index)
    {
        e.age += Time.deltaTime;
        if (e.age >= e.life || e.rt == null)
        {
            if (e.rt != null) Destroy(e.rt.gameObject);
            embers.RemoveAt(index);
            return;
        }
        float x = Mathf.Sin(e.age * 1.7f + e.sway) * 30f * Time.deltaTime;
        e.rt.anchoredPosition += new Vector2(x, e.speed * Time.deltaTime);
        Color c = e.img.color;
        c.a = Mathf.Sin(e.age / e.life * Mathf.PI) * 0.9f;
        e.img.color = c;
    }

    // ================= 장면 =================
    // 하얗게 번쩍 -> 라가 드러남 (살짝 다가오는 줌) -> 이름
    public IEnumerator Open()
    {
        running = true;
        flash.color = WithAlpha(flash.color, 1f);
        StartCoroutine(Scale(stage, 1.12f, 1f, 5f));
        yield return Fade(a => flash.color = WithAlpha(flash.color, a), 1f, 0f, 1.8f);
        yield return Fade(a => titleGroup.alpha = a, 0f, 1f, 0.8f);
    }

    // 큐브 등장 -> 라의 면이 큐브에서 "라" 칸으로 날아가 박힘 -> 번쩍 + 금빛 맥박
    public IEnumerator ShowCube(string caption)
    {
        captionText.text = caption;
        statusText.text = "큐브가 첫 번째 면을 기다린다.";
        titleGroup.alpha = 0f;
        cubeImage.rectTransform.localScale = Vector3.one * 0.6f;
        yield return Fade(a => cubeGroup.alpha = a, 0f, 1f, 0.6f);
        yield return Scale(cubeImage.rectTransform, 0.6f, 1f, 0.5f);
        yield return Cine.Wait(1f);
        yield return FlyFaceToSlot();

        GameAudio.Play("sfx_discover", 1f);
        GameAudio.Play("sfx_rumble", 0.6f, 1.3f);
        StartCoroutine(Fade(a => flash.color = WithAlpha(flash.color, a), 0.7f, 0f, 0.7f));
        statusText.text = $"큐브가 응답한다! 라의 면이 빛난다.  (형판 {CountPlates()} / {RaFinaleArt.FaceCount})";
        StartCoroutine(Pop(cubeImage.rectTransform));
        yield return PulseGlow(1.6f);
        yield return Cine.Wait(1.2f);
    }

    public IEnumerator HideCube()
    {
        yield return Fade(a => cubeGroup.alpha = a, 1f, 0f, 0.6f);
        titleGroup.alpha = 1f;
    }

    // 큐브 가운데에서 라의 면이 떠올라 포물선을 그리며 "라" 칸으로 (바스테트 엔딩과 같은 움직임)
    private IEnumerator FlyFaceToSlot()
    {
        RectTransform fly = flyingFace.rectTransform;
        Image target = slots[RaFinaleArt.RaFace];
        Vector2 from = CubePos, to = target.rectTransform.anchoredPosition;
        Vector2 fromSize = new Vector2(360f, 360f), toSize = target.rectTransform.sizeDelta;
        flyingFace.enabled = true;

        float t = 0f;
        while (t < FaceFlyTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / FaceFlyTime);
            float e = k * k * (3f - 2f * k);
            fly.anchoredPosition = Vector2.Lerp(from, to, e) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 120f;
            fly.sizeDelta = Vector2.Lerp(fromSize, toSize, e);
            yield return null;
        }

        flyingFace.enabled = false;
        target.sprite = flyingFace.sprite;
        target.color = target.sprite != null ? Color.white : Gold;
        target.preserveAspect = true;
        yield return Pop(target.rectTransform);
    }

    private IEnumerator Pop(RectTransform rt)
    {
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float s = 1f + 0.12f * Mathf.Sin(Mathf.Clamp01(t / 0.5f) * Mathf.PI);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private IEnumerator PulseGlow(float seconds)
    {
        Color baseColor = cubeGlow.color;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float k = 1f + Mathf.Sin(t * 9f) * 0.12f + Mathf.Max(0f, 1f - t * 2f) * 0.5f;
            cubeGlow.rectTransform.localScale = Vector3.one * k;
            cubeGlow.color = WithAlpha(baseColor, Mathf.Clamp01(baseColor.a * k));
            yield return null;
        }
        cubeGlow.rectTransform.localScale = Vector3.one;
        cubeGlow.color = baseColor;
    }

    public void Close()
    {
        running = false;
        Destroy(gameObject);
    }

    // ================= 형판 개수 (바스테트 엔딩과 같은 기준) =================
    private static int CountPlates()
    {
        int n = 0;
        for (int i = 0; i < RaFinaleArt.FaceCount; i++)
        {
            if (i == RaFinaleArt.RaFace || HasPlate(i)) n++;
        }
        return n;
    }

    private static bool HasPlate(int i)
    {
        if (ItemInventory.Instance != null && ItemInventory.Instance.Has(PlateIds[i])) return true;
        return i == RaFinaleArt.RaFace && GameManager.Instance != null && GameManager.Instance.HasFlag("F_RA_PLATE_GIVEN");
    }

    // ================= 도우미 =================
    private static IEnumerator Fade(Action<float> apply, float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            apply(Mathf.Lerp(from, to, t / seconds));
            yield return null;
        }
        apply(to);
    }

    private static IEnumerator Scale(RectTransform rt, float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds && rt != null)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / seconds);
            rt.localScale = Vector3.one * Mathf.Lerp(from, to, p);
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one * to;
    }

    private static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }
}
