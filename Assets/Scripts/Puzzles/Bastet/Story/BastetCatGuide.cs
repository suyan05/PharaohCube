using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// [추가] 바스테트의 검은 고양이 = 길잡이 (스토리 기획서: 신의 동물이 길을 안내)
// - 호루스의 매(HorusFalconGuide)와 같은 역할, 고양이답게 다르게:
//   매는 날아가서 앉지만, 고양이는 "어둠 속으로 먼저 들어가" 사라졌다가 목표 앞에서 두 눈부터 나타남
//   (가까우면 그냥 걸어감. 멀면 사라졌다 나타나므로 벽을 뚫고 걷는 길찾기가 필요 없음)
// - 몸은 거의 검은색(조명에 윤곽만), 두 눈은 Unlit + 작은 빛 -> 어두운 방에서도 눈만 빛남
// - 지금 목표 = steps에서 doneFlag가 아직 안 켜진 첫 단계 (봉인석 / 비석 / 출구 앞에 앉음)
// - 플레이어가 가까이 오면 반투명 (목표를 가리지 않게), 화면 밖이면 가장자리에 두 눈 아이콘
// - 방 스토리 연출(BastetStoryDirector)이 끝나면 등장. 연출 중엔 Directed = true로 연출이 직접 움직임
// [임시] 그림은 코드로 그린 검은 고양이. 아트가 오면 Sit Sprite / Eyes Sprite 칸만 바꾸면 됨
public class BastetCatGuide : MonoBehaviour
{
    [Serializable]
    public class Step
    {
        [Tooltip("로그용 이름")]
        public string label = "";
        [Tooltip("이 플래그가 켜지면 다음 단계로 (비우면 끝나지 않는 마지막 단계)")]
        public string doneFlag = "";
        [Tooltip("고양이가 가서 앉을 곳 (봉인석, 비석, 문)")]
        public Transform target;
        [Tooltip("목표 기준 앉을 자리 (방 안쪽으로 살짝)")]
        public Vector2 sitOffset = new Vector2(0f, -1f);
    }

    [Header("길 안내 순서 (위에서부터)")]
    [SerializeField] private List<Step> steps = new List<Step>();

    [Header("등장")]
    [Tooltip("이 플래그가 켜진 뒤부터 보임 (방 스토리 연출)")]
    [SerializeField] private string appearFlag = BastetStoryIds.FlagRoomStory;

    [Header("그림 (비우면 코드로 그린 임시 검은 고양이)")]
    [SerializeField] private Sprite sitSprite;
    [Tooltip("걷기 프레임 (비우면 앉은 그림을 통통 튀게)")]
    [SerializeField] private Sprite[] walkSprites = new Sprite[0];
    [SerializeField] private Sprite eyesSprite;
    [SerializeField] private Sprite edgeSprite;
    [Range(0.5f, 3f)] [SerializeField] private float size = 1.1f;

    [Header("눈빛")]
    [SerializeField] private Color eyeColor = new Color(0.85f, 1f, 0.5f);
    [Range(0f, 2f)] [SerializeField] private float eyeLightIntensity = 0.55f;
    [Range(0.3f, 3f)] [SerializeField] private float eyeLightRadius = 1.3f;

    [Header("움직임")]
    [Range(1f, 8f)] [SerializeField] private float walkSpeed = 3.2f;
    [Tooltip("이보다 멀면 어둠 속으로 사라졌다가 목표 앞에서 눈부터 나타남")]
    [SerializeField] private float vanishDistance = 4.5f;
    [Tooltip("플레이어가 이만큼 가까이 오면 반투명")]
    [SerializeField] private float reachRadius = 2f;
    [Tooltip("따라오기(메인 클리어 후): 플레이어와의 거리")]
    [SerializeField] private float followDistance = 1.3f;

    [Header("화면 밖 표시")]
    [SerializeField] private bool showEdgeIcon = true;

    // 연출이 직접 움직이는 중 (Update가 목표로 데려가지 않음)
    public bool Directed { get; set; }
    public bool Moving => moving;
    public Vector3 Position => root != null ? root.position : transform.position;

    private Transform root, player;
    private SpriteRenderer body, eyes;
    private Light2D eyeLight;
    private Image edgeIcon;
    private Vector3 baseScale = Vector3.one;
    private Step perched;
    private bool shown, moving, following;
    private float bodyVis = 1f, eyeVis = 1f, reachFade = 1f;
    private float blinkTimer = 3f, walkTimer;
    private int walkFrame;

    // ================= 초기화 =================
    private void Start()
    {
        try
        {
            if (sitSprite == null) sitSprite = BastetPixelArt.CatSit;
            if (eyesSprite == null) eyesSprite = BastetPixelArt.CatEyes;
            if (edgeSprite == null) edgeSprite = BastetPixelArt.EyesIcon;
            Transform p = CineActor.Player();
            player = p;
            BuildCat(p);
            BuildEdge();
            following = BastetStoryIds.Has(BastetStoryIds.FlagCatFollow);
            SetShown(false);
            // 다시 들어왔을 때: 이미 등장한 고양이는 바로 목표 앞에
            if (BastetStoryIds.Has(appearFlag)) { SetShown(true); SnapToCurrent(); }
            Debug.Log($"[검은 고양이] 준비 완료 (단계 {steps.Count}개, 등장 {(shown ? "O" : "X")})", this);
        }
        catch (Exception e)
        {
            Debug.LogError($"[검은 고양이] 준비 오류: {e}", this);
        }
    }

    private void BuildCat(Transform p)
    {
        root = new GameObject("BlackCat").transform;
        root.SetParent(transform, false);

        // 플레이어와 같은 정렬 레이어 (Y축 정렬로 앞뒤가 자연스럽게)
        SpriteRenderer playerSr = p != null ? p.GetComponentInChildren<SpriteRenderer>() : null;
        string layer = playerSr != null ? playerSr.sortingLayerName : "Objects";
        int order = playerSr != null ? playerSr.sortingOrder : 0;

        body = new GameObject("Body").AddComponent<SpriteRenderer>();
        body.transform.SetParent(root, false);
        body.sprite = sitSprite;
        body.sortingLayerName = layer;
        body.sortingOrder = order;
        baseScale = Vector3.one * size;
        body.transform.localScale = baseScale;

        eyes = new GameObject("Eyes").AddComponent<SpriteRenderer>();
        eyes.transform.SetParent(body.transform, false);
        eyes.sprite = eyesSprite;
        eyes.color = eyeColor;
        eyes.sortingLayerName = layer;
        eyes.sortingOrder = order + 1;
        if (RaDarkness.Unlit != null) eyes.sharedMaterial = RaDarkness.Unlit; // 어둠 속에서도 눈은 보이게

        // 눈 높이 (그림 위쪽 60% 근처)에 작은 빛
        float eyeY = sitSprite != null ? sitSprite.bounds.size.y * 0.6f * size : 0.6f;
        eyeLight = RaGlowFx.PointLight(root, new Vector3(0f, eyeY, 0f), eyeColor, eyeLightRadius, eyeLightIntensity);
    }

    private void BuildEdge()
    {
        if (!showEdgeIcon) return;
        var canvasGo = new GameObject("CatEdgeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40; // 퍼즐 패널(50)보다 아래
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var iconGo = new GameObject("CatEyes", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(canvasGo.transform, false);
        edgeIcon = iconGo.GetComponent<Image>();
        edgeIcon.sprite = edgeSprite;
        edgeIcon.preserveAspect = true;
        edgeIcon.raycastTarget = false;
        edgeIcon.rectTransform.sizeDelta = new Vector2(96f, 32f);
        edgeIcon.enabled = false;
    }

    private void SetShown(bool on)
    {
        shown = on;
        if (root != null) root.gameObject.SetActive(on);
        if (!on && edgeIcon != null) edgeIcon.enabled = false;
    }

    // ================= 매 프레임 =================
    private void Update()
    {
        if (root == null) return;
        if (!shown)
        {
            if (!Directed && BastetStoryIds.Has(appearFlag)) { SetShown(true); bodyVis = 0f; eyeVis = 0f; StartCoroutine(MoveToStep(CurrentStep())); }
            return;
        }

        if (!Directed && !moving && !Cine.Running)
        {
            if (following) Follow();
            else
            {
                Step s = CurrentStep();
                if (s != null && s != perched && s.target != null) StartCoroutine(MoveToStep(s));
            }
        }

        if (!moving) Idle();
        Blink();
        UpdateReach();
        ApplyColors();
        UpdateEdge();
    }

    // 끝나지 않은 첫 단계
    private Step CurrentStep()
    {
        for (int i = 0; i < steps.Count; i++)
        {
            Step s = steps[i];
            if (s == null) continue;
            if (string.IsNullOrEmpty(s.doneFlag) || !BastetStoryIds.Has(s.doneFlag)) return s;
        }
        return steps.Count > 0 ? steps[steps.Count - 1] : null;
    }

    private static Vector3 SitPoint(Step s) => s.target.position + (Vector3)s.sitOffset;

    // 앉아서 숨 쉬기 (꼬리 대신 몸이 살짝 부풀었다 줄었다)
    private void Idle()
    {
        float breathe = Mathf.Sin(Time.time * 2.4f) * 0.025f;
        body.transform.localScale = new Vector3(baseScale.x * (1f - breathe * 0.5f), baseScale.y * (1f + breathe), 1f);
        body.transform.localPosition = Vector3.zero;
        if (body.sprite != sitSprite) body.sprite = sitSprite;
    }

    // 가끔 눈을 깜빡
    private void Blink()
    {
        blinkTimer -= Time.deltaTime;
        if (blinkTimer < 0f) blinkTimer = UnityEngine.Random.Range(2.5f, 5.5f);
        eyes.enabled = blinkTimer > 0.12f;
    }

    // 플레이어가 도착하면 반투명 (목표 그림과 겹치지 않게)
    private void UpdateReach()
    {
        bool near = !moving && !Directed && player != null && Vector2.Distance(player.position, root.position) <= reachRadius;
        reachFade = Mathf.MoveTowards(reachFade, near && !following ? 0.4f : 1f, Time.deltaTime * 2.5f);
    }

    private void ApplyColors()
    {
        float b = bodyVis * reachFade;
        body.color = new Color(1f, 1f, 1f, b);
        eyes.color = new Color(eyeColor.r, eyeColor.g, eyeColor.b, eyeVis * Mathf.Max(reachFade, 0.6f));
        if (eyeLight != null) eyeLight.intensity = eyeLightIntensity * eyeVis * (eyes.enabled ? 1f : 0.3f);
    }

    // ================= 이동 =================
    private IEnumerator MoveToStep(Step s)
    {
        if (s == null || s.target == null) yield break;
        perched = s;
        Debug.Log($"[검은 고양이] 다음 목표: {s.label} ({s.target.name})", this);
        yield return MoveTo(SitPoint(s));
    }

    private IEnumerator MoveTo(Vector3 to)
    {
        moving = true;
        float dist = Vector2.Distance(root.position, to);
        if (bodyVis < 0.05f || dist > vanishDistance) yield return VanishTo(to);
        else yield return WalkTo(to);
        moving = false;
    }

    // 가까우면 걸어감 (통통 튀며)
    public IEnumerator WalkTo(Vector3 to)
    {
        bool wasMoving = moving;
        moving = true;
        bodyVis = Mathf.Max(bodyVis, 0.01f);
        while (Vector2.Distance(root.position, to) > 0.04f && !Cine.Skipping)
        {
            Vector3 dir = to - root.position;
            root.position = Vector3.MoveTowards(root.position, to, walkSpeed * Time.deltaTime);
            body.flipX = dir.x < 0f;
            Trot();
            bodyVis = Mathf.MoveTowards(bodyVis, 1f, Time.deltaTime * 2f);
            eyeVis = Mathf.MoveTowards(eyeVis, 1f, Time.deltaTime * 2f);
            yield return null;
        }
        root.position = to;
        body.transform.localPosition = Vector3.zero;
        body.sprite = sitSprite;
        bodyVis = 1f; eyeVis = 1f;
        moving = wasMoving;
    }

    private void Trot()
    {
        walkTimer += Time.deltaTime;
        body.transform.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(walkTimer * 14f)) * 0.07f, 0f);
        if (walkSprites != null && walkSprites.Length > 0 && walkTimer > 0.1f * (walkFrame + 1))
        {
            walkFrame = (walkFrame + 1) % walkSprites.Length;
            body.sprite = walkSprites[walkFrame];
        }
    }

    // 멀면: 어둠 속으로 걸어 들어가 사라짐 -> 목표 앞 어둠에서 두 눈이 먼저 깜빡 -> 몸이 드러남
    private IEnumerator VanishTo(Vector3 to)
    {
        // 1) 목표 쪽으로 조금 걸으며 몸부터 사라짐 (눈은 남음)
        if (bodyVis > 0.05f)
        {
            Vector3 dir = (to - root.position).normalized;
            Vector3 fadeTo = root.position + dir * 1.2f;
            float t = 0f;
            while (t < 0.6f && !Cine.Skipping)
            {
                t += Time.deltaTime;
                root.position = Vector3.MoveTowards(root.position, fadeTo, walkSpeed * Time.deltaTime);
                body.flipX = dir.x < 0f;
                Trot();
                bodyVis = 1f - t / 0.6f;
                ApplyColors();
                yield return null;
            }
            bodyVis = 0f;
            // 2) 눈도 꺼짐
            yield return FadeEyes(0f, 0.25f);
            yield return Wait(0.35f);
        }

        // 3) 목표 앞에서 두 눈부터 깜빡깜빡
        root.position = to;
        body.transform.localPosition = Vector3.zero;
        body.sprite = sitSprite;
        eyeVis = 1f; yield return Wait(0.12f);
        eyeVis = 0f; yield return Wait(0.08f);
        eyeVis = 1f;
        // 4) 몸이 드러남
        float k = 0f;
        while (k < 1f && !Cine.Skipping)
        {
            k += Time.deltaTime / 0.5f;
            bodyVis = Mathf.Clamp01(k);
            yield return null;
        }
        bodyVis = 1f; eyeVis = 1f;
        if (UnityEngine.Random.value < 0.4f) Meow();
    }

    private IEnumerator FadeEyes(float to, float seconds)
    {
        float from = eyeVis, t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            eyeVis = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        eyeVis = to;
    }

    private static IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds && !Cine.Skipping) { t += Time.deltaTime; yield return null; }
    }

    // 메인 클리어 후: 플레이어 뒤를 따라감
    private void Follow()
    {
        if (player == null) return;
        Vector3 p = player.position;
        float d = Vector2.Distance(root.position, p);
        if (d > 8f) { root.position = p + Vector3.left * followDistance; return; } // 너무 멀어지면 옆으로
        if (d <= followDistance) { body.transform.localPosition = Vector3.zero; body.sprite = sitSprite; return; }
        Vector3 to = p + (root.position - p).normalized * followDistance;
        root.position = Vector3.MoveTowards(root.position, to, walkSpeed * 1.2f * Time.deltaTime);
        body.flipX = (p.x - root.position.x) < 0f;
        Trot();
    }

    // ================= 화면 밖 표시 =================
    private void UpdateEdge()
    {
        if (edgeIcon == null) return;
        Camera cam = Camera.main;
        if (cam == null) { edgeIcon.enabled = false; return; }

        Vector3 vp = cam.WorldToViewportPoint(root.position + Vector3.up * 0.5f);
        bool inside = vp.x > 0.03f && vp.x < 0.97f && vp.y > 0.03f && vp.y < 0.97f;
        bool show = shown && !inside && !following && !Cine.Running && !PlayerLock.IsLocked && eyeVis > 0.5f && bodyVis > 0.5f;
        edgeIcon.enabled = show;
        if (!show) return;

        Vector2 center = new Vector2(Screen.width, Screen.height) * 0.5f;
        Vector2 dir = ((Vector2)cam.WorldToScreenPoint(root.position) - center).normalized;
        Vector2 half = center - Vector2.one * 70f;
        float k = Mathf.Min(Mathf.Abs(half.x / Mathf.Max(Mathf.Abs(dir.x), 0.001f)), Mathf.Abs(half.y / Mathf.Max(Mathf.Abs(dir.y), 0.001f)));
        float bob = Mathf.Sin(Time.unscaledTime * 3f);
        edgeIcon.rectTransform.position = center + dir * k + Vector2.up * bob * 5f;
        // 깜빡이는 눈 (어둠 속에서 쳐다보는 느낌)
        edgeIcon.color = new Color(1f, 1f, 1f, eyes.enabled ? 1f : 0.15f);
    }

    // ================= 연출에서 부르는 것 =================
    // 어둠 속 두 눈만 (몸은 안 보임)
    public void ShowEyesOnly(Vector3 at)
    {
        SetShown(true);
        root.position = at;
        body.sprite = sitSprite;
        bodyVis = 0f;
        eyeVis = 1f;
        ApplyColors();
    }

    // 높은 곳(눈이 빛나던 곳)에서 뛰어내려 내려앉음
    public IEnumerator JumpTo(Vector3 to, float arc = 1.2f)
    {
        SetShown(true);
        moving = true;
        Vector3 from = root.position;
        float seconds = Mathf.Clamp(Vector2.Distance(from, to) / 5f, 0.35f, 0.9f);
        float t = 0f;
        body.flipX = to.x < from.x;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / seconds);
            root.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * arc;
            bodyVis = Mathf.Max(bodyVis, k);
            // 공중에선 길쭉하게
            body.transform.localScale = new Vector3(baseScale.x * 0.9f, baseScale.y * 1.12f, 1f);
            ApplyColors();
            yield return null;
        }
        root.position = to;
        bodyVis = 1f; eyeVis = 1f;
        // 착지: 납작했다가 돌아옴
        body.transform.localScale = new Vector3(baseScale.x * 1.15f, baseScale.y * 0.85f, 1f);
        for (int i = 0; i < 6; i++) RaGlowFx.Spark(to + new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), 0.05f, 0f), new Color(0.9f, 0.85f, 0.7f), body.sortingLayerName, body.sortingOrder + 2);
        yield return Wait(0.12f);
        body.transform.localScale = baseScale;
        moving = false;
    }

    public void Meow() => GameAudio.Play("sfx_cat_meow", 0.6f, UnityEngine.Random.Range(0.95f, 1.08f));

    // 가짜 단서를 끼우려 할 때: 하악! 털을 세움 (지금은 효과만. 가짜 단서 시스템에서 부르면 됨)
    public void Hiss()
    {
        if (!shown || root == null) return;
        GameAudio.Play("sfx_cat_hiss", 0.6f);
        StartCoroutine(HissRoutine());
    }

    private IEnumerator HissRoutine()
    {
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float j = (1f - t / 0.6f) * 0.04f;
            body.transform.localScale = new Vector3(baseScale.x * 1.18f, baseScale.y * 1.12f, 1f);
            body.transform.localPosition = new Vector3(UnityEngine.Random.Range(-j, j), UnityEngine.Random.Range(-j, j), 0f);
            yield return null;
        }
        body.transform.localScale = baseScale;
        body.transform.localPosition = Vector3.zero;
    }

    // 메인 클리어 후 따라오기 시작
    public void StartFollow()
    {
        following = true;
        perched = null;
        BastetStoryIds.Set(BastetStoryIds.FlagCatFollow);
        Debug.Log("[검은 고양이] 이제 플레이어를 따라감", this);
    }

    // 연출이 끝난 뒤 지금 목표로 (다음 Update에서 이동)
    public void ReleaseToGuide()
    {
        Directed = false;
        perched = null;
    }

    public void SnapToCurrent()
    {
        Step s = CurrentStep();
        if (s == null || s.target == null) return;
        perched = s;
        root.position = SitPoint(s);
        bodyVis = 1f; eyeVis = 1f;
    }

    // ================= 테스트 =================
    [ContextMenu("테스트: 지금 바로 등장 (목표 앞)")]
    private void TestAppear()
    {
        if (!Application.isPlaying) return;
        SetShown(true);
        SnapToCurrent();
    }

    [ContextMenu("테스트: 하악!")]
    private void TestHiss() { if (Application.isPlaying) Hiss(); }

    [ContextMenu("테스트: 야옹")]
    private void TestMeow() { if (Application.isPlaying) Meow(); }

    // Scene 뷰: 단계별 앉을 자리와 순서
    private void OnDrawGizmos()
    {
        if (steps == null) return;
        Vector3 prev = Vector3.zero;
        bool hasPrev = false;
        for (int i = 0; i < steps.Count; i++)
        {
            Step s = steps[i];
            if (s == null || s.target == null) continue;
            Vector3 p = SitPoint(s);
            Gizmos.color = new Color(0.85f, 1f, 0.5f, 0.9f);
            Gizmos.DrawWireSphere(p, 0.35f);
            if (hasPrev) { Gizmos.color = new Color(0.85f, 1f, 0.5f, 0.35f); Gizmos.DrawLine(prev, p); }
#if UNITY_EDITOR
            UnityEditor.Handles.Label(p + Vector3.up * 0.6f, $"고양이 {i + 1}. {s.label}");
#endif
            prev = p; hasPrev = true;
        }
    }

#if UNITY_EDITOR
    public void EditorSetup(List<Step> newSteps, Sprite sit, Sprite eye, Sprite edge)
    {
        steps = newSteps;
        if (sit != null) sitSprite = sit;
        if (eye != null) eyesSprite = eye;
        if (edge != null) edgeSprite = edge;
    }
#endif
}
