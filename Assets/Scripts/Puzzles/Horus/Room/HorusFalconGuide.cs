using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 호루스의 매 = 길잡이 (스토리 기획서: 신의 동물이 길을 안내)
// - 지금 목표(HorusObjectiveGuide.CurrentTarget) 위에 앉아 있음. 목표가 바뀌면 푸드덕 날아가 새 목표 위에 앉음
// - 목표가 화면 밖이면 화면 가장자리에 매 아이콘만 (화살표 없음 - 매가 고개를 그쪽으로 돌림)
// - 어두운 방에서도 보이게: 몸은 Unlit + 금빛 후광 + 작은 빛, 날 때는 바닥에 그림자
// - 본당 첫 입장 연출(HorusCutscenes)이 ArriveFrom으로 처음 불러옴 -> 그 전엔 안 보임
// [임시] 그림은 금색 매(H1_03). 동물 아트가 오면 Falcon Sprite 칸만 바꾸면 됨
public class HorusFalconGuide : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private HorusObjectiveGuide guide;
    [SerializeField] private Sprite falconSprite;
    [Tooltip("이 플래그가 켜진 뒤부터 보임 (본당 첫 입장 연출)")]
    [SerializeField] private string appearFlag = "F_HO_AWAKENED";

    [Header("모양 / 움직임")]
    [SerializeField] private float size = 1.1f;
    [SerializeField] private float perchHeight = 0.3f;
    [SerializeField] private float flySpeed = 6f;
    [SerializeField] private float arcHeight = 1.6f;
    [SerializeField] private Color glowColor = new Color(1f, 0.82f, 0.4f, 0.55f);

    private SpriteRenderer body, shadow;
    private Transform root;
    private Vector3 baseScale;
    private SpriteRenderer perchedOn;
    private bool flying, shown;
    private float idleTimer;
    private Image edgeIcon;

    public bool Flying => flying;
    public Vector3 Position => root != null ? root.position : transform.position;

    private void Start()
    {
        try
        {
            if (guide == null) guide = FindFirstObjectByType<HorusObjectiveGuide>();
            HorusFx.MatchPlayer();
            BuildBird();
            BuildEdge();
            SetShown(HorusDirector.HasFlag(appearFlag));
            if (shown) SnapToTarget();
        }
        catch (Exception e)
        {
            Debug.LogError($"[매 길잡이] 준비 오류: {e}");
        }
    }

    private void BuildBird()
    {
        root = new GameObject("FalconGuide").transform;
        root.SetParent(transform, false);
        body = HorusFx.Spawn("Body", falconSprite, root.position, size, Color.white, HorusFx.Above);
        body.transform.SetParent(root, true);
        baseScale = body.transform.localScale;
        RaGlowFx.Glow(root, Vector3.zero, size * 1.8f, glowColor, HorusFx.Layer, HorusFx.Above - 1);
        RaGlowFx.PointLight(root, Vector3.zero, new Color(1f, 0.85f, 0.5f), 2.2f, 0.7f);
        shadow = HorusFx.Spawn("Shadow", FxSprites.Soft, root.position, size * 0.9f, new Color(0f, 0f, 0f, 0.45f), HorusFx.Floor);
        shadow.transform.SetParent(transform, true);
    }

    private void BuildEdge()
    {
        RectTransform canvas = UIBuild.CreateCanvas(transform, "FalconEdgeCanvas", 80, out GameObject canvasGo);
        canvasGo.GetComponent<GraphicRaycaster>().enabled = false;
        // [수정] 세모 화살표는 없애고 매 아이콘만 씀 (길잡이는 매 하나로 통일)
        edgeIcon = UIBuild.Image(canvas, "Falcon", Vector2.zero, new Vector2(72f, 72f), falconSprite, Color.white);
        edgeIcon.preserveAspect = true;
        edgeIcon.color = Color.white;
        edgeIcon.raycastTarget = false;
        HideEdge(); // 처음엔 꺼 둠 (화면 밖일 때만 UpdateEdge가 켬)
    }

    private void SetShown(bool on)
    {
        shown = on;
        if (root != null) root.gameObject.SetActive(on);
        if (shadow != null) shadow.enabled = on;
        if (!on) HideEdge(); // [수정] 등장 전엔 화면 가장자리 표시도 숨김 (화면 가운데에 떠 있던 것)
    }

    private void HideEdge()
    {
        if (edgeIcon != null) edgeIcon.enabled = false;
    }

    // ================= 매 프레임 =================
    private void Update()
    {
        if (!shown || root == null) return;
        if (!HorusDirector.HasFlag(appearFlag) && !flying) return;
        SpriteRenderer target = guide != null ? guide.CurrentTarget : null;
        // 연출 중엔 연출이 매를 직접 움직임 (제단 위에 앉혀 둔 걸 마음대로 데려가지 않게)
        if (!flying && !Cine.Running && target != null && target != perchedOn && !HorusFocus.PanelOpen) StartCoroutine(FlyToTarget(target, false));
        if (!flying) Idle();
        UpdateEdge();
    }

    // 앉아 있을 때: 살짝 숨 쉬듯 + 가끔 날개 한 번 퍼덕
    private void Idle()
    {
        idleTimer -= Time.deltaTime;
        float flap = 0f;
        if (idleTimer < 0f) idleTimer = UnityEngine.Random.Range(2.5f, 5f);
        else if (idleTimer < 0.35f) flap = Mathf.Sin(idleTimer / 0.35f * Mathf.PI * 2f) * 0.2f;
        float breathe = Mathf.Sin(Time.time * 2f) * 0.03f;
        body.transform.localScale = new Vector3(baseScale.x * (1f + flap), baseScale.y * (1f + breathe - flap * 0.5f), 1f);
        shadow.transform.position = root.position + Vector3.down * 0.5f;
    }

    // ================= 날기 =================
    public IEnumerator FlyToCurrent(bool followCamera)
    {
        SpriteRenderer target = guide != null ? guide.CurrentTarget : null;
        if (target != null) yield return FlyToTarget(target, followCamera);
    }

    private IEnumerator FlyToTarget(SpriteRenderer target, bool followCamera)
    {
        perchedOn = target;
        Bounds b = target.bounds;
        yield return FlyTo(new Vector3(b.center.x, b.max.y + perchHeight, 0f), b.min.y, followCamera);
    }

    // 포물선으로 날아감. 날개는 빠르게 퍼덕, 바닥엔 그림자가 따라옴
    public IEnumerator FlyTo(Vector3 to, float groundY, bool followCamera)
    {
        flying = true;
        Vector3 from = root.position;
        float seconds = Mathf.Clamp(Vector3.Distance(from, to) / flySpeed, 0.5f, 3f);
        GameAudio.Play("sfx_wind_gust", 0.35f, 1.4f);
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            Vector3 ground = Vector3.Lerp(new Vector3(from.x, from.y - 0.5f, 0f), new Vector3(to.x, groundY, 0f), k);
            root.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * arcHeight;
            Flap(t, to.x - from.x);
            shadow.transform.position = ground;
            if (followCamera) CineCamera.Focus(root.position + Lead(to));
            yield return null;
        }
        Land(to);
    }

    // 카메라가 매보다 살짝 앞(가는 방향)을 비춤 -> 매가 화면 가운데에서 앞으로 나아가는 느낌
    private Vector3 Lead(Vector3 to)
    {
        Vector3 d = to - root.position;
        return d.magnitude < 0.01f ? Vector3.zero : d.normalized * Mathf.Min(1.2f, d.magnitude * 0.5f);
    }

    private void Flap(float t, float dx)
    {
        float f = Mathf.Sin(t * 22f);
        body.transform.localScale = new Vector3(baseScale.x * (1f + f * 0.25f), baseScale.y * (1f - f * 0.15f), 1f);
        body.flipX = dx < 0f;
        body.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(-dx, -1f, 1f) * 8f);
    }

    private void Land(Vector3 at)
    {
        root.position = at;
        body.transform.localScale = baseScale;
        body.transform.localRotation = Quaternion.identity;
        HorusFx.StarBurst(at, new Vector2(0.3f, 0.2f), 6, new Color(1f, 0.85f, 0.5f));
        flying = false;
    }

    // ================= 연출에서 부르는 것 =================
    // 처음 등장: 화면 밖에서 날아 들어와 방 위를 한 바퀴 돌고 첫 목표 위에 앉음
    public IEnumerator ArriveFrom(Vector3 from, Vector3 circleCenter)
    {
        SetShown(true);
        root.position = from;
        yield return FlyTo(circleCenter + Vector3.right * 3f, circleCenter.y - 2f, true);
        yield return CircleAround(circleCenter, 3f, 1.8f, true);
        yield return FlyToCurrent(true);
    }

    // 본당 첫 입장 연출: 하늘 틈에서 뛰어들 듯 날아옴 (카메라가 따라가며 점점 넓게)
    public IEnumerator DiveIn(Vector3 from, Vector3 to)
    {
        SetShown(true);
        root.position = from;
        CineCamera.Zoom(6.5f, 1.4f);
        yield return FlyTo(to, to.y - 2f, true);
    }

    // 목표가 아닌 곳(제단 위 등)에 앉힘. 연출이 끝나면 Update/FlyToCurrent가 다시 목표로 데려감
    public IEnumerator PerchAt(Vector3 at, bool followCamera)
    {
        perchedOn = null;
        yield return FlyTo(at, at.y - 0.8f, followCamera);
    }

    // 날개를 활짝 펴고 빛남 (위엄 포즈)
    public IEnumerator Spread(float seconds)
    {
        if (body == null) yield break;
        StartCoroutine(HorusFx.Pulse(root.position, size * 3f, glowColor, seconds));
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / seconds) * Mathf.PI);
            body.transform.localScale = new Vector3(baseScale.x * (1f + 0.45f * k), baseScale.y * (1f + 0.1f * k), 1f);
            yield return null;
        }
        body.transform.localScale = baseScale;
    }

    public IEnumerator CircleAround(Vector3 center, float radius, float seconds, bool followCamera)
    {
        flying = true;
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            float a = t / seconds * Mathf.PI * 2f;
            root.position = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.6f, 0f) * radius;
            Flap(t, -Mathf.Sin(a));
            shadow.transform.position = root.position + Vector3.down * 1.4f;
            if (followCamera) CineCamera.Focus(root.position);
            yield return null;
        }
        flying = false;
    }

    // 다시 들어왔을 때: 바로 목표 위에
    private void SnapToTarget()
    {
        SpriteRenderer target = guide != null ? guide.CurrentTarget : null;
        if (target == null) return;
        perchedOn = target;
        root.position = new Vector3(target.bounds.center.x, target.bounds.max.y + perchHeight, 0f);
    }

    // ================= 화면 밖이면 가장자리 표시 =================
    private void UpdateEdge()
    {
        Camera cam = Camera.main;
        if (cam == null || edgeIcon == null) return;
        Vector3 vp = cam.WorldToViewportPoint(root.position);
        bool inside = vp.x > 0.03f && vp.x < 0.97f && vp.y > 0.03f && vp.y < 0.97f;
        bool show = !inside && !HorusFocus.PanelOpen;
        edgeIcon.enabled = show;
        if (!show) return;
        Vector2 center = new Vector2(Screen.width, Screen.height) * 0.5f;
        Vector2 dir = ((Vector2)cam.WorldToScreenPoint(root.position) - center).normalized;
        Vector2 half = center - Vector2.one * 80f;
        float k = Mathf.Min(Mathf.Abs(half.x / Mathf.Max(Mathf.Abs(dir.x), 0.001f)), Mathf.Abs(half.y / Mathf.Max(Mathf.Abs(dir.y), 0.001f)));
        Vector2 pos = center + dir * k;
        // 매가 가야 할 쪽을 바라보게 좌우 뒤집기 + 둥실 + 살짝 커졌다 작아짐 (눈에 띄게)
        float bob = Mathf.Sin(Time.unscaledTime * 4f);
        edgeIcon.rectTransform.position = pos + Vector2.up * bob * 6f;
        float pulse = 1f + Mathf.Abs(bob) * 0.12f;
        edgeIcon.rectTransform.localScale = new Vector3((dir.x < 0f ? -1f : 1f) * pulse, pulse, 1f);
    }
}
