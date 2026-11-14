using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// [추가] 바스테트 벽화 보기 화면 (호루스 MuralStage와 같은 그림·대본을 쓰되, 바스테트 전용으로 조작을 고침)
// 순서: (1) 벽화 그림을 먼저 크게 보여 줌 (제목 + 벽에 새겨진 글)  -> E
//       (2) 이야기 연출 (MuralShot 대본: 올려다보기 -> 그림 속 인물이 움직임)
// 조작: E / Space / Enter / 클릭 = 다음 줄. 마지막 줄에서 누르면 남은 움직임을 빨리 감고 바로 다음 장면 (기다리지 않음)
//       ESC 길게 = 전체 건너뛰기 (Cine 공용)
// 씬 오브젝트라 씬이 바뀌면 같이 사라짐 -> 다른 씬에 갔다 와도 '재생 중' 상태가 남지 않음
// 입력: New Input System (Keyboard.current / Mouse.current), 글자: TextMeshPro
// 붙이는 곳: Bastet_Story/BastetMuralViewer (메뉴가 만듦. 없으면 처음 쓸 때 자동으로 만듦)
public class BastetMuralViewer : MonoBehaviour
{
    private const string ArtPath = "Horus/Mural/MU_";
    private const float ArtScale = 1.5f;     // 벽화 1280x720 -> 화면 1920x1080
    private const float BarHeight = 140f;

    [Header("글꼴 (비우면 TMP 기본 글꼴)")]
    [SerializeField] private TMP_FontAsset font;

    [Header("(1) 그림 먼저 보기")]
    [Tooltip("화면 대비 그림 크기")]
    [Range(0.4f, 1f)] [SerializeField] private float pictureScale = 0.7f;
    [SerializeField] private Color frameColor = new Color(0.86f, 0.66f, 0.26f);
    [SerializeField] private Color nameColor = new Color(0.93f, 0.74f, 0.32f);

    [Header("시간 (초)")]
    [Range(0.1f, 1f)] [SerializeField] private float fadeSeconds = 0.35f;
    [Tooltip("한 줄을 최소 이만큼은 보여 줌 (연타로 다 넘어가지 않게)")]
    [Range(0f, 1f)] [SerializeField] private float minLineSeconds = 0.3f;
    [Tooltip("마지막 줄에서 E를 누르면 남은 움직임을 이 시간 안에 마무리")]
    [Range(0.1f, 1.5f)] [SerializeField] private float fastForwardSeconds = 0.4f;
    [SerializeField] private int sortingOrder = 300;

    private static BastetMuralViewer instance;
    public static bool IsPlaying => instance != null && instance.playing;

    // UI
    private GameObject canvasGo;
    private Image black;
    private CanvasGroup pictureGroup;
    private Image pictureImg;
    private TextMeshProUGUI picTitle, picText, picHint;
    private GameObject shotGroup;
    private RectTransform artRoot, dustRoot;
    private TextMeshProUGUI nameText, lineText, hintText;

    private readonly List<(MuralLayer layer, Image img)> live = new List<(MuralLayer, Image)>();
    private readonly List<(Image img, Vector2 home, float speed, float phase)> motes = new List<(Image, Vector2, float, float)>();
    private bool playing;
    private int ignoreUntilFrame;
    private static Sprite white;

    public static BastetMuralViewer Get()
    {
        if (instance == null) instance = FindFirstObjectByType<BastetMuralViewer>();
        if (instance == null) instance = new GameObject("BastetMuralViewer").AddComponent<BastetMuralViewer>();
        return instance;
    }

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void OnDisable()
    {
        playing = false;
    }

    // ================= 재생 =================
    // title: 그림 위 제목, wallText: 그림 아래 글, pictureArt: 그림 이름(MU_{pictureArt}_bg), shots: 이야기 연출 (비우면 그림만)
    public IEnumerator Play(string title, string wallText, string pictureArt, MuralShot[] shots)
    {
        if (playing) yield break;
        if (!TryBuild()) yield break;
        playing = true;
        ignoreUntilFrame = Time.frameCount + 1; // 벽화를 연 그 E로 바로 넘어가지 않게
        bool hasStory = shots != null && shots.Length > 0;

        // (1) 그림 먼저
        ShowPicture(title, wallText, pictureArt, hasStory);
        canvasGo.SetActive(true);
        yield return FadeGroup(pictureGroup, 0f, 1f);
        float t = 0f;
        while (!Cine.Skipping && (t < minLineSeconds || !Pressed()))
        {
            t += Time.deltaTime;
            yield return null;
        }
        Debug.Log($"[BastetMuralViewer] 그림 '{title}' 봄 -> {(hasStory ? "이야기 연출" : "닫기")}", this);

        // (2) 이야기 연출
        if (hasStory && !Cine.Skipping)
        {
            yield return FadeBlack(0f, 1f);
            pictureGroup.gameObject.SetActive(false);
            shotGroup.SetActive(true);
            foreach (MuralShot shot in shots)
            {
                if (Cine.Skipping) break;
                if (!BuildShot(shot)) continue;
                yield return FadeBlack(1f, 0f);
                yield return RunShot(shot);
                yield return FadeBlack(0f, 1f);
            }
            ClearShot();
            shotGroup.SetActive(false);
            yield return FadeBlack(1f, 0f);
        }
        else
        {
            yield return FadeGroup(pictureGroup, 1f, 0f);
        }

        canvasGo.SetActive(false);
        playing = false;
    }

    // 다음 줄 입력 (New Input System)
    private bool Pressed()
    {
        if (Time.frameCount <= ignoreUntilFrame) return false;
        Keyboard k = Keyboard.current;
        Mouse m = Mouse.current;
        bool key = k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame);
        bool click = m != null && m.leftButton.wasPressedThisFrame;
        if (key || click) ignoreUntilFrame = Time.frameCount; // 같은 프레임에 두 번 넘어가지 않게
        return key || click;
    }

    // 대사와 움직임을 같이 돌림. 마지막 줄을 넘기면 남은 움직임을 빨리 감고 끝
    private IEnumerator RunShot(MuralShot shot)
    {
        float time = 0f, u = 0f, lineTimer = 0f;
        int line = 0;
        bool done = shot.lines.Count == 0, fastForward = false;
        SetLine(shot.lines.Count > 0 ? shot.lines[0] : null);

        while (!Cine.Skipping)
        {
            float dt = Time.deltaTime;
            time += dt;
            lineTimer += dt;
            u = Mathf.Max(u, Mathf.Clamp01(time / Mathf.Max(0.1f, shot.duration)));
            if (fastForward) u = Mathf.MoveTowards(u, 1f, dt / fastForwardSeconds);
            Apply(shot, u, time);

            if (!done && lineTimer >= minLineSeconds && Pressed())
            {
                lineTimer = 0f;
                line++;
                if (line < shot.lines.Count) SetLine(shot.lines[line]);
                else { done = true; SetLine(null); }
            }
            if (done)
            {
                if (u >= 1f) break;
                fastForward = true;
            }
            yield return null;
        }
        SetLine(null);
    }

    // ================= 그림 먼저 보기 =================
    private void ShowPicture(string title, string wallText, string art, bool hasStory)
    {
        Sprite s = Resources.Load<Sprite>($"{ArtPath}{art}_bg");
        if (s == null) Debug.LogWarning($"[BastetMuralViewer] 그림 없음: Resources/{ArtPath}{art}_bg (그림 파일 임포트 확인)", this);
        pictureImg.sprite = s;
        pictureImg.color = s != null ? Color.white : new Color(0.85f, 0.72f, 0.5f);
        picTitle.text = title;
        picText.text = wallText;
        picHint.text = hasStory ? "E  이야기 보기 ▶" : "E  닫기";
        pictureGroup.alpha = 0f;
        pictureGroup.gameObject.SetActive(true);
        shotGroup.SetActive(false);
        SetBlack(0f);
    }

    // ================= 이야기 장면 =================
    private bool BuildShot(MuralShot shot)
    {
        try
        {
            ClearShot();
            Sprite bg = Resources.Load<Sprite>($"{ArtPath}{shot.art}_bg");
            if (bg == null) Debug.LogWarning($"[BastetMuralViewer] 배경 그림 없음: {ArtPath}{shot.art}_bg", this);
            MakeImage(artRoot, "BG", Vector2.zero, new Vector2(1280f, 720f) * ArtScale, bg, bg != null ? Color.white : new Color(0.85f, 0.72f, 0.5f));
            foreach (MuralLayer l in shot.layers)
            {
                Sprite s = Resources.Load<Sprite>($"{ArtPath}{shot.art}_{l.name}");
                if (s == null) { Debug.LogWarning($"[BastetMuralViewer] 조각 그림 없음: {ArtPath}{shot.art}_{l.name}", this); continue; }
                live.Add((l, MakeImage(artRoot, l.name, ToUi(l.pos), s.rect.size * ArtScale, s, Color.white)));
            }
            BuildDust(shot.dust);
            Apply(shot, 0f, 0f);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[BastetMuralViewer] 장면 '{shot.id}' 준비 오류: {e}", this);
            return false;
        }
    }

    private void Apply(MuralShot shot, float u, float time)
    {
        float e = Mathf.SmoothStep(0f, 1f, u);
        float zoom = Mathf.Lerp(shot.zoomFrom, shot.zoomTo, e);
        Vector2 pan = Vector2.Lerp(shot.panFrom, shot.panTo, e);
        artRoot.localScale = Vector3.one * zoom;
        artRoot.anchoredPosition = new Vector2(-pan.x, pan.y) * ArtScale * zoom;

        foreach (var pair in live)
        {
            MuralLayer l = pair.layer;
            MuralPose p = MuralShot.Evaluate(l, u);
            float bob = l.bobAmp > 0f ? Mathf.Sin(time * l.bobSpeed + l.pos.x * 0.01f) * l.bobAmp : 0f;
            pair.img.rectTransform.anchoredPosition = ToUi(l.pos + p.offset + new Vector2(0f, bob));
            pair.img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, p.rot);
            float pulse = l.pulseAmp > 0f ? 1f - l.pulseAmp * (0.5f + 0.5f * Mathf.Sin(time * l.pulseSpeed)) : 1f;
            pair.img.color = new Color(1f, 1f, 1f, p.alpha * pulse);
        }

        foreach (var m in motes)
        {
            float y = Mathf.Repeat(m.home.y + 450f + time * m.speed, 900f) - 450f;
            float x = m.home.x + Mathf.Sin(time * 0.6f + m.phase) * 24f;
            m.img.rectTransform.anchoredPosition = new Vector2(x, y);
            m.img.color = new Color(1f, 0.88f, 0.65f, 0.25f + 0.25f * Mathf.Sin(time * 1.7f + m.phase));
        }
    }

    // 벽화 픽셀(왼쪽 위 0,0, 아래로 +y) -> 화면 UI(가운데 0,0, 위로 +y)
    private static Vector2 ToUi(Vector2 art) => new Vector2(art.x - 640f, 360f - art.y) * ArtScale;

    private void BuildDust(int count)
    {
        var rng = new System.Random(count * 31 + 7);
        for (int i = 0; i < count; i++)
        {
            Vector2 home = new Vector2((float)rng.NextDouble() * 1920f - 960f, (float)rng.NextDouble() * 900f - 450f);
            float size = 3f + (float)rng.NextDouble() * 5f;
            Image img = MakeImage(dustRoot, "Mote", home, new Vector2(size, size), null, new Color(1f, 0.88f, 0.65f, 0f));
            motes.Add((img, home, 12f + (float)rng.NextDouble() * 26f, (float)rng.NextDouble() * 10f));
        }
    }

    private void ClearShot()
    {
        live.Clear();
        motes.Clear();
        if (artRoot != null) for (int i = artRoot.childCount - 1; i >= 0; i--) Destroy(artRoot.GetChild(i).gameObject);
        if (dustRoot != null) for (int i = dustRoot.childCount - 1; i >= 0; i--) Destroy(dustRoot.GetChild(i).gameObject);
    }

    private void SetLine(DialogueLine l)
    {
        nameText.text = l != null ? l.speaker : "";
        lineText.text = l != null ? l.text : "";
        hintText.enabled = l != null;
    }

    // ================= 페이드 =================
    private IEnumerator FadeBlack(float from, float to)
    {
        float t = 0f;
        while (t < fadeSeconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            SetBlack(Mathf.Lerp(from, to, t / fadeSeconds));
            yield return null;
        }
        SetBlack(to);
    }

    private IEnumerator FadeGroup(CanvasGroup g, float from, float to)
    {
        float t = 0f;
        while (t < fadeSeconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            g.alpha = Mathf.Lerp(from, to, t / fadeSeconds);
            yield return null;
        }
        g.alpha = to;
    }

    private void SetBlack(float a)
    {
        black.color = new Color(0f, 0f, 0f, a);
    }

    // ================= 화면 만들기 (처음 한 번) =================
    private bool TryBuild()
    {
        try
        {
            if (canvasGo == null) Build();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[BastetMuralViewer] 화면 만들기 오류: {e}", this);
            return false;
        }
    }

    private void Build()
    {
        if (font == null || !font.HasCharacter('벽', true)) font = FindKoreanFont();

        canvasGo = new GameObject("BastetMuralCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform root = (RectTransform)canvasGo.transform;

        // 이야기 장면 (벽화 + 먼지 + 위아래 검은 띠 + 자막)
        RectTransform shot = Stretch(root, "Shot");
        shotGroup = shot.gameObject;
        MakeImage(shot, "Back", Vector2.zero, new Vector2(6000f, 4000f), null, Color.black);
        artRoot = MakeRect(shot, "Art", Vector2.zero, new Vector2(1920f, 1080f));
        dustRoot = MakeRect(shot, "Dust", Vector2.zero, new Vector2(1920f, 1080f));
        Bar(shot, true);
        RectTransform bottom = Bar(shot, false);
        nameText = MakeText(bottom, "Name", new Vector2(0f, 38f), new Vector2(1400f, 44f), 30, nameColor, TextAlignmentOptions.Center);
        lineText = MakeText(bottom, "Line", new Vector2(0f, -10f), new Vector2(1500f, 80f), 34, new Color(0.96f, 0.94f, 0.9f), TextAlignmentOptions.Center);
        hintText = MakeText(bottom, "Hint", new Vector2(860f, -52f), new Vector2(180f, 30f), 22, new Color(1f, 1f, 1f, 0.55f), TextAlignmentOptions.Right);
        hintText.text = "E ▶";

        // 그림 먼저 보기 (어두운 바탕 + 금 테두리 그림 + 제목 + 글)
        RectTransform pic = Stretch(root, "Picture");
        pictureGroup = pic.gameObject.AddComponent<CanvasGroup>();
        Image dim = MakeImage(pic, "Dim", Vector2.zero, Vector2.zero, null, new Color(0f, 0f, 0f, 0.85f));
        StretchFull(dim.rectTransform);
        Vector2 size = new Vector2(1920f, 1080f) * pictureScale;
        MakeImage(pic, "Frame", new Vector2(0f, 20f), size + new Vector2(28f, 28f), null, frameColor);
        MakeImage(pic, "FrameInner", new Vector2(0f, 20f), size + new Vector2(12f, 12f), null, new Color(0.25f, 0.16f, 0.08f));
        pictureImg = MakeImage(pic, "Mural", new Vector2(0f, 20f), size, null, Color.white);
        picTitle = MakeText(pic, "Title", new Vector2(0f, size.y * 0.5f + 70f), new Vector2(1600f, 60f), 44, nameColor, TextAlignmentOptions.Center);
        picText = MakeText(pic, "Text", new Vector2(0f, -size.y * 0.5f - 40f), new Vector2(1600f, 60f), 32, new Color(0.96f, 0.94f, 0.9f), TextAlignmentOptions.Center);
        picHint = MakeText(pic, "Hint", new Vector2(0f, -size.y * 0.5f - 105f), new Vector2(600f, 40f), 26, new Color(1f, 1f, 1f, 0.6f), TextAlignmentOptions.Center);

        // 암전 (맨 위)
        black = MakeImage(root, "Fade", Vector2.zero, Vector2.zero, null, Color.black);
        StretchFull(black.rectTransform);
        SetBlack(0f);
        canvasGo.SetActive(false);
    }

    // 글꼴 칸이 비었을 때: 이미 불러온 글꼴 중 한글이 되는 것 (Galmuri 우선). 기본 LiberationSans는 한글이 네모로 나옴
    private static TMP_FontAsset FindKoreanFont()
    {
        TMP_FontAsset pick = null;
        foreach (TMP_FontAsset f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (f == null || !f.HasCharacter('벽', true)) continue;
            if (f.name.Contains("Galmuri")) return f;
            if (pick == null) pick = f;
        }
        if (pick == null) Debug.LogWarning("[BastetMuralViewer] 한글 글꼴을 못 찾음 -> 메뉴 '★ 스토리·검은 고양이 세팅'을 다시 실행해 글꼴 연결");
        return pick != null ? pick : TMP_Settings.defaultFontAsset;
    }

    // ================= UI 도우미 =================
    private static Sprite White()
    {
        if (white != null) return white;
        var tex = Texture2D.whiteTexture;
        white = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        return white;
    }

    private static RectTransform MakeRect(Transform parent, string objName, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static RectTransform Stretch(Transform parent, string objName)
    {
        RectTransform rt = MakeRect(parent, objName, Vector2.zero, Vector2.zero);
        StretchFull(rt);
        return rt;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Image MakeImage(Transform parent, string objName, Vector2 pos, Vector2 size, Sprite sprite, Color color)
    {
        RectTransform rt = MakeRect(parent, objName, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite != null ? sprite : White();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private TextMeshProUGUI MakeText(Transform parent, string objName, Vector2 pos, Vector2 size, float fontSize, Color color, TextAlignmentOptions align)
    {
        RectTransform rt = MakeRect(parent, objName, pos, size);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }

    // 화면 위/아래 끝에 붙는 검은 띠 (해상도가 달라도 끝에 붙게 앵커 사용)
    private static RectTransform Bar(Transform parent, bool top)
    {
        RectTransform rt = MakeRect(parent, top ? "BarTop" : "BarBottom", Vector2.zero, Vector2.zero);
        rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rt.sizeDelta = new Vector2(0f, BarHeight);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = White();
        img.color = Color.black;
        img.raycastTarget = false;
        // 자막 기준점을 띠 가운데로
        return MakeRect(rt, "Center", Vector2.zero, new Vector2(1920f, BarHeight));
    }

#if UNITY_EDITOR
    public void EditorSetup(TMP_FontAsset f) { font = f; }
#endif
}
