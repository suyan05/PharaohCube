using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// 착시 퍼즐 본체 (2.5D: 3D 블록 + 직교 아이소메트릭 카메라)
// - 마우스 드래그(왼쪽/오른쪽 버튼) 또는 Q/E: 신전을 돌림 -> 손을 떼면 45°/135°/225°/315°에 붙음
// - 클릭: 그 칸까지 걸어감 (지금 각도에서 "화면에 이어져 보이는" 길만 걸을 수 있음)
// - R: 처음으로
// - 끊어진 길도 어떤 각도에서는 화면에서 맞붙어 보이고, 그때는 실제로 건너갈 수 있음
// 붙이는 곳: 씬의 SobekIllusion 오브젝트 (Tools/Sobek/착시 퍼즐 1 만들기가 자동 배치)
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

    [Header("조작")]
    [Tooltip("이만큼(픽셀) 끌어야 회전으로 인식 (그보다 짧으면 클릭)")]
    [Range(2f, 40f)] [SerializeField] private float dragThreshold = 8f;
    [Tooltip("1칸 걷는 시간")]
    [Range(0.05f, 1f)] [SerializeField] private float stepTime = 0.22f;
    [Tooltip("캐릭터 키 (유닛)")]
    [Range(0.3f, 2f)] [SerializeField] private float avatarHeight = 0.95f;

    [Header("안내")]
    [Tooltip("각도를 맞췄을 때 지금 갈 수 있게 된 착시 길 양 끝을 잠깐 빛냄")]
    [SerializeField] private bool flashNewLinks = true;
    [SerializeField] private Color linkFlashColor = new Color(0.45f, 1f, 1f, 0.75f);
    [SerializeField] private Color blockedFlashColor = new Color(1f, 0.35f, 0.3f, 0.6f);

    [Header("풀었을 때")]
    [SerializeField] private string clearFlag = SobekIds.FlagIllusion1Clear;
    [TextArea(1, 3)] [SerializeField] private string clearMessage = "끊어진 수로가 하나로 이어졌다. 소베크의 눈은 보는 자리에 따라 길을 바꾼다.";
    [Tooltip("켜면 풀고 나서 아래 씬으로 이동")]
    [SerializeField] private bool returnAfterClear = false;
    [SerializeField] private string returnScene = SobekIds.HubScene;
    [SerializeField] private UnityEvent onCleared;

    private SobekIllusionGraph graph;
    private Vector3Int cell;
    private Vector3 avatarPos;
    private bool walking, cleared;
    private int yaw = -1;            // -1 = 도는 중 (길 없음)
    private int lastYaw = -1;
    private int steps, turns;
    private readonly HashSet<string> shownLinks = new HashSet<string>();

    // 입력 상태
    private bool pressing, dragging, pressRight;
    private Vector2 pressPos, lastPos;

    // 강조 표시
    private readonly List<MeshRenderer> markers = new List<MeshRenderer>();
    private MaterialPropertyBlock mpb;
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public bool IsCleared => cleared;
    public Vector3Int PlayerCell => cell;

    private void Start()
    {
        if (level == null) { Debug.LogError("[SobekIllusionBoard] level(SobekIllusionLevel)이 비어 있음", this); enabled = false; return; }
        if (cameraRig == null) cameraRig = FindFirstObjectByType<SobekIllusionCamera>();
        if (cameraRig == null) { Debug.LogError("[SobekIllusionBoard] SobekIllusionCamera를 못 찾음", this); enabled = false; return; }

        graph = level.BuildGraph();
        BuildVisuals();

        cameraRig.transform.position = level.Center();
        if (cameraRig.Cam != null)
        {
            cameraRig.Cam.orthographicSize = level.CameraSize;
            cameraRig.Cam.backgroundColor = level.BackgroundColor;
            cameraRig.Cam.clearFlags = CameraClearFlags.SolidColor;
        }
        cameraRig.Snapped += OnSnapped;
        cameraRig.RotateStarted += OnRotateStarted;
        cameraRig.SetYawImmediate(level.StartYaw);

        PlacePlayer(level.Start);
        yaw = lastYaw = level.StartYaw;
        UpdateHud();
        if (!string.IsNullOrEmpty(level.Intro)) AnubisToast.Show(level.Intro, 5f);
        Debug.Log($"[SobekIllusionBoard] {level.DisplayName}: 블록 {graph.Blocks.Count}개, 걸을 수 있는 칸 {graph.Walkable.Count}개, 시작 각도 {SobekIllusionGraph.YawDegrees(yaw)}°", this);
    }

    private void OnDestroy()
    {
        if (cameraRig != null) { cameraRig.Snapped -= OnSnapped; cameraRig.RotateStarted -= OnRotateStarted; }
        var hud = FindFirstObjectByType<SobekHud>();
        if (hud != null) hud.Hide();
    }

    // ================= 그림 만들기 =================
    private void BuildVisuals()
    {
        var blocks = new GameObject("Blocks");
        blocks.transform.SetParent(transform, false);
        blocks.AddComponent<MeshFilter>().sharedMesh = SobekIllusionMesh.Build(graph, level);
        blocks.AddComponent<MeshRenderer>().sharedMaterial = blockMaterial;

        Vector3 c = level.Center();
        var water = new GameObject("Water");
        water.transform.SetParent(transform, false);
        water.AddComponent<MeshFilter>().sharedMesh = SobekIllusionMesh.Water(c, level.WaterY, 40f, level.WaterColor);
        water.AddComponent<MeshRenderer>().sharedMaterial = blockMaterial;

        mpb = new MaterialPropertyBlock();
        Mesh markerMesh = SobekIllusionMesh.Marker();
        for (int i = 0; i < 12; i++)
        {
            var m = new GameObject($"Marker_{i}");
            m.transform.SetParent(transform, false);
            m.AddComponent<MeshFilter>().sharedMesh = markerMesh;
            var r = m.AddComponent<MeshRenderer>();
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

    private static Vector3 Top(Vector3Int c) => new Vector3(c.x, c.y + 0.5f, c.z);

    private void PlacePlayer(Vector3Int c)
    {
        cell = c;
        avatarPos = Top(c);
    }

    // 그림이 항상 카메라를 보게 + 발이 칸 위에 오게
    private void LateUpdate()
    {
        if (cameraRig == null || cameraRig.Cam == null) return;
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
            goalProp.transform.position = Top(level.Goal) + camT.up * FootOffset(goalProp) + toCam * 0.35f;
        }
    }

    // 피벗이 가운데든 아래든 그림 아래 끝이 발판에 닿도록 올릴 거리
    private static float FootOffset(SpriteRenderer sr)
    {
        if (sr.sprite == null) return 0.5f;
        return -sr.sprite.bounds.min.y * sr.transform.localScale.y;
    }

    // ================= 입력 (New Input System) =================
    private void Update()
    {
        if (graph == null) return;
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (kb != null && !walking)
        {
            if (kb.qKey.wasPressedThisFrame) cameraRig.Step(-1);
            if (kb.eKey.wasPressedThisFrame) cameraRig.Step(1);
            if (kb.rKey.wasPressedThisFrame) ResetPlayer();
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
            if (!dragging && !walking && !cleared && Mathf.Abs(pos.x - pressPos.x) > dragThreshold)
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
        yaw = -1;
        UpdateHud();
    }

    private void OnSnapped(int index)
    {
        if (index != lastYaw) turns++; // 같은 각도로 돌아오면 회전으로 안 셈
        lastYaw = index;
        yaw = index;
        UpdateHud();
        if (flashNewLinks && !walking && !cleared) FlashReachableLinks();
    }

    // ================= 클릭 -> 걷기 =================
    private void Click(Vector2 screen)
    {
        if (walking || cleared || yaw < 0 || cameraRig.IsRotating) return;
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

    // 화면 한 점에서 카메라 방향으로 광선을 쏴 처음 닿는 블록 (물리 없이 격자로 계산)
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
                Debug.Log($"[SobekIllusionBoard] 착시 길 건넘 {cell} -> {e.To} (각도 {SobekIllusionGraph.YawDegrees(yaw)}°, 보는 방향 {e.K}칸)", this);
                yield return Move(b - half, b, stepTime * 0.5f);
            }
            cell = e.To;
            steps++;
            UpdateHud();
            if (cell == level.Goal) { walking = false; StartCoroutine(Clear()); yield break; }
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

    private void ResetPlayer()
    {
        if (walking || cameraRig.IsRotating) return;
        cleared = false;
        PlacePlayer(level.Start);
        cameraRig.SetYawImmediate(level.StartYaw);
        yaw = lastYaw = level.StartYaw;
        steps = turns = 0;
        shownLinks.Clear();
        UpdateHud();
        AnubisToast.Show("처음으로 돌아간다.", 1.2f);
    }

    private IEnumerator Clear()
    {
        cleared = true;
        Flash(level.Goal, new Color(1f, 0.9f, 0.4f, 0.8f), 1.2f);
        SobekStory.SetFlag(clearFlag);
        Debug.Log($"[SobekIllusionBoard] {level.DisplayName} 클리어! 걸음 {steps} · 회전 {turns}", this);
        AnubisToast.Show(clearMessage, 4f);
        UpdateHud();
        onCleared?.Invoke();
        if (!returnAfterClear) yield break;
        yield return new WaitForSeconds(3f);
        yield return ScreenFader.FadeOut(1f);
        SceneFlow.LoadByNameOrNext(returnScene);
    }

    // ================= 강조 표시 =================
    private void FlashReachableLinks()
    {
        HashSet<Vector3Int> reach = graph.Reachable(cell, yaw);
        foreach (Vector3Int n in reach)
            foreach (SobekIllusionGraph.Edge e in graph.Neighbors(n, yaw))
            {
                if (!e.IsIllusion) continue;
                string key = $"{yaw}:{Min(n, e.To)}:{Max(n, e.To)}";
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
        if (level == null) return;
        var sb = new System.Text.StringBuilder();
        sb.Append("<b>").Append(level.DisplayName).Append("</b>\n");
        sb.Append("<size=80%>마우스 드래그 / <color=#FFD27A>Q E</color> 신전 돌리기 · <color=#FFD27A>클릭</color> 이동 · <color=#FFD27A>R</color> 처음으로</size>\n");
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
        SobekIllusionGraph g = level.BuildGraph();
        var sol = g.Solve(level.Start, level.Goal, level.StartYaw, out int states);
        if (sol == null) { Debug.LogWarning($"[SobekIllusionBoard] 정답 없음! (상태 {states}개)", this); return; }
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < sol.Count; i++)
        {
            var (c, y) = sol[i];
            if (i > 0 && sol[i - 1].cell == c) sb.Append($"\n  회전 -> {SobekIllusionGraph.YawDegrees(y)}°");
            else sb.Append($"\n  {c} ({SobekIllusionGraph.YawDegrees(y)}°)");
        }
        Debug.Log($"[SobekIllusionBoard] 정답 {sol.Count - 1}수 (상태 {states}개){sb}", this);
    }

    [ContextMenu("테스트: 목표 칸으로 순간 이동")]
    private void TestTeleportGoal()
    {
        if (!Application.isPlaying || graph == null) return;
        PlacePlayer(level.Goal);
        StartCoroutine(Clear());
    }

    [Header("기즈모")]
    [Tooltip("씬 뷰에 표시할 착시 길 각도 (0=45° 1=135° 2=225° 3=315°)")]
    [Range(0, 3)] [SerializeField] private int gizmoYaw = 2;

    // 씬 뷰: 블록 테두리, 걸을 수 있는 칸(초록 점), 고른 각도의 착시 길(하늘색 선), 시작/목표
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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Top(level.Start), 0.3f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Top(level.Goal), 0.3f);
    }

#if UNITY_EDITOR
    public void EditorSetup(SobekIllusionLevel lv, SobekIllusionCamera rig, Material block, Material marker, SpriteRenderer avatarRenderer, SpriteRenderer goalRenderer, TMP_FontAsset font)
    {
        level = lv; cameraRig = rig; blockMaterial = block; markerMaterial = marker; avatar = avatarRenderer; goalProp = goalRenderer; hudFont = font;
        gizmoYaw = lv != null ? lv.StartYaw : 2;
    }
#endif
}
