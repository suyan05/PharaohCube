using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// 모든 방 공통: 플레이어와 물건의 앞뒤를 "발밑 높이"로 자동 정렬 (레이어 순서를 하나하나 안 맞춰도 됨)
// - 플레이어와 같은 정렬 레이어(보통 Objects)에 있는 그림만 다룸. 다른 레이어(바닥, 벽, UI)는 그대로
// - 그림 아래쪽(발밑)이 화면에서 더 아래에 있을수록 앞에 그림 -> 뒤로 지나가기 / 앞으로 지나가기가 저절로 맞음
// - [수정] 붙어 있는 장식은 큰 물건에 "붙여서" 같이 정렬 (스핑크스 얼굴 위 눈빛, 벽 위 횃불, 벽 장식 등)
//   -> 장식 가운데가 더 큰 물건 그림 안에 있으면 그 물건을 따라가고, 둘 사이 원래 순서(위/아래)는 그대로
//   -> 막히는 충돌 상자가 있는 물건(진짜 서 있는 물건)이 주인이 되고, 빛/후광 같은 장식은 주인이 될 수 없음
// - [수정] 정렬되는 물건은 원래 순서 0 근처의 바닥 타일맵, 효과보다 항상 위 구간에 그림 (스핑크스가 바닥에 묻히던 문제)
// - 예외: 이름에 Floor/Ground/Rug/바닥 또는 순서 -5 이하 = 항상 아래 / 순서 20 이상 = 항상 위
//   그 밖의 예외는 그 오브젝트에 YSortHint를 붙여서 정함
// - 씬에 안 놓아도 씬이 열릴 때 자동으로 생김 (플레이어가 없는 씬은 아무것도 안 함)
public class YSortSystem : MonoBehaviour
{
    private const int SortedBase = 10000;       // 정렬 구간 가운데 (타일맵, 효과의 작은 번호보다 항상 위)
    private const int SortedMin = 1000;
    private const int SortedMax = 25000;
    private const int Step = 20;                // 높이 0.05칸마다 20칸 띄움 (붙은 장식이 들어갈 자리)
    private const int AttachRange = 9;          // 붙은 장식은 원래 순서 차이를 -9 ~ +9 안에서 유지
    // [수정] 바닥에 붙은 것(계단, 구멍, 갈라짐)은 바닥 타일(작은 번호)보다는 위, 정렬되는 물건(1000~)보다는 아래
    //        예전 -30000은 바닥 타일 밑으로 들어가서 싱크홀이 아예 안 보였음
    private const int BelowBase = 500;
    private const int AboveBase = 30000;
    private const int NormalMin = -4;
    private const int NormalMax = 19;
    private const float RescanSeconds = 1f;

    private enum Kind { Sorted, Below, Above }

    private class Entry
    {
        public Transform root;
        public SpriteRenderer main;
        public SortingGroup group;
        public Kind kind;
        public float footOffset;
        public int original;
        public bool isPlayer;
        public bool solid;                       // 막히는 충돌 상자가 있음 (실제로 서 있는 물건)
        public Entry anchor;                     // 붙어 있는 큰 물건 (없으면 자기 발밑으로)
    }

    // 전체 끄기 스위치: 문제가 생기면 false로 바꾸면 예전처럼 직접 정한 순서로 돌아감
    public static bool Enabled = true;

    private static YSortSystem instance;

    private readonly List<Entry> entries = new List<Entry>();
    private readonly HashSet<Transform> roots = new HashSet<Transform>();
    private int layerId;
    private bool ready;
    private float rescanTimer;

    public static bool Active => instance != null && instance.ready;

    // ================= 자동 생성 =================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!Enabled || mode != LoadSceneMode.Single || instance != null) return;
        new GameObject("YSortSystem (자동)").AddComponent<YSortSystem>();
    }

    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Start()
    {
        try
        {
            PlayerController2D player = FindFirstObjectByType<PlayerController2D>();
            SpriteRenderer sr = player != null ? player.GetComponentInChildren<SpriteRenderer>() : null;
            if (sr == null)
            {
                enabled = false; // 플레이어가 없는 씬 (타이틀 등)
                return;
            }
            layerId = sr.sortingLayerID;
            ready = true;
            Scan();
        }
        catch (Exception e)
        {
            Debug.LogError($"[YSortSystem] 시작 오류: {e}");
            enabled = false;
        }
    }

    // ================= 매 프레임 =================
    private void LateUpdate()
    {
        try
        {
            rescanTimer += Time.unscaledDeltaTime;
            if (rescanTimer >= RescanSeconds)
            {
                rescanTimer = 0f;
                Scan();
            }
            UpdateOrders();
        }
        catch (Exception e)
        {
            Debug.LogError($"[YSortSystem] 정렬 오류: {e}");
            enabled = false;
        }
    }

    private void UpdateOrders()
    {
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry e = entries[i];
            if (e.root == null || (e.kind == Kind.Sorted && e.group == null))
            {
                if (e.root != null) roots.Remove(e.root);
                entries.RemoveAt(i);
                continue;
            }
            if (e.kind == Kind.Sorted) e.group.sortingOrder = OrderOf(e);
        }
    }

    // 붙은 장식: 큰 물건 번호 + 원래 순서 차이 / 나머지: 자기 발밑 높이
    private static int OrderOf(Entry e)
    {
        Entry a = e.anchor != null && e.anchor.root != null ? e.anchor : null;
        if (a == null) return KeyAt(FootY(e));
        int diff = Mathf.Clamp(e.original - a.original, -AttachRange, AttachRange);
        return KeyAt(FootY(a)) + diff;
    }

    private static float FootY(Entry e)
    {
        if (e.main != null && e.main.enabled && e.main.sprite != null) return e.main.bounds.min.y + e.footOffset;
        return e.root.position.y + e.footOffset;
    }

    // 발밑 높이 -> 정렬 번호 (아래에 있을수록 큰 번호 = 앞)
    private static int KeyAt(float y)
    {
        return Mathf.Clamp(SortedBase - Mathf.RoundToInt(y * 20f) * Step, SortedMin, SortedMax);
    }

    // ================= 다른 스크립트용 =================
    // 연출 효과(먼지 등)를 만들 때: 그 위치 높이에 맞는 정렬 번호 (자동 정렬이 꺼져 있거나 다른 레이어면 원래 번호)
    public static int OrderFor(string layerName, int order, Vector3 worldPos)
    {
        if (!Active || order < NormalMin || order > NormalMax) return order;
        if (SortingLayer.NameToID(layerName) != instance.layerId) return order;
        return KeyAt(worldPos.y) + AttachRange + 1;
    }

    // ================= 찾기 / 등록 =================
    private void Scan()
    {
        bool added = false;
        foreach (SpriteRenderer sr in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (sr == null || sr.sortingLayerID != layerId) continue;
            Transform root = RootOf(sr.transform);
            if (root == null || roots.Contains(root)) continue;
            Register(root);
            added = true;
        }
        if (added) FindAnchors();
    }

    // 같은 레이어 그림이 있는 가장 바깥 부모 (그 위로는 그림이 없는 정리용 폴더)
    private Transform RootOf(Transform t)
    {
        Transform root = t;
        for (Transform p = t.parent; p != null; p = p.parent)
        {
            SpriteRenderer psr = p.GetComponent<SpriteRenderer>();
            if (psr == null || psr.sortingLayerID != layerId) continue;
            if (IsGroundName(psr.gameObject.name)) break; // 바닥 밑의 물건은 바닥과 따로
            root = p;
        }
        YSortHint hint = root.GetComponentInParent<YSortHint>();
        return hint != null && hint.mode == YSortHint.Mode.Ignore ? null : root;
    }

    private void Register(Transform root)
    {
        roots.Add(root);
        SpriteRenderer main = root.GetComponent<SpriteRenderer>();
        YSortHint hint = root.GetComponent<YSortHint>();
        var e = new Entry
        {
            root = root,
            main = main,
            kind = Classify(main, hint),
            footOffset = hint != null ? hint.footOffset : 0f,
            original = main != null ? main.sortingOrder : 0,
            isPlayer = root.GetComponentInChildren<PlayerController2D>() != null,
            solid = HasSolidCollider(root)
        };

        if (e.kind == Kind.Sorted)
        {
            e.group = root.GetComponent<SortingGroup>();
            if (e.group == null) e.group = root.gameObject.AddComponent<SortingGroup>();
            e.group.sortingLayerID = layerId;
        }
        else if (main != null)
        {
            // 항상 아래 / 항상 위: 그림 순서만 맨 아래(위) 구간으로 옮김 (원래 순서끼리의 차이는 유지)
            main.sortingOrder = (e.kind == Kind.Below ? BelowBase : AboveBase) + Mathf.Clamp(e.original, -400, 400);
        }
        entries.Add(e);
    }

    private static Kind Classify(SpriteRenderer main, YSortHint hint)
    {
        if (hint != null && hint.mode == YSortHint.Mode.Sort) return Kind.Sorted;
        if (hint != null && hint.mode == YSortHint.Mode.AlwaysBelow) return Kind.Below;
        if (hint != null && hint.mode == YSortHint.Mode.AlwaysAbove) return Kind.Above;
        if (main == null) return Kind.Sorted;

        if (main.GetComponent<PlayerController2D>() != null) return Kind.Sorted;
        if (main.sortingOrder < NormalMin || IsGroundName(main.gameObject.name)) return Kind.Below;
        if (main.sortingOrder > NormalMax) return Kind.Above;
        return Kind.Sorted;
    }

    private static bool HasSolidCollider(Transform root)
    {
        foreach (Collider2D c in root.GetComponentsInChildren<Collider2D>())
        {
            if (c != null && !c.isTrigger) return true;
        }
        return false;
    }

    private static bool IsGroundName(string n)
    {
        // [추가] 계단, 구멍, 그림자처럼 바닥에 붙은 것도 항상 아래 (스핑크스 계단 위에서 플레이어가 가려지던 문제)
        return n.Contains("Floor") || n.Contains("Ground") || n.Contains("Rug") || n.Contains("바닥")
            || n.Contains("Stairs") || n.Contains("Hole") || n.Contains("Pit") || n.Contains("Shadow") || n.Contains("Carpet")
            || n.Contains("Crack") || n.Contains("Decal"); // [추가] 발굴 현장 땅 갈라짐(구멍)도 바닥
    }

    // ================= 붙은 장식 찾기 =================
    // 내 그림 가운데가 다른 물건 그림 안에 있고, 그 물건이 나보다 크면 (같은 크기면 원래 순서가 더 아래인 쪽이 주인) 그 물건에 붙음
    private void FindAnchors()
    {
        foreach (Entry e in entries)
        {
            e.anchor = null;
            if (e.kind != Kind.Sorted || e.isPlayer || !HasBounds(e)) continue;

            Entry best = null;
            foreach (Entry other in entries)
            {
                if (!CanAnchor(e, other)) continue;
                if (best == null || Better(other, best)) best = other;
            }
            e.anchor = best;
        }
        foreach (Entry e in entries) e.anchor = TopAnchor(e);
    }

    // 막히는 물건 우선, 같으면 더 큰 쪽
    private static bool Better(Entry a, Entry b)
    {
        if (a.solid != b.solid) return a.solid;
        return Area(a) > Area(b);
    }

    private static bool CanAnchor(Entry e, Entry other)
    {
        if (other == e || other.kind != Kind.Sorted || other.isPlayer || !HasBounds(other)) return false;
        if (e.solid && !other.solid) return false;      // 서 있는 물건은 빛/장식에 붙지 않음
        Bounds b = other.main.bounds;
        Vector3 c = e.main.bounds.center;
        if (c.x < b.min.x || c.x > b.max.x || c.y < b.min.y || c.y > b.max.y) return false;

        float mine = Area(e), theirs = Area(other);
        if (other.solid && !e.solid) return true;        // 장식 -> 그 자리의 서 있는 물건
        if (theirs > mine * 1.05f) return true;
        return theirs >= mine * 0.95f && other.original < e.original; // 같은 크기: 아래 순서가 주인
    }

    // 붙은 물건이 또 다른 물건에 붙어 있으면 맨 위 주인까지 (순환 방지로 최대 8번)
    private static Entry TopAnchor(Entry e)
    {
        Entry a = e.anchor;
        for (int i = 0; i < 8 && a != null && a.anchor != null && a.anchor != e; i++) a = a.anchor;
        return a;
    }

    private static bool HasBounds(Entry e)
    {
        return e.main != null && e.main.sprite != null;
    }

    private static float Area(Entry e)
    {
        Vector3 s = e.main.bounds.size;
        return s.x * s.y;
    }
}
