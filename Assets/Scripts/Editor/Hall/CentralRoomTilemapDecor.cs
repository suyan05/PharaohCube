using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// [추가] 중앙 홀(타일맵) 꾸미기 - 너무 휑하던 홀 (CentralRoomTilemapSetup이 부름, 여러 번 눌러도 한 번만 만듦)
// - 벽 횃대 8개: 청동 횃대 + 움직이는 불꽃(HorusPixelFlame). 홀 첫 연출(F_HALL_TEMP_INTRO)이 끝나야 불이 붙음
// - 기둥 4개: 가운데 막힌 방(아누비스) 네 귀퉁이
// - 바닥: 모래 물결, 토기 조각, 자갈 (길은 안 막음)
// - 신전 문마다 그 신의 색 빛 (열린 문만 켜짐, HallDoorBoard)
// 위치가 마음에 안 들면 HallDecor 아래 오브젝트를 끌어서 옮기면 됨
public static class CentralRoomTilemapDecor
{
    private const string Root = "HallDecor";
    private const string Fx = "Horus/PixelFx/";
    private const string PillarPath = "Assets/image/Hall/World/Hall_Obj_Pillar.png";
    private const string IntroFlag = "F_HALL_TEMP_INTRO";

    // 벽 칸 바로 아래가 바닥인 곳 (타일맵에서 계산한 값)
    private static readonly Vector2[] Sconces =
    {
        new Vector2(-3.5f, 13.4f), new Vector2(3.5f, 13.4f),     // 큐브 복도 끝
        new Vector2(-6.5f, 10.4f), new Vector2(6.5f, 10.4f),     // 복도 양쪽 비스듬한 벽
        new Vector2(-14.5f, 5.4f), new Vector2(14.5f, 5.4f),     // 바스테트 / 토트 문 위
        new Vector2(-14.5f, -2.6f), new Vector2(14.5f, -2.6f)    // 호루스 / 소베크 문 위
    };

    private static readonly Vector2[] Pillars =
    {
        new Vector2(-4.5f, 5.6f), new Vector2(3.5f, 5.6f), new Vector2(-4.5f, -0.6f), new Vector2(3.5f, -0.6f)
    };

    private static readonly (string art, Vector2 pos, bool flip)[] Decals =
    {
        ("Ripple", new Vector2(-1.5f, -10.6f), false), ("Ripple", new Vector2(1.2f, -11.4f), true),
        ("Ripple", new Vector2(-9.5f, -6.2f), false), ("Ripple", new Vector2(9.8f, 1.6f), true),
        ("Shards", new Vector2(-11.2f, 6.4f), false), ("Shards", new Vector2(10.6f, -6.4f), true),
        ("Pebbles_A", new Vector2(-6.2f, -7.4f), false), ("Pebbles_B", new Vector2(7.4f, 7.6f), true),
        ("Pebbles_A", new Vector2(12.2f, 3.8f), false), ("Pebbles_B", new Vector2(-12.6f, -4.4f), true)
    };

    public static void Build(Transform player, StringBuilder log)
    {
        if (GameObject.Find(Root) != null)
        {
            log.AppendLine("- 홀 장식: 이미 있음 (다시 만들려면 HallDecor 지우고 메뉴 실행)");
            return;
        }
        var root = new GameObject(Root).transform;
        SpriteRenderer reference = player != null ? player.GetComponentInChildren<SpriteRenderer>() : null;
        string layer = reference != null ? reference.sortingLayerName : "Default";
        int order = reference != null ? reference.sortingOrder : 0;
        int n = 0;
        foreach (Vector2 p in Sconces) n += MakeSconce(root, p, layer, order) ? 1 : 0;
        foreach (Vector2 p in Pillars) MakePillar(root, p, layer, order);
        foreach (var d in Decals) MakeDecal(root, d.art, d.pos, d.flip, layer, order - 1);
        log.AppendLine($"- 홀 장식: 벽 횃대 {n}개, 기둥 {Pillars.Length}개, 바닥 장식 {Decals.Length}개");
    }

    private static bool MakeSconce(Transform root, Vector2 pos, string layer, int order)
    {
        Sprite sconce = Resources.Load<Sprite>(Fx + "HO_Sconce_Base");
        if (sconce == null) return false;
        SpriteRenderer sr = MakeSprite(root, "Sconce", sconce, pos, layer, order);
        var glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(sr.transform, false);
        glowGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        Light2D glow = glowGo.AddComponent<Light2D>();
        glow.lightType = Light2D.LightType.Point;
        glow.color = new Color(1f, 0.72f, 0.38f);
        glow.intensity = 0.9f;
        glow.pointLightOuterRadius = 3.2f;
        HorusPixelFlame flame = sr.gameObject.AddComponent<HorusPixelFlame>();
        var so = new SerializedObject(flame);
        SetFrames(so.FindProperty("frames"));
        so.FindProperty("baseSprite").objectReferenceValue = sconce;
        so.FindProperty("litFlag").stringValue = IntroFlag;
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    private static void SetFrames(SerializedProperty frames)
    {
        frames.arraySize = 8;
        for (int i = 0; i < 8; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = Resources.Load<Sprite>($"{Fx}HO_Flame_{i}");
    }

    private static void MakePillar(Transform root, Vector2 pos, string layer, int order)
    {
        Sprite pillar = LoadFirstSprite(PillarPath);
        if (pillar == null) return;
        SpriteRenderer sr = MakeSprite(root, "Pillar", pillar, pos, layer, order);
        BoxCollider2D box = sr.gameObject.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, 0.4f);
        box.offset = new Vector2(0f, 0.2f); // 기둥 밑동만 막음
    }

    private static void MakeDecal(Transform root, string art, Vector2 pos, bool flip, string layer, int order)
    {
        Sprite s = Resources.Load<Sprite>($"{Fx}DG_{art}");
        if (s == null) return;
        MakeSprite(root, "Decal_" + art, s, pos, layer, order).flipX = flip;
    }

    private static SpriteRenderer MakeSprite(Transform root, string name, Sprite sprite, Vector2 pos, string layer, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return sr;
    }

    // ================= 문 빛 =================
    private static readonly Color[] GodColors =
    {
        new Color(1f, 0.7f, 0.3f), new Color(0.55f, 0.9f, 0.6f), new Color(0.75f, 0.8f, 1f),
        new Color(0.45f, 0.65f, 1f), new Color(0.35f, 0.85f, 0.8f)
    };

    // 문 아래 'Glow' 빛 (없을 때만 만듦). index: 0 라, 1 바스테트, 2 토트, 3 호루스, 4 소베크
    public static void AddDoorGlow(SceneDoor door, int index)
    {
        if (door == null || door.transform.Find("Glow") != null) return;
        var go = new GameObject("Glow");
        go.transform.SetParent(door.transform, false);
        Light2D l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = GodColors[Mathf.Clamp(index, 0, GodColors.Length - 1)];
        l.intensity = 1.1f;
        l.pointLightOuterRadius = 3f;
        go.SetActive(false); // HallDoorBoard가 열린 문만 켬
    }

    private static Sprite LoadFirstSprite(string path)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path)) if (o is Sprite s) return s;
        return null;
    }
}
