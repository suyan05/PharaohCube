using System;
using System.Collections.Generic;
using UnityEngine;

// 블록들을 메시 1개로 합침 (블록끼리 맞닿은 면은 빼서 가볍게)
// 면마다 밝기를 다르게 칠함 + 윗면은 테두리를 어둡게 -> 빛 없이도 칸이 또렷하게 보임
// [추가] 무늬 그림(아틀라스) 좌표(UV): 그림 한 장에 32x32 칸 8개가 가로로 붙어 있음
//        0 돌 윗면 / 1 돌 옆면 / 2 통나무 뗏목 윗면 / 3 통나무 옆면 / 4 악어 등 윗면 / 5 악어 옆면 / 6 발판(새김 무늬) / 7 예비
// [추가] 얇은 블록 (뗏목): bottom을 -0.5보다 크게 주면 윗면은 그대로, 아래만 얇아짐
public static class SobekIllusionMesh
{
    public const int AtlasSlots = 8;
    public const int SlotStoneTop = 0, SlotStoneSide = 1, SlotLogTop = 2, SlotLogSide = 3, SlotCrocTop = 4, SlotCrocSide = 5, SlotPlate = 6;
    private const float Inset = 0.5f / 32f; // 점 필터에서 옆 칸 색이 번지지 않게 반 픽셀 안쪽

    // 윗면 색을 칸마다 정하는 함수 (inner = 가운데, edge = 테두리)
    public delegate void TopStyle(Vector3Int cell, ref Color inner, ref Color edge);

    // 블록 그림 칸 (윗면·옆면). topOf: 칸마다 윗면 그림을 바꿀 때 (발판 등, -1이면 기본)
    public struct Skin
    {
        public int top, side;
        public Func<Vector3Int, int> topOf;
        public static Skin Stone => new Skin { top = SlotStoneTop, side = SlotStoneSide };
    }

    // cells: 이 메시에 들어갈 블록 (절대 좌표), origin: 메시 원점 (장치는 회전축/처음 자리)
    // tint/tintAmount: 장치 블록은 옆면을 장치 색으로 살짝 물들임
    public static Mesh Build(ICollection<Vector3Int> cells, Vector3 origin, SobekIllusionLevel level, Color tint, float tintAmount, TopStyle style)
        => Build(cells, origin, level, tint, tintAmount, style, Skin.Stone, -0.5f);

    // [추가] skin = 무늬 칸, bottom = 블록 아래 끝 (-0.5 = 정육면체, -0.1 = 얇은 판)
    public static Mesh Build(ICollection<Vector3Int> cells, Vector3 origin, SobekIllusionLevel level, Color tint, float tintAmount, TopStyle style, Skin skin, float bottom)
    {
        var set = cells as HashSet<Vector3Int> ?? new HashSet<Vector3Int>(cells);
        var verts = new List<Vector3>();
        var cols = new List<Color>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        bottom = Mathf.Clamp(bottom, -0.5f, 0.4f);

        foreach (Vector3Int b in set)
        {
            Color side = Color.Lerp(ShadeByHeight(level.SideColor, level.MossColor, b.y), tint, tintAmount);
            Vector3 p = b - origin;
            Side(verts, cols, uvs, tris, set, b, p, new Vector3Int(1, 0, 0), side * 0.92f, skin.side, bottom);
            Side(verts, cols, uvs, tris, set, b, p, new Vector3Int(-1, 0, 0), side * 0.74f, skin.side, bottom);
            Side(verts, cols, uvs, tris, set, b, p, new Vector3Int(0, 0, 1), side * 0.82f, skin.side, bottom);
            Side(verts, cols, uvs, tris, set, b, p, new Vector3Int(0, 0, -1), side * 0.64f, skin.side, bottom);
            if (!set.Contains(b + Vector3Int.down) || bottom > -0.5f)
            {
                Color under = side * 0.45f; under.a = 1f;
                Vector3 o = p + new Vector3(0f, bottom, 0f);
                Quad(verts, cols, uvs, tris, o + new Vector3(-0.5f, 0, -0.5f), o + new Vector3(0.5f, 0, -0.5f), o + new Vector3(0.5f, 0, 0.5f), o + new Vector3(-0.5f, 0, 0.5f),
                    under, Vector3.down, skin.side, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            }

            if (set.Contains(b + Vector3Int.up)) continue;
            Color top = level.TopColor;
            if (((b.x + b.z) & 1) == 0) top *= 0.96f; // 칸 구분용 체크 무늬
            Color edge = top * 0.7f;
            style?.Invoke(b, ref top, ref edge);
            top.a = 1f;
            int slot = skin.topOf != null ? skin.topOf(b) : -1;
            TopFace(verts, cols, uvs, tris, p, top, edge, slot >= 0 ? slot : skin.top);
        }

        var mesh = new Mesh { name = "SobekIllusionBlocks" };
        if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetColors(cols);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // 낮은 블록일수록 이끼 색 (물가 느낌)
    private static Color ShadeByHeight(Color side, Color moss, int y)
    {
        float t = Mathf.Clamp01(1f - (y + 1) / 3f) * 0.55f;
        Color c = Color.Lerp(side, moss, t);
        c.a = 1f;
        return c;
    }

    // 아틀라스 칸 slot 안의 (u, v) 0~1 -> 실제 UV
    private static Vector2 AtlasUV(int slot, Vector2 uv)
    {
        float u = Mathf.Lerp(Inset, 1f - Inset, Mathf.Clamp01(uv.x));
        float v = Mathf.Lerp(Inset, 1f - Inset, Mathf.Clamp01(uv.y));
        return new Vector2((slot + u) / AtlasSlots, v);
    }

    // 옆면 (위 0.5 ~ 아래 bottom). 그림은 위쪽이 칸 위, 얇으면 아래를 잘라 씀
    private static void Side(List<Vector3> v, List<Color> c, List<Vector2> uv, List<int> t, HashSet<Vector3Int> set, Vector3Int b, Vector3 local, Vector3Int n, Color col, int slot, float bottom)
    {
        if (set.Contains(b + n)) return;
        col.a = 1f;
        Vector3 center = local + (Vector3)n * 0.5f;
        Vector3 u = n.x != 0 ? Vector3.forward : Vector3.right;
        float vb = Mathf.InverseLerp(0.5f, -0.5f, bottom); // 그림 세로 중 쓸 부분 (위에서부터)
        Vector3 lo = Vector3.up * bottom, hi = Vector3.up * 0.5f;
        Quad(v, c, uv, t,
            center - u * 0.5f + lo, center + u * 0.5f + lo, center + u * 0.5f + hi, center - u * 0.5f + hi, col, n, slot,
            new Vector2(0, 1f - vb), new Vector2(1, 1f - vb), new Vector2(1, 1), new Vector2(0, 1));
    }

    // 윗면 = 안쪽 사각형 + 어두운 테두리 4조각 (그림은 한 칸에 한 장)
    private static void TopFace(List<Vector3> v, List<Color> c, List<Vector2> uv, List<int> t, Vector3 local, Color inner, Color edge, int slot)
    {
        const float h = 0.5f, ins = 0.38f;
        Vector3 o = local + new Vector3(0f, h, 0f);
        Vector3 A = o + new Vector3(-h, 0, -h), B = o + new Vector3(h, 0, -h), C = o + new Vector3(h, 0, h), D = o + new Vector3(-h, 0, h);
        Vector3 a = o + new Vector3(-ins, 0, -ins), bb = o + new Vector3(ins, 0, -ins), cc = o + new Vector3(ins, 0, ins), d = o + new Vector3(-ins, 0, ins);
        edge.a = 1f;
        Vector3 up = Vector3.up;
        Vector2 U(Vector3 p) => new Vector2(p.x - o.x + 0.5f, p.z - o.z + 0.5f);
        Quad(v, c, uv, t, a, bb, cc, d, inner, up, slot, U(a), U(bb), U(cc), U(d));
        Quad(v, c, uv, t, A, B, bb, a, edge, up, slot, U(A), U(B), U(bb), U(a));
        Quad(v, c, uv, t, B, C, cc, bb, edge, up, slot, U(B), U(C), U(cc), U(bb));
        Quad(v, c, uv, t, C, D, d, cc, edge, up, slot, U(C), U(D), U(d), U(cc));
        Quad(v, c, uv, t, D, A, a, d, edge, up, slot, U(D), U(A), U(a), U(d));
    }

    // 네 점으로 사각형 (법선 n 쪽이 앞면이 되도록 감는 방향을 맞춤)
    private static void Quad(List<Vector3> v, List<Color> c, List<Vector2> uv, List<int> t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Color col, Vector3 n,
        int slot, Vector2 u0, Vector2 u1, Vector2 u2, Vector2 u3)
    {
        int i = v.Count;
        v.Add(p0); v.Add(p1); v.Add(p2); v.Add(p3);
        c.Add(col); c.Add(col); c.Add(col); c.Add(col);
        uv.Add(AtlasUV(slot, u0)); uv.Add(AtlasUV(slot, u1)); uv.Add(AtlasUV(slot, u2)); uv.Add(AtlasUV(slot, u3));
        // 유니티: Cross(b-a, c-a)가 보는 사람 쪽을 향하면 앞면
        bool keep = Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), n) > 0f;
        if (keep) { t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3); }
        else { t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2); }
    }

    // 물 (큰 사각형 1장, 높이는 오브젝트 위치로 움직임)
    public static Mesh Water(Vector3 center, float size, Color col) => Water(center, size, col, 8f);

    // [추가] 둥근 연못 (반지름 size). tile = 물결 그림 한 장이 덮는 칸 수 (UV가 세상 좌표를 따라감 -> 섬이 돌아도 물결은 제자리)
    // 가운데 밝음 -> 가장자리 어두움 -> 맨 바깥 돌 테두리 (바깥은 배경 그림)
    public static Mesh Water(Vector3 center, float size, Color col, float tile)
    {
        const int seg = 64;
        var mesh = new Mesh { name = "SobekIllusionWater" };
        var v = new List<Vector3>(); var c = new List<Color>(); var uv = new List<Vector2>(); var t = new List<int>();
        Vector3 o = new Vector3(center.x, 0f, center.z);
        col.a = 1f;
        Color mid = col * 0.82f; mid.a = 1f;
        Color far = col * 0.55f; far.a = 1f;
        Color rim = new Color(0.30f, 0.24f, 0.17f, 1f);
        Color rimOut = new Color(0.16f, 0.12f, 0.09f, 1f);
        tile = Mathf.Max(0.5f, tile);
        float[] radii = { 0.6f, 0.97f, 0.97f, 1.04f };       // 안쪽 고리, 물 끝, 테두리 시작(같은 자리, 색만 바뀜), 테두리 끝
        Color[] rc = { mid, far, rim, rimOut };

        v.Add(o); c.Add(col); uv.Add(new Vector2(o.x, o.z) / tile);
        for (int r = 0; r < radii.Length; r++)
            for (int k = 0; k < seg; k++)
            {
                float a = k * Mathf.PI * 2f / seg;
                Vector3 p = o + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * size * radii[r];
                v.Add(p); c.Add(rc[r]); uv.Add(new Vector2(p.x, p.z) / tile);
            }
        // 앞면(위에서 보이는 면)이 되도록 감는 방향을 법선으로 맞춤
        void Tri(int a, int b, int d)
        {
            if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[d] - v[a]), Vector3.up) > 0f) { t.Add(a); t.Add(b); t.Add(d); }
            else { t.Add(a); t.Add(d); t.Add(b); }
        }
        for (int k = 0; k < seg; k++) Tri(0, 1 + k, 1 + (k + 1) % seg);
        for (int r = 0; r + 1 < radii.Length; r++)
        {
            if (Mathf.Approximately(radii[r], radii[r + 1])) continue; // 색만 바뀌는 자리
            int i0 = 1 + r * seg, i1 = 1 + (r + 1) * seg;
            for (int k = 0; k < seg; k++)
            {
                int a0 = i0 + k, b0 = i0 + (k + 1) % seg, a1 = i1 + k, b1 = i1 + (k + 1) % seg;
                Tri(a0, a1, b1);
                Tri(a0, b1, b0);
            }
        }
        mesh.SetVertices(v); mesh.SetColors(c); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
        return mesh;
    }

    // 강조 표시용 얇은 사각형 (윗면 바로 위)
    public static Mesh Marker()
    {
        var mesh = new Mesh { name = "SobekIllusionMarker" };
        const float h = 0.46f;
        mesh.SetVertices(new List<Vector3> { new Vector3(-h, 0, -h), new Vector3(h, 0, -h), new Vector3(h, 0, h), new Vector3(-h, 0, h) });
        mesh.SetColors(new List<Color> { Color.white, Color.white, Color.white, Color.white });
        mesh.SetUVs(0, new List<Vector2> { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero });
        mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
