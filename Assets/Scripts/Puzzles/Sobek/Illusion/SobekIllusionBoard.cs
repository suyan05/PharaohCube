using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// 착시 퍼즐 판 하나 (2.5D: 3D 블록 + 직교 아이소메트릭 카메라)
// - 마우스 드래그(왼쪽/오른쪽 버튼) 또는 Q/E: 신전을 돌림 -> 손을 떼면 45°/135°/225°/315°에 붙음
// - 클릭: 그 칸까지 걸어감 (지금 각도에서 "화면에 이어져 보이는" 길만 걸을 수 있음)
// - 색 발판을 밟으면 같은 색 장치가 한 단계 움직임: 청록 = 도는 섬, 주황 = 악어 등 다리, 파랑 = 물높이(뗏목)
// - Z: 한 수 되돌리기, R: 처음으로
// 붙이는 곳: 퍼즐 씬의 Board 오브젝트 (Tools/Sobek/5 가 자동 배치). 여러 구역이면 SobekIllusionFlow가 하나씩 켬
public class SobekIllusionBoard : MonoBehaviour
{
    [Header("데이터 · 연결")]
    [SerializeField] private SobekIllusionLevel level;
    [SerializeField] private SobekIllusionCamera cameraRig;
    [Tooltip("Sobek/IllusionUnlit 셰이더 머티리얼 (불투명)")]
    [SerializeField] private Material blockMaterial;
    [Tooltip("Sobek/IllusionUnlit 셰이더 머티리얼 (반투명: Src 5, Dst 10, ZWrite 0)")]
    [SerializeField] private Material markerMaterial;
    [Tooltip("걷는 캐릭터 그림 (카메라를 항상 바라봄)")]
    [SerializeField] private SpriteRenderer avatar;
    [Tooltip("목표 칸 위에 세울 그림 (제단 등, 비워도 됨)")]
    [SerializeField] private SpriteRenderer goalProp;
    [SerializeField] private TMP_FontAsset hudFont;
    [Tooltip("안내 패널 첫 줄 앞에 붙는 글 (예: 구역 2/3)")]
    [SerializeField] private string hudPrefix = "";

    [Header("조작")]
    [Tooltip("이만큼(픽셀) 끌어야 회전으로 인식 (그보다 짧으면 클릭)")]
    [Range(2f, 40f)] [SerializeField] private float dragThreshold = 8f;
    [Tooltip("1칸 걷는 시간")]
    [Range(0.05f, 1f)] [SerializeField] private float stepTime = 0.22f;
    [Tooltip("장치가 한 단계 움직이는 시간")]
    [Range(0.1f, 2f)] [SerializeField] private float mechTime = 0.55f;
    [Tooltip("캐릭터 키 (유닛)")]
    [Range(0.3f, 2f)] [SerializeField] private float avatarHeight = 0.95f;

    [Header("안내")]
    [Tooltip("각도를 맞췄을 때 지금 갈 수 있게 된 착시 길 양 끝을 잠깐 빛냄")]
    [SerializeField] private bool flashNewLinks = true;
    [SerializeField] private Color linkFlashColor = new Color(0.45f, 1f, 1f, 0.75f);
    [SerializeField] private Color blockedFlashColor = new Color(1f, 0.35f, 0.3f, 0.6f);

    [Header("풀었을 때")]
    [TextArea(1, 3)] [SerializeField] private string clearMessage = "끊어진 길이 하나로 이어졌다.";
    [SerializeField] private UnityEvent onCleared;

    public event Action<SobekIllusionBoard> Cleared;

    private SobekIllusionModel model;
    private SobekIllusionGraph graph;
    private int[] mechStates;
    private Vector3Int cell;
    private Vector3 avatarPos;
    private bool walking, cleared, animating, initialized;
    private int yaw = -1;            // -1 = 도는 중 (길 없음)
    private int lastYaw = -1;
    private int steps, turns;
    private readonly HashSet<string> shownLinks = new HashSet<string>();
    private readonly Stack<(Vector3Int cell, int[] mechs)> undo = new Stack<(Vector3Int, int[])>();

    // 그림
    private Transform[] mechRoots;
    private Transform waterTr;

    // 입력 상태
    private bool pressing, dragging, pressRight;
    private Vector2 pressPos, lastPos;

    // 강조 표시
    private readonly List<MeshRenderer> markers = new List<MeshRenderer>();
    private MaterialPropertyBlock mpb;
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public bool InputLocked { get; set; }
    public bool IsCleared => cleared;
    public SobekIllusionLevel Level => level;

    private void Start()
    {
        if (level == null) { Debug.LogError("[SobekIllusionBoard] level(SobekIllusionLevel)이 비어 있음", this); enabled = false; return; }
        if (cameraRig == null) cameraRig = FindFirstObjectByType<SobekIllusionCamera>();
        if (cameraRig == null) { Debug.LogError("[SobekIllusionBoard] SobekIllusionCamera를 못 찾음", this); enabled = false; return; }

        model = level.BuildModel();
        mechStates = model.StartStates();
        graph = model.GraphAt(mechStates);
        BuildVisuals();
        cameraRig.Snapped += OnSnapped;
        cameraRig.RotateStarted += OnRotateStarted;
        initialized = true;

        ApplyCamera();
        PlacePlayer(level.Start);
        yaw = lastYaw = level.StartYaw;
        UpdateHud();
        Debug.Log($"[SobekIllusionBoard] {level.DisplayName}: 블록 {graph.Blocks.Count}개, 장치 {model.Mechs.Count}개, 시작 각도 {SobekIllusionGraph.YawDegrees(yaw)}°", this);
    }

    // 여러 구역을 번갈아 켤 때 카메라를 이 판에 맞춤
    private void OnEnable()
    {
        if (initialized) { ApplyCamera(); UpdateHud(); }
    }

    private void ApplyCamera()
    {
        cameraRig.transform.position = level.Center();
        if (cameraRig.Cam != null)
        {
            cameraRig.Cam.orthographicSize = Mathf.Max(level.CameraSize, FitSize(cameraRig.Cam.aspect));
            cameraRig.Cam.backgroundColor = level.BackgroundColor;
            cameraRig.Cam.clearFlags = CameraClearFlags.SolidColor;
        }
        cameraRig.SetYawImmediate(yaw < 0 ? level.StartYaw : yaw);
    }

    // 네 각도 어디서 봐도 판 전체가 화면에 들어오는 카메라 크기 (장치가 움직일 자리 포함)
    private float FitSize(float aspect)
    {
        if (model == null) return 0f;
        var pts = new List<Vector3Int>(model.Static);
        for (int m = 0; m < model.Mechs.Count; m++)
            for (int s = 0; s < Mathf.Max(1, model.Mechs[m].count); s++)
                foreach (Vector3Int c in model.Mechs[m].cells) pts.Add(model.MechCell(m, c, s));
        Vector3 center = level.Center();
        float need = 0f;
        for (int y = 0; y < 4; y++)
        {
            Quaternion q = Quaternion.Euler(SobekIllusionCamera.IsoPitch, SobekIllusionGraph.YawDegrees(y), 0f);
            Vector3 right = q * Vector3.right, up = q * Vector3.up;
            foreach (Vector3Int p in pts)
            {
                if (p.y < -1) continue; // 물 밑 기둥 끝은 빼고
                Vector3 d = (Vector3)p - center;
                need = Mathf.Max(need, Mathf.Abs(Vector3.Dot(d, up)) + 1.2f, (Mathf.Abs(Vector3.Dot(d, right)) + 1.2f) / Mathf.Max(0.3f, aspect));
            }
        }
        return need;
    }

    private void OnDestroy()
    {
        if (cameraRig != null) { cameraRig.Snapped -= OnSnapped; cameraRig.RotateStarted -= OnRotateStarted; }
    }

    // ================= 그림 만들기 =================
    private void BuildVisuals()
    {
        // 발판 색 (고정 블록 윗면)
        var plateColor = new Dictionary<Vector3Int, Color>();
        foreach (SobekIllusionModel.Plate p in model.Plates)
            plateColor[p.cell] = SobekIllusionLevel.MechColor(model.Mechs[p.mech].kind);

        SobekIllusionMesh.TopStyle staticStyle = (Vector3Int c, ref Color inner, ref Color edge) =>
        {
            if (c == level.Start) inner = level.StartColor;
            else if (c == level.Goal) inner = level.GoalColor;
            if (plateColor.TryGetValue(c, out Color pc)) { inner = pc; edge = pc * 0.55f; edge.a = 1f; }
        };
        MakeMesh("Blocks", transform, model.Static, Vector3.zero, Color.white, 0f, staticStyle);

        // 장치마다 따로 (움직일 때 오브젝트째로 돌리거나 옮김)
        mechRoots = new Transform[model.Mechs.Count];
        for (int m = 0; m < model.Mechs.Count; m++)
        {
            SobekIllusionModel.Mech mech = model.Mechs[m];
            Color mc = SobekIllusionLevel.MechColor(mech.kind);
            var root = new GameObject($"Mech_{m}_{mech.kind}").transform;
            root.SetParent(transform, false);
            Vector3 origin = mech.kind == SobekIllusionModel.Kind.Rotate ? new Vector3(mech.pivot.x, 0f, mech.pivot.z) : Vector3.zero;
            root.localPosition = origin;
            SobekIllusionMesh.TopStyle groupStyle = (Vector3Int c, ref Color inner, ref Color edge) =>
            {
                if (c == level.Goal) inner = level.GoalColor;
                edge = mc * 0.8f; edge.a = 1f; // 움직이는 블록은 테두리가 장치 색
            };
            MakeMesh("Mesh", root, mech.cells, origin, mc, 0.22f, groupStyle);
            mechRoots[m] = root;
        }
        SnapMechVisuals();

        Vector3 center = level.Center();
        waterTr = new GameObject("Water").transform;
        waterTr.SetParent(transform, false);
        waterTr.gameObject.AddComponent<MeshFilter>().sharedMesh = SobekIllusionMesh.Water(center, 40f, level.WaterColor);
        waterTr.gameObject.AddComponent<MeshRenderer>().sharedMaterial = blockMaterial;
        waterTr.localPosition = new Vector3(0f, model.WaterY(mechStates), 0f);

        mpb = new MaterialPropertyBlock();
        Mesh markerMesh = SobekIllusionMesh.Marker();
        for (int i = 0; i < 12; i++)
        {
            var mk = new GameObject($"Marker_{i}");
            mk.transform.SetParent(transform, false);
            mk.AddComponent<MeshFilter>().sharedMesh = markerMesh;
            var r = mk.AddComponent<MeshRenderer>();
            r.sharedMaterial = markerMaterial;
            r.enabled = false;
            markers.Add(r);
        }

        if (avatar != null && avatar.sprite != null)
        {
            float h = avatar.sprite.bounds.size.y;
            if (h > 0.001f) avatar.transform.localScale = Vector3.one * (avatarHeight / h);
        }
        if (goalProp != null && goalProp.sprite != null)
        {
            float h = goalProp.sprite.bounds.size.y;
            if (h > 0.001f) goalProp.transform.localScale = Vector3.one * (1.1f / h);
        }
    }

    private void MakeMesh(string objName, Transform parent, ICollection<Vector3Int> cells, Vector3 origin, Color tint, float tintAmount, SobekIllusionMesh.TopStyle style)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = SobekIllusionMesh.Build(cells, origin, level, tint, tintAmount, style);
        go.AddComponent<MeshRenderer>().sharedMaterial = blockMaterial;
    }

    // 장치 오브젝트를 지금 상태 자리에 바로 놓음
    private void SnapMechVisuals()
    {
        for (int m = 0; m < mechRoots.Length; m++) SetMechVisual(m, mechStates[m], mechStates[m], 1f);
        if (waterTr != null) waterTr.localPosition = new Vector3(0f, model.WaterY(mechStates), 0f);
    }

    // from -> to 상태 사이를 t(0~1)만큼
    private void SetMechVisual(int m, int from, int to, float t)
    {
        SobekIllusionModel.Mech mech = model.Mechs[m];
        Transform r = mechRoots[m];
        switch (mech.kind)
        {
            case SobekIllusionModel.Kind.Rotate:
                // 마지막 -> 0 으로 돌아올 때도 같은 방향으로 계속 돎
                float a0 = 90f * from, a1 = 90f * to;
                if (mech.count == 4 && to < from) a1 = 90f * (to + 4); // 4단계는 같은 방향으로 한 바퀴 (그 외는 되돌아감)
                r.localRotation = Quaternion.Euler(0f, Mathf.Lerp(a0, a1, t), 0f);
                break;
            case SobekIllusionModel.Kind.Slide:
                Vector3 o0 = Offset(mech, from), o1 = Offset(mech, to);
                r.localPosition = Vector3.Lerp(o0, o1, t);
                break;
            default:
                r.localPosition = new Vector3(0f, Mathf.Lerp(from, to, t), 0f);
                break;
        }
    }

    private static Vector3 Offset(SobekIllusionModel.Mech mech, int s) =>
        mech.offsets.Count > 0 ? (Vector3)mech.offsets[Mathf.Clamp(s, 0, mech.offsets.Count - 1)] : Vector3.zero;

    private static Vector3 Top(Vector3Int c) => new Vector3(c.x, c.y + 0.5f, c.z);

    private void PlacePlayer(Vector3Int c)
    {
        cell = c;
        avatarPos = Top(c);
    }

    // 그림이 항상 카메라를 보게 + 발이 칸 위에 오게
    private void LateUpdate()
    {
        if (cameraRig == null || cameraRig.Cam == null || level == null) return;
        Transform camT = cameraRig.Cam.transform;
        Vector3 toCam = -camT.forward;
        if (avatar != null)
        {
            avatar.transform.rotation = camT.rotation;
            // 그림의 발(아래 끝)이 칸 위에 오게 + 카메라 쪽으로 조금 당겨서 자기 발판에 묻히지 않게
            avatar.transform.position = avatarPos + camT.up * FootOffset(avatar) + toCam * 0.35f;
        }
        if (goalProp != null)
        {
            goalProp.transform.rotation = camT.rotation;
            goalProp.transform.position = GoalTop() + camT.up * FootOffset(goalProp) + toCam * 0.35f;
        }
    }

    // 목표가 움직이는 장치 위에 있어도 따라가게
    private Vector3 GoalTop()
    {
        if (model == null) return Top(level.Goal);
        for (int m = 0; m < model.Mechs.Count; m++)
            if (model.Mechs[m].cells.Contains(level.Goal) && mechRoots != null && mechRoots[m] != null)
            {
                Vector3 origin = model.Mechs[m].kind == SobekIllusionModel.Kind.Rotate ? new Vector3(model.Mechs[m].pivot.x, 0f, model.Mechs[m].pivot.z) : Vector3.zero;
                return mechRoots[m].TransformPoint(Top(level.Goal) - origin);
            }
        return Top(level.Goal);
    }

    // 피벗이 가운데든 아래든 그림 아래 끝이 발판에 닿도록 올릴 거리
    private static float FootOffset(SpriteRenderer sr)
    {
        if (sr.sprite == null) return 0.5f;
        return -sr.sprite.bounds.min.y * sr.transform.localScale.y;
    }

    // ================= 입력 (New Input System) =================
    private bool Busy => walking || animating || cleared || InputLocked || SobekStory.Busy;

    private void Update()
    {
        if (graph == null) return;
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (kb != null && !Busy)
        {
            if (kb.qKey.wasPressedThisFrame) cameraRig.Step(-1);
            if (kb.eKey.wasPressedThisFrame) cameraRig.Step(1);
            if (kb.rKey.wasPressedThisFrame) ResetBoard();
            if (kb.zKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame) Undo();
        }

        if (mouse == null) return;
        Vector2 pos = mouse.position.ReadValue();
        bool leftDown = mouse.leftButton.wasPressedThisFrame, rightDown = mouse.rightButton.wasPressedThisFrame;
        if (leftDown || rightDown)
        {
            pressing = true; dragging = false; pressRight = rightDown;
            pressPos = lastPos = pos;
        }
        if (pressing)
        {
            if (!dragging && !Busy && Mathf.Abs(pos.x - pressPos.x) > dragThreshold)
            {
                dragging = true;
                cameraRig.BeginDrag();
            }
            if (dragging) cameraRig.Drag(-(pos.x - lastPos.x)); // 끄는 방향으로 신전이 돎
            lastPos = pos;

            bool released = pressRight ? mouse.rightButton.wasReleasedThisFrame : mouse.leftButton.wasReleasedThisFrame;
            if (released)
            {
                pressing = false;
                if (dragging) { dragging = false; cameraRig.EndDrag(); }
                else if (!pressRight) Click(pos);
            }
        }
    }

    private void OnRotateStarted()
    {
        if (!isActiveAndEnabled) return;
        yaw = -1;
        UpdateHud();
    }

    private void OnSnapped(int index)
    {
        if (!isActiveAndEnabled) return;
        if (index != lastYaw) turns++; // 같은 각도로 돌아오면 회전으로 안 셈
        lastYaw = index;
        yaw = index;
        UpdateHud();
        if (flashNewLinks && !Busy) FlashReachableLinks();
    }

    // ================= 클릭 -> 걷기 =================
    private void Click(Vector2 screen)
    {
        if (Busy || yaw < 0 || cameraRig.IsRotating) return;
        if (!Pick(screen, out Vector3Int target, out bool topFace)) return;
        if (!topFace || !graph.IsWalkable(target)) return;
        if (target == cell) return;

        List<SobekIllusionGraph.Edge> path = graph.FindPath(cell, target, yaw);
        if (path == null)
        {
            Flash(target, blockedFlashColor, 0.35f);
            Debug.Log($"[SobekIllusionBoard] {target}까지 길 없음 (각도 {SobekIllusionGraph.YawDegrees(yaw)}°)", this);
            return;
        }
        StartCoroutine(Walk(path));
    }

    // 화면 한 점에서 카메라 방향으로 광선을 쏴 처음 닿는 (보이는) 블록 (물리 없이 격자로 계산)
    private bool Pick(Vector2 screen, out Vector3Int hit, out bool topFace)
    {
        hit = default; topFace = false;
        Ray ray = cameraRig.Cam.ScreenPointToRay(screen);
        for (float t = 0f; t < 150f; t += 0.02f)
        {
            Vector3 p = ray.origin + ray.direction * t;
            var c = new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));
            if (!graph.IsBlock(c)) continue;
            hit = c;
            topFace = p.y > c.y + 0.5f - 0.05f;
            return true;
        }
        return false;
    }

    private IEnumerator Walk(List<SobekIllusionGraph.Edge> path)
    {
        walking = true;
        foreach (SobekIllusionGraph.Edge e in path)
        {
            undo.Push((cell, (int[])mechStates.Clone()));
            Vector3 a = Top(cell), b = Top(e.To);
            if (!e.IsIllusion)
            {
                yield return Move(a, b, stepTime);
            }
            else
            {
                // 착시 길: 내 칸의 가장자리까지 간 다음, 화면에서 같은 자리인 상대 칸의 가장자리로 순간 이동
                // (직교 카메라에서 보는 방향으로 옮기면 화면 위치가 그대로라 끊김 없이 보임)
                Vector3 half = (Vector3)e.Dir * 0.5f;
                yield return Move(a, a + half, stepTime * 0.5f);
                Debug.Log($"[SobekIllusionBoard] 착시 길 건넘 {cell} -> {e.To} (각도 {SobekIllusionGraph.YawDegrees(yaw)}°)", this);
                yield return Move(b - half, b, stepTime * 0.5f);
            }
            cell = e.To;
            steps++;
            UpdateHud();
            if (cell == level.Goal) { walking = false; StartCoroutine(Clear()); yield break; }

            // 발판: 장치 작동 -> 길이 바뀌므로 여기서 멈춤
            int pm = model.PlateMechAt(cell);
            if (pm >= 0)
            {
                walking = false;
                yield return Trigger(pm);
                yield break;
            }
        }
        walking = false;
    }

    private IEnumerator Move(Vector3 from, Vector3 to, float time)
    {
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            avatarPos = Vector3.Lerp(from, to, t / time);
            yield return null;
        }
        avatarPos = to;
    }

    // 발판을 밟음 -> 장치 한 단계
    private IEnumerator Trigger(int m)
    {
        SobekIllusionModel.Mech mech = model.Mechs[m];
        if (!model.TryAdvance(mechStates, m, cell, out int[] next, out string reason))
        {
            AnubisToast.Show(reason, 2f);
            Flash(cell, blockedFlashColor, 0.5f);
            Debug.Log($"[SobekIllusionBoard] 장치 {m}({mech.kind}) 못 움직임: {reason}", this);
            yield break;
        }
        animating = true;
        Flash(cell, SobekIllusionLevel.MechColor(mech.kind), 0.6f);
        int from = mechStates[m], to = next[m];
        float w0 = model.WaterY(mechStates), w1 = model.WaterY(next);
        if (cameraRig.Cam != null && mech.kind != SobekIllusionModel.Kind.Slide) AnubisToast.Show(MechToast(mech.kind, from, to), 1.6f);
        for (float t = 0f; t < mechTime; t += Time.deltaTime)
        {
            float e = Mathf.SmoothStep(0f, 1f, t / mechTime);
            SetMechVisual(m, from, to, e);
            if (waterTr != null) waterTr.localPosition = new Vector3(0f, Mathf.Lerp(w0, w1, e), 0f);
            yield return null;
        }
        mechStates = next;
        graph = model.GraphAt(mechStates);
        SnapMechVisuals();
        animating = false;
        Debug.Log($"[SobekIllusionBoard] 장치 {m}({mech.kind}) {from} -> {to}", this);
        UpdateHud();
        if (flashNewLinks && yaw >= 0) FlashReachableLinks();
    }

    private static string MechToast(SobekIllusionModel.Kind k, int from, int to)
    {
        if (k == SobekIllusionModel.Kind.Water) return to > from ? "물이 차오른다…" : "물이 빠진다…";
        return "섬이 돌아간다…";
    }

    private void Undo()
    {
        if (undo.Count == 0) { AnubisToast.Show("더 되돌릴 수 없다.", 1f); return; }
        var (c, st) = undo.Pop();
        mechStates = st;
        graph = model.GraphAt(mechStates);
        SnapMechVisuals();
        PlacePlayer(c);
        steps = Mathf.Max(0, steps - 1);
        UpdateHud();
    }

    private void ResetBoard()
    {
        cleared = false;
        mechStates = model.StartStates();
        graph = model.GraphAt(mechStates);
        SnapMechVisuals();
        PlacePlayer(level.Start);
        cameraRig.SetYawImmediate(level.StartYaw);
        yaw = lastYaw = level.StartYaw;
        steps = turns = 0;
        undo.Clear();
        shownLinks.Clear();
        UpdateHud();
        AnubisToast.Show("처음으로 돌아간다.", 1.2f);
    }

    private IEnumerator Clear()
    {
        cleared = true;
        Flash(level.Goal, new Color(1f, 0.9f, 0.4f, 0.8f), 1.2f);
        Debug.Log($"[SobekIllusionBoard] {level.DisplayName} 클리어! 걸음 {steps} · 회전 {turns}", this);
        if (!string.IsNullOrEmpty(clearMessage)) AnubisToast.Show(clearMessage, 3f);
        UpdateHud();
        yield return new WaitForSeconds(0.6f);
        onCleared?.Invoke();
        Cleared?.Invoke(this);
    }

    // ================= 강조 표시 =================
    private void FlashReachableLinks()
    {
        HashSet<Vector3Int> reach = graph.Reachable(cell, yaw);
        string stKey = SobekIllusionModel.Key(mechStates).ToString();
        foreach (Vector3Int n in reach)
            foreach (SobekIllusionGraph.Edge e in graph.Neighbors(n, yaw))
            {
                if (!e.IsIllusion) continue;
                string key = $"{stKey}:{yaw}:{Min(n, e.To)}:{Max(n, e.To)}";
                if (!shownLinks.Add(key)) continue;
                Flash(n, linkFlashColor, 0.9f);
                Flash(e.To, linkFlashColor, 0.9f);
            }
    }

    private static Vector3Int Min(Vector3Int a, Vector3Int b) => a.x * 10000 + a.y * 100 + a.z < b.x * 10000 + b.y * 100 + b.z ? a : b;
    private static Vector3Int Max(Vector3Int a, Vector3Int b) => Min(a, b) == a ? b : a;

    private void Flash(Vector3Int c, Color color, float time)
    {
        MeshRenderer r = markers.Find(m => !m.enabled);
        if (r == null) return;
        StartCoroutine(FlashRoutine(r, Top(c) + Vector3.up * 0.01f, color, time));
    }

    private IEnumerator FlashRoutine(MeshRenderer r, Vector3 pos, Color color, float time)
    {
        r.transform.position = pos;
        r.enabled = true;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            Color c = color;
            c.a *= Mathf.Sin(Mathf.Clamp01(t / time) * Mathf.PI); // 켜졌다 꺼짐
            mpb.SetColor(ColorId, c);
            r.SetPropertyBlock(mpb);
            yield return null;
        }
        r.enabled = false;
    }

    // ================= 안내 패널 =================
    private void UpdateHud()
    {
        if (level == null || !isActiveAndEnabled) return;
        var sb = new System.Text.StringBuilder();
        sb.Append("<b>").Append(hudPrefix).Append(level.DisplayName).Append("</b>\n");
        sb.Append("<size=80%>드래그 / <color=#FFD27A>Q E</color> 신전 돌리기 · <color=#FFD27A>클릭</color> 이동 · <color=#FFD27A>Z</color> 되돌리기 · <color=#FFD27A>R</color> 처음으로 · <color=#FFD27A>Esc</color> 나가기</size>\n");
        if (model != null && model.Mechs.Count > 0)
        {
            sb.Append("<size=75%>발판: ");
            var seen = new HashSet<SobekIllusionModel.Kind>();
            foreach (SobekIllusionModel.Mech m in model.Mechs)
            {
                if (!seen.Add(m.kind)) continue;
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(SobekIllusionLevel.MechColor(m.kind))).Append(">■ ")
                  .Append(SobekIllusionLevel.MechName(m.kind)).Append("</color>  ");
            }
            sb.Append("</size>\n");
        }
        sb.Append("<size=80%>각도 ").Append(yaw < 0 ? "도는 중…" : SobekIllusionGraph.YawDegrees(yaw) + "°");
        sb.Append(" · 걸음 ").Append(steps).Append(" · 회전 ").Append(turns);
        if (cleared) sb.Append(" · <color=#FFD27A>완료!</color>");
        sb.Append("</size>");
        SobekHud.Get(hudFont).Show(sb.ToString());
    }

    // ================= 테스트 · 디버그 =================
    [ContextMenu("정답 확인 (BFS)")]
    private void CheckSolution()
    {
        if (level == null) return;
        SobekIllusionModel m = level.BuildModel();
        var sol = m.Solve(level.StartYaw, out int states);
        if (sol == null) { Debug.LogWarning($"[SobekIllusionBoard] {level.DisplayName}: 정답 없음! (상태 {states}개)", this); return; }
        var sb = new System.Text.StringBuilder();
        for (int i = 1; i < sol.Count; i++)
        {
            SobekIllusionModel.Step s = sol[i];
            sb.Append(s.action == "rotate" ? $"\n  회전 -> {SobekIllusionGraph.YawDegrees(s.yaw)}°" : $"\n  {s.cell} {s.action}");
        }
        Debug.Log($"[SobekIllusionBoard] {level.DisplayName}: 정답 {sol.Count - 1}수 (상태 {states}개){sb}", this);
    }

    [ContextMenu("테스트: 목표 칸으로 순간 이동")]
    private void TestTeleportGoal()
    {
        if (!Application.isPlaying || graph == null || cleared) return;
        PlacePlayer(level.Goal);
        StartCoroutine(Clear());
    }

    [Header("기즈모")]
    [Tooltip("씬 뷰에 표시할 착시 길 각도 (0=45° 1=135° 2=225° 3=315°)")]
    [Range(0, 3)] [SerializeField] private int gizmoYaw = 2;

    // 씬 뷰: 블록 테두리, 걸을 수 있는 칸(초록 점), 고른 각도의 착시 길(하늘색 선), 발판(장치 색), 시작/목표
    private void OnDrawGizmos()
    {
        if (level == null) return;
        SobekIllusionGraph g = Application.isPlaying && graph != null ? graph : level.BuildGraph();
        Gizmos.color = new Color(1f, 0.85f, 0.5f, 0.35f);
        foreach (Vector3Int b in g.Blocks) Gizmos.DrawWireCube(b, Vector3.one);
        Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.8f);
        foreach (Vector3Int w in g.Walkable) Gizmos.DrawSphere(Top(w), 0.07f);
        Gizmos.color = Color.cyan;
        foreach (Vector3Int w in g.Walkable)
            foreach (SobekIllusionGraph.Edge e in g.Neighbors(w, gizmoYaw))
                if (e.IsIllusion) Gizmos.DrawLine(Top(w), Top(e.To));
        foreach (SobekIllusionLevel.PlateDef p in level.Plates)
        {
            if (p.mech < 0 || p.mech >= level.Mechs.Count) continue;
            Gizmos.color = SobekIllusionLevel.MechColor(level.Mechs[p.mech].kind);
            Gizmos.DrawWireCube(Top(p.cell), new Vector3(0.8f, 0.05f, 0.8f));
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Top(level.Start), 0.3f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Top(level.Goal), 0.3f);
    }

#if UNITY_EDITOR
    public void EditorSetup(SobekIllusionLevel lv, SobekIllusionCamera rig, Material block, Material marker, SpriteRenderer avatarRenderer, SpriteRenderer goalRenderer, TMP_FontAsset font, string prefix, string clearText)
    {
        level = lv; cameraRig = rig; blockMaterial = block; markerMaterial = marker; avatar = avatarRenderer; goalProp = goalRenderer; hudFont = font;
        hudPrefix = prefix; clearMessage = clearText;
        gizmoYaw = lv != null ? lv.StartYaw : 2;
    }
#endif
}
