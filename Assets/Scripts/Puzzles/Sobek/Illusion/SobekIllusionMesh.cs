using System.Collections.Generic;
using UnityEngine;

// 블록들을 메시 1개로 합침 (블록끼리 맞닿은 면은 빼서 가볍게)
// 면마다 밝기를 다르게 칠함 + 윗면은 테두리를 어둡게 -> 빛 없이도 칸이 또렷하게 보임
public static class SobekIllusionMesh
{
    public static Mesh Build(SobekIllusionGraph g, SobekIllusionLevel level)
    {
        var verts = new List<Vector3>();
        var cols = new List<Color>();
        var tris = new List<int>();

        foreach (Vector3Int b in g.Blocks)
        {
            Color side = ShadeByHeight(level.SideColor, level.MossColor, b.y);
            // 옆면 4개 (붙은 블록이 있으면 생략)
            Face(verts, cols, tris, g, b, new Vector3Int(1, 0, 0), side * 0.92f);
            Face(verts, cols, tris, g, b, new Vector3Int(-1, 0, 0), side * 0.74f);
            Face(verts, cols, tris, g, b, new Vector3Int(0, 0, 1), side * 0.82f);
            Face(verts, cols, tris, g, b, new Vector3Int(0, 0, -1), side * 0.64f);
            Face(verts, cols, tris, g, b, new Vector3Int(0, -1, 0), side * 0.45f);

            if (g.IsBlock(b + Vector3Int.up)) continue;
            Color top = level.TopColor;
            if (b == level.Start) top = level.StartColor;
            else if (b == level.Goal) top = level.GoalColor;
            else if (((b.x + b.z) & 1) == 0) top *= 0.96f; // 칸 구분용 체크 무늬
            top.a = 1f;
            TopFace(verts, cols, tris, b, top, top * 0.7f);
        }

        var mesh = new Mesh { name = "SobekIllusionBlocks" };
        if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetColors(cols);
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

    private static void Face(List<Vector3> v, List<Color> c, List<int> t, SobekIllusionGraph g, Vector3Int b, Vector3Int n, Color col)
    {
        if (g.IsBlock(b + n)) return;
        col.a = 1f;
        Vector3 center = b + (Vector3)n * 0.5f;
        Vector3 u, w;
        if (n.y != 0) { u = Vector3.right; w = Vector3.forward; }
        else if (n.x != 0) { u = Vector3.forward; w = Vector3.up; }
        else { u = Vector3.right; w = Vector3.up; }
        Quad(v, c, t, center - u * 0.5f - w * 0.5f, center + u * 0.5f - w * 0.5f, center + u * 0.5f + w * 0.5f, center - u * 0.5f + w * 0.5f, col, n);
    }

    // 윗면 = 안쪽 사각형 + 어두운 테두리 4조각
    private static void TopFace(List<Vector3> v, List<Color> c, List<int> t, Vector3Int b, Color inner, Color edge)
    {
        const float h = 0.5f, ins = 0.38f;
        Vector3 o = b + new Vector3(0f, h, 0f);
        Vector3 A = o + new Vector3(-h, 0, -h), B = o + new Vector3(h, 0, -h), C = o + new Vector3(h, 0, h), D = o + new Vector3(-h, 0, h);
        Vector3 a = o + new Vector3(-ins, 0, -ins), bb = o + new Vector3(ins, 0, -ins), cc = o + new Vector3(ins, 0, ins), d = o + new Vector3(-ins, 0, ins);
        edge.a = 1f;
        Vector3 up = Vector3.up;
        Quad(v, c, t, a, bb, cc, d, inner, up);
        Quad(v, c, t, A, B, bb, a, edge, up);
        Quad(v, c, t, B, C, cc, bb, edge, up);
        Quad(v, c, t, C, D, d, cc, edge, up);
        Quad(v, c, t, D, A, a, d, edge, up);
    }

    // 네 점으로 사각형 (법선 n 쪽이 앞면이 되도록 감는 방향을 맞춤)
    private static void Quad(List<Vector3> v, List<Color> c, List<int> t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Color col, Vector3 n)
    {
        int i = v.Count;
        v.Add(p0); v.Add(p1); v.Add(p2); v.Add(p3);
        c.Add(col); c.Add(col); c.Add(col); c.Add(col);
        // 유니티: Cross(b-a, c-a)가 보는 사람 쪽을 향하면 앞면
        bool keep = Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), n) > 0f;
        if (keep) { t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3); }
        else { t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2); }
    }

    // 물 (큰 사각형 1장)
    public static Mesh Water(Vector3 center, float y, float size, Color col)
    {
        var mesh = new Mesh { name = "SobekIllusionWater" };
        var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
        Vector3 o = new Vector3(center.x, y, center.z);
        Color far = col * 0.7f; far.a = 1f; col.a = 1f;
        int i = 0;
        // 가운데가 밝고 가장자리가 어두운 4조각
        Vector3[] ring = { new Vector3(-1, 0, -1), new Vector3(1, 0, -1), new Vector3(1, 0, 1), new Vector3(-1, 0, 1) };
        v.Add(o); c.Add(col);
        foreach (Vector3 r in ring) { v.Add(o + r * size); c.Add(far); }
        for (int k = 0; k < 4; k++)
        {
            int a = 1 + k, b = 1 + (k + 1) % 4;
            // 위에서 볼 때 시계 방향
            t.Add(i); t.Add(b); t.Add(a);
        }
        mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
        return mesh;
    }

    // 강조 표시용 얇은 사각형 (윗면 바로 위)
    public static Mesh Marker()
    {
        var mesh = new Mesh { name = "SobekIllusionMarker" };
        const float h = 0.46f;
        mesh.SetVertices(new List<Vector3> { new Vector3(-h, 0, -h), new Vector3(h, 0, -h), new Vector3(h, 0, h), new Vector3(-h, 0, h) });
        mesh.SetColors(new List<Color> { Color.white, Color.white, Color.white, Color.white });
        mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
