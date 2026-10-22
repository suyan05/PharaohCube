using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

// 메뉴 한 번으로 발굴 현장 오브젝트를 기획서 배치대로 만들어주는 도구
// 메뉴: PharaohCube -> Prologue -> 발굴 현장 배치 만들기   (Prologue_Dig 씬을 열고 실행)
// 순서: "발굴 현장 바닥 깔기"(DigGrid)를 먼저 하고 이걸 하면 카메라 경계까지 자동으로 맞춤
//
// 만드는 것: 맵 경계, 텐트, 야자수, 책상(일지), 장비 상자, 무전기, 모래 더미 3개,
//            발굴 구역 로프 + 입구(BrushGate) + 첫 입장 연출(DigZoneEntry),
//            진행/오프닝 연출/모래 묶음/붕괴 시스템 (서로 연결까지)
// 하는 것:   플레이어를 시작 칸(5, 8)으로 옮김, Global Light 2D를 낮빛(255, 225, 160)으로,
//            Main Camera: Z -10, CameraFollow2D Bounds Tilemap = DigGrid/Floor, Bottom Padding 0
//
// 좌표 규칙: 맵 28 x 14칸, 맵 왼쪽 아래 = 월드 (0, 0). 기획서 칸 (x, y)는 왼쪽 위 기준
// 그림은 Assets/image/Prologue/World/ 의 요청서 파일 이름으로 찾음 (없으면 경고만, 그림 없이 만듦)
// Ctrl+Z로 한 번에 되돌릴 수 있음
public static class PrologueDigBuilder
{
    private const string WorldPath = "Assets/image/Prologue/World/";
    private const string RootName = "PrologueDig";
    private const float MapW = 28f;
    private const float MapH = 14f;

    private static string sortingLayer = "Default";
    private static int standOrder;

    [MenuItem("PharaohCube/Prologue/발굴 현장 배치 만들기")]
    private static void Build()
    {
        try
        {
            if (GameObject.Find(RootName) != null)
            {
                EditorUtility.DisplayDialog("발굴 현장", $"이미 '{RootName}'가 있어요. 다시 만들려면 먼저 지워주세요.", "확인");
                return;
            }

            ReadPlayerSorting();
            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "발굴 현장 배치");

            BuildAll(root.transform);
            MovePlayer();
            SetDayLight();
            FixCamera();

            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("[PrologueDigBuilder] 발굴 현장 배치 완료 -> Ctrl+S로 씬 저장");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PrologueDigBuilder] 배치 오류: {e}");
        }
    }

    private static void BuildAll(Transform root)
    {
        BuildBounds(root);
        BuildDecor(root);
        Transform objects = Group(root, "Interactables");
        BuildJournal(objects);
        BuildCrate(objects);
        Transform radio = BuildRadio(objects);
        Inspectable[] piles = BuildSandPiles(objects);
        BuildRopeZone(root);
        BuildSystems(root, piles, radio);
    }

    // ================= 맵 경계, 장식 =================
    private static void BuildBounds(Transform root)
    {
        GameObject go = Group(root, "MapBounds").gameObject;
        Box(go, new Vector2(0.5f, MapH), new Vector2(-0.25f, MapH * 0.5f), false);
        Box(go, new Vector2(0.5f, MapH), new Vector2(MapW + 0.25f, MapH * 0.5f), false);
        Box(go, new Vector2(MapW, 0.5f), new Vector2(MapW * 0.5f, -0.25f), false);
        Box(go, new Vector2(MapW, 0.5f), new Vector2(MapW * 0.5f, MapH + 0.25f), false);
    }

    private static void BuildDecor(Transform root)
    {
        Transform decor = Group(root, "Decor");

        GameObject tent = MakeSprite(decor, "Tent", "Pro_Obj_Tent", Foot(2, 3, 4, 3), 0);
        Box(tent, new Vector2(3.8f, 2.6f), new Vector2(0f, 1.3f), false);

        GameObject palm = MakeSprite(decor, "Palm", "Pro_Obj_Palm", Foot(25, 3, 1, 1), 0);
        Box(palm, new Vector2(0.4f, 0.3f), new Vector2(0f, 0.15f), false);
    }

    // ================= 조사 오브젝트 =================
    private static void BuildJournal(Transform parent)
    {
        GameObject desk = MakeSprite(parent, "Desk_Journal", "Pro_Obj_Desk", Foot(8, 5, 2, 1), 0);
        Box(desk, new Vector2(1.9f, 0.9f), new Vector2(0f, 0.45f), false);

        const string text = "기자는 모래 아래 어딘가에 '여섯 신의 신전'이 있다고 한다. 오늘도 발견은 없었다.";
        Inspectable insp = AddInspectable(desk, "PRO_OBJ_JOURNAL", "발굴 일지 읽기");
        Edit(insp, so =>
        {
            SetLines(so, "lines", "발굴 일지", text);
            SetLines(so, "afterDoneLines", "발굴 일지", text);
        });
    }

    private static void BuildCrate(Transform parent)
    {
        GameObject crate = MakeSprite(parent, "Crate", "Pro_Obj_Crate_Closed", Foot(12, 5, 1, 1), 0);
        Box(crate, new Vector2(0.9f, 0.9f), new Vector2(0f, 0.45f), false);

        Inspectable insp = AddInspectable(crate, "PRO_OBJ_CRATE", "장비 상자 열기");
        Edit(insp, so =>
        {
            Prop(so, "doneSprite").objectReferenceValue = Load("Pro_Obj_Crate_Open");
            SetStrings(so, "grantItems", "TOOL_BRUSH");
            SetLines(so, "lines", "", "손에 익은 붓이다.");
            SetLines(so, "afterDoneLines", "", "이제 비어 있다.");
        });
    }

    private static Transform BuildRadio(Transform parent)
    {
        GameObject radio = MakeSprite(parent, "Radio", "Pro_Obj_Radio", Foot(2, 9, 1, 1), 0);
        Box(radio, new Vector2(0.9f, 0.8f), new Vector2(0f, 0.4f), false);

        Inspectable insp = AddInspectable(radio, "PRO_OBJ_RADIO", "무전기 켜기");
        Edit(insp, so =>
        {
            Prop(so, "onceOnly").boolValue = false;
            SetLines(so, "lines", "", "치직... 본부는 다시 조용하다.");
        });
        return radio.transform;
    }

    private static Inspectable[] BuildSandPiles(Transform parent)
    {
        return new[]
        {
            BuildSandPile(parent, "PRO_OBJ_SAND_01", Foot(19, 8, 1, 1)),
            BuildSandPile(parent, "PRO_OBJ_SAND_02", Foot(22, 10, 1, 1)),
            BuildSandPile(parent, "PRO_OBJ_SAND_03", Foot(24, 7, 1, 1))
        };
    }

    // 바닥 물건: 트리거 콜라이더 (옆에 서서 털기), 플레이어보다 한 칸 뒤에 그림
    private static Inspectable BuildSandPile(Transform parent, string id, Vector3 pos)
    {
        GameObject pile = MakeSprite(parent, id, "Pro_Obj_SandPile_Big", pos, -1);
        Box(pile, new Vector2(0.9f, 0.7f), new Vector2(0f, 0.35f), true);

        Inspectable insp = AddInspectable(pile, id, "모래 더미");
        Edit(insp, so =>
        {
            Prop(so, "requiredTool").stringValue = "TOOL_BRUSH";
            Prop(so, "holdSeconds").floatValue = 1.5f;
            Prop(so, "holdPromptText").stringValue = "길게 눌러 털기";
            SetObjects(so, "holdStageSprites", Load("Pro_Obj_SandPile_Big"), Load("Pro_Obj_SandPile_Small"));
            Prop(so, "doneSprite").objectReferenceValue = Load("Pro_Obj_SandPile_Gone");
        });
        return insp;
    }

    // ================= 발굴 구역 로프 =================
    // 구역: 칸 (18, 6) ~ (25, 11) -> 월드 x 18.5 ~ 25.5, y 2.4 ~ 7.4 (로프 높이에 맞춤)
    // 왼쪽 가운데 2칸(월드 y 5 ~ 7)이 입구 -> BrushGate가 막음
    private static void BuildRopeZone(Transform root)
    {
        GameObject zone = Group(root, "DigZone").gameObject;
        BuildRopeVisuals(zone.transform);

        Box(zone, new Vector2(7f, 0.2f), new Vector2(22f, 2.4f), false);   // 아래
        Box(zone, new Vector2(7f, 0.2f), new Vector2(22f, 7.4f), false);   // 위
        Box(zone, new Vector2(0.2f, 5f), new Vector2(25.5f, 4.9f), false); // 오른쪽
        Box(zone, new Vector2(0.2f, 2.6f), new Vector2(18.5f, 3.7f), false); // 왼쪽 아래
        Box(zone, new Vector2(0.2f, 0.4f), new Vector2(18.5f, 7.2f), false); // 왼쪽 위

        BuildGate(zone.transform);
        BuildZoneEntry(zone.transform);
    }

    // 구역 안쪽을 덮는 트리거. Ignore Raycast 레이어라 E 조사 대상 찾기에 걸리지 않음
    private static void BuildZoneEntry(Transform zone)
    {
        GameObject entry = Group(zone, "ZoneEntry").gameObject;
        entry.layer = 2; // Ignore Raycast
        Box(entry, new Vector2(6.8f, 4.8f), new Vector2(22f, 4.9f), true);
        entry.AddComponent<DigZoneEntry>();
    }

    private static void BuildRopeVisuals(Transform zone)
    {
        Transform ropes = Group(zone, "Ropes");
        float[] cornersX = { 18.5f, 25.5f };
        float[] rowsY = { 2f, 7f };

        foreach (float x in cornersX)
            foreach (float y in rowsY) MakeSprite(ropes, "Rope_Post", "Pro_Obj_Rope_Post", new Vector3(x, y), 0);

        for (int cx = 19; cx <= 24; cx++)
            foreach (float y in rowsY) MakeSprite(ropes, "Rope_H", "Pro_Obj_Rope_H", new Vector3(cx + 0.5f, y), 0);

        for (int cy = 3; cy <= 6; cy++)
        {
            MakeSprite(ropes, "Rope_V", "Pro_Obj_Rope_V", new Vector3(25.5f, cy), 0);
            if (cy < 5) MakeSprite(ropes, "Rope_V", "Pro_Obj_Rope_V", new Vector3(18.5f, cy), 0);
        }
    }

    private static void BuildGate(Transform zone)
    {
        GameObject gate = Group(zone, "BrushGate").gameObject;
        BoxCollider2D blocker = Box(gate, new Vector2(0.2f, 2f), new Vector2(18.5f, 6f), false);

        Transform closed = Group(gate.transform, "ClosedRope");
        MakeSprite(closed, "Rope_V", "Pro_Obj_Rope_V", new Vector3(18.5f, 5f), 0);
        MakeSprite(closed, "Rope_V", "Pro_Obj_Rope_V", new Vector3(18.5f, 6f), 0);

        BrushGate brushGate = gate.AddComponent<BrushGate>();
        Edit(brushGate, so =>
        {
            Prop(so, "blocker").objectReferenceValue = blocker;
            Prop(so, "closedVisual").objectReferenceValue = closed.gameObject;
        });
    }

    // ================= 진행 시스템 =================
    private static void BuildSystems(Transform root, Inspectable[] piles, Transform radio)
    {
        GameObject sys = Group(root, "PrologueSystems").gameObject;
        Sprite[] grains = { Load("Pro_FX_SandGrain_01"), Load("Pro_FX_SandGrain_02"), Load("Pro_FX_SandGrain_03") };

        DigIntroCutscene intro = sys.AddComponent<DigIntroCutscene>();
        Edit(intro, so =>
        {
            Prop(so, "radio").objectReferenceValue = radio;
            SetObjects(so, "dustSprites", grains);
        });

        DigSiteDirector director = sys.AddComponent<DigSiteDirector>();
        Edit(director, so => Prop(so, "intro").objectReferenceValue = intro);

        DigCollapseSequence collapse = BuildCollapse(sys, grains);
        SandPileGroup group = sys.AddComponent<SandPileGroup>();
        Edit(group, so =>
        {
            SetObjects(so, "piles", piles);
            SetObjects(so, "dustSprites", grains);
            Prop(so, "scarabSprite").objectReferenceValue = Load("Pro_Obj_SandPile_Scarab");
            Prop(so, "collapse").objectReferenceValue = collapse;
        });
    }

    private static DigCollapseSequence BuildCollapse(GameObject sys, Sprite[] grains)
    {
        DigCollapseSequence collapse = sys.AddComponent<DigCollapseSequence>();
        Edit(collapse, so =>
        {
            SetObjects(so, "crackFrames", Load("Pro_FX_Crack_01"), Load("Pro_FX_Crack_02"), Load("Pro_FX_Crack_03"));
            SetObjects(so, "grainSprites", grains);
            Prop(so, "crackSortingLayer").stringValue = sortingLayer;
            Prop(so, "crackSortingOrder").intValue = standOrder - 1;
            Prop(so, "grainSortingOrder").intValue = standOrder + 1;
        });
        return collapse;
    }

    // ================= 플레이어, 조명 =================
    private static void ReadPlayerSorting()
    {
        PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
        SpriteRenderer sr = player != null ? player.GetComponentInChildren<SpriteRenderer>() : null;
        if (sr == null)
        {
            Debug.LogWarning("[PrologueDigBuilder] 플레이어 그림을 못 찾음 -> 정렬 Default / 0 사용");
            return;
        }
        sortingLayer = sr.sortingLayerName;
        standOrder = sr.sortingOrder;
    }

    private static void MovePlayer()
    {
        PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
        if (player == null)
        {
            Debug.LogWarning("[PrologueDigBuilder] 씬에 플레이어가 없음 -> 라의 방에서 Player를 복사해 오기");
            return;
        }
        Undo.RecordObject(player.transform, "플레이어 시작 위치");
        player.transform.position = new Vector3(5.5f, MapH - 8f - 0.5f, player.transform.position.z);
    }

    private static void SetDayLight()
    {
        foreach (Light2D light in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (light.lightType != Light2D.LightType.Global) continue;
            Undo.RecordObject(light, "낮빛");
            light.color = new Color(255f / 255f, 225f / 255f, 160f / 255f);
            light.intensity = 1f;
        }
    }

    // ================= 카메라 =================
    // Z가 0 이상이면 그림이 카메라 뒤에 있어서 배경색만 보임 -> -10
    // Bounds Tilemap: 라의 방에서 복사해 오면 연결이 끊겨서 비어 있음 -> DigGrid/Floor
    // Bottom Padding: 라의 방용(3칸, 검은 타일 여백). 프롤로그는 여백 타일이 없어서 0
    private static void FixCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[PrologueDigBuilder] Main Camera가 없음 (Tag가 MainCamera인지 확인)");
            return;
        }

        Undo.RecordObject(cam.transform, "카메라 Z");
        Vector3 p = cam.transform.position;
        if (p.z > -1f) cam.transform.position = new Vector3(p.x, p.y, -10f);

        CameraFollow2D follow = cam.GetComponent<CameraFollow2D>();
        if (follow == null)
        {
            Debug.LogWarning("[PrologueDigBuilder] Main Camera에 CameraFollow2D가 없음 -> 라의 방 카메라에서 복사");
            return;
        }
        SetCameraBounds(follow);
    }

    private static void SetCameraBounds(CameraFollow2D follow)
    {
        GameObject floor = GameObject.Find("DigGrid/Floor");
        Tilemap map = floor != null ? floor.GetComponent<Tilemap>() : null;
        if (map == null) Debug.LogWarning("[PrologueDigBuilder] DigGrid/Floor가 없음 -> '발굴 현장 바닥 깔기' 먼저 하고 다시 실행");

        Undo.RecordObject(follow, "카메라 경계");
        Edit(follow, so =>
        {
            if (map != null) Prop(so, "boundsTilemap").objectReferenceValue = map;
            SerializedProperty pad = so.FindProperty("bottomPadding");
            if (pad != null) pad.floatValue = 0f;
        });
    }

    // ================= 도우미 =================
    // 기획서 칸 (x, y, 발자리 w x h) -> 그 물건의 바닥 가운데 월드 좌표
    private static Vector3 Foot(int x, int y, int w, int h)
    {
        return new Vector3(x + w * 0.5f, MapH - y - h, 0f);
    }

    private static Transform Group(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static GameObject MakeSprite(Transform parent, string name, string file, Vector3 pos, int orderOffset)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Load(file);
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = standOrder + orderOffset;
        return go;
    }

    private static Sprite Load(string file)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WorldPath + file + ".png");
        if (sprite == null) Debug.LogWarning($"[PrologueDigBuilder] 그림 없음: {WorldPath}{file}.png");
        return sprite;
    }

    // offset = 이 오브젝트 위치 기준. 그룹(MapBounds, DigZone, BrushGate)은 (0, 0)에 있어서 월드 좌표와 같음
    private static BoxCollider2D Box(GameObject go, Vector2 size, Vector2 offset, bool trigger)
    {
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        box.offset = offset;
        box.isTrigger = trigger;
        return box;
    }

    private static Inspectable AddInspectable(GameObject go, string id, string prompt)
    {
        Inspectable insp = go.AddComponent<Inspectable>();
        Edit(insp, so =>
        {
            Prop(so, "objectId").stringValue = id;
            Prop(so, "promptText").stringValue = prompt;
            Prop(so, "targetRenderer").objectReferenceValue = go.GetComponent<SpriteRenderer>();
        });
        return insp;
    }

    private static void Edit(Object target, Action<SerializedObject> change)
    {
        var so = new SerializedObject(target);
        change(so);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static SerializedProperty Prop(SerializedObject so, string field)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null) throw new InvalidOperationException($"{so.targetObject.GetType().Name}에 '{field}' 칸이 없음 (스크립트가 바뀌었는지 확인)");
        return p;
    }

    private static void SetLines(SerializedObject so, string field, string speaker, params string[] texts)
    {
        SerializedProperty arr = Prop(so, field);
        arr.arraySize = texts.Length;
        for (int i = 0; i < texts.Length; i++)
        {
            SerializedProperty line = arr.GetArrayElementAtIndex(i);
            line.FindPropertyRelative("speaker").stringValue = speaker;
            line.FindPropertyRelative("text").stringValue = texts[i];
        }
    }

    private static void SetStrings(SerializedObject so, string field, params string[] values)
    {
        SerializedProperty arr = Prop(so, field);
        arr.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) arr.GetArrayElementAtIndex(i).stringValue = values[i];
    }

    private static void SetObjects(SerializedObject so, string field, params Object[] values)
    {
        SerializedProperty arr = Prop(so, field);
        arr.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
