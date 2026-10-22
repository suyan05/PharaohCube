#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

// 메뉴: Tools/Bastet/★ 지금 맵(타일)에 맞춰 배치
// Puzzle_Test_Bastet_Tliemap에 이미 칠해 둔 맵(Grid/Ground 타일맵)에 바스테트 5개 방을 맞춰 넣음
//
//  지금 맵 (Ground 칸 좌표, 왼쪽 아래 = 칸 모서리)
//            [ 북쪽 방 = 방 5 신상의 방 (B5) ]   x -8..7, y 16..22  (위쪽에 토트 출구)
//                     | 북쪽 통로 (Gate_T = 방 5 봉인석, 발톱 인장을 넣음)
//  [방 2 눈 (B2)] == [ 가운데 홀 (시작, 횃불 제단) ] == [방 3 목걸이 (B3)]     위: y 4..12
//  [방 1 발자국(B1)] == [                         ] == [방 4 발톱 (B4)]       아래: y -9..-3
//
// - 퍼즐 그림 크기는 그대로, 발자국·석상 간격만 작은 방에 맞게 줄임
// - B3·B4 제단, 입구 비석은 지금 놓인 자리를 그대로 둠
// - 예전 Bastet_Area(요청서 배치도 크기)가 있으면 걷는 퍼즐만 꺼내고 지움
// - 여러 번 눌러도 중복으로 만들지 않음 (두 번째부터는 위치만 다시 맞춤)
public static class BastetMapFitBuilder
{
    private const string RootName = "Bastet_MapFit";
    private const string OldAreaName = "Bastet_Area";

    // 칸 좌표 -> 월드 (Ground 타일맵 기준)
    private static Vector3 origin;
    private static Vector3 C(float x, float y) => origin + new Vector3(x, y, 0f);

    private struct Area
    {
        public string obj, label; public int x0, y0, x1, y1; public Color color;
        public Area(string o, string l, int a, int b, int c, int d, Color col) { obj = o; label = l; x0 = a; y0 = b; x1 = c; y1 = d; color = col; }
        public Vector3 Center => C((x0 + x1 + 1) * 0.5f, (y0 + y1 + 1) * 0.5f);
        public Vector2 Size => new Vector2(x1 - x0 + 1, y1 - y0 + 1);
    }

    private static readonly Area[] Areas =
    {
        new Area("Hall_Center",     "가운데 홀 (시작)",        -8, -9,   7,  8, new Color(0.8f, 0.8f, 0.8f)),
        new Area("Room1_Footprint", "방 1 발자국의 방 (B1)", -19, -9, -13, -3, new Color(1f, 0.55f, 0.65f)),
        new Area("Room2_Eye",       "방 2 눈의 방 (B2)",     -22,  4, -14, 12, new Color(0.6f, 0.85f, 1f)),
        new Area("Room3_Necklace",  "방 3 목걸이의 방 (B3)",  13,  4,  21, 12, new Color(0.75f, 0.9f, 0.45f)),
        new Area("Room4_Claw",      "방 4 발톱의 방 (B4)",    12, -9,  18, -3, new Color(1f, 0.8f, 0.45f)),
        new Area("Room5_Shrine",    "방 5 신상의 방 (B5)",    -8, 16,   7, 22, new Color(1f, 0.85f, 0.3f)),
    };

    // 방이 있는지 확인할 칸 (각 방 가운데)
    private static readonly Vector2Int[] CheckCells =
    {
        new Vector2Int(0, 0), new Vector2Int(-16, -6), new Vector2Int(-18, 8), new Vector2Int(17, 8), new Vector2Int(15, -6), new Vector2Int(0, 19)
    };

    [MenuItem("Tools/Bastet/★ 지금 맵(타일)에 맞춰 배치", priority = 1)]
    public static void BuildMenu()
    {
        if (!Build(true)) return;
    }

    // ask = 맵 모양이 다를 때 물어볼지
    public static bool Build(bool ask)
    {
        var log = new StringBuilder("[BastetMapFitBuilder] 지금 맵에 맞춰 배치\n");

        // 1) Ground 타일맵 찾기
        Tilemap ground = FindGround();
        if (ground == null)
        {
            EditorUtility.DisplayDialog("바스테트 배치", "씬에서 Ground 타일맵을 찾지 못했습니다.\nGrid 아래 Ground 타일맵이 있는 Puzzle_Test_Bastet_Tliemap 씬에서 실행해 주세요.", "확인");
            return false;
        }
        origin = ground.CellToWorld(Vector3Int.zero);
        int missing = 0;
        foreach (Vector2Int c in CheckCells) if (!ground.HasTile(new Vector3Int(c.x, c.y, 0))) missing++;
        log.AppendLine($"- Ground 타일맵 '{ground.name}', 기준점 {origin}, 방 확인 {CheckCells.Length - missing}/{CheckCells.Length}");
        if (missing > 0 && ask && !EditorUtility.DisplayDialog("바스테트 배치",
                $"맵 모양이 예상과 {missing}곳 다릅니다 (방 가운데에 바닥 타일이 없음).\n그래도 배치할까요? 배치 후 위치를 직접 옮기셔야 할 수 있습니다.", "배치", "취소"))
            return false;

        // 2) 예전 요청서 크기 배치(Bastet_Area)가 있으면: 걷는 퍼즐만 꺼내고 지움
        GameObject oldArea = BastetWalkBuilder.FindInScene(OldAreaName);
        if (oldArea != null)
        {
            foreach (var w in oldArea.GetComponentsInChildren<BastetWalkPuzzleBase>(true))
                Undo.SetTransformParent(w.transform, null, "퍼즐 꺼내기");
            foreach (var a in oldArea.GetComponentsInChildren<BastetAltarInteractable>(true))
                Undo.SetTransformParent(a.transform, null, "제단 꺼내기");
            foreach (var s in oldArea.GetComponentsInChildren<AnubisClueStele>(true))
                Undo.SetTransformParent(s.transform, null, "비석 꺼내기");
            Undo.DestroyObjectImmediate(oldArea);
            log.AppendLine("- 예전 Bastet_Area(요청서 배치도 크기) 지움 (걷는 퍼즐·제단·비석은 꺼내서 다시 배치)");
        }

        // 3) 루트 + 방 가이드
        GameObject root = BastetWalkBuilder.FindInScene(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, RootName);
        }
        root.transform.position = Vector3.zero;
        var roomT = new Dictionary<string, Transform>();
        foreach (Area a in Areas)
        {
            Transform t = root.transform.Find(a.obj);
            if (t == null)
            {
                var go = new GameObject(a.obj);
                Undo.RegisterCreatedObjectUndo(go, a.obj);
                go.transform.SetParent(root.transform, false);
                t = go.transform;
            }
            t.position = a.Center;
            var ra = t.GetComponent<BastetRoomArea>();
            if (ra == null) ra = Undo.AddComponent<BastetRoomArea>(t.gameObject);
            ra.EditorSetup(a.label, a.Size, a.color);
            roomT[a.obj] = t;
        }
        log.AppendLine("- 방 가이드 6개 (가운데 홀 + 방 1~5)");

        // 4) 걷는 퍼즐: 있으면 옮기고 없으면 만든 뒤, 작은 방에 맞게 간격 정리
        GameObject b1 = Place<BastetFootprintWalk>("B1", roomT["Room1_Footprint"], C(-15.5f, -5.5f), BastetWalkBuilder.BuildB1At, log);
        GameObject b2 = Place<BastetEyeStatueRing>("B2", roomT["Room2_Eye"], C(-17.5f, 8.5f), BastetWalkBuilder.BuildB2At, log);
        GameObject b5 = Place<BastetSunMoonWalk>("B5", roomT["Room5_Shrine"], C(0f, 19.4f), BastetWalkBuilder.BuildB5At, log);
        // [추가] 그림 붙이기 (Assets/image/Bastet/Puzzle/ 그림이 있으면) -> 그 다음 발 위치 기준으로 간격 정리
        BastetArtApplier.Apply(BastetArtApplier.BigPlate, log);
        FitB1(b1.transform);
        FitB2(b2.transform);
        FitB5(b5.transform);
        log.AppendLine("- [수정] 간격: B1 발자국 ∧ 모양 좌우 대칭, B2 석상 좌우 대칭 반원(1.5칸), B5 발판 1.7칸 이상");

        // 5) 북쪽 출구 막이: 토트 출구(Exit_ToThoth) 바로 앞. B5를 풀면 꺼짐
        GameObject exit = BastetWalkBuilder.FindInScene("Exit_ToThoth");
        Vector3 doorPos = exit != null ? exit.transform.position + new Vector3(0f, -0.9f, 0f) : C(0f, 22f);
        GameObject door = BastetWalkBuilder.FindInScene("Door_North_Exit");
        if (door == null)
        {
            door = BastetWalkBuilder.MakeBox("Door_North_Exit", roomT["Room5_Shrine"], Vector3.zero, new Vector2(2f, 0.6f), new Color(0.66f, 0.42f, 0.36f), 0);
            door.AddComponent<BoxCollider2D>();
            Undo.RegisterCreatedObjectUndo(door, "Door_North_Exit");
        }
        Undo.RecordObject(door.transform, "출구 막이");
        door.transform.position = doorPos;
        var sun = b5.GetComponent<BastetSunMoonWalk>();
        var sso = new SerializedObject(sun);
        sso.FindProperty("worldExitDoor").objectReferenceValue = door;
        sso.ApplyModifiedProperties();
        log.AppendLine("- Door_North_Exit: 토트 출구 앞 (B5 클리어 때 꺼짐)");

        // 6) 예전 UI B1·B2·B5 제단 끄기 (B3·B4 제단은 지금 자리 그대로)
        foreach (var altar in Object.FindObjectsByType<BastetAltarInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            PuzzleBase pz = altar.TargetPuzzle;
            if ((pz is BastetFootprintPuzzle || pz is BastetEyeSequencePuzzle || pz is BastetSunMoonMemoryPuzzle || pz is BastetMainPuzzle)
                && altar.gameObject.activeSelf)
            {
                Undo.RecordObject(altar.gameObject, "예전 제단 끄기");
                altar.gameObject.SetActive(false);
                log.AppendLine($"- 예전 UI 제단 '{altar.name}' 끔 (걷는 퍼즐로 대체)");
            }
        }

        // 7) 봉인석 (앞 방 보상을 넣으면 열림, 아이템 사라짐) + 가운데 홀 바닥 문양
        Transform hall = roomT["Hall_Center"];
        BastetAreaBuilder.MakeGate("Gate_Room1", "발자국의 방", "", BastetItemIds.FlagB1Clear, null, C(-6.5f, -4f), hall, false, log);
        BastetAreaBuilder.MakeGate("Gate_Room2", "눈의 방", BastetItemIds.Paw, BastetItemIds.FlagB2Clear, C(-10.5f, 7f), C(-6.5f, 7f), hall, true, log);
        BastetAreaBuilder.MakeGate("Gate_Room3", "목걸이의 방", BastetItemIds.Eye, BastetItemIds.FlagB3Clear, C(10.5f, 7f), C(6.5f, 7f), hall, true, log);
        BastetAreaBuilder.MakeGate("Gate_Room4", "발톱의 방", BastetItemIds.Necklace, BastetItemIds.FlagB4Clear, C(10f, -4f), C(6.5f, -4f), hall, true, log);
        SetupMainGate(hall, log);

        // 8) 가운데 홀 횃불 제단 2개 (시작 지점 양옆)
        var altarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Common/TorchAltar.prefab");
        for (int i = 0; i < 2; i++)
        {
            string n = $"TorchAltar_Hall_{i}";
            Vector3 p = C(i == 0 ? -5.5f : 5.5f, -7.5f);
            Transform old = hall.Find(n);
            if (old != null) { Undo.RecordObject(old, "제단 위치"); old.position = p; continue; }
            GameObject b = altarPrefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(altarPrefab, hall)
                                               : BastetWalkBuilder.MakeBrazierAt(n, hall, p);
            b.name = n;
            b.transform.position = p;
            if (altarPrefab != null) Undo.RegisterCreatedObjectUndo(b, n);
            log.AppendLine($"- {n} ({(altarPrefab != null ? "TorchAltar 프리팹" : "임시 화로")}) 가운데 홀 아래");
        }

        // 9) 방 불빛 + 다음 방 안내 (시작: 가운데 홀·방 1만 밝음)
        SetupZones(root.transform, roomT, log);

        // 10) 걷는 퍼즐 비석 (실전 방 세팅이 만든 비석이 있으면 석상 옆으로. B3·B4·입구 비석은 그대로)
        BastetAreaBuilder.PlaceStele("Stele_B1", C(-13.9f, -4.2f), roomT["Room1_Footprint"], log);
        BastetAreaBuilder.PlaceStele("Stele_B2", C(-15.2f, 10.8f), roomT["Room2_Eye"], log);
        BastetAreaBuilder.PlaceStele("Stele_B5", C(-6.2f, 20.5f), roomT["Room5_Shrine"], log);

        // 11) 전역 조명이 밝으면 불빛 차이가 안 보임
        foreach (Light2D l in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (l.lightType != Light2D.LightType.Global || l.intensity <= 0.45f) continue;
            Undo.RecordObject(l, "전역 조명");
            l.intensity = 0.35f;
            log.AppendLine($"- 전역 조명 '{l.name}' 밝기 -> 0.35");
        }

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log(log.ToString(), root);
        return true;
    }

    // ================= [수정] 퍼즐 간격 (발판끼리 1.7칸 이상, 석상은 발 위치 기준) =================
    // [수정] B1: 방 7x7칸 (가운데 기준 -3~3칸). 석상은 위 가운데, 발자국 7개는 석상 아래 ∧ 모양 (좌우 대칭)
    //            [석상]
    //              3
    //           2     4
    //        1           5
    //     0                 6        (번호 = Footprint_i, 밟는 순서는 BastetFootprintWalk.sequence)
    // 이웃 발자국 1.18칸, 밟힘 범위(반지름 0.35)끼리 0.48칸 떨어져 있어 두 개가 같이 눌리지 않음
    private const float B1Apex = 0.35f;  // 가운데(꼭대기) 발자국 높이
    private const float B1StepX = 0.95f; // 옆 발자국까지 가로 간격
    private const float B1StepY = 0.7f;  // 옆으로 갈수록 내려가는 높이

    private static void FitB1(Transform root)
    {
        SetStatue(root, "PawStatue", new Vector2(0f, 1.25f));
        for (int i = 0; i < 7; i++)
        {
            int k = i - 3; // -3 ~ 3 (왼쪽 끝 ~ 오른쪽 끝)
            SetLocal(root, $"Footprint_{i}", new Vector2(k * B1StepX, B1Apex - Mathf.Abs(k) * B1StepY));
        }
    }

    // [수정] B2: 방 9x9칸 (-4~4칸). 큰 석상은 위 가운데(x 0), 작은 석상 6개는 1.5칸 간격 반원 (좌우 대칭)
    // 오른쪽 아래(y -2.5~-0.5)가 홀로 나가는 통로라서 반원을 통로보다 위에 둠
    private static void FitB2(Transform root)
    {
        SetStatue(root, "BigCatStatue", new Vector2(0f, 1.6f));
        for (int i = 0; i < 6; i++)
        {
            float x = -3.75f + i * 1.5f;
            SetStatue(root, $"SmallCat_{i}", new Vector2(x, -0.1f - x * x * 0.035f));
        }
    }

    // B5: 방 16x7칸 (가로 -7.5~7.5, 세로 -3~3). 신상은 가운데, 석상(바깥 타원)·발판(안쪽 타원)
    // 발판 타원 가로 4.8 / 세로 2.2칸 -> 이웃 발판 1.7칸 이상
    private static void FitB5(Transform root)
    {
        SetLocal(root, "BastetShrine", new Vector2(0f, -0.2f)); // 신상 그림 전: 임시 네모 가운데 기준 (위쪽 토트 출구로 가는 길을 남김)
        float[] angles = { 150f, 195f, 240f, 300f, 345f, 30f };
        for (int i = 0; i < 6; i++)
        {
            float a = angles[i] * Mathf.Deg2Rad;
            Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            SetStatue(root, $"RingCat_{i}", new Vector2(d.x * 6.8f, d.y * 2.9f));
            SetLocal(root, $"RingStep_{i}", new Vector2(d.x * 4.8f, d.y * 2.2f));
        }
    }

    // [추가] 석상: 그림(바닥 기준)이면 발 위치 그대로, 임시 네모(가운데 기준)면 높이 절반만큼 올림
    private static void SetStatue(Transform root, string child, Vector2 feet)
    {
        Transform t = root.Find(child);
        var sr = t != null ? t.GetComponent<SpriteRenderer>() : null;
        bool placeholder = sr == null || sr.sprite == null || sr.sprite.name == "UISprite";
        SetLocal(root, child, placeholder && t != null ? feet + new Vector2(0f, t.localScale.y * 0.5f) : feet);
    }

    private static void SetLocal(Transform root, string child, Vector2 local)
    {
        Transform t = root.Find(child);
        if (t == null) { Debug.LogWarning($"[BastetMapFitBuilder] {root.name}/{child} 없음 (이름을 바꿨으면 원래대로)"); return; }
        Undo.RecordObject(t, "간격");
        t.localPosition = new Vector3(local.x, local.y, t.localPosition.z);
    }

    private static GameObject Place<TP>(string label, Transform room, Vector3 worldPos, System.Func<Vector3, GameObject> build, StringBuilder log)
        where TP : Component
    {
        TP existing = Object.FindFirstObjectByType<TP>(FindObjectsInactive.Include);
        if (existing != null)
        {
            BastetAreaBuilder.MoveTo(existing.transform, room, worldPos, $"{label} 걷는 퍼즐 '{existing.name}'", log);
            return existing.gameObject;
        }
        GameObject root = build(worldPos);
        root.transform.SetParent(room, true);
        log.AppendLine($"- {label} 걷는 퍼즐 새로 만듦 -> {room.name}");
        return root;
    }

    // ================= 방 5 입구 (Gate_T) =================
    // 예전 Gate_T(AnubisItemGate, 아이템 4개 확인)를 봉인석으로 바꿈: 발톱 인장을 넣으면 열림 (사라짐)
    private static void SetupMainGate(Transform hall, StringBuilder log)
    {
        GameObject gateT = BastetWalkBuilder.FindInScene("Gate_T");
        if (gateT == null)
        {
            BastetAreaBuilder.MakeGate("Gate_Room5", "신상의 방", BastetItemIds.Claw, BastetItemIds.FlagMainDone, C(0f, 10f), C(0f, 7.5f), hall, true, log);
            return;
        }
        var oldGate = gateT.GetComponent<AnubisItemGate>();
        if (oldGate != null)
        {
            Undo.DestroyObjectImmediate(oldGate); // E 대상이 둘이면 예전 것이 먼저 잡히므로 지움
            log.AppendLine("- Gate_T의 AnubisItemGate(아이템 4개 확인) 지움");
        }
        if (gateT.GetComponent<Collider2D>() == null) Undo.AddComponent<BoxCollider2D>(gateT);
        if (gateT.GetComponent<SpriteRenderer>() == null)
        {
            var sr = Undo.AddComponent<SpriteRenderer>(gateT);
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.color = new Color(0.66f, 0.42f, 0.36f);
            sr.sortingLayerName = "Objects";
        }

        // 바닥 문양 (가운데 홀 위쪽, 북쪽 통로 앞)
        Transform glyphT = hall.Find("Gate_T_Glyph");
        SpriteRenderer glyph;
        Light2D gl;
        if (glyphT == null)
        {
            var g = new GameObject("Gate_T_Glyph");
            Undo.RegisterCreatedObjectUndo(g, "Gate_T_Glyph");
            g.transform.SetParent(hall, false);
            g.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
            glyph = g.AddComponent<SpriteRenderer>();
            glyph.sprite = BastetWalkBuilder.Knob();
            glyph.sortingLayerName = "Objects";
            glyph.sortingOrder = -7;
            gl = BastetWalkBuilder.AddPointLight(g, 1.2f);
            gl.intensity = 0.6f;
            glyphT = g.transform;
        }
        else { glyph = glyphT.GetComponent<SpriteRenderer>(); gl = glyphT.GetComponent<Light2D>(); }
        glyphT.position = C(0f, 7.5f);

        var gate = gateT.GetComponent<BastetRoomGate>();
        if (gate == null) gate = Undo.AddComponent<BastetRoomGate>(gateT);
        Undo.RecordObject(gate, "방 5 봉인석");
        gate.EditorSetup("신상의 방", BastetItemIds.Claw, BastetItemIds.FlagMainDone, glyph, gl);
        EditorUtility.SetDirty(gate);
        log.AppendLine("- Gate_T -> 방 5 봉인석 (발톱 인장을 넣으면 열림, 아이템 사라짐)");
    }

    // ================= 방 불빛 =================
    private static void SetupZones(Transform root, Dictionary<string, Transform> roomT, StringBuilder log)
    {
        Transform guideT = root.Find("ZoneGuide");
        if (guideT == null)
        {
            var go = new GameObject("ZoneGuide");
            Undo.RegisterCreatedObjectUndo(go, "ZoneGuide");
            go.transform.SetParent(root, false);
            guideT = go.transform;
        }
        var guide = guideT.GetComponent<AnubisZoneGuide>();
        if (guide == null) guide = Undo.AddComponent<AnubisZoneGuide>(guideT.gameObject);

        var zones = new (string room, string label, string item, string msg)[]
        {
            ("Hall_Center", "가운데 홀", "", ""),
            ("Room1_Footprint", "발자국의 방", "", ""),
            ("Room2_Eye", "눈의 방", BastetItemIds.Paw, "서쪽 위 눈의 방에 불이 켜졌다! 발자국 인장을 봉인석에 넣자."),
            ("Room3_Necklace", "목걸이의 방", BastetItemIds.Eye, "동쪽 위 목걸이의 방에 불이 켜졌다! 호박 눈을 봉인석에 넣자."),
            ("Room4_Claw", "발톱의 방", BastetItemIds.Necklace, "동쪽 아래 발톱의 방에 불이 켜졌다! 목걸이 구슬을 봉인석에 넣자."),
            ("Room5_Shrine", "신상의 방", BastetItemIds.Claw, "북쪽 신상의 방에 불이 켜졌다! 발톱 인장을 북쪽 봉인석에 넣자."),
        };

        var so = new SerializedObject(guide);
        SerializedProperty relock = so.FindProperty("relockOnConsume");
        if (relock != null) relock.boolValue = false;
        SerializedProperty arr = so.FindProperty("zones");
        arr.arraySize = zones.Length;
        for (int i = 0; i < zones.Length; i++)
        {
            Transform room = roomT[zones[i].room];
            Light2D[] lights = BastetAreaBuilder.EnsureRoomLights(room);
            BastetAreaBuilder.FillZone(arr.GetArrayElementAtIndex(i), zones[i].label, zones[i].item, zones[i].msg, room, lights);
        }
        so.ApplyModifiedProperties();
        log.AppendLine("- ZoneGuide: 시작 = 가운데 홀·방 1 불 켜짐, 보상마다 다음 방 카메라 이동 + 흔들림 + 불 켜짐");
    }

    private static Tilemap FindGround()
    {
        Tilemap best = null;
        foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (tm.name == "Ground") return tm;
            if (best == null) best = tm;
        }
        return best;
    }
}
#endif
