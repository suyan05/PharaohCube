#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 메뉴: Tools/Bastet/5개 방 배치 만들기 (요청서 배치도)
// 아트 요청서 3장 배치도(58 x 31칸)대로 방 5개 가이드 + 퍼즐 + 봉인석 + 바닥 문양 + 화로를 한 번에 배치
// - 기준점 = 메인 방(방 5) 가운데 = Scene 뷰 가운데(정수 칸으로 맞춤). 이미 Bastet_Area가 있으면 그 위치를 씀
// - 이미 있는 걷는 퍼즐(Walk_B1/B2/B5)과 B3·B4 제단은 새로 만들지 않고 방 자리로 옮김
// - 타일(바닥·벽)은 Scene 뷰의 분홍 가이드 사각형을 보고 Tile Palette로 직접 칠함
// - 여러 번 눌러도 중복으로 만들지 않음
public static class BastetAreaBuilder
{
    private const string AreaName = "Bastet_Area";
    private const string ObjectLayer = "Objects";

    // 요청서 배치도 칸 좌표(왼쪽 위 0,0, 아래로 +) -> 메인 방 가운데(32,18) 기준 유니티 좌표(위로 +)
    private static Vector3 T(float tx, float ty) => new Vector3(tx - 32f, 18f - ty, 0f);

    // 바닥 사각형 (x0, y0, x1, y1 칸, 포함) -> 가운데와 크기
    private static void RectOf(int x0, int y0, int x1, int y1, out Vector3 center, out Vector2 size)
    {
        size = new Vector2(x1 - x0 + 1, y1 - y0 + 1);
        center = T(x0 + size.x * 0.5f, y0 + size.y * 0.5f);
    }

    private struct RoomDef
    {
        public string objName, label; public int x0, y0, x1, y1; public Color color;
        public RoomDef(string o, string l, int a, int b, int c, int d, Color col) { objName = o; label = l; x0 = a; y0 = b; x1 = c; y1 = d; color = col; }
    }

    private static readonly RoomDef[] Rooms =
    {
        new RoomDef("Room1_Footprint", "방 1 발자국의 방 (B1)", 2, 17, 15, 27, new Color(1f, 0.55f, 0.65f)),
        new RoomDef("Room2_Eye",       "방 2 눈의 방 (B2)",     1, 3, 17, 12, new Color(0.6f, 0.85f, 1f)),
        new RoomDef("Room3_Necklace",  "방 3 목걸이의 방 (B3)", 46, 5, 56, 12, new Color(0.75f, 0.9f, 0.45f)),
        new RoomDef("Room4_Claw",      "방 4 발톱의 방 (B4)",   46, 18, 56, 26, new Color(1f, 0.8f, 0.45f)),
        new RoomDef("Room5_Shrine",    "방 5 신상의 방 (메인 B5)", 22, 8, 41, 27, new Color(1f, 0.85f, 0.3f)),
        new RoomDef("Corridor_1_5",    "통로 1-5", 16, 21, 21, 22, new Color(0.8f, 0.8f, 0.8f)),
        new RoomDef("Corridor_2_5",    "통로 2-5", 18, 9, 21, 10, new Color(0.8f, 0.8f, 0.8f)),
        new RoomDef("Corridor_5_3",    "통로 5-3", 42, 10, 45, 11, new Color(0.8f, 0.8f, 0.8f)),
        new RoomDef("Corridor_5_4",    "통로 5-4", 42, 21, 45, 22, new Color(0.8f, 0.8f, 0.8f)),
        new RoomDef("Corridor_South",  "남쪽 입구 (중앙 홀)", 31, 28, 32, 30, new Color(0.8f, 0.8f, 0.8f)),
        new RoomDef("Corridor_North",  "북쪽 출구", 31, 6, 32, 7, new Color(0.8f, 0.8f, 0.8f)),
    };

    [MenuItem("Tools/Bastet/5개 방 배치 만들기 (요청서 배치도)")]
    public static void Build()
    {
        var log = new StringBuilder("[BastetAreaBuilder] 5개 방 배치 결과\n");

        // 1) 구역 루트
        GameObject area = BastetWalkBuilder.FindInScene(AreaName);
        if (area == null)
        {
            Vector3 p = BastetWalkBuilder.Pivot();
            area = new GameObject(AreaName);
            area.transform.position = new Vector3(Mathf.Round(p.x), Mathf.Round(p.y), 0f); // 타일 칸에 맞춤
            Undo.RegisterCreatedObjectUndo(area, AreaName);
            log.AppendLine($"- {AreaName} 새로 만듦 (기준점 = 방 5 가운데 {area.transform.position})");
        }
        else log.AppendLine($"- 기존 {AreaName} 사용 ({area.transform.position})");
        Transform A = area.transform;

        // [추가] 크기 배율 (Tools/Bastet/5개 방 크기 조절). 처음 만들 때 = 1
        var layout = area.GetComponent<BastetAreaLayout>();
        if (layout == null) layout = Undo.AddComponent<BastetAreaLayout>(area);
        float S = layout.Scale;
        float puzzleRatio = S / Mathf.Max(0.01f, layout.PuzzleScale); // 이미 있는 걷는 퍼즐 간격을 맞출 비율
        log.AppendLine($"- 크기 배율 {S:0.00} (걷는 퍼즐 간격 x{puzzleRatio:0.00})");
        System.Func<Vector3, Vector3> W = local => A.position + local * S; // 구역 기준 -> 월드 (배율 적용)

        // 2) 방/통로 가이드
        var roomT = new Dictionary<string, Transform>();
        foreach (RoomDef r in Rooms)
        {
            RectOf(r.x0, r.y0, r.x1, r.y1, out Vector3 c, out Vector2 size);
            ScaleRect(r.objName.StartsWith("Corridor"), S, ref c, ref size); // [추가] 배율 + 칸에 맞춤
            Transform t = A.Find(r.objName);
            if (t == null)
            {
                var go = new GameObject(r.objName);
                Undo.RegisterCreatedObjectUndo(go, r.objName);
                go.transform.SetParent(A, false);
                t = go.transform;
            }
            t.localPosition = c;
            var ra = t.GetComponent<BastetRoomArea>();
            if (ra == null) ra = Undo.AddComponent<BastetRoomArea>(t.gameObject); // ?? 대신 if (에디터의 가짜 null 때문)
            ra.EditorSetup(r.label, size, r.color);
            roomT[r.objName] = t;
        }
        log.AppendLine($"- 방 5개 + 통로 6개 가이드 (Scene 뷰 분홍 사각형 = 바닥 칠할 자리, 위쪽 띠 = 벽 2칸)");

        // 3) 북쪽 출구 막이 (B5 클리어 때 꺼짐). 없으면 임시로 만듦
        GameObject northDoor = BastetWalkBuilder.FindInScene("Door_North_Exit");
        if (northDoor != null && northDoor.transform.parent == roomT["Corridor_North"]) northDoor.transform.position = W(T(32f, 7.5f)); // [추가] 크기 조절 때 따라감
        if (northDoor == null)
        {
            GameObject door = BastetWalkBuilder.MakeBox("Door_North_Exit", null, Vector3.zero, new Vector2(2f, 1f), new Color(0.66f, 0.42f, 0.36f), 0);
            door.transform.position = W(T(32f, 7.5f));
            door.transform.SetParent(roomT["Corridor_North"], true);
            door.AddComponent<BoxCollider2D>();
            Undo.RegisterCreatedObjectUndo(door, "Door_North_Exit");
            log.AppendLine("- Door_North_Exit 임시 막이 만듦 (북쪽 출구 통로)");
        }

        // 4) 걷는 퍼즐 B1 / B2 / B5: 있으면 옮기고, 없으면 만듦
        PlacePuzzle<BastetFootprintWalk>("B1", roomT["Room1_Footprint"], W(T(8.5f, 23.4f)), BastetWalkBuilder.BuildB1At, S, puzzleRatio, log);
        PlacePuzzle<BastetEyeStatueRing>("B2", roomT["Room2_Eye"], W(T(9f, 8.4f)), BastetWalkBuilder.BuildB2At, S, puzzleRatio, log);
        PlacePuzzle<BastetSunMoonWalk>("B5", roomT["Room5_Shrine"], W(T(31.5f, 19.4f)), BastetWalkBuilder.BuildB5At, S, puzzleRatio, log);
        Undo.RecordObject(layout, "퍼즐 배율");
        layout.EditorSetPuzzleScale(S);

        // 5) B3·B4 제단 (UI 퍼즐) 옮기기, 예전 UI B1·B2·B5 제단은 끄기
        foreach (var altar in Object.FindObjectsByType<BastetAltarInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            PuzzleBase pz = altar.TargetPuzzle;
            if (pz is BastetNecklacePuzzle) MoveTo(altar.transform, roomT["Room3_Necklace"], W(T(51.5f, 8.4f)), "B3 목걸이 제단", log);
            else if (pz is BastetClawPatternPuzzle) MoveTo(altar.transform, roomT["Room4_Claw"], W(T(51.5f, 17.6f)), "B4 발톱 벽 (북쪽 벽 앞)", log);
            else if (pz is BastetFootprintPuzzle || pz is BastetEyeSequencePuzzle || pz is BastetSunMoonMemoryPuzzle)
            {
                if (altar.gameObject.activeSelf)
                {
                    Undo.RecordObject(altar.gameObject, "예전 제단 끄기");
                    altar.gameObject.SetActive(false);
                    log.AppendLine($"- 예전 UI 제단 '{altar.name}' 끔 (걷는 퍼즐로 대체, 확인 후 지워도 됨)");
                }
            }
        }

        // 6) 서브 방 입구: 봉인석 + 메인 방 바닥 문양 (방 1은 처음부터 열림 -> 봉인석 없이 문양만)
        Transform r5 = roomT["Room5_Shrine"];
        // [수정] 봉인석에 앞 방 보상을 넣으면 열림 (아이템 사라짐), 문양 금색 = 그 방 클리어 플래그
        MakeGate("Gate_Room1", "발자국의 방", "", BastetItemIds.FlagB1Clear, null, W(T(23.5f, 22f)), r5, false, log);
        MakeGate("Gate_Room2", "눈의 방", BastetItemIds.Paw, BastetItemIds.FlagB2Clear, W(T(20f, 10f)), W(T(23.5f, 10f)), r5, true, log);
        MakeGate("Gate_Room3", "목걸이의 방", BastetItemIds.Eye, BastetItemIds.FlagB3Clear, W(T(44f, 11f)), W(T(40.5f, 11f)), r5, true, log);
        MakeGate("Gate_Room4", "발톱의 방", BastetItemIds.Necklace, BastetItemIds.FlagB4Clear, W(T(44f, 22f)), W(T(40.5f, 22f)), r5, true, log);

        // 7) 방 5 입구 횃불 제단 2개 (꺼진 횃불을 다시 붙이는 곳). [수정] TorchAltar 프리팹이 있으면 그것을 씀
        var altarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Common/TorchAltar.prefab");
        for (int i = 0; i < 2; i++)
        {
            string n = $"Brazier_Room5_{i}";
            Vector3 bp = W(T(i == 0 ? 29.5f : 34.5f, 26.5f));
            Transform oldB = r5.Find(n);
            if (oldB != null) { Undo.RecordObject(oldB, "제단 위치"); oldB.position = bp; continue; } // [수정] 크기 조절 때 위치만 갱신
            GameObject b;
            if (altarPrefab != null)
            {
                b = (GameObject)PrefabUtility.InstantiatePrefab(altarPrefab, r5);
                b.name = n;
                b.transform.position = bp;
                Undo.RegisterCreatedObjectUndo(b, n);
            }
            else
            {
                b = BastetWalkBuilder.MakeBrazierAt(n, null, bp);
                b.transform.SetParent(r5, true);
            }
            log.AppendLine($"- {n} ({(altarPrefab != null ? "TorchAltar 프리팹" : "임시 화로, 프리팹을 먼저 만들면 제단으로 바뀜")}) 방 5 입구 옆");
        }

        // 9) [추가] 방 불빛 + 다음 방 안내 연출 (아누비스 방과 같은 AnubisZoneGuide)
        //    시작: 방 5(통로)와 방 1만 불이 켜짐 -> 서브 보상을 얻으면 카메라가 다음 방을 비추고 흔들리며 불이 켜짐
        SetupZoneGuide(A, roomT, log);

        // 10) [추가] 토트 출구 막이(Gate_T): 보상 아이템은 봉인석에 넣어 사라지므로 형판(PLATE_BASTET)만 확인
        GameObject gateT = BastetWalkBuilder.FindInScene("Gate_T");
        var itemGate = gateT != null ? gateT.GetComponent<AnubisItemGate>() : null;
        if (itemGate != null)
        {
            var gso = new SerializedObject(itemGate);
            SerializedProperty req = gso.FindProperty("requiredItems");
            if (req != null)
            {
                req.arraySize = 1;
                req.GetArrayElementAtIndex(0).stringValue = BastetItemIds.Plate;
                gso.ApplyModifiedProperties();
                log.AppendLine("- Gate_T 필요 아이템 -> PLATE_BASTET 하나로 (서브 보상은 봉인석에 넣어 사라지므로)");
            }
        }

        // 11) [추가] 방이 너무 밝으면 불빛 차이가 안 보임 -> 전역 조명 낮춤
        foreach (Light2D l in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (l.lightType != Light2D.LightType.Global || l.intensity <= 0.45f) continue;
            Undo.RecordObject(l, "전역 조명");
            l.intensity = 0.35f;
            log.AppendLine($"- 전역 조명 '{l.name}' 밝기 -> 0.35 (불 켜진 방만 밝게 보이게)");
        }

        // 8) 비석: 실전 방 세팅이 만든 비석이 있으면 각 방 석상/제단 옆으로 (없으면 건너뜀)
        PlaceStele("Slate_Bastet_Hall", W(T(25f, 25.5f)), r5, log);
        foreach (var st in Object.FindObjectsByType<BastetPuzzleStarter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (st.Target is BastetFootprintWalk) PlaceStele("Stele_B1", st.transform.position + new Vector3(2.2f, 0.3f, 0f), roomT["Room1_Footprint"], log);
            else if (st.Target is BastetEyeStatueRing) PlaceStele("Stele_B2", st.transform.position + new Vector3(2.2f, 0.3f, 0f), roomT["Room2_Eye"], log);
            else if (st.Target is BastetSunMoonWalk) PlaceStele("Stele_B5", W(T(27f, 25.5f)), r5, log);
        }
        PlaceStele("Stele_B3", W(T(48.5f, 8.6f)), roomT["Room3_Necklace"], log);
        PlaceStele("Stele_B4", W(T(48.5f, 21.6f)), roomT["Room4_Claw"], log);

        EditorSceneManager.MarkSceneDirty(area.scene);
        Selection.activeGameObject = area;
        log.AppendLine("\n다음: 1) 분홍 가이드대로 타일 칠하기  2) Tools/Bastet/실전 방 세팅 다시 실행 (잠금·비석)  3) Ctrl+S");
        Debug.Log(log.ToString(), area);
    }

    // [추가] 방마다 불빛 2개 + AnubisZoneGuide 구역 연결
    private struct ZoneDef
    {
        public string room, label, item, msg;
        public ZoneDef(string r, string l, string i, string m) { room = r; label = l; item = i; msg = m; }
    }

    private static readonly ZoneDef[] Zones =
    {
        new ZoneDef("Room5_Shrine", "신상의 방", "", ""),
        new ZoneDef("Room1_Footprint", "발자국의 방", "", ""),
        new ZoneDef("Room2_Eye", "눈의 방", BastetItemIds.Paw, "서북쪽 눈의 방에 불이 켜졌다! 발자국 인장을 봉인석에 넣자."),
        new ZoneDef("Room3_Necklace", "목걸이의 방", BastetItemIds.Eye, "동북쪽 목걸이의 방에 불이 켜졌다! 호박 눈을 봉인석에 넣자."),
        new ZoneDef("Room4_Claw", "발톱의 방", BastetItemIds.Necklace, "동남쪽 발톱의 방에 불이 켜졌다! 목걸이 구슬을 봉인석에 넣자."),
    };

    private static void SetupZoneGuide(Transform area, Dictionary<string, Transform> roomT, StringBuilder log)
    {
        Transform guideT = area.Find("ZoneGuide");
        if (guideT == null)
        {
            var go = new GameObject("ZoneGuide");
            Undo.RegisterCreatedObjectUndo(go, "ZoneGuide");
            go.transform.SetParent(area, false);
            guideT = go.transform;
        }
        var guide = guideT.GetComponent<AnubisZoneGuide>();
        if (guide == null) guide = Undo.AddComponent<AnubisZoneGuide>(guideT.gameObject);

        var so = new SerializedObject(guide);
        SerializedProperty relock = so.FindProperty("relockOnConsume");
        if (relock != null) relock.boolValue = false; // 봉인석에 넣어도 불 유지
        SerializedProperty zones = so.FindProperty("zones");
        zones.arraySize = Zones.Length + 1; // + 신상 불빛 (발톱 인장)
        for (int i = 0; i < Zones.Length; i++)
        {
            ZoneDef z = Zones[i];
            Transform room = roomT[z.room];
            Light2D[] lights = EnsureRoomLights(room);
            FillZone(zones.GetArrayElementAtIndex(i), z.label, z.item, z.msg, room, lights);
        }
        // 신상: 발톱 인장을 얻으면 신상 위 불빛이 켜지며 카메라가 비춤
        Transform r5 = roomT["Room5_Shrine"];
        Light2D shrineLight = EnsureLight(r5, "ShrineLight", new Vector3(-0.5f, 4.5f, 0f), 3.5f, new Color(1f, 0.85f, 0.45f));
        FillZone(zones.GetArrayElementAtIndex(Zones.Length), "바스테트 신상", BastetItemIds.Claw,
            "바스테트 신상의 눈이 빛난다! 발톱 인장을 신상에 바치자.", shrineLight.transform, new[] { shrineLight });
        so.ApplyModifiedProperties();
        log.AppendLine("- ZoneGuide (AnubisZoneGuide): 시작 = 방 5·방 1 불 켜짐, 보상마다 다음 방 카메라 이동 + 흔들림 + 불 켜짐");
    }

    internal static void FillZone(SerializedProperty z, string label, string item, string msg, Transform focus, Light2D[] lights)
    {
        z.FindPropertyRelative("zoneName").stringValue = label;
        z.FindPropertyRelative("unlockItem").stringValue = item;
        z.FindPropertyRelative("unlockMessage").stringValue = msg;
        z.FindPropertyRelative("focusPoint").objectReferenceValue = string.IsNullOrEmpty(item) ? null : focus; // 시작부터 켜진 방은 카메라 이동 없음
        SerializedProperty fl = z.FindPropertyRelative("fadeLights");
        fl.arraySize = lights.Length;
        for (int k = 0; k < lights.Length; k++) fl.GetArrayElementAtIndex(k).objectReferenceValue = lights[k];
    }

    // 방 가운데 양옆에 불빛 2개 (방 크기에 맞춘 반경)
    internal static Light2D[] EnsureRoomLights(Transform room)
    {
        var area = room.GetComponent<BastetRoomArea>();
        Vector2 size = area != null ? area.Size : new Vector2(12f, 10f);
        float r = Mathf.Max(size.x, size.y) * 0.45f;
        Color warm = new Color(1f, 0.78f, 0.5f);
        return new[]
        {
            EnsureLight(room, "RoomLight_L", new Vector3(-size.x * 0.25f, 0f, 0f), r, warm),
            EnsureLight(room, "RoomLight_R", new Vector3(size.x * 0.25f, 0f, 0f), r, warm),
        };
    }

    internal static Light2D EnsureLight(Transform parent, string name, Vector3 local, float radius, Color color)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, name);
            go.transform.SetParent(parent, false);
            t = go.transform;
        }
        t.localPosition = local;
        var l = t.GetComponent<Light2D>();
        if (l == null) l = Undo.AddComponent<Light2D>(t.gameObject);
        l.lightType = Light2D.LightType.Point;
        l.pointLightOuterRadius = radius;
        l.pointLightInnerRadius = radius * 0.3f;
        l.intensity = 0.9f;
        l.color = color;
        return l;
    }

    internal static void PlaceStele(string name, Vector3 worldPos, Transform room, StringBuilder log)
    {
        GameObject go = BastetWalkBuilder.FindInScene(name);
        if (go == null) return;
        MoveTo(go.transform, room, worldPos, $"비석 {name}", log);
    }

    // 퍼즐 루트가 있으면 방 아래로 옮기고, 없으면 그 자리에 새로 만듦
    private static void PlacePuzzle<TP>(string label, Transform room, Vector3 worldPos, System.Func<Vector3, GameObject> build,
        float scale, float ratio, StringBuilder log)
        where TP : Component
    {
        TP existing = Object.FindFirstObjectByType<TP>(FindObjectsInactive.Include);
        if (existing != null)
        {
            MoveTo(existing.transform, room, worldPos, $"{label} 걷는 퍼즐 '{existing.name}'", log);
            if (!Mathf.Approximately(ratio, 1f)) ScaleSpacing(existing.transform, ratio); // [추가] 발자국·석상 간격
            return;
        }
        GameObject root = build(worldPos);
        root.transform.SetParent(room, true);
        if (!Mathf.Approximately(scale, 1f)) ScaleSpacing(root.transform, scale); // [추가] 새로 만든 퍼즐도 배율대로
        log.AppendLine($"- {label} 걷는 퍼즐 새로 만듦 -> {room.name}");
    }

    // [추가] 퍼즐 루트 바로 아래 오브젝트(발자국·석상·시작 석상) 사이 간격만 줄임. 그림 크기는 그대로
    private static void ScaleSpacing(Transform root, float ratio)
    {
        foreach (Transform child in root)
        {
            Undo.RecordObject(child, "간격 조절");
            Vector3 p = child.localPosition;
            child.localPosition = new Vector3(p.x * ratio, p.y * ratio, p.z);
        }
    }

    // [추가] 방 사각형에 배율을 적용하고 타일 칸에 맞춤 (통로는 폭 2칸 유지, 길이만 줄임)
    private static void ScaleRect(bool corridor, float s, ref Vector3 center, ref Vector2 size)
    {
        Vector2 sz;
        if (corridor)
        {
            bool horizontal = size.x >= size.y;
            sz = horizontal ? new Vector2(Mathf.Max(2f, Mathf.Round(size.x * s)), size.y)
                            : new Vector2(size.x, Mathf.Max(2f, Mathf.Round(size.y * s)));
        }
        else sz = new Vector2(Mathf.Max(5f, Mathf.Round(size.x * s)), Mathf.Max(5f, Mathf.Round(size.y * s)));

        // 가장자리가 정수 칸에 오게: 왼쪽 아래 모서리를 반올림
        Vector2 c = new Vector2(center.x * s, center.y * s);
        Vector2 min = new Vector2(Mathf.Round(c.x - sz.x * 0.5f), Mathf.Round(c.y - sz.y * 0.5f));
        center = new Vector3(min.x + sz.x * 0.5f, min.y + sz.y * 0.5f, 0f);
        size = sz;
    }

    internal static void MoveTo(Transform t, Transform parent, Vector3 worldPos, string label, StringBuilder log)
    {
        Undo.SetTransformParent(t, parent, "방으로 옮기기");
        Undo.RecordObject(t, "위치");
        t.position = worldPos;
        log.AppendLine($"- {label} -> {parent.name} 로 옮김");
    }

    // 봉인석(길 막이, 선택) + 바닥 문양(메인 방)
    internal static void MakeGate(string name, string room, string required, string clearFlag, Vector3? stonePos, Vector3 glyphPos,
        Transform parent, bool withStone, StringBuilder log)
    {
        Transform old = parent.Find(name);
        if (old != null)
        {
            // [수정] 이미 있으면 설정만 갱신 (위치·그림은 유지)
            var og = old.GetComponent<BastetRoomGate>();
            Transform oldGlyph = parent.Find($"{name}_Glyph");
            Undo.RecordObject(old, "봉인석 위치");
            old.position = withStone && stonePos.HasValue ? stonePos.Value : glyphPos; // [추가] 크기 조절 때 따라감
            if (oldGlyph != null) { Undo.RecordObject(oldGlyph, "문양 위치"); oldGlyph.position = glyphPos; }
            if (og != null)
            {
                Undo.RecordObject(og, "봉인석 갱신");
                og.EditorSetup(room, required, clearFlag,
                    oldGlyph != null ? oldGlyph.GetComponent<SpriteRenderer>() : null,
                    oldGlyph != null ? oldGlyph.GetComponent<Light2D>() : null);
                EditorUtility.SetDirty(og);
            }
            log.AppendLine($"- {name} 이미 있음 -> 설정만 갱신");
            return;
        }

        // 바닥 문양 (흰 원 임시 그림, 색은 BastetRoomGate가 칠함)
        var glyphGo = new GameObject($"{name}_Glyph");
        glyphGo.transform.SetParent(parent, false);
        glyphGo.transform.position = glyphPos;
        glyphGo.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
        var glyph = glyphGo.AddComponent<SpriteRenderer>();
        glyph.sprite = BastetWalkBuilder.Knob();
        glyph.sortingLayerName = ObjectLayer;
        glyph.sortingOrder = -7; // 바닥 (발자국 판보다 아래)
        Light2D gl = BastetWalkBuilder.AddPointLight(glyphGo, 1.2f);
        gl.intensity = 0.6f;
        Undo.RegisterCreatedObjectUndo(glyphGo, glyphGo.name);

        // 봉인석 (단단한 콜라이더로 통로를 막음). 방 1은 봉인석 없이 빈 오브젝트에 게이트만
        GameObject gate;
        if (withStone && stonePos.HasValue)
        {
            gate = BastetWalkBuilder.MakeBox(name, parent, Vector3.zero, new Vector2(1f, 2f), new Color(0.66f, 0.42f, 0.36f), 1);
            gate.transform.position = stonePos.Value;
            gate.AddComponent<BoxCollider2D>();
        }
        else
        {
            gate = new GameObject(name);
            gate.transform.SetParent(parent, false);
            gate.transform.position = glyphPos;
        }
        Undo.RegisterCreatedObjectUndo(gate, name);

        var g = gate.AddComponent<BastetRoomGate>();
        g.EditorSetup(room, required, clearFlag, glyph, gl);
        log.AppendLine($"- {name}: {room} ({(string.IsNullOrEmpty(required) ? "처음부터 열림" : required + "을(를) 넣으면 열림")}, 클리어 {clearFlag})");
    }
}
#endif
