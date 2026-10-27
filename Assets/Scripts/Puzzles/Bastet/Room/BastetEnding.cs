using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// [추가] 바스테트의 방 엔딩 연출 (라의 방 DemoEnding과 같은 흐름)
// 메인 퍼즐(해와 달의 기억) 클리어 -> 고양이 형판 획득 팝업 -> 여신 대사가 끝나면 BastetSunMoonWalk가 Play() 호출
//  1) 바스테트 일러스트 컷: 여신 그림 + 벽화 + 이야기 문장
//  2) 형판 공개: 고양이 형판이 빛나며 나타남
//  3) 중앙 홀 복귀: 큐브 그림 + 여섯 면 칸 -> 형판이 바스테트 칸에 박힘
//  4) Space/클릭 -> 다음 신의 방 (Scene List 이름)
// 화면은 코드가 직접 만듦 (씬에는 이 컴포넌트 하나만 두면 됨). 그림·글꼴은 Tools/Bastet/★ 엔딩 연출 세팅 메뉴가 연결
public class BastetEnding : MonoBehaviour
{
    [Header("그림 (메뉴가 자동 연결)")]
    [Tooltip("일러스트 컷 가운데 여신 그림 (UI_Portrait_Bastet_Glow)")]
    [SerializeField] private Sprite illustration;
    [Tooltip("일러스트 컷 뒤 벽화 (Bastet_UI_Clue_B5_SunMoon)")]
    [SerializeField] private Sprite mural;
    [Tooltip("고양이 형판 (image/Cube/Bastet)")]
    [SerializeField] private Sprite plateSprite;
    [Tooltip("큐브 그림 (image/Cube/Cube)")]
    [SerializeField] private Sprite cubeSprite;
    [Tooltip("큐브 여섯 면 칸에 들어갈 형판 그림: 0 라, 1 바스테트, 2 토트, 3 호루스, 4 소베크, 5 아누비스")]
    [SerializeField] private Sprite[] faceSprites = new Sprite[6];
    [SerializeField] private TMP_FontAsset font;

    [Header("일러스트 컷 문장 (한 줄씩 나타남)")]
    [TextArea(1, 2)]
    [SerializeField] private string[] storyLines =
    {
        "태양의 배가 저승을 건너던 밤,",
        "혼돈의 뱀 아펩이 어둠 속에서 배를 노렸다.",
        "그때 고양이의 모습을 한 바스테트가 뱀을 물리쳤고",
        "다시 새벽이 열렸다."
    };
    [SerializeField] private string goddessLine = "\"그 형판을 가져가라. 큐브의 한 면이 될 것이다.\"";

    [Header("형판 / 큐브")]
    [SerializeField] private string plateName = "고양이 형판";
    [SerializeField] private string plateDesc = "큐브의 두 번째 면에 끼우는 바스테트의 형판";
    [Tooltip("여섯 면 칸에서 바스테트 자리 (0부터)")]
    [Range(0, 5)] [SerializeField] private int bastetSlot = 1;
    [Tooltip("팀 PlayerInventory의 바스테트 형판 번호 (0 라, 1 아누비스, 2 바스테트 ...)")]
    [SerializeField] private int playerInventoryFace = 2;
    [SerializeField] private string plateGivenFlag = "F_BASTET_PLATE_GIVEN";
    [SerializeField] private string endingDoneFlag = "F_BASTET_ENDING_DONE";

    [Header("중앙 홀 복귀 후 다음 방")]
    [Tooltip("Scene List에 있는 씬 이름. 비우면 Scene List 다음 번호 씬")]
    [SerializeField] private string nextSceneName = "Puzzle_Test_Anubis_Tutorial";
    [TextArea(1, 2)] [SerializeField] private string exitMessage = "다음 신의 방으로 향합니다...";

    [Header("시간 (초)")]
    [Range(0.2f, 3f)] [SerializeField] private float fadeTime = 0.9f;
    [Range(0.5f, 5f)] [SerializeField] private float lineTime = 2.2f;
    [Range(0.3f, 2f)] [SerializeField] private float plateFlyTime = 0.9f;

    // 여섯 신 이름 (칸 아래 글자)
    private static readonly string[] GodNames = { "라", "바스테트", "토트", "호루스", "소베크", "아누비스" };
    private static readonly string[] OtherPlateIds = { "PLATE_RA", "PLATE_BASTET", "PLATE_THOTH", "PLATE_HORUS", "PLATE_SOBEK", "PLATE_ANUBIS" };

    private static readonly Color Gold = new Color(0.91f, 0.70f, 0.23f);
    private static readonly Color Paper = new Color(0.96f, 0.93f, 0.85f);
    private static readonly Color Night = new Color(0.07f, 0.09f, 0.18f);
    private static readonly Color SlotEmpty = new Color(0.16f, 0.12f, 0.08f, 0.9f);

    private bool playing;
    private Canvas canvas;
    private CanvasGroup black, cut, platePage, hall;
    private Image portrait, plateBig, cubeImage, flyingPlate;
    private Image[] slots;
    private TextMeshProUGUI storyText, hallStatus, hintText;
    private RectTransform cutRoot;

    public bool IsPlaying => playing;

    // ================= 시작 =================
    public void Play()
    {
        if (playing) return;
        playing = true;
        PlayerLock.Lock(this);
        GiveFlags();
        StartCoroutine(Run());
    }

    private void OnDestroy() => PlayerLock.Unlock(this);

    private void GiveFlags()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && !gm.HasFlag(plateGivenFlag)) gm.SetFlag(plateGivenFlag);
        // 팀 큐브 시스템에도 등록 (없으면 건너뜀)
        PlayerInventory inv = FindFirstObjectByType<PlayerInventory>();
        if (inv != null) inv.AddPlate(playerInventoryFace);
    }

    private IEnumerator Run()
    {
        Build();
        GameAudio.Music(BastetSfx.MusicEnding, 2f);

        // 0) 화면이 어두워짐
        yield return Fade(black, 0f, 1f, fadeTime);

        // 1) 일러스트 컷
        cut.alpha = 1f;
        BastetSfx.Chime();
        yield return Fade(black, 1f, 0f, fadeTime);
        StartCoroutine(SlowZoom(cutRoot, 1f, 1.06f, lineTime * (storyLines.Length + 2)));
        for (int i = 0; i < storyLines.Length; i++)
        {
            yield return ShowLine(storyLines[i]);
        }
        portrait.color = Color.white;
        yield return ShowLine(goddessLine, Gold);
        yield return Fade(black, 0f, 1f, fadeTime);
        cut.alpha = 0f;

        // 2) 형판 공개
        platePage.alpha = 1f;
        yield return Fade(black, 1f, 0f, fadeTime * 0.7f);
        BastetSfx.RoundClear();
        yield return PopIn(plateBig.rectTransform, 0.6f);
        yield return WaitAdvance(2.5f);
        yield return Fade(black, 0f, 1f, fadeTime);
        platePage.alpha = 0f;

        // 3) 중앙 홀 복귀 + 큐브 공개
        hall.alpha = 1f;
        hallStatus.text = "중앙 홀로 돌아왔다. 큐브가 형판을 기다린다.";
        yield return Fade(black, 1f, 0f, fadeTime);
        yield return WaitAdvance(1.2f);
        yield return FlyPlateToSlot();
        int have = CountPlates();
        hallStatus.text = $"큐브가 응답한다! 바스테트의 면이 빛난다.  (형판 {have} / 6)";
        if (GameManager.Instance != null) GameManager.Instance.SetFlag(endingDoneFlag);
        Debug.Log($"[BastetEnding] 형판 장착 완료 ({have}/6)", this);

        // 4) 다음 방
        yield return new WaitForSecondsRealtime(1f);
        hintText.text = "Space / 클릭: 다음 신의 방으로";
        yield return WaitAdvance(float.MaxValue);
        GoNext();
    }

    private void GoNext()
    {
        PlayerLock.Unlock(this);
        if (!string.IsNullOrEmpty(nextSceneName) && Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            AnubisSceneFader.Begin(nextSceneName, exitMessage, font, 0.8f, 0.6f, 0.8f); // 팀 공용 어두워짐 연출
            return;
        }
        Debug.LogWarning($"[BastetEnding] '{nextSceneName}' 씬이 Scene List에 없음 -> 다음 번호 씬", this);
        SceneFlow.LoadByNameOrNext(nextSceneName);
    }

    // ================= 연출 도우미 =================
    private IEnumerator ShowLine(string line, Color? color = null)
    {
        storyText.text = line;
        storyText.color = color ?? Paper;
        yield return FadeText(storyText, 0f, 1f, 0.5f);
        yield return WaitAdvance(lineTime);
        yield return FadeText(storyText, 1f, 0f, 0.35f);
    }

    // 형판이 화면 가운데에서 바스테트 칸으로 날아가 박힘
    private IEnumerator FlyPlateToSlot()
    {
        RectTransform fly = flyingPlate.rectTransform;
        RectTransform target = slots[bastetSlot].rectTransform;
        flyingPlate.enabled = true;
        Vector3 from = cubeImage.rectTransform.position;
        Vector3 to = target.position;
        Vector2 fromSize = new Vector2(360, 360), toSize = target.sizeDelta;
        float t = 0f;
        while (t < plateFlyTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / plateFlyTime);
            float e = k * k * (3f - 2f * k);
            fly.position = Vector3.Lerp(from, to, e) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 120f;
            fly.sizeDelta = Vector2.Lerp(fromSize, toSize, e);
            yield return null;
        }
        flyingPlate.enabled = false;
        slots[bastetSlot].sprite = plateSprite;
        slots[bastetSlot].color = Color.white;
        BastetSfx.Mount();
        yield return Flash(slots[bastetSlot].rectTransform);
        yield return Flash(cubeImage.rectTransform);
    }

    private IEnumerator Flash(RectTransform rt)
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

    private IEnumerator PopIn(RectTransform rt, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            float s = Mathf.LerpUnclamped(0.6f, 1f, 1f + 1.6f * Mathf.Pow(k - 1f, 3) + 0.6f * Mathf.Pow(k - 1f, 2)); // 살짝 튕김
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private IEnumerator SlowZoom(RectTransform rt, float from, float to, float time)
    {
        float t = 0f;
        while (t < time && cut.alpha > 0f)
        {
            t += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(from, to, t / time);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }

    private static IEnumerator Fade(CanvasGroup g, float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / time));
            yield return null;
        }
        g.alpha = to;
    }

    private static IEnumerator FadeText(TextMeshProUGUI txt, float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            txt.alpha = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        txt.alpha = to;
    }

    // 시간이 지나거나 Space/Enter/클릭이면 넘어감 (막 누른 입력은 0.25초 무시)
    private static IEnumerator WaitAdvance(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            if (t > 0.25f && Pressed()) yield break;
            yield return null;
        }
    }

    private static bool Pressed()
    {
        Keyboard k = Keyboard.current;
        Mouse m = Mouse.current;
        return (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame))
               || (m != null && m.leftButton.wasPressedThisFrame);
    }

    private int CountPlates()
    {
        int n = 0;
        for (int i = 0; i < 6; i++) if (i == bastetSlot || HasPlate(i)) n++;
        return n;
    }

    private static bool HasPlate(int i)
    {
        if (ItemInventory.Instance != null && ItemInventory.Instance.Has(OtherPlateIds[i])) return true;
        return i == 0 && GameManager.Instance != null && GameManager.Instance.HasFlag("F_RA_PLATE_GIVEN"); // 라 형판은 플래그로도 확인
    }

    // ================= 화면 만들기 (처음 Play 때 한 번) =================
    private void Build()
    {
        if (font == null) font = TMP_Settings.defaultFontAsset;

        var root = new GameObject("BastetEnding_Canvas", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900; // 알림·대사창보다 위
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>(); // 뒤 UI 클릭 막기
        RectTransform r = (RectTransform)root.transform;

        // ---- 1) 일러스트 컷 ----
        cut = Page(r, "Cut", Night);
        cutRoot = Stretch(cut.transform, "Zoom");
        if (mural != null) { Image m = Img(cutRoot, "Mural", mural, Vector2.zero, new Vector2(1920, 800)); m.color = new Color(1f, 1f, 1f, 0.22f); }
        portrait = Img(cutRoot, "Portrait", illustration, new Vector2(0, 90), new Vector2(560, 560));
        portrait.color = new Color(0.85f, 0.85f, 0.9f);
        storyText = MakeText(cut.transform, "", 46, new Vector2(0, -330), new Vector2(1600, 140), Paper);
        storyText.alpha = 0f;

        // ---- 2) 형판 공개 ----
        platePage = Page(r, "Plate", new Color(0.09f, 0.07f, 0.05f));
        Image halo = Img(platePage.transform, "Halo", null, new Vector2(0, 60), new Vector2(620, 620));
        halo.color = new Color(Gold.r, Gold.g, Gold.b, 0.18f);
        plateBig = Img(platePage.transform, "PlateBig", plateSprite, new Vector2(0, 60), new Vector2(520, 520));
        MakeText(platePage.transform, plateName, 60, new Vector2(0, -290), new Vector2(1200, 80), Gold);
        MakeText(platePage.transform, plateDesc, 34, new Vector2(0, -360), new Vector2(1400, 60), Paper);

        // ---- 3) 중앙 홀 ----
        hall = Page(r, "Hall", new Color(0.08f, 0.06f, 0.04f));
        MakeText(hall.transform, "중앙 홀", 64, new Vector2(0, 430), new Vector2(1200, 90), Gold);
        MakeText(hall.transform, "여섯 신의 큐브", 32, new Vector2(0, 365), new Vector2(1200, 50), Paper);
        cubeImage = Img(hall.transform, "Cube", cubeSprite, new Vector2(-430, 20), new Vector2(560, 560));
        slots = new Image[6];
        for (int i = 0; i < 6; i++)
        {
            int col = i % 3, row = i / 3;
            Vector2 pos = new Vector2(250 + col * 230, 150 - row * 290);
            bool has = i == bastetSlot ? false : HasPlate(i);
            Sprite s = has && faceSprites != null && i < faceSprites.Length ? faceSprites[i] : null;
            slots[i] = Img(hall.transform, $"Slot_{GodNames[i]}", s, pos, new Vector2(190, 190));
            slots[i].color = s != null ? Color.white : SlotEmpty;
            MakeText(hall.transform, GodNames[i], 28, pos + new Vector2(0, -125), new Vector2(220, 40), i == bastetSlot ? Gold : Paper);
        }
        hallStatus = MakeText(hall.transform, "", 36, new Vector2(0, -360), new Vector2(1600, 60), Paper);
        hintText = MakeText(hall.transform, "", 28, new Vector2(0, -430), new Vector2(1200, 50), new Color(0.8f, 0.72f, 0.55f));
        flyingPlate = Img(r, "FlyingPlate", plateSprite, Vector2.zero, new Vector2(360, 360));
        flyingPlate.enabled = false;

        // ---- 검은 막 (맨 위) ----
        black = Page(r, "Black", Color.black);
        black.alpha = 0f;
        black.blocksRaycasts = true;
    }

    private static CanvasGroup Page(Transform parent, string name, Color bg)
    {
        RectTransform rt = Stretch(parent, name);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = bg;
        CanvasGroup g = rt.gameObject.AddComponent<CanvasGroup>();
        g.alpha = 0f;
        return g;
    }

    private static RectTransform Stretch(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static Image Img(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
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
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        return t;
    }

    // ================= 테스트 =================
    [ContextMenu("테스트: 엔딩 바로 보기")]
    private void TestPlay()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[BastetEnding] Play 중에 눌러 주세요", this); return; }
        Play();
    }

#if UNITY_EDITOR
    public void EditorSetup(Sprite illust, Sprite muralSprite, Sprite plate, Sprite cube, Sprite[] faces, TMP_FontAsset f, string next)
    {
        illustration = illust; mural = muralSprite; plateSprite = plate; cubeSprite = cube; faceSprites = faces; font = f;
        if (!string.IsNullOrEmpty(next)) nextSceneName = next;
    }
#endif
}
