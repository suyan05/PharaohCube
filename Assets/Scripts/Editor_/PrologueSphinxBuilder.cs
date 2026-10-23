using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

// 메뉴 한 번으로 스핑크스의 꿈을 기획서 배치대로 만드는 도구 (바닥 타일 + 물건 + 연출 + 카메라 + 조명)
// 메뉴: PharaohCube -> Prologue -> 스핑크스의 꿈 만들기   (Prologue_Sphinx 씬을 열고 실행)
//
// 좌표 규칙: 맵 22 x 13칸, 맵 왼쪽 아래 = 월드 (0, 0). 기획서 칸 (x, y)는 왼쪽 위 기준 (세부 아트 요청서 2.1)
// 그림: Assets/image 아래에서 요청서 파일 이름으로 찾음 (Pro_Obj_SphinxHead 등). 없으면 경고만
// 타일 그림이 32px가 아니면(예: 64px) 1칸에 맞도록 그 그림의 PPU를 자동으로 그림 크기로 바꿈
// 이미 만든 게 있으면 멈춤 -> SphinxDream, DreamGrid 지우고 다시. Ctrl+Z로 한 번에 되돌릴 수 있음
public static class PrologueSphinxBuilder
{
    private const string RootName = "SphinxDream";
    private const string GridName = "DreamGrid";
    private const string TileFolder = "Assets/Tile/Prologue";
    private const int MapW = 22;
    private const int MapH = 13;

    private static string sortingLayer = "Default";
    private static int standOrder;

    [MenuItem("PharaohCube/Prologue/스핑크스의 꿈 만들기")]
    private static void Build()
    {
        if (GameObject.Find(RootName) != null || GameObject.Find(GridName) != null)
        {
            EditorUtility.DisplayDialog("스핑크스의 꿈", $"이미 '{RootName}' 또는 '{GridName}'가 있어요. '다시 만들기' 메뉴를 써주세요.", "확인");
            return;
        }
        Create();
    }

    // 예전 것(SphinxDream, DreamGrid)을 지우고 새로 만듦. 그림을 바꿨거나 스크립트가 바뀌었을 때
    [MenuItem("PharaohCube/Prologue/스핑크스의 꿈 다시 만들기")]
    private static void Rebuild()
    {
        foreach (string name in new[] { RootName, GridName })
        {
            GameObject old = GameObject.Find(name);
            if (old != null) Undo.DestroyObjectImmediate(old);
        }
        Create();
    }

    private static void Create()
    {
        try
        {
            ReadPlayerSorting();
            Tilemap floor = BuildTiles();
            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "스핑크스의 꿈");

            BuildObjects(root.transform);
            MovePlayer();
            SetDreamLight();
            FixCamera(floor);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("[PrologueSphinxBuilder] 스핑크스의 꿈 완료 -> Ctrl+S로 씬 저장");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PrologueSphinxBuilder] 만들기 오류: {e}");
        }
    }

    // ================= 바닥 타일 =================
    private static Tilemap BuildTiles()
    {
        var grid = new GameObject(GridName, typeof(Grid));
        Undo.RegisterCreatedObjectUndo(grid, "꿈 바닥");

        Tilemap floor = CreateTilemap(grid.transform, "Floor", standOrder - 10, false);
        Tilemap edge = CreateTilemap(grid.transform, "Edge", standOrder - 9, true);
        FillFloor(floor);
        FillEdge(edge);
        return floor;
    }

    private static Tilemap CreateTilemap(Transform parent, string name, int order, bool solid)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent, false);

        TilemapRenderer r = go.GetComponent<TilemapRenderer>();
        r.sortingLayerName = sortingLayer;
        r.sortingOrder = order;
        if (solid) go.AddComponent<TilemapCollider2D>();
        return go.GetComponent<Tilemap>();
    }

    private static void FillFloor(Tilemap map)
    {
        TileBase a = Tile("FloorA"), b = Tile("FloorB"), c = Tile("FloorC");
        var rng = new System.Random(2213);
        for (int x = 0; x < MapW; x++)
        {
            for (int y = 0; y < MapH; y++)
            {
                double r = rng.NextDouble();
                map.SetTile(new Vector3Int(x, y, 0), r < 0.7 ? a : (r < 0.9 ? b : c));
            }
        }
    }

    // 위 2줄 = 윗면 + 앞면(벽), 왼쪽, 오른쪽 1줄 = 옆 가장자리, 아래 1줄 = 낮은 턱
    private static void FillEdge(Tilemap map)
    {
        for (int x = 0; x < MapW; x++)
        {
            Set(map, x, MapH - 1, "EdgeTop");
            Set(map, x, MapH - 2, x == 0 ? "CornerL" : (x == MapW - 1 ? "CornerR" : "EdgeFront"));
            Set(map, x, 0, x == 0 || x == MapW - 1 ? "EdgeTop" : "EdgeB");
        }
        for (int y = 1; y < MapH - 2; y++)
        {
            Set(map, 0, y, "EdgeL");
            Set(map, MapW - 1, y, "EdgeR");
        }
    }

    private static void Set(Tilemap map, int x, int y, string name)
    {
        map.SetTile(new Vector3Int(x, y, 0), Tile(name));
    }

    private static TileBase Tile(string name)
    {
        string assetPath = $"{TileFolder}/Pro_Tile_Dream_{name}.asset";
        TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(assetPath);
        if (tile != null) return tile;

        Sprite sprite = PrologueAssetFinder.TileSprite("Pro_Tile_Dream_" + name);
        if (sprite == null) return null;

        PrologueAssetFinder.EnsureFolder(TileFolder);
        Tile created = ScriptableObject.CreateInstance<Tile>();
        created.sprite = sprite;
        AssetDatabase.CreateAsset(created, assetPath);
        return created;
    }

    // ================= 물건 =================
    private static void BuildObjects(Transform root)
    {
        BuildBounds(root);
        SphinxHead head = BuildHead(root);
        BuildPaws(root);
        Inspectable[] sands = { BuildPawSand(root, "PawSand_L", "PRO_OBJ_PAWSAND_L", 7f), BuildPawSand(root, "PawSand_R", "PRO_OBJ_PAWSAND_R", 15f) };
        Inspectable stele = BuildStele(root);
        DreamStairs stairs = BuildStairs(root);
        BuildSystems(root, head, sands, stele, stairs);
    }

    private static void BuildBounds(Transform root)
    {
        GameObject go = Group(root, "MapBounds").gameObject;
        Box(go, new Vector2(0.5f, MapH), new Vector2(-0.25f, MapH * 0.5f), false);
        Box(go, new Vector2(0.5f, MapH), new Vector2(MapW + 0.25f, MapH * 0.5f), false);
        Box(go, new Vector2(MapW, 0.5f), new Vector2(MapW * 0.5f, -0.25f), false);
        Box(go, new Vector2(MapW, 0.5f), new Vector2(MapW * 0.5f, MapH + 0.25f), false);
    }

    // 머리 (7, 1) 8x4 -> 바닥 가운데 (11, 8). 맨 뒤 고정 (세부 요청서 2.2 레이어 5)
    private static SphinxHead BuildHead(Transform root)
    {
        GameObject go = MakeSprite(root, "SphinxHead", "Pro_Obj_SphinxHead", Foot(7, 1, 8, 4), -3);
        Box(go, new Vector2(7.6f, 4.4f), new Vector2(0f, 2.6f), false);

        SphinxHead head = go.AddComponent<SphinxHead>();
        Edit(head, so =>
        {
            Prop(so, "target").objectReferenceValue = go.GetComponent<SpriteRenderer>();
            Prop(so, "normalSprite").objectReferenceValue = Load("Pro_Obj_SphinxHead");
            Prop(so, "glowSprite").objectReferenceValue = Load("Pro_Obj_SphinxHead_EyeGlow");
        });
        return head;
    }

    // 앞발 (6, 5) / (14, 5) 각 2x4. 오른쪽은 좌우 반전
    private static void BuildPaws(Transform root)
    {
        GameObject left = MakeSprite(root, "SphinxPaw_L", "Pro_Obj_SphinxPaw", Foot(6, 5, 2, 4), -2);
        Box(left, new Vector2(1.8f, 3.6f), new Vector2(0f, 1.9f), false);

        GameObject right = MakeSprite(root, "SphinxPaw_R", "Pro_Obj_SphinxPaw", Foot(14, 5, 2, 4), -2);
        right.GetComponent<SpriteRenderer>().flipX = true;
        Box(right, new Vector2(1.8f, 3.6f), new Vector2(0f, 1.9f), false);
    }

    // 앞발 모래 (6, 9) / (14, 9) 각 2x1 바닥. 발끝을 덮도록 0.4칸 올리고 앞발보다 앞에 그림
    private static Inspectable BuildPawSand(Transform root, string name, string id, float x)
    {
        GameObject go = MakeSprite(root, name, "Pro_Obj_PawSand_Big", new Vector3(x, MapH - 9 - 1 + 0.4f, 0f), -1);
        Box(go, new Vector2(1.8f, 0.8f), new Vector2(0f, 0.4f), true);

        Inspectable insp = AddInspectable(go, id, "앞발의 모래");
        Edit(insp, so =>
        {
            Prop(so, "requiredTool").stringValue = "TOOL_BRUSH";
            SetLines(so, "missingToolLines", "나", "맨손으론 무리다. 붓이 있으면...");
            Prop(so, "holdSeconds").floatValue = 1.5f;
            Prop(so, "holdPromptText").stringValue = "길게 눌러 털기";
            SetObjects(so, "holdStageSprites", Load("Pro_Obj_PawSand_Big"), Load("Pro_Obj_PawSand_Small"));
            Prop(so, "doneSprite").objectReferenceValue = Load("Pro_Obj_PawSand_Gone");
        });
        return insp;
    }

    // 비석 (10, 6) 2x2. 묻혀 있을 때는 조사 막힘, 드러난 뒤 렌즈 필요
    private static Inspectable BuildStele(Transform root)
    {
        GameObject go = MakeSprite(root, "DreamStele", "Pro_Obj_Stele_Buried", Foot(10, 6, 2, 2), 0);
        Box(go, new Vector2(1.6f, 0.8f), new Vector2(0f, 0.4f), false);

        Inspectable insp = AddInspectable(go, "PRO_OBJ_STELE", "비석 읽기");
        Edit(insp, so =>
        {
            Prop(so, "requiredFlag").stringValue = "F_PRO_STELE_SHOWN";
            SetLines(so, "lockedLines", "나", "모래 속에 무언가 묻혀 있다.");
            Prop(so, "requiredTool").stringValue = "TOOL_LENS";
            SetLines(so, "missingToolLines", "나", "닳은 글자가 빼곡하다. 맨눈으론 읽을 수 없다.");
            SetLines(so, "afterDoneLines", "나", "첫 문은 태양의 신이 지킨다...");
        });
        return insp;
    }

    // 계단 (10, 9) 2x2 바닥. 처음엔 바닥처럼 보이고, 입구 트리거는 열릴 때 켜짐
    private static DreamStairs BuildStairs(Transform root)
    {
        GameObject go = MakeSprite(root, "DreamStairs", "Pro_Obj_Stairs_Closed", Foot(10, 9, 2, 2), -1);
        go.layer = 2; // Ignore Raycast (E 조사 대상 찾기에 안 걸리게)
        BoxCollider2D box = Box(go, new Vector2(1.4f, 1.4f), new Vector2(0f, 1f), true);
        box.enabled = false;

        DreamStairs stairs = go.AddComponent<DreamStairs>();
        Edit(stairs, so =>
        {
            Prop(so, "target").objectReferenceValue = go.GetComponent<SpriteRenderer>();
            Prop(so, "openSprite").objectReferenceValue = Load("Pro_Obj_Stairs_Open");
            Prop(so, "entryPoint").vector2Value = (Vector2)go.transform.position + Vector2.up;
            SetObjects(so, "dustSprites", Grains());
        });
        return stairs;
    }

    // ================= 진행 시스템 =================
    private static void BuildSystems(Transform root, SphinxHead head, Inspectable[] sands, Inspectable stele, DreamStairs stairs)
    {
        GameObject sys = Group(root, "PrologueSystems").gameObject;

        SphinxWakeCutscene wake = sys.AddComponent<SphinxWakeCutscene>();
        Edit(wake, so =>
        {
            Prop(so, "head").objectReferenceValue = head;
            Prop(so, "wakePoint").vector2Value = Cell(4, 10);
            Prop(so, "povFocus").vector2Value = (Vector2)head.transform.position + new Vector2(0f, 2.4f);
            SetObjects(so, "sparkleSprites", Load("Pro_FX_DreamMote_01"), Load("Pro_FX_DreamMote_02"));
        });

        SphinxDreamDirector director = sys.AddComponent<SphinxDreamDirector>();
        Edit(director, so =>
        {
            Prop(so, "wake").objectReferenceValue = wake;
            Prop(so, "head").objectReferenceValue = head;
            SetObjects(so, "pawSands", sands);
            Prop(so, "stele").objectReferenceValue = stele;
            Prop(so, "steleRenderer").objectReferenceValue = stele.GetComponent<SpriteRenderer>();
            Prop(so, "steleShownSprite").objectReferenceValue = Load("Pro_Obj_Stele_Shown");
            Prop(so, "stairs").objectReferenceValue = stairs;
            SetObjects(so, "dustSprites", Grains());
        });
        BuildAmbience(root);
    }

    // 꿈 분위기: 빛 알갱이 + 흐릿한 빛 덩어리 + 후처리(DreamFX)
    private static void BuildAmbience(Transform root)
    {
        GameObject go = Group(root, "DreamAmbience").gameObject;

        DreamMotes motes = go.AddComponent<DreamMotes>();
        Edit(motes, so =>
        {
            SetObjects(so, "sprites", Load("Pro_FX_DreamMote_01"), Load("Pro_FX_DreamMote_02"));
            Prop(so, "sortingLayer").stringValue = sortingLayer;
            Prop(so, "sortingOrder").intValue = standOrder + 30;
        });

        GameObject orbs = Group(go.transform, "GlowOrbs").gameObject;
        DreamGlowOrbs glow = orbs.AddComponent<DreamGlowOrbs>();
        Edit(glow, so =>
        {
            Prop(so, "sortingLayer").stringValue = sortingLayer;
            Prop(so, "sortingOrder").intValue = standOrder + 25;
        });

        DreamVolumeBuilder.Create(go.transform);
    }

    // ================= 플레이어, 조명, 카메라 =================
    private static void ReadPlayerSorting()
    {
        PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
        SpriteRenderer sr = player != null ? player.GetComponentInChildren<SpriteRenderer>() : null;
        if (sr == null)
        {
            Debug.LogWarning("[PrologueSphinxBuilder] 플레이어 그림을 못 찾음 -> 정렬 Default / 0 사용 (Player를 먼저 씬에 넣기)");
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
            Debug.LogWarning("[PrologueSphinxBuilder] 씬에 플레이어가 없음 -> Prologue_Dig에서 Player 복사해 오기");
            return;
        }
        Undo.RecordObject(player.transform, "플레이어 시작 위치");
        Vector2 p = Cell(4, 10);
        player.transform.position = new Vector3(p.x, p.y, player.transform.position.z);
    }

    // 보랏빛 별빛 (200, 180, 255) - 아트 요청서 1.3
    private static void SetDreamLight()
    {
        foreach (Light2D light in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (light.lightType != Light2D.LightType.Global) continue;
            Undo.RecordObject(light, "꿈 조명");
            light.color = new Color(200f / 255f, 180f / 255f, 1f);
            light.intensity = 0.9f;
        }
    }

    // 아래 여유 3칸: 플레이어가 맨 아래에 있어도 대사창에 안 가려지게. 여백은 꿈의 어둠 색(#120E1A)으로
    private static void FixCamera(Tilemap floor)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[PrologueSphinxBuilder] Main Camera가 없음 (Tag가 MainCamera인지 확인)");
            return;
        }

        Undo.RecordObject(cam, "카메라 배경");
        Undo.RecordObject(cam.transform, "카메라 Z");
        Vector3 p = cam.transform.position;
        if (p.z > -1f) cam.transform.position = new Vector3(p.x, p.y, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0x12 / 255f, 0x0E / 255f, 0x1A / 255f);
        DreamVolumeBuilder.EnablePostProcessing(cam);

        CameraFollow2D follow = cam.GetComponent<CameraFollow2D>();
        if (follow == null)
        {
            Debug.LogWarning("[PrologueSphinxBuilder] Main Camera에 CameraFollow2D가 없음 -> Prologue_Dig 카메라에서 복사");
            return;
        }
        Undo.RecordObject(follow, "카메라 경계");
        Edit(follow, so =>
        {
            Prop(so, "boundsTilemap").objectReferenceValue = floor;
            SerializedProperty pad = so.FindProperty("bottomPadding");
            if (pad != null) pad.floatValue = 3f;
        });
    }

    // ================= 도우미 =================
    // 기획서 칸 (x, y, 발자리 w x h) -> 그 물건의 바닥 가운데 월드 좌표
    private static Vector3 Foot(int x, int y, int w, int h)
    {
        return new Vector3(x + w * 0.5f, MapH - y - h, 0f);
    }

    // 기획서 칸 하나의 가운데 (플레이어 위치용)
    private static Vector2 Cell(int x, int y)
    {
        return new Vector2(x + 0.5f, MapH - y - 0.5f);
    }

    private static Sprite[] Grains()
    {
        return new[] { Load("Pro_FX_SandGrain_01"), Load("Pro_FX_SandGrain_02"), Load("Pro_FX_SandGrain_03") };
    }

    private static Sprite Load(string file)
    {
        return PrologueAssetFinder.Sprite(file);
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

    // offset = 이 오브젝트 위치 기준 (그룹은 (0, 0)이라 월드 좌표와 같음)
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

    private static void SetObjects(SerializedObject so, string field, params Object[] values)
    {
        SerializedProperty arr = Prop(so, field);
        arr.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
