using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 호루스 타일맵 씬에 새로 놓거나 바꾸는 물건 (HorusRoomBuilder가 부름). 위치는 작업 도면 그대로
// - 입구 전실: 신화 책 독서대(HA1), 횃불 제단(HA2, 라의 불씨가 있으면 사라짐)
// - H4 옆: 벽화 모자이크(H08, E = 원본 벽화 + 수첩)
// - 본당: 기둥 4개에 걸린 별 횃불(HL1~4) + 그 기둥 옆 단서 비석 3개(HS1~3), 입구 비석/경고 석판을 단서 비석으로 바꿈
// - 매 석상, 매의 회랑, 연출 감독, 목표 표시는 HorusGalleryBuild
// 아트가 없는 것은 기본 도형(사각형, 원) + 색으로 임시 제작 -> 나중에 그림만 바꾸면 됨
public static class HorusRoomObjects
{
    private const string TorchAltarPrefab = "Assets/Prefabs/Common/TorchAltar.prefab";
    // [수정] 예전엔 테스트용 횃불(Light_off/on, 막대 위 네모)이라 바닥에 세운 것처럼 보였음 -> 기둥에 박힌 청동 횃대 도트 그림
    private const string TorchOff = "Assets/image/Horus/Object/HO_Sconce_Off.png";
    private const string TorchOn = "Assets/image/Horus/Object/HO_Sconce_On.png";

    public static Sprite Square { get; private set; }
    public static Sprite Circle { get; private set; }
    public static string Layer { get; private set; }

    public static void Build(Sprite squareSprite, Sprite circleSprite, string sortingLayer)
    {
        Square = squareSprite;
        Circle = circleSprite;
        Layer = sortingLayer;
        Transform root = new GameObject("HorusRoom").transform;
        HorusClueSetup.EnsurePanel();
        BuildAntechamber(root);
        BuildMural();
        WallTorch t1 = Torch(root, "OBJ_HL1_StarTorch", "Pillar_NW", "HO_1");
        WallTorch t2 = Torch(root, "OBJ_HL2_StarTorch", "Pillar_NE", "HO_2");
        WallTorch t3 = Torch(root, "OBJ_HL3_StarTorch", "Pillar_SW", "HO_3");
        Torch(root, "OBJ_HL4_StarTorch", "Pillar_SE", "HO_4"); // H4/벽화 쪽을 밝히는 불 (비석 없음)
        BuildPuzzleSteles(root, t1, t2, t3);
        ConvertNotes();
        HorusGalleryBuild.Build(root);
    }

    // ================= 입구 전실 =================
    private static void BuildAntechamber(Transform root)
    {
        GameObject book = Shape(root, "OBJ_HA1_MythBook", new Vector2(9.5f, -4f), Square, new Vector2(0.9f, 0.7f), new Color(0.55f, 0.4f, 0.24f));
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
        altar.AddComponent<HorusEmberSkip>(); // 라의 방에서 불씨(히든)를 가져왔으면 필요 없음
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

    // ================= 벽화: H4 바닥 별판 바로 옆(남쪽)의 흐릿한 모자이크 (E = 원본 벽화 + 수첩) =================
    private static void BuildMural()
    {
        GameObject mural = HorusArtSetup.Find("OBJ_H08_Mural");
        if (mural == null) return;
        mural.transform.position = new Vector3(17.8f, 0.6f, 0f); // H4 별판(17.8, 3.4) 바로 아래 남쪽 벽 앞 -> 퍼즐 앞에서 보고 풀 수 있게
        HorusWorldArt.Fit("OBJ_H08_Mural", HorusWorldArt.MuralMosaic, HorusWorldArt.MuralSize);
        BoxCollider2D box = mural.GetComponent<BoxCollider2D>();
        if (box != null) box.isTrigger = true; // 벽에 붙은 그림이라 길을 막지 않음 (조사만)
        HorusClueSetup.ConvertNoteStone("OBJ_H08_Mural", "MURAL", "남쪽 벽화 - 매의 비행", "H4/H4_03", Layer, false);
    }

    // ================= 별 횃불: 기둥에 걸린 횃대 (WallTorch: 내 횃불로 E 길게 -> 켜짐 -> 옆 단서 비석이 읽힘) =================
    // 바닥에 덩그러니 세우지 않고 기둥 앞면 가운데쯤에 붙임. 기둥 그림보다 한 칸 앞(정렬 +1)에 그려서 기둥을 따라 앞뒤 정렬됨
    private static WallTorch Torch(Transform root, string objName, string pillarName, string torchId)
    {
        GameObject pillar = HorusArtSetup.Find(pillarName);
        Vector2 pos = pillar != null ? (Vector2)pillar.transform.position + new Vector2(0f, 0.35f) : Vector2.zero;
        Sprite off = HorusArtSetup.Pixel(TorchOff);
        GameObject go = Shape(root, objName, pos, off != null ? off : Circle, Vector2.one, Color.white);
        if (off != null) HorusWorldArt.FitObject(go, off, new Vector2(0.56f, 0.94f), Color.white);
        go.GetComponent<SpriteRenderer>().sortingOrder = 1;
        SconceCollider(go);
        Light2D light = PointLight(go.transform, new Color(1f, 0.75f, 0.45f), 4.5f);
        light.transform.position = go.transform.position + Vector3.up * 0.3f; // 횃대 불꽃 자리
        WallTorch torch = go.AddComponent<WallTorch>();
        var so = new SerializedObject(torch);
        HorusArtSetup.Prop(so, "torchId").stringValue = torchId;
        HorusArtSetup.Prop(so, "target").objectReferenceValue = go.GetComponent<SpriteRenderer>();
        HorusArtSetup.Prop(so, "offSprite").objectReferenceValue = off;
        HorusArtSetup.Prop(so, "onSprite").objectReferenceValue = HorusArtSetup.Pixel(TorchOn);
        HorusArtSetup.Prop(so, "lightSource").objectReferenceValue = light;
        HorusArtSetup.Prop(so, "dayAutoFlag").stringValue = "";
        HorusArtSetup.Prop(so, "nightAutoFlag").stringValue = "";
        HorusArtSetup.Prop(so, "nightFlag").stringValue = "F_HO_NO_NIGHT"; // 호루스 방엔 낮/밤이 없음 (아무도 안 켜는 플래그)
        so.ApplyModifiedPropertiesWithoutUndo();
        return torch;
    }

    // 기둥 아래에 서서도 E가 닿게: 조사 상자를 기둥 밑동까지 늘림 (트리거라 길은 안 막음)
    private static void SconceCollider(GameObject go)
    {
        float k = Mathf.Max(go.transform.localScale.x, 0.001f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.8f, 1.7f) / k;
        box.offset = new Vector2(0f, -0.65f) / k;
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

    // ================= 퍼즐 단서 비석 (옆 별 횃불을 켜야 읽힘) =================
    private static void BuildPuzzleSteles(Transform root, WallTorch t1, WallTorch t2, WallTorch t3)
    {
        // [수정] H1이 '매의 거울(빛 반사)'로 바뀌어서 단서도 바꿈
        Stele(root, "OBJ_HS1_ClueStele", new Vector2(6.8f, 10.0f), t1, 11, "S1", "해의 눈", "H1/H1_01",
            "살아 있는 파라오는 호루스였고, 죽은 파라오는 오시리스가 되었다. 호루스의 오른 눈은 해다. 해의 빛은 곧게 날아가고, 매의 거울만이 그 길을 꺾는다. 세트의 붉은 돌은 빛을 삼킨다. 빛은 제 길을 한 번 가로질러야 매의 눈에 닿는다.");
        Stele(root, "OBJ_HS2_ClueStele", new Vector2(17.2f, 10.0f), t2, 12, "S2", "별의 길", "H2/H2_05",
            "천장의 별 몇 개가 검게 지워져 있다. 별을 삼킨 아펩의 자국이다. 별은 한 번 걸은 길을 다시 걷지 않는다. 선이 홀수 개 모이는 별에서 시작해, 그런 별에서 끝난다.");
        // [수정] H3 난이도: 잇는 순서(큰 몫부터) + 감각으로 몫을 알아내야 함 (실제 우자트 분수 신화)
        Stele(root, "OBJ_HS3_ClueStele", new Vector2(9.2f, 4.0f), t3, 13, "S3", "찢긴 눈", "H3/H3_02",
            "세트는 아들의 눈을 여섯 몫으로 찢었다. 냄새는 2분의 1, 보는 것은 4분의 1, 생각은 8분의 1, 듣는 것은 16분의 1, 맛은 32분의 1, 닿는 것은 64분의 1. 토트는 가장 큰 몫부터 이었다. 코에 가까운 흰자는 냄새를, 귀에 가까운 흰자는 소리를 맡고, 눈썹 아래에서 생각이 자란다. 싹 트는 나선은 맛, 땅을 딛는 눈물선은 닿음이다.");
    }

    private static void Stele(Transform root, string objName, Vector2 pos, WallTorch torch, int noteId,
        string id, string title, string art, string body)
    {
        Sprite slate = HorusArtSetup.Art(HorusClueSetup.Slate);
        GameObject go = Shape(root, objName, pos, slate != null ? slate : Square, Vector2.one, Color.white);
        if (slate != null) HorusWorldArt.FitObject(go, slate, new Vector2(0.75f, 1.1f), Color.white);
        go.AddComponent<BoxCollider2D>();
        ClueData data = HorusClueSetup.Data(id, title, body, art);
        HorusClueSetup.AddStele(go, data, torch, $"F_HO_CLUE_{id}_READ", noteId, body, Layer, true);
    }

    // 입구 비석(노트 1), 경고 석판(노트 4): 로그만 남던 NoteStone -> 단서 패널이 뜨는 비석
    private static void ConvertNotes()
    {
        HorusWorldArt.Fit("OBJ_H02_GuideStone", HorusClueSetup.Slate, new Vector2(0.9f, 1.3f));
        HorusWorldArt.Fit("OBJ_H11_WarningSlab", HorusClueSetup.Slate, new Vector2(1f, 1.4f));
        HorusClueSetup.ConvertNoteStone("OBJ_H02_GuideStone", "ENTRY", "입구 비석", "", Layer, true);
        HorusClueSetup.ConvertNoteStone("OBJ_H11_WarningSlab", "WARNING", "붉은 별의 경고", "H5/H5_07", Layer, true);
        LapisStars();
    }

    // 청금석 별 3개: E로 줍는 수집 요소 (FUN-4)
    private static void LapisStars()
    {
        for (int i = 1; i <= 3; i++)
        {
            GameObject go = HorusArtSetup.Find($"OBJ_H16_LapisStar_{i}");
            if (go == null) continue;
            if (go.GetComponent<Collider2D>() == null) go.AddComponent<CircleCollider2D>().isTrigger = true;
            HorusLapisStar star = go.GetComponent<HorusLapisStar>();
            if (star == null) star = go.AddComponent<HorusLapisStar>();
            var so = new SerializedObject(star);
            HorusArtSetup.Prop(so, "starId").intValue = i;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // ================= 도우미 =================
    // 기본 도형(Square, Circle)은 1 x 1칸 그림이라 scale = 월드 크기
    public static GameObject Shape(Transform parent, string objName, Vector2 pos, Sprite sprite, Vector2 scale, Color color)
    {
        var go = new GameObject(objName, typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingLayerName = Layer;
        return go;
    }

    // 조사하면 대사가 나오는 물건 (몇 번이든 다시 읽을 수 있게)
    public static void Inspect(GameObject go, string objectId, string prompt, string[,] lines)
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
