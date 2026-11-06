#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// 메뉴: Tools/Sobek/2. 허브 씬 만들기, 0. 전체 한 번에 (퍼즐 씬은 5번 SobekIllusionBuilder)
// 허브(Sobek_Hub) = 2D 탐험 방: 물에 잠긴 본당 + 좁은 길 + 퍼즐 섬 4개(게이트) + 소베크의 입(메인)
// 퍼즐은 각자 다른 씬(Sobek_S1~S4, Sobek_Main)의 2.5D 착시 퍼즐. 메인을 풀면 허브 물이 빠지고 물속 아누비스의 문이 드러남
// 허브 맵은 스프라이트 바닥 (나중에 수연님 타일맵으로 바꿔도 판·문·비석 오브젝트는 그대로 옮겨 쓰면 됨)
public static class SobekSceneBuilder
{
    private const string SceneFolder = "Assets/Scenes/Puzzle_Test";
    private const string HubPath = SceneFolder + "/" + SobekIds.HubScene + ".unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";

    // 허브 크기 (유닛 = 칸)
    private const float HubW = 46f, HubH = 32f; // 본당 크기 (유닛)
    private const float HallBottom = 8f;        // 이 아래는 마른 전실, 위는 물에 잠긴 본당

    // 물 위로 걸을 수 있는 곳 (가운데, 크기). 나머지 본당은 전부 물 (물 막이 콜라이더)
    private struct Walk { public string name; public Vector2 center, size; public bool island; }
    private static readonly Walk[] Walkways =
    {
        new Walk { name = "Landing", center = new Vector2(23f, 8.5f), size = new Vector2(12f, 2f) },
        new Walk { name = "Spine", center = new Vector2(23f, 18.5f), size = new Vector2(2f, 18f) },
        new Walk { name = "GaugeIsland", center = new Vector2(23f, 15f), size = new Vector2(6f, 5f), island = true },
        new Walk { name = "StatueIsland", center = new Vector2(23f, 23.5f), size = new Vector2(8f, 5f), island = true },
        new Walk { name = "DoorLedge", center = new Vector2(23f, 29.5f), size = new Vector2(12f, 4f) },
        new Walk { name = "Bridge_S1", center = new Vector2(15f, 25f), size = new Vector2(8f, 2f) },
        new Walk { name = "Bridge_S2", center = new Vector2(31f, 25f), size = new Vector2(8f, 2f) },
        new Walk { name = "Bridge_S3", center = new Vector2(15.5f, 15f), size = new Vector2(9f, 2f) },
        new Walk { name = "Bridge_S4", center = new Vector2(30.5f, 15f), size = new Vector2(9f, 2f) },
        new Walk { name = "Island_S1", center = new Vector2(7f, 25f), size = new Vector2(8f, 8f), island = true },
        new Walk { name = "Island_S2", center = new Vector2(39f, 25f), size = new Vector2(8f, 8f), island = true },
        new Walk { name = "Island_S3", center = new Vector2(7f, 15f), size = new Vector2(8f, 8f), island = true },
        new Walk { name = "Island_S4", center = new Vector2(39f, 15f), size = new Vector2(8f, 8f), island = true },
    };

    private static bool OnWalkway(Vector2 p)
    {
        foreach (Walk w in Walkways)
            if (Mathf.Abs(p.x - w.center.x) <= w.size.x * 0.5f && Mathf.Abs(p.y - w.center.y) <= w.size.y * 0.5f) return true;
        return false;
    }

    // 본당을 1칸 격자로 보고, 길이 아닌 칸을 가로로 이어 붙인 박스 콜라이더로 막음
    private static void BuildWaterBlockers(Transform parent, out Collider2D[] result)
    {
        var go = new GameObject("WaterBlockers");
        go.transform.SetParent(parent, false);
        var list = new List<Collider2D>();
        for (int y = (int)HallBottom; y < (int)HubH; y++)
        {
            int runStart = -1;
            for (int x = 0; x <= (int)HubW; x++)
            {
                bool water = x < (int)HubW && !OnWalkway(new Vector2(x + 0.5f, y + 0.5f));
                if (water && runStart < 0) runStart = x;
                if (!water && runStart >= 0)
                {
                    var col = go.AddComponent<BoxCollider2D>();
                    col.size = new Vector2(x - runStart, 1f);
                    col.offset = new Vector2((runStart + x) * 0.5f, y + 0.5f);
                    list.Add(col);
                    runStart = -1;
                }
            }
        }
        result = list.ToArray();
    }

    [MenuItem("Tools/Sobek/0. 전체 한 번에 (데이터 -> 착시 퍼즐 씬 -> 허브)", priority = 0)]
    public static void All()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SobekDataSetup.Run(true);
        SobekIllusionBuilder.BuildAll(); // 서브 S1~S4 + 메인 (착시 2.5D)
        BuildHub(false); // 마지막에 허브를 열어 둠
    }

    [MenuItem("Tools/Sobek/2. 허브 씬 만들기 (Sobek_Hub)", priority = 2)]
    public static void BuildHubMenu() => BuildHub(true);

    // 메인 씬은 이제 착시 퍼즐: Tools/Sobek/5 (SobekIllusionBuilder)가 서브와 함께 만듦

    // ================= 허브 (본당 = 수로 신전) =================
    // 배치 (유닛): 아래 전실(y 0~7) / 본당 왼쪽 S1·S3, 오른쪽 S2·S4 / 가운데 물 찬 수로 + 수위계 기둥 / 위 소베크의 입
    //             숨은 방(물 빠진 하층)은 오른쪽 바깥(x 52~)에 따로 두고 계단으로 순간 이동
    private static void BuildHub(bool askSave)
    {
        if (!Prepare(askSave, HubPath)) return;
        var log = new List<string>();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TMP_FontAsset font = FindFont();
        GameObject player = BuildCommon(new Vector3(23f, 3f, 0f), font, 7f);
        AddStory(log);
        CluePanel panel = EnsureCluePanel(log);

        var root = new GameObject("SobekHub");
        Transform r = root.transform;
        Sprite floor = W("Sobek_Tile_Floor"), floorB = W("Sobek_Tile_FloorB"), wall = W("Sobek_Tile_Wall"), top = W("Sobek_Tile_WallTop");
        Sprite water = W("Sobek_Tile_Water"), water2 = W("Sobek_Tile_Water2"), mud = W("Sobek_Tile_Mud");

        // ---- 물에 잠긴 본당: 진흙 바닥(물 빠지면 보임) 위에 물이 가득, 좁은 길과 섬만 물 위로 ----
        Vector2 hallC = new Vector2(HubW * 0.5f, (HallBottom + HubH) * 0.5f), hallS = new Vector2(HubW, HubH - HallBottom);
        Tiled(r, "Hall_Mud", mud, hallC, hallS, new Color(0.85f, 0.8f, 0.72f), -41, false);
        var pool = Tiled(r, "FloodWater", water, hallC, hallS, Color.white, -30, false);
        Anim(pool, 1.5f, water, water2);
        Tiled(r, "Floor_Ante", floor, new Vector2(HubW * 0.5f, HallBottom * 0.5f), new Vector2(HubW, HallBottom), Color.white, -40, false);
        foreach (var w in Walkways)
        {
            Sprite spr = w.island ? W("Sobek_Tile_Island") : floorB;
            Tiled(r, "Path_" + w.name, spr != null ? spr : floor, w.center, w.size, Color.white, w.island ? -27 : -26, false);
        }
        BuildWaterBlockers(r, out Collider2D[] blockers);
        log.Add($"본당: 물 가득 + 좁은 길·섬 {Walkways.Length}개, 물 막이 콜라이더 {blockers.Length}개 (물 빠지면 꺼짐)");

        // 벽
        Tiled(r, "Wall_N", wall, new Vector2(HubW * 0.5f, HubH + 0.5f), new Vector2(HubW + 2f, 1f), Color.white, -38, true);
        Tiled(r, "Wall_S", top, new Vector2(HubW * 0.5f, -0.5f), new Vector2(HubW + 2f, 1f), Color.white, -38, true);
        Tiled(r, "Wall_W", top, new Vector2(-0.5f, HubH * 0.5f), new Vector2(1f, HubH + 2f), Color.white, -38, true);
        Tiled(r, "Wall_E", top, new Vector2(HubW + 0.5f, HubH * 0.5f), new Vector2(1f, HubH + 2f), Color.white, -38, true);
        // 전실 칸막이 (가운데 x 21~25만 뚫림)
        Tiled(r, "Wall_Ante_L", wall, new Vector2(10.5f, 7.5f), new Vector2(21f, 1f), Color.white, -38, true);
        Tiled(r, "Wall_Ante_R", wall, new Vector2(35.5f, 7.5f), new Vector2(21f, 1f), Color.white, -38, true);
        Label(r, "전실", new Vector2(23f, 6.4f), font);
        Prop(r, "Pillar_0", W("Sobek_Obj_Pillar"), new Vector2(18f, 8.4f), true);
        Prop(r, "Pillar_1", W("Sobek_Obj_Pillar"), new Vector2(28f, 8.4f), true);
        Prop(r, "Pillar_2", W("Sobek_Obj_Pillar"), new Vector2(18f, 28.4f), true);
        Prop(r, "Pillar_3", W("Sobek_Obj_Pillar"), new Vector2(28f, 28.4f), true);

        // ---- 서브 퍼즐 섬 4개: 게이트(E: 착시 퍼즐 씬으로) + 비석 + 장치 발판 장식 ----
        var islands = new[]
        {
            (id: "S1", scene: SobekIds.SubScene1, title: "소용돌이 섬", flag: SobekIds.FlagS1Clear, c: new Vector2(7f, 25f), plate: "Sobek_Plate_Rotate", clue: "SOBEK_S1"),
            (id: "S2", scene: SobekIds.SubScene2, title: "악어 등 다리", flag: SobekIds.FlagS2Clear, c: new Vector2(39f, 25f), plate: "Sobek_Plate_Lift", clue: "SOBEK_S2"),
            (id: "S3", scene: SobekIds.SubScene3, title: "범람하는 나일", flag: SobekIds.FlagS3Clear, c: new Vector2(7f, 15f), plate: "Sobek_Plate_Water", clue: "SOBEK_S3"),
            (id: "S4", scene: SobekIds.SubScene4, title: "두 개의 수면", flag: SobekIds.FlagS4Clear, c: new Vector2(39f, 15f), plate: "Sobek_Plate_Mirror", clue: "SOBEK_S4"),
        };
        foreach (var isl in islands)
        {
            var gateGo = Prop(r, "Gate_" + isl.id, W("Sobek_Obj_Gate"), isl.c + new Vector2(0f, 1.6f), false);
            var gateCol = gateGo.AddComponent<BoxCollider2D>();
            gateCol.size = new Vector2(2f, 1.2f);
            gateCol.offset = new Vector2(0f, 0.6f);
            var spawnPt = MakePoint(r, "GateSpawn_" + isl.id, isl.c + new Vector2(0f, 0.4f));
            TextMeshPro gateLabel = WorldText(gateGo.transform, isl.title, new Vector2(0f, 2.9f), font, 3.5f);
            gateLabel.rectTransform.sizeDelta = new Vector2(8f, 1.2f);
            gateGo.AddComponent<SobekPuzzleGate>().EditorSetup(isl.id, isl.scene, isl.title, isl.flag, spawnPt, gateLabel);
            var deco = Prop(r, "PlateDeco_" + isl.id, W(isl.plate), isl.c + new Vector2(0f, -1.8f), false);
            deco.GetComponent<SpriteRenderer>().sortingLayerName = "Ground";
            deco.GetComponent<SpriteRenderer>().sortingOrder = -20;
            Stele(r, "Stele_" + isl.id, isl.c + new Vector2(-2.6f, 1.2f), isl.clue, panel, "");
            Prop(r, "IslandPillar_" + isl.id, W("Sobek_Obj_Pillar"), isl.c + new Vector2(2.8f, 2f), true);
        }
        Stele(r, "Stele_Hall", new Vector2(26.5f, 3.2f), "SOBEK_HALL", panel, "");
        log.Add("서브 퍼즐 섬 4개: 게이트(E -> Sobek_S1~S4 착시 퍼즐 씬, 돌아오면 같은 게이트 앞) + 비석");

        // ---- 소베크의 입 (큰 문 -> 메인 착시 퍼즐) ----
        var door = Sprite2D(r, "SobekHubDoor", W("Sobek_Obj_Door"), new Vector2(23f, 30.6f), new Vector2(3f, 2f), Color.white, 0, "Objects");
        door.AddComponent<BoxCollider2D>().size = new Vector2(3f, 2f);
        door.AddComponent<SobekHubDoor>();
        Label(door.transform, "소베크의 입", new Vector2(0f, 1.5f), font);
        Stele(r, "Stele_Door", new Vector2(19.6f, 29.4f), "SOBEK_DOOR", panel, "");
        var spawn = new GameObject("ReturnSpawn").transform;
        spawn.SetParent(r, false);
        spawn.position = new Vector3(23f, 28.6f, 0f);

        // ---- 소베크 석상 + 갈대 (단서 3) : 가운데 섬 ----
        Prop(r, "Sobek_Statue", W("Sobek_Obj_Statue"), new Vector2(23f, 23.2f), true);
        var reedsR = Prop(r, "Reeds_R", W("Sobek_Obj_Reeds"), new Vector2(25.6f, 23.6f), false);
        Anim(reedsR, 0.8f, W("Sobek_Obj_Reeds"), W("Sobek_Obj_Reeds_Shake"));
        var reedsL = Prop(r, "Reeds_Clue3", W("Sobek_Obj_Reeds"), new Vector2(20.4f, 23.6f), false);
        Anim(reedsL, 0.8f, W("Sobek_Obj_Reeds"), W("Sobek_Obj_Reeds_Shake"));
        var reedsCol = reedsL.AddComponent<BoxCollider2D>();
        reedsCol.isTrigger = true;
        reedsCol.size = new Vector2(1f, 1f);
        var clue3 = reedsL.AddComponent<SobekClueItem>();
        clue3.EditorSetup(3, SobekIds.FlagDrainPlayed, "갈대가 빽빽하다. 바람도 없는데 가끔 흔들린다…",
            "갈대 덤불 속 조각", "밤의 일곱째 시간, 뱀이 강을 마셔 배가 멈췄다.", new string[0], 0, true);

        var puddle = Prop(r, "Puddle", W("Sobek_Obj_Puddle"), new Vector2(26.5f, 11f), false);
        puddle.GetComponent<SpriteRenderer>().sortingLayerName = "Ground";
        puddle.GetComponent<SpriteRenderer>().sortingOrder = -29;

        // 단서 1 (전실 바닥) / 단서 2 (물 빠진 수로 진흙)
        var clue1 = Prop(r, "Clue1_FloodShard", W("Sobek_Obj_ClueShard"), new Vector2(19.5f, 3f), false);
        AddTrigger(clue1, 1f).gameObject.AddComponent<SobekClueItem>().EditorSetup(1, "", "",
            "범람 비석 조각", "나일이 넘치면 검은 흙이 남고, 땅은 다시 산다.",
            new[] { SobekStory.Khepri, "비석 조각이야. 이 신전 어딘가에 맞는 홈이 있을 거야." }, 0, false);
        var clue2 = Prop(r, "Clue2_MudShard", W("Sobek_Obj_ClueShard"), new Vector2(17.8f, 12.2f), false);
        clue2.GetComponent<SpriteRenderer>().color = new Color(0.75f, 0.62f, 0.48f);
        clue2.GetComponent<SpriteRenderer>().sortingOrder = -20;
        AddTrigger(clue2, 1f).gameObject.AddComponent<SobekClueItem>().EditorSetup(2, SobekIds.FlagDrainPlayed, "",
            "진흙 속 조각", "소베크는 강에 던져진 호루스의 두 손을 건져 올렸다.", new string[0], 0, false);

        // ---- 물속에 잠긴 아누비스의 문 (메인을 풀어 물이 빠지면 드러남) ----
        var anubis = Sprite2D(r, "Exit_ToAnubis", W("Sobek_Obj_StairsDown"), new Vector2(31f, 19.5f), new Vector2(2.4f, 2f), new Color(0.8f, 0.85f, 1f), -20, "Ground");
        var anubisCol = anubis.AddComponent<BoxCollider2D>();
        anubisCol.isTrigger = true;
        anubisCol.size = new Vector2(2f, 1.4f);
        anubis.AddComponent<SobekSceneExit>().EditorSetup(SobekIds.AnubisScene, "물이 빠진 수로 바닥… 자칼의 신전으로 내려간다.");
        Label(anubis.transform, "아누비스의 신전", new Vector2(0f, 1.6f), font);
        log.Add("물속 아누비스의 문: 메인 클리어 -> 물 빠짐 연출 -> 드러남 -> " + SobekIds.AnubisScene);

        // ---- 수위계 기둥 (소켓) + 돌판 -> 계단 + 아펩 그림자 ----
        var gauge = Prop(r, "WaterGauge_Socket", W("Sobek_Obj_Gauge_0"), new Vector2(23f, 15.2f), true);
        gauge.GetComponent<SpriteRenderer>().sortingOrder = 10;
        var gaugeTrigger = gauge.AddComponent<CircleCollider2D>();
        gaugeTrigger.isTrigger = true;
        gaugeTrigger.radius = 5.4f; // 물가에서도 E가 닿게
        gaugeTrigger.offset = new Vector2(0f, 1f);
        var slab = Prop(r, "Slab", W("Sobek_Obj_Slab"), new Vector2(25.2f, 14.2f), false);
        slab.GetComponent<SpriteRenderer>().sortingOrder = -15;
        var stairs = Prop(r, "StairsDown", W("Sobek_Obj_StairsDown"), new Vector2(25.2f, 14.2f), false);
        stairs.GetComponent<SpriteRenderer>().sortingOrder = -15;
        AddTrigger(stairs, 1.4f);
        var apepGo = Prop(r, "ApepShadow", W("Sobek_Obj_ApepShadow"), new Vector2(25.2f, 14.6f), false);
        apepGo.GetComponent<SpriteRenderer>().sortingOrder = 30;
        var apep = apepGo.AddComponent<SobekApepShadow>();
        var socket = gauge.AddComponent<SobekClueSocket>();
        socket.EditorSetup(gauge.GetComponent<SpriteRenderer>(),
            new[] { W("Sobek_Obj_Gauge_0"), W("Sobek_Obj_Gauge_1"), W("Sobek_Obj_Gauge_2"), W("Sobek_Obj_Gauge_3") },
            slab, stairs, apep);
        stairs.SetActive(false);

        // ---- 숨은 방 (물 빠진 하층, 오른쪽 바깥) ----
        var lower = new GameObject("HiddenRoom_Lower").transform;
        lower.SetParent(r, false);
        Vector2 lc = new Vector2(57f, 5f), ls = new Vector2(10f, 8f);
        Tiled(lower, "Lower_Floor", mud, lc, ls, new Color(0.75f, 0.7f, 0.65f), -40, false);
        Tiled(lower, "Lower_Wall_N", wall, lc + new Vector2(0f, ls.y * 0.5f + 0.5f), new Vector2(ls.x + 2f, 1f), Color.white, -38, true);
        Tiled(lower, "Lower_Wall_S", top, lc - new Vector2(0f, ls.y * 0.5f + 0.5f), new Vector2(ls.x + 2f, 1f), Color.white, -38, true);
        Tiled(lower, "Lower_Wall_W", top, lc - new Vector2(ls.x * 0.5f + 0.5f, 0f), new Vector2(1f, ls.y), Color.white, -38, true);
        Tiled(lower, "Lower_Wall_E", top, lc + new Vector2(ls.x * 0.5f + 0.5f, 0f), new Vector2(1f, ls.y), Color.white, -38, true);
        Label(lower, "물 빠진 하층", lc + new Vector2(0f, ls.y * 0.5f - 0.6f), font);
        var lowerSpawn = new GameObject("Lower_Spawn").transform;
        lowerSpawn.SetParent(lower, false);
        lowerSpawn.position = new Vector3(53.5f, 3f, 0f);
        var lowerStairs = Prop(lower, "Lower_StairsUp", W("Sobek_Obj_StairsDown"), new Vector2(53.5f, 1.8f), false);
        lowerStairs.GetComponent<SpriteRenderer>().sortingOrder = -15;
        AddTrigger(lowerStairs, 1.4f).gameObject.AddComponent<SobekStairs>().EditorSetup(MakePoint(r, "Stairs_Return", new Vector2(25.2f, 12.8f)), "E: 계단 올라가기", "", "");
        stairs.AddComponent<SobekStairs>().EditorSetup(lowerSpawn, "E: 계단 내려가기", SobekIds.FlagHiddenOpen, "물 빠진 하층… 동쪽 끝 마른 진흙이 붓질 자국처럼 반짝인다.");
        Prop(lower, "Lower_Altar", W("Sobek_Obj_Altar"), new Vector2(60f, 6f), false);
        var tooth = Prop(lower, "Hidden_SobekTooth", W("Sobek_Obj_Tooth"), new Vector2(60f, 4.4f), false);
        AddTrigger(tooth, 1f).gameObject.AddComponent<SobekHiddenPickup>();
        Stele(lower, "Stele_Lower", new Vector2(56f, 7.6f), "SOBEK_LOWER", panel, "");
        log.Add("숨은 방: 단서 3개 -> 수위계 기둥 -> 돌판이 계단으로 -> 물 빠진 하층 (소베크의 이빨 + 마지막 문장)");

        // ---- 악어 (안내 동물) ----
        var crocGo = Prop(r, "Sobek_Croc", W("Sobek_Croc_Idle_0"), new Vector2(26.5f, 13.2f), false);
        crocGo.GetComponent<SpriteRenderer>().sortingLayerName = "Objects";
        crocGo.GetComponent<SpriteRenderer>().sortingOrder = 4;
        var crocCol = crocGo.AddComponent<BoxCollider2D>();
        crocCol.isTrigger = true;
        crocCol.size = new Vector2(2f, 1f);
        var emote = WorldText(crocGo.transform, "", new Vector2(0.6f, 0.9f), font, 5f);
        var leg0 = new SobekCrocGuide.Leg
        {
            path = new[] { MakePoint(r, "CrocPath_0a", new Vector2(24.5f, 18.5f)), MakePoint(r, "CrocPath_0b", new Vector2(21.2f, 21f)), MakePoint(r, "CrocPath_0c", new Vector2(21.4f, 23f)) },
            clue = clue3,
            signalText = "악어가 꼬리로 갈대를 친다. 갈대 사이에서 무언가 반짝인다!",
        };
        var leg1 = new SobekCrocGuide.Leg
        {
            path = new[] { MakePoint(r, "CrocPath_1a", new Vector2(21.5f, 19f)), MakePoint(r, "CrocPath_1b", new Vector2(21.8f, 15f)) },
            clue = null,
            signalText = "악어가 수위계 기둥 앞에서 꼬리로 물보라를 일으킨다. 빈 눈금이 셋…",
        };
        crocGo.AddComponent<SobekCrocGuide>().EditorSetup(new[] { leg0, leg1 },
            new[] { W("Sobek_Croc_Idle_0"), W("Sobek_Croc_Idle_1") }, new[] { W("Sobek_Croc_Walk_0"), W("Sobek_Croc_Walk_1") },
            W("Sobek_Croc_Eyes"), emote);

        // ---- 중앙 홀로 가는 출구 (물이 빠진 뒤) ----
        var exit = Sprite2D(r, "Exit_ToCentralHall", W("Sobek_Obj_EntranceStone"), new Vector2(23f, 0.6f), new Vector2(2f, 1f), new Color(1f, 0.9f, 0.6f), -20, "Ground");
        var exitCol = exit.AddComponent<BoxCollider2D>();
        exitCol.isTrigger = true;
        exitCol.size = new Vector2(2f, 1f);
        exit.AddComponent<SobekSceneExit>().EditorSetup(SobekIds.CentralHallScene, "중앙 홀로 돌아간다… 악어가 뒤를 따라 기어온다.");
        Label(exit.transform, "중앙 홀로", new Vector2(0f, 1.1f), font);

        // ---- 진행 상태 + 빈 홈 HUD ----
        var state = root.AddComponent<SobekHubState>();
        var so = new SerializedObject(state);
        SetArray(so, "waterRenderers", pool.GetComponent<SpriteRenderer>());
        SetArrayMany(so, "waterBlockers", blockers);
        SetArrayMany(so, "showWhenDrained", puddle, exit, anubis);
        so.FindProperty("returnSpawn").objectReferenceValue = spawn;
        so.ApplyModifiedPropertiesWithoutUndo();
        root.AddComponent<SobekClueHud>().EditorSetup(new[] { U("Common_Icon_SobekClue1"), U("Common_Icon_SobekClue2"), U("Common_Icon_SobekClue3") }, font);

        Selection.activeGameObject = root;
        Save(scene, HubPath, log);
        Debug.Log("[SobekSceneBuilder] 허브 씬 완료\n- " + string.Join("\n- ", log));
    }

    // ================= 메인 마무리 연출 (형판 공개·큐브) =================
    // 메인 씬은 착시 퍼즐(SobekIllusionBuilder)이 만들고, 마지막 연출만 여기서 붙임 (바스테트와 같은 BastetEnding, 칸 = 소베크)
    public static BastetEnding AddSobekEnding(Transform parent, TMP_FontAsset font, List<string> log)
    {
        var endGo = new GameObject("SobekEnding");
        endGo.transform.SetParent(parent, false);
        var ending = endGo.AddComponent<BastetEnding>();
        var faces = new Sprite[6];
        string[] faceFiles = { "Ra.jpg", "Bastet.jpg", "Thoth.jpg", "Horus.jpg", "Sobek.jpg", "Anubis.jpg" };
        for (int i = 0; i < 6; i++) faces[i] = LoadSprite("Assets/image/Cube/" + faceFiles[i]);
        ending.EditorSetup(U("UI_Portrait_Sobek_Glow"), U("Sobek_UI_Clue_Main"), faces[4], LoadSprite("Assets/image/Cube/Cube.png"), faces, font, SobekIds.HubScene);
        var eso = new SerializedObject(ending);
        SetStrings(eso, "storyLines", new[]
        {
            "밤의 일곱째 시간, 혼돈의 뱀 아펩이 저승의 강을 마셨다.",
            "태양의 배는 마른 모래 위에 멈춰 섰다.",
            "물의 주인 소베크가 가둬 둔 마지막 범람을 강으로 돌려보내자",
            "배는 다시 물 위에 떠올랐다.",
        });
        eso.FindProperty("goddessLine").stringValue = "\"그 형판을 가져가라. 큐브의 다섯 번째 면이 될 것이다.\"";
        eso.FindProperty("plateName").stringValue = "악어 형판";
        eso.FindProperty("plateDesc").stringValue = "큐브의 다섯 번째 면에 끼우는 소베크의 형판";
        eso.FindProperty("bastetSlot").intValue = 4;
        eso.FindProperty("playerInventoryFace").intValue = 4; // [확인 필요] 팀 PlayerInventory 번호
        eso.FindProperty("plateGivenFlag").stringValue = SobekIds.FlagPlateGiven;
        eso.FindProperty("endingDoneFlag").stringValue = SobekIds.FlagEndingDone;
        eso.FindProperty("exitMessage").stringValue = "물이 빠지는 소리가 들린다… 신전으로 돌아간다.";
        eso.ApplyModifiedPropertiesWithoutUndo();
        log.Add("메인 클리어: 소베크 대사(초상화) -> 형판 공개(악어 형판) -> 중앙 홀 큐브 소베크 칸 -> 허브(물 빠짐 연출)");
        return ending;
    }

    // ================= v3 도우미 =================
    private static Sprite W(string name)
    {
        Sprite s = SobekDataSetup.World(name);
        if (s == null) Debug.LogWarning($"[SobekSceneBuilder] 그림 없음: {SobekDataSetup.WorldFolder}/{name}.png");
        return s;
    }

    private static Sprite U(string name) => SobekDataSetup.Ui(name);

    private static Sprite LoadSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;
        foreach (Object o in AssetDatabase.LoadAllAssetRepresentationsAtPath(path)) if (o is Sprite sp) return sp;
        return null;
    }

    public static TMP_FontAsset FindFontPublic() => FindFont();
    public static void AddStoryPublic() => AddStory(new List<string>());

    private static void AddStory(List<string> log)
    {
        var go = new GameObject("SobekStory");
        go.AddComponent<SobekStory>().EditorSetup(U("UI_Portrait_Sobek_Default"), LoadSprite("Assets/image/Common/Icon/Common_Icon_Scarab.png"));
        log.Add("대사 초상화: 소베크 / 케프리");
    }

    // 물건 하나 (발밑 기준 그림은 위치 = 발밑)
    private static GameObject Prop(Transform parent, string objName, Sprite sprite, Vector2 pos, bool solid)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Objects";
        sr.sortingOrder = 0;
        if (solid)
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.6f);
            col.offset = new Vector2(0f, 0.3f);
        }
        return go;
    }

    private static Collider2D AddTrigger(GameObject go, float size)
    {
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(size, size);
        return col;
    }

    private static Transform MakePoint(Transform parent, string objName, Vector2 pos)
    {
        var t = new GameObject(objName).transform;
        t.SetParent(parent, false);
        t.position = pos;
        return t;
    }

    private static void Anim(GameObject go, float fps, params Sprite[] frames)
    {
        go.AddComponent<SobekSpriteAnim>().SetFrames(frames, fps);
    }

    // 비석 (라의 방 방식 BastetClueStele 재사용: 렌즈 해독 -> 벽화 + 글, 불 필요 없음)
    private static void Stele(Transform parent, string objName, Vector2 pos, string clueId, CluePanel panel, string unlockFlag)
    {
        GameObject go = Prop(parent, objName, W("Sobek_Obj_Stele"), pos, false);
        AddTrigger(go, 1.2f).offset = new Vector2(0f, 0.7f);
        var bubbleGo = new GameObject("NewBubble");
        bubbleGo.transform.SetParent(go.transform, false);
        bubbleGo.transform.localPosition = new Vector3(0f, 1.9f, 0f);
        var bubble = bubbleGo.AddComponent<SpriteRenderer>();
        bubble.sprite = FindSprite("UI_Prompt_New");
        bubble.sortingLayerName = "Objects";
        bubble.sortingOrder = 20;
        var stele = go.AddComponent<BastetClueStele>();
        stele.EditorSetup(SobekDataSetup.LoadClue(clueId), panel, bubble, unlockFlag, 0, "");
        var so = new SerializedObject(stele);
        SerializedProperty light = so.FindProperty("needLight");
        if (light != null) light.boolValue = false; // 소베크 방은 밝음
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static CluePanel EnsureCluePanel(List<string> log)
    {
        var canvasGo = new GameObject("SobekStoryCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasGo.layer = 5;
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 65;
        var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var go = new GameObject("Clue_Overlay", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvasGo.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var panel = go.AddComponent<CluePanel>();
        SetObj(panel, "frameSprite", FindSprite("UI_Journal_Frame"));
        log.Add("비석 화면 (CluePanel)");
        return panel;
    }

    private static Sprite FindSprite(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Sprite"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) != name) continue;
            Sprite s = LoadSprite(path);
            if (s != null) return s;
        }
        return null;
    }

    private static TextMeshPro WorldText(Transform parent, string text, Vector2 localPos, TMP_FontAsset font, float size)
    {
        var go = new GameObject("Emote");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var tmp = go.AddComponent<TextMeshPro>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.95f, 0.6f);
        tmp.rectTransform.sizeDelta = new Vector2(2f, 1f);
        tmp.sortingLayerID = SortingLayer.NameToID("Objects");
        tmp.sortingOrder = 60;
        return tmp;
    }

    private static void SetStrings(SerializedObject so, string prop, string[] values)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null) return;
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).stringValue = values[i];
    }

    private static void SetArrayMany(SerializedObject so, string prop, params Object[] values)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null) return;
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    // ================= 공통: 카메라 · 조명 · 플레이어 · 매니저 =================
    private static GameObject BuildCommon(Vector3 spawn, TMP_FontAsset font, float camSize)
    {
        // 매니저 (GameManager는 최상위: DontDestroyOnLoad 때문)
        new GameObject("GameManager_Sobek").AddComponent<GameManager>();
        var managers = new GameObject("Managers");
        managers.AddComponent<ItemInventory>();
        var toast = managers.AddComponent<AnubisToast>();
        SetObj(toast, "font", font);
        var prompt = managers.AddComponent<AnubisPromptUI>();
        SetObj(prompt, "font", font);

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        // 조명 (URP 2D: 전역 조명이 없으면 그림이 어둡게 나옴)
        var lightGo = new GameObject("Global Light 2D");
        var light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;

        // 플레이어
        GameObject player = null;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab != null)
        {
            player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.transform.position = spawn;
        }
        else Debug.LogWarning($"[SobekSceneBuilder] {PlayerPrefabPath} 없음 -> 플레이어를 직접 넣어 주세요");

        // 카메라
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = camSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.07f, 0.06f);
        camGo.transform.position = new Vector3(spawn.x, spawn.y, -10f);
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<CameraFollow2D>();
        return player;
    }

    // ================= 작은 도우미 =================
    private static bool Prepare(bool askSave, string path)
    {
        if (askSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        if (SobekDataSetup.LoadBoard(SobekDataSetup.S1) == null) SobekDataSetup.Run();
        if (File.Exists(path) &&
            !EditorUtility.DisplayDialog("소베크 씬", $"{path} 가 이미 있습니다.\n새로 만들면 그 씬에서 고친 내용이 사라집니다. 덮어쓸까요?", "덮어쓰기", "취소"))
            return false;
        SobekDataSetup.EnsureFolder(SceneFolder);
        return true;
    }

    private static void Save(Scene scene, string path, List<string> log)
    {
        EditorSceneManager.SaveScene(scene, path);
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == path))
        {
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            log.Add($"Build Profiles 씬 목록에 추가: {path}");
        }
        log.Add($"저장: {path}");
    }

    private static GameObject Tiled(Transform parent, string objName, Sprite sprite, Vector2 center, Vector2 size, Color color, int order, bool solid)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.color = color;
        sr.sortingLayerName = "Ground";
        sr.sortingOrder = order;
        if (solid) go.AddComponent<BoxCollider2D>().size = size;
        return go;
    }

    private static GameObject Sprite2D(Transform parent, string objName, Sprite sprite, Vector2 pos, Vector2 size, Color color, int order, string layer)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.color = color;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return go;
    }

    private static void Label(Transform parent, string text, Vector2 localPos, TMP_FontAsset font)
    {
        if (font == null) return;
        var go = new GameObject("Sign_" + text);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = 3.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.95f, 0.8f);
        tmp.rectTransform.sizeDelta = new Vector2(10f, 1.2f);
        tmp.sortingLayerID = SortingLayer.NameToID("Objects");
        tmp.sortingOrder = 50;
    }

    private static TMP_FontAsset FindFont()
    {
        foreach (string guid in AssetDatabase.FindAssets("MonoplexKR-Regular SDF t:TMP_FontAsset"))
        {
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (f != null) return f;
        }
        return null;
    }

    private static void SetObj(Object target, string prop, Object value)
    {
        if (target == null || value == null) return;
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(prop);
        if (p == null) return;
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(SerializedObject so, string prop, Object value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null || value == null) return;
        p.arraySize = 1;
        p.GetArrayElementAtIndex(0).objectReferenceValue = value;
    }
}
#endif
