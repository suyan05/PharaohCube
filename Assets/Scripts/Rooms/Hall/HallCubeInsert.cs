using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// [추가] 중앙 홀에서 큐브에 형판을 끼우는 연출 (신전을 끝내고 돌아왔을 때 HallTempDirector가 부름)
// 예전에는 바스테트 씬 안(BastetEnding)에서 큐브를 보여 줬는데, 이제 진짜 중앙 홀에 와서 끼움
//  1) 큐브 그림 + 여섯 면 칸 (이미 끼운 면은 그림, 나머지는 빈칸) "큐브가 형판을 기다린다"
//  2) E/Space -> 형판이 아래에서 날아와 그 신의 칸에 박힘 + 반짝 + "큐브가 응답한다! ○○의 면이 빛난다 (형판 n/6)"
//  3) E/Space -> 화면이 걷히고 홀로 돌아감 (그다음 큐브 빛·목소리·북쪽 문은 HallTempDirector)
// 화면은 코드가 만듦. 그림·글꼴은 메뉴 Tools/Bastet/★ 클리어 후 흐름 세팅 이 연결 (중앙 홀 씬에서 실행)
// 붙이는 곳: 중앙 홀 씬의 HallTempDirector와 같은 오브젝트
public class HallCubeInsert : MonoBehaviour
{
    public static readonly string[] GodNames = { "라", "바스테트", "토트", "호루스", "소베크", "아누비스" };

    [Header("그림 (메뉴가 자동 연결)")]
    [Tooltip("큐브 그림 (image/Cube/Cube)")]
    [SerializeField] private Sprite cubeSprite;
    [Tooltip("여섯 면 형판: 0 라, 1 바스테트, 2 토트, 3 호루스, 4 소베크, 5 아누비스 (image/Cube)")]
    [SerializeField] private Sprite[] faceSprites = new Sprite[6];
    [SerializeField] private TMP_FontAsset font;

    [Header("시간 (초)")]
    [Range(0.2f, 2f)] [SerializeField] private float fadeTime = 0.6f;
    [Range(0.3f, 2f)] [SerializeField] private float flyTime = 0.9f;

    private static readonly Color Gold = new Color(0.91f, 0.70f, 0.23f);
    private static readonly Color Paper = new Color(0.96f, 0.93f, 0.85f);
    private static readonly Color SlotEmpty = new Color(0.16f, 0.12f, 0.08f, 0.9f);

    private GameObject root;
    private CanvasGroup page;
    private Image cubeImage, flying;
    private Image[] slots;
    private TextMeshProUGUI status, hint;

    public bool IsPlaying { get; private set; }

    // slot: 끼울 칸 (0~5), filled: 이미 끼운 칸
    public IEnumerator Play(int slot, bool[] filled)
    {
        if (IsPlaying || slot < 0 || slot >= GodNames.Length) yield break;
        IsPlaying = true;
        Build(slot, filled);
        int have = 1;
        if (filled != null) foreach (bool f in filled) if (f) have++;
        Debug.Log($"[HallCubeInsert] {GodNames[slot]} 형판 끼우기 ({have}/6)", this);

        status.text = $"{GodNames[slot]}의 형판을 꺼냈다. 큐브가 형판을 기다린다.";
        hint.text = "E / Space : 형판 끼우기";
        yield return Fade(0f, 1f);
        yield return WaitPress();

        hint.text = "";
        yield return FlyToSlot(slot);
        status.text = $"큐브가 응답한다! {GodNames[slot]}의 면이 빛난다.  (형판 {have} / 6)";
        hint.text = "E / Space : 계속";
        yield return WaitPress();

        yield return Fade(1f, 0f);
        Destroy(root);
        IsPlaying = false;
    }

    private void OnDisable()
    {
        IsPlaying = false;
    }

    // ================= 연출 =================
    // 형판이 화면 아래 가운데에서 큐브 칸으로 날아가 박힘
    private IEnumerator FlyToSlot(int slot)
    {
        RectTransform fly = flying.rectTransform;
        RectTransform target = slots[slot].rectTransform;
        flying.sprite = Face(slot);
        flying.enabled = true;
        Vector3 from = cubeImage.rectTransform.position + new Vector3(0f, -260f, 0f) * cubeImage.canvas.scaleFactor;
        Vector3 to = target.position;
        Vector2 fromSize = new Vector2(300f, 300f), toSize = target.sizeDelta;
        GameAudio.Play("sfx_dream_shimmer", 0.6f);
        float t = 0f;
        while (t < flyTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / flyTime);
            float e = k * k * (3f - 2f * k);
            fly.position = Vector3.Lerp(from, to, e) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 120f * cubeImage.canvas.scaleFactor;
            fly.sizeDelta = Vector2.Lerp(fromSize, toSize, e);
            yield return null;
        }
        flying.enabled = false;
        slots[slot].sprite = Face(slot);
        slots[slot].color = Color.white;
        GameAudio.Play("sfx_rumble", 0.6f, 1.3f);
        yield return Pop(slots[slot].rectTransform);
        yield return Pop(cubeImage.rectTransform);
    }

    private static IEnumerator Pop(RectTransform rt)
    {
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.unscaledDeltaTime;
            float s = 1f + 0.12f * Mathf.Sin(Mathf.Clamp01(t / 0.5f) * Mathf.PI);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            page.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / fadeTime));
            yield return null;
        }
        page.alpha = to;
    }

    // E / Space / Enter / 클릭 (막 눌린 입력은 0.25초 무시)
    private static IEnumerator WaitPress()
    {
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            Keyboard k = Keyboard.current;
            Mouse m = Mouse.current;
            bool pressed = (k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
                           || (m != null && m.leftButton.wasPressedThisFrame);
            if (Cine.Skipping || (t > 0.25f && pressed)) yield break; // ESC 길게 = 건너뛰기
            yield return null;
        }
    }

    private Sprite Face(int i) => faceSprites != null && i >= 0 && i < faceSprites.Length ? faceSprites[i] : null;

    // ================= 화면 만들기 =================
    private void Build(int slot, bool[] filled)
    {
        if (font == null) font = TMP_Settings.defaultFontAsset;
        root = new GameObject("HallCubeInsert_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900; // 대사창·목표 띠보다 위
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform pageRt = Stretch(root.transform, "Page");
        pageRt.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.04f, 0.96f);
        page = pageRt.gameObject.AddComponent<CanvasGroup>();
        page.alpha = 0f;

        MakeText(pageRt, "중앙 홀", 64, new Vector2(0f, 430f), new Vector2(1200f, 90f), Gold);
        MakeText(pageRt, "여섯 신의 큐브", 32, new Vector2(0f, 365f), new Vector2(1200f, 50f), Paper);
        cubeImage = Img(pageRt, "Cube", cubeSprite, new Vector2(-430f, 20f), new Vector2(560f, 560f));

        slots = new Image[GodNames.Length];
        for (int i = 0; i < GodNames.Length; i++)
        {
            int col = i % 3, row = i / 3;
            Vector2 pos = new Vector2(250f + col * 230f, 150f - row * 290f);
            bool has = i != slot && filled != null && i < filled.Length && filled[i];
            Sprite s = has ? Face(i) : null;
            slots[i] = Img(pageRt, $"Slot_{GodNames[i]}", s, pos, new Vector2(190f, 190f));
            slots[i].color = s != null ? Color.white : SlotEmpty;
            MakeText(pageRt, GodNames[i], 28, pos + new Vector2(0f, -125f), new Vector2(220f, 40f), i == slot ? Gold : Paper);
        }
        status = MakeText(pageRt, "", 36, new Vector2(0f, -360f), new Vector2(1600f, 60f), Paper);
        hint = MakeText(pageRt, "", 28, new Vector2(0f, -430f), new Vector2(1200f, 50f), new Color(0.8f, 0.72f, 0.55f));
        flying = Img(pageRt, "FlyingPlate", null, Vector2.zero, new Vector2(300f, 300f));
        flying.enabled = false;
    }

    private static RectTransform Stretch(Transform parent, string objName)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static Image Img(Transform parent, string objName, Sprite sprite, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = sprite != null;
        img.raycastTarget = false;
        return img;
    }

    private TextMeshProUGUI MakeText(Transform parent, string text, float size, Vector2 pos, Vector2 box, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    // ================= 테스트 =================
    [ContextMenu("테스트: 바스테트 형판 끼우기 (라는 이미 끼움)")]
    private void TestBastet()
    {
        if (Application.isPlaying && !IsPlaying) StartCoroutine(Play(1, new[] { true, false, false, false, false, false }));
    }

#if UNITY_EDITOR
    public void EditorSetup(Sprite cube, Sprite[] faces, TMP_FontAsset f)
    {
        cubeSprite = cube; faceSprites = faces; font = f;
    }
#endif
}
