using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 호루스 타일맵 씬에 새로 놓는 물건 (HorusRoomBuilder가 부름). 위치는 작업 도면 그대로
// - 입구 전실: 신화 책 독서대(HA1), 횃불 제단(HA2)
// - 본당: 별화로 3개(HL1~3, 횃불로 켜는 불), 매 석상 2개(H12)
// - 매의 회랑(숨은 공간): 막힌 벽(HG0), 히든 자리 표시(HG1), 파피루스 조각 자리(HG2)
// 아트가 없는 것은 기본 도형(사각형, 원) + 색으로 임시 제작 -> 나중에 그림만 바꾸면 됨
public static class HorusRoomObjects
{
    private const string TorchAltarPrefab = "Assets/Prefabs/Common/TorchAltar.prefab";
    private const string BrazierOff = "Assets/image/Hall/World/Hall_Obj_Brazier_Off.png";
    private const string BrazierOn = "Assets/image/Hall/World/Hall_Obj_Brazier_On.png";

    private static Sprite square, circle;
    private static string layer;

    public static void Build(Sprite squareSprite, Sprite circleSprite, string sortingLayer)
    {
        square = squareSprite;
        circle = circleSprite;
        layer = sortingLayer;
        Transform root = new GameObject("HorusRoom").transform;
        BuildAntechamber(root);
        Brazier(root, "OBJ_HL1_StarBrazier", new Vector2(1.5f, 14.5f), "HO_1");
        Brazier(root, "OBJ_HL2_StarBrazier", new Vector2(22.5f, 14.5f), "HO_2");
        Brazier(root, "OBJ_HL3_StarBrazier", new Vector2(1.5f, 1.5f), "HO_3");
        BuildStatues(root);
        BuildGallery(root);
    }

    // ================= 입구 전실 =================
    private static void BuildAntechamber(Transform root)
    {
        GameObject book = Shape(root, "OBJ_HA1_MythBook", new Vector2(9.5f, -4f), square, new Vector2(0.9f, 0.7f), new Color(0.55f, 0.4f, 0.24f));
        book.AddComponent<BoxCollider2D>();
        Inspect(book, "HO_MYTH_BOOK", "신화 책 읽기", MythLines());

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TorchAltarPrefab);
        if (prefab == null)
        {
            Debug.LogWarning($"[HorusRoomObjects] 횃불 제단 프리팹 없음: {TorchAltarPrefab}");
            return;
        }
        var altar = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
        altar.name = "OBJ_HA2_TorchAltar";
        altar.transform.position = new Vector3(14.5f, -4f, 0f);
    }

    // 스토리 기획서 5.4 신화 책 '호루스와 세트'. 오시리스·이시스가 호루스의 부모라는 걸 여기서 알려줌 (H5 노트 4·5와 이어짐)
    private static string[,] MythLines()
    {
        return new[,]
        {
            { "", "신화 책 - 호루스와 세트" },
            { "", "오시리스는 이집트를 다스린 선한 왕이었다. 오시리스와 이시스 사이에서 아들 호루스가 태어났다." },
            { "", "동생 세트는 형 오시리스를 죽이고 왕좌를 빼앗았다." },
            { "", "호루스는 왕좌를 두고 세트와 싸우다 한쪽 눈을 잃었고, 토트가 그 눈을 고쳐 주었다. 고친 눈이 우자트다." }
        };
    }

    // ================= 별화로 (WallTorch: 내 횃불로 E 길게 -> 켜짐 -> 주변이 밝아짐) =================
    private static void Brazier(Transform root, string objName, Vector2 pos, string torchId)
    {
        Sprite off = HorusArtSetup.Art(BrazierOff);
        GameObject go = Shape(root, objName, pos, off != null ? off : circle, Vector2.one, Color.white);
        go.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 0.6f);
        Light2D light = PointLight(go.transform, new Color(1f, 0.75f, 0.45f), 4.5f);
        WallTorch torch = go.AddComponent<WallTorch>();
        var so = new SerializedObject(torch);
        HorusArtSetup.Prop(so, "torchId").stringValue = torchId;
        HorusArtSetup.Prop(so, "target").objectReferenceValue = go.GetComponent<SpriteRenderer>();
        HorusArtSetup.Prop(so, "offSprite").objectReferenceValue = off;
        HorusArtSetup.Prop(so, "onSprite").objectReferenceValue = HorusArtSetup.Art(BrazierOn);
        HorusArtSetup.Prop(so, "lightSource").objectReferenceValue = light;
        HorusArtSetup.Prop(so, "dayAutoFlag").stringValue = "";
        HorusArtSetup.Prop(so, "nightAutoFlag").stringValue = "";
        HorusArtSetup.Prop(so, "nightFlag").stringValue = "F_HO_NO_NIGHT"; // 호루스 방엔 낮/밤이 없음 (아무도 안 켜는 플래그)
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Light2D PointLight(Transform parent, Color color, float radius)
    {
        var go = new GameObject("Light", typeof(Light2D));
        go.transform.SetParent(parent, false);
        Light2D light = go.GetComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = 0f; // 꺼진 상태로 시작 (WallTorch가 켬)
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = 0.4f;
        return light;
    }

    // ================= 매 석상 (기획서 OBJ_H12). 왼쪽 = 한쪽 눈이 빈 석상 (우자트를 끼우는 곳) =================
    private static void BuildStatues(Transform root)
    {
        Sprite falcon = HorusArtSetup.Art("H1/H1_03");
        Color stone = new Color(0.85f, 0.78f, 0.62f);
        GameObject left = Statue(root, "OBJ_H12_FalconStatue_L", new Vector2(9.5f, 12.8f), falcon, stone);
        Statue(root, "OBJ_H12_FalconStatue_R", new Vector2(14.5f, 12.8f), falcon, stone);
        // [확인 필요] 우자트(토트의 방 보상)를 끼우면 회랑이 열리는 연출은 토트 방이 붙은 뒤에 만듦. 지금은 조사 대사만
        Inspect(left, "HO_FALCON_STATUE", "매 석상 조사", new[,] { { "나", "매 석상의 한쪽 눈이 비어 있다. 무언가를 끼울 수 있을 것 같다." } });
    }

    // 그림 크기를 먼저 맞추고 충돌 상자를 붙임 (붙일 때 그림 크기에 자동으로 맞음)
    private static GameObject Statue(Transform root, string objName, Vector2 pos, Sprite art, Color tint)
    {
        GameObject go = Shape(root, objName, pos, art != null ? art : square, Vector2.one, tint);
        if (art != null) HorusWorldArt.FitObject(go, art, new Vector2(1.4f, 1f), tint);
        go.AddComponent<BoxCollider2D>();
        return go;
    }

    // ================= 매의 회랑 (숨은 공간) =================
    private static void BuildGallery(Transform root)
    {
        // 막힌 벽: 벽 윗면 그림을 1 x 2칸으로 늘림 + 본당 쪽에 금색 선 (조사하면 열리는 벽 표시)
        Sprite wallTop = HorusRoomMap.WallTopSprite();
        Sprite art = wallTop != null ? wallTop : square;
        GameObject wall = Shape(root, "OBJ_HG0_HiddenWall", new Vector2(-0.5f, 8f), art, Vector2.one, new Color(0.9f, 0.82f, 0.66f));
        Vector2 b = art.bounds.size;
        wall.transform.localScale = new Vector3(1f / b.x, 2f / b.y, 1f);
        wall.AddComponent<BoxCollider2D>();
        Shape(root, "OBJ_HG0_GoldEdge", new Vector2(-0.04f, 8f), square, new Vector2(0.08f, 2f), new Color(0.91f, 0.70f, 0.23f));

        // 히든 아이템은 다른 개발자가 연결 -> 자리 표시만 (금색 원 = 히든, 종이색 사각형 = 파피루스 조각)
        Shape(root, "HiddenItemSpot_Horus", new Vector2(-5f, 10f), circle, new Vector2(0.5f, 0.5f), new Color(1f, 0.85f, 0.35f, 0.8f));
        Shape(root, "PapyrusSpot_Horus_2", new Vector2(-7f, 6f), square, new Vector2(0.4f, 0.3f), new Color(0.93f, 0.87f, 0.72f));
    }

    // ================= 도우미 =================
    // 기본 도형(square, circle)은 1 x 1칸 그림이라 scale = 월드 크기
    private static GameObject Shape(Transform parent, string objName, Vector2 pos, Sprite sprite, Vector2 scale, Color color)
    {
        var go = new GameObject(objName, typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingLayerName = layer;
        return go;
    }

    // 조사하면 대사가 나오는 물건 (몇 번이든 다시 읽을 수 있게)
    private static void Inspect(GameObject go, string objectId, string prompt, string[,] lines)
    {
        Inspectable ins = go.AddComponent<Inspectable>();
        var so = new SerializedObject(ins);
        HorusArtSetup.Prop(so, "objectId").stringValue = objectId;
        HorusArtSetup.Prop(so, "promptText").stringValue = prompt;
        HorusArtSetup.Prop(so, "onceOnly").boolValue = false;
        FillLines(HorusArtSetup.Prop(so, "lines"), lines);
        FillLines(HorusArtSetup.Prop(so, "afterDoneLines"), lines);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void FillLines(SerializedProperty list, string[,] lines)
    {
        if (list == null) return;
        list.arraySize = lines.GetLength(0);
        for (int i = 0; i < lines.GetLength(0); i++)
        {
            SerializedProperty e = list.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("speaker").stringValue = lines[i, 0];
            e.FindPropertyRelative("text").stringValue = lines[i, 1];
        }
    }
}
