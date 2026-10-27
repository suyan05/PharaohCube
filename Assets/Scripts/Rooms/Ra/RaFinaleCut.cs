using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 라 엔딩 전체 화면 컷 (RaFinale이 만들고 지움)
// - 일러스트(없으면 도형) + 뒤에서 도는 빛줄기 + 해 + 위로 날리는 불티
// - 큐브 공개: 화면이 어두워지고 큐브가 나타남 -> 큐브의 라 면으로 바뀜 -> 태양 형판이 위에서 내려와 빈 칸에 박힘 -> 번쩍
// - 정렬 94: 검은 바(90)보다 위, 건너뛰기 안내(95), 대사창(100), 아이템 팝업(110), 검은 화면(200)보다 아래
public class RaFinaleCut : MonoBehaviour
{
    private const int SortingOrder = 94;
    private const float CubeSize = 600f;
    private const float FaceSize = 560f;
    private static readonly Vector2 SunPos = new Vector2(0f, 150f);
    private static readonly Vector2 CubePos = new Vector2(0f, 60f);
    private static readonly Color Gold = new Color(1f, 0.78f, 0.3f, 1f);

    private class Ember
    {
        public RectTransform rt;
        public Image img;
        public float speed, sway, age, life;
    }

    private readonly List<Ember> embers = new List<Ember>();
    private RaFinaleArt art;
    private Font titleFont, bodyFont;
    private RectTransform root, stage, cubeRoot, rays;
    private CanvasGroup titleGroup, cubeGroup;
    private Image flash, cubeGlow, cubeImage, faceImage, plateImage;
    private Text captionText;
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
        BuildSun(stage);
        if (art != null && art.raIllustration != null) UIBuild.Image(stage, "Illustration", Vector2.zero, new Vector2(1920f, 1080f), art.raIllustration, Color.white);
        else RaFinaleShapes.Placeholder(stage, bodyFont);

        BuildTitle(lines);
        BuildCube();
        flash = UIBuild.Stretch(root, "Flash").gameObject.AddComponent<Image>();
        flash.color = new Color(1f, 0.95f, 0.82f, 0f);
        flash.raycastTarget = false;
    }

    private void BuildSun(RectTransform parent)
    {
        rays = UIBuild.Image(parent, "Rays", SunPos, new Vector2(2600f, 2600f), RaFinaleShapes.Rays(), Color.white).rectTransform;
        rays.GetComponent<Image>().color = new Color(1f, 0.68f, 0.25f, 0.28f);
        Image glow = UIBuild.Image(parent, "SunGlow", SunPos, new Vector2(1400f, 1400f), FxSprites.Soft, Color.white);
        glow.color = new Color(1f, 0.5f, 0.12f, 0.6f);
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

    private void BuildCube()
    {
        cubeRoot = UIBuild.Stretch(root, "CubeReveal");
        cubeGroup = cubeRoot.gameObject.AddComponent<CanvasGroup>();
        cubeGroup.alpha = 0f;

        Image dim = cubeRoot.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.72f);
        dim.raycastTarget = false;
        cubeGlow = UIBuild.Image(cubeRoot, "Glow", CubePos, new Vector2(1150f, 1150f), FxSprites.Soft, Color.white);
        cubeGlow.color = new Color(1f, 0.7f, 0.25f, 0.55f);
        cubeImage = UIBuild.Image(cubeRoot, "Cube", CubePos, new Vector2(CubeSize, CubeSize), art != null ? art.cubeSprite : null, new Color(0.12f, 0.23f, 0.37f, 1f));
        faceImage = UIBuild.Image(cubeRoot, "RaFace", CubePos, new Vector2(FaceSize, FaceSize), art != null ? art.faceSprite : null, new Color(0.85f, 0.45f, 0.3f, 1f));
        faceImage.color = WithAlpha(faceImage.color, 0f);
        plateImage = UIBuild.Image(cubeRoot, "Plate", CubePos + new Vector2(0f, 520f), new Vector2(150f, 150f), art != null ? art.plateSprite : null, Gold);
        captionText = UIBuild.Text(cubeRoot, "Caption", titleFont, 40, new Vector2(0f, 440f), new Vector2(1400f, 60f), Gold);
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

    // 큐브 등장 -> 라의 면으로 바뀜 -> 형판이 빈 칸에 박힘 -> 번쩍 + 금빛 맥박
    public IEnumerator ShowCube(string caption)
    {
        captionText.text = caption;
        titleGroup.alpha = 0f;
        cubeImage.rectTransform.localScale = Vector3.one * 0.6f;
        yield return Fade(a => cubeGroup.alpha = a, 0f, 1f, 0.6f);
        yield return Scale(cubeImage.rectTransform, 0.6f, 1f, 0.5f);
        yield return Cine.Wait(1.2f);
        yield return CubeToFace();
        yield return DropPlate();
        GameAudio.Play("sfx_discover", 1f);
        GameAudio.Play("sfx_rumble", 0.6f, 1.3f);
        StartCoroutine(Fade(a => flash.color = WithAlpha(flash.color, a), 0.85f, 0f, 0.7f));
        yield return PulseGlow(1.6f);
    }

    public IEnumerator HideCube()
    {
        yield return Fade(a => cubeGroup.alpha = a, 1f, 0f, 0.6f);
        titleGroup.alpha = 1f;
    }

    // 큐브가 살짝 커지며 사라지고 그 자리에 라의 면이 나타남 (큐브 앞으로 다가가는 느낌)
    private IEnumerator CubeToFace()
    {
        StartCoroutine(Scale(cubeImage.rectTransform, 1f, 1.35f, 0.7f));
        yield return Fade(a =>
        {
            cubeImage.color = WithAlpha(cubeImage.color, 1f - a);
            faceImage.color = WithAlpha(faceImage.color, a);
        }, 0f, 1f, 0.7f);
        yield return Cine.Wait(0.3f);
    }

    // 위에서 크게 -> 라 면의 빈 칸으로 내려와 딱 맞는 크기로 박힘
    private IEnumerator DropPlate()
    {
        Vector2 slot = art != null ? art.faceSlot : new Vector2(0.506f, 0.378f);
        float slotHeight = (art != null ? art.faceSlotHeight : 0.238f) * FaceSize;
        Vector2 target = CubePos + (slot - new Vector2(0.5f, 0.5f)) * FaceSize;
        Vector2 start = plateImage.rectTransform.anchoredPosition;
        float endScale = slotHeight / plateImage.rectTransform.sizeDelta.y;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.9f;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            plateImage.rectTransform.anchoredPosition = Vector2.Lerp(start, target, p);
            plateImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.6f, endScale, p);
            yield return null;
        }
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
