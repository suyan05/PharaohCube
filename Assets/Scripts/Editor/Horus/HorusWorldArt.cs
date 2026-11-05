using UnityEditor;
using UnityEngine;

// 방 안 물건(퍼즐 스테이션, 비석, 문, 기둥)에 그림 넣기 (HorusArtSetup이 부름)
// - 그림은 정한 크기(월드 칸)에 맞게 비율 그대로 줄임
// - 충돌 상자는 예전 월드 크기 그대로 둠 -> 상호작용 거리, 막히는 범위가 안 바뀜
// - 호루스 전용 방 그림이 아직 없어서: 퍼즐 그림을 작게 쓰고, 비석/문/기둥은 라의 방 그림을 빌림
public static class HorusWorldArt
{
    private const string Slate = "Assets/image/Test/Object/Slate.png";
    private const string Door = "Assets/image/Test/Object/Door_top.png";
    private const string Pillar = "Assets/image/Hall/World/Hall_Obj_Pillar.png"; // 중앙 홀과 같은 돌기둥
    public const string MuralMosaic = "H4/H4_03_Mosaic";                        // 벽화를 픽셀로 뭉갠 흐릿한 모자이크 (E로 보면 원본)
    public static readonly Vector2 MuralSize = new Vector2(2.8f, 0.84f);
    private static readonly Color Lapis = new Color(0.55f, 0.7f, 1f, 1f);

    public static int Apply()
    {
        int n = 0;
        n += Fit("H2_ConstellationChart", "H2/H2_01", new Vector2(2.2f, 2.2f));
        n += Fit("H1_MirrorWing", "H1/H1_01", new Vector2(2.8f, 2.24f));
        n += Fit("H3_WedjatRestore", "H3/H3_01", new Vector2(2.4f, 1.8f));
        n += Fit("H4_StarPathLock", "H4/H4_01", new Vector2(2.8f, 2.8f));
        n += Fit("H5_SkyAlign", "H5/H5_01", new Vector2(3.2f, 2.4f));
        n += Fit("OBJ_H08_Mural", MuralMosaic, MuralSize);
        n += Fit("OBJ_H04_MoonEyeMount", "Object/Object_02", new Vector2(0.8f, 0.8f));
        n += Fit("OBJ_H02_GuideStone", Slate, new Vector2(0.9f, 1.3f));
        n += Fit("OBJ_H11_WarningSlab", Slate, new Vector2(1f, 1.4f));
        n += Fit("OBJ_H13_NorthDoor", Door, new Vector2(2.4f, 2f));
        foreach (string p in new[] { "Pillar_NW", "Pillar_NE", "Pillar_SW", "Pillar_SE" }) n += Fit(p, Pillar, new Vector2(0.9f, 2.7f));
        n += LapisStars();
        return n;
    }

    // 청금석 별 3개 (3번은 테스트 씬에서 길 칸 크기로 잘못 들어가 있어서 기획서 자리로 되돌림)
    private static int LapisStars()
    {
        GameObject third = HorusArtSetup.Find("OBJ_H16_LapisStar_3");
        if (third != null) third.transform.position = new Vector3(16.3f, 11.2f, 0f);
        int n = 0;
        for (int i = 1; i <= 3; i++) n += Fit($"OBJ_H16_LapisStar_{i}", "H2/H2_star_on", new Vector2(0.6f, 0.6f), Lapis);
        return n;
    }

    public static int Fit(string objName, string art, Vector2 artSize) => Fit(objName, art, artSize, Color.white);

    public static int Fit(string objName, string art, Vector2 artSize, Color tint)
    {
        GameObject go = HorusArtSetup.Find(objName);
        if (go == null)
        {
            Debug.LogWarning($"[HorusWorldArt] '{objName}' 오브젝트 없음 - 건너뜀");
            return 0;
        }
        return FitObject(go, HorusArtSetup.Art(art), artSize, tint) ? 1 : 0;
    }

    public static bool FitObject(GameObject go, Sprite sprite, Vector2 artSize, Color tint)
    {
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null || sprite == null) return false;
        Vector2 colliderWorld = ColliderWorldSize(go);
        Vector2 b = sprite.bounds.size;
        float k = Mathf.Min(artSize.x / b.x, artSize.y / b.y);
        sr.sprite = sprite;
        sr.color = tint;
        sr.drawMode = SpriteDrawMode.Simple;
        go.transform.localScale = new Vector3(k, k, 1f);
        KeepCollider(go, colliderWorld, k);
        EditorUtility.SetDirty(go);
        return true;
    }

    private static Vector2 ColliderWorldSize(GameObject go)
    {
        Vector3 s = go.transform.lossyScale;
        Vector2 scale = new Vector2(Mathf.Abs(s.x), Mathf.Abs(s.y));
        if (go.TryGetComponent(out BoxCollider2D box)) return Vector2.Scale(box.size, scale);
        if (go.TryGetComponent(out CircleCollider2D circle))
        {
            float d = circle.radius * 2f * Mathf.Max(scale.x, scale.y);
            return new Vector2(d, d);
        }
        return Vector2.zero;
    }

    // 부모 크기는 1이라고 봄 (Interactables / Puzzles / Environment 모두 크기 1)
    private static void KeepCollider(GameObject go, Vector2 world, float k)
    {
        if (world == Vector2.zero || k <= 0f) return;
        if (go.TryGetComponent(out BoxCollider2D box))
        {
            box.size = world / k;
            box.offset = Vector2.zero;
        }
        if (go.TryGetComponent(out CircleCollider2D circle)) circle.radius = world.x / 2f / k;
    }
}
