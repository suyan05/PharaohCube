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

// 메뉴: Tools/Sobek/2. 허브 씬 만들기, 3. 메인 씬 만들기, 0. 전체 한 번에
// 새 씬 파일을 처음부터 만듦 (카메라, 조명, 플레이어 프리팹, 매니저, 판, 문, 물, 출구) + Build Profiles 씬 목록에 추가
// v3: 소베크 도트 그림(Assets/image/Sobek/World)으로 바닥·벽·물·석상·수위계·악어·숨은 방까지 배치
// 허브 맵은 스프라이트 바닥 (나중에 수연님 타일맵으로 바꿔도 판·문·비석 오브젝트는 그대로 옮겨 쓰면 됨)
public static class SobekSceneBuilder
{
    private const string SceneFolder = "Assets/Scenes/Puzzle_Test";
    private const string HubPath = SceneFolder + "/" + SobekIds.HubScene + ".unity";
    private const string MainPath = SceneFolder + "/" + SobekIds.MainScene + ".unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";

    // 허브 크기 (유닛 = 칸)
    private const float HubW = 46f, HubH = 32f; // 본당 크기 (유닛)

    [MenuItem("Tools/Sobek/0. 전체 한 번에 (데이터 -> 허브 -> 메인)", priority = 0)]
    public static void All()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SobekDataSetup.Run(true);
        BuildMain(false);
        BuildHub(false); // 마지막에 허브를 열어 둠
    }

    [MenuItem("Tools/Sobek/2. 허브 씬 만들기 (Sobek_Hub)", priority = 2)]
    public static void BuildHubMenu() => BuildHub(true);

    [MenuItem("Tools/Sobek/3. 메인 씬 만들기 (Sobek_Main)", priority = 3)]
    public static void BuildMainMenu() => BuildMain(true);

    // ================= 허브 (본당 = 수로 신전) =================
    // 배치 (유닛): 아래 전실(y 0~7) / 본당 왼쪽 S1·S3, 오른쪽 S2·S4 / 가운데 물 찬 수로 + 수위계 기둥 / 위 소베크의 입
    //             숨은 방(물 빠진 하층)은 오른쪽 바깥(x 52~)에 따로 두고 계단으로 순간 이동
    private static void BuildHub(bool askSave)
    {
        if (!Prepare(askSave, HubPath)) return;
        var log = new List<string>();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TMP_FontAsset font = FindFont();
        SobekTheme theme = SobekDataSetup.LoadTheme();

        GameObject player = BuildCommon(new Vector3(23f, 3f, 0f), font, 7f);
        AddStory(log);
        CluePanel panel = EnsureCluePanel(log);

        var root = new GameObject("SobekHub");
        Transform r = root.transform;
        Sprite floor = W("Sobek_Tile_Floor"), floorB = W("Sobek_Tile_FloorB"), wall = W("Sobek_Tile_Wall"), top = W("Sobek_Tile_WallTop");
        Sprite water = W("Sobek_Tile_Water"), water2 = W("Sobek_Tile_Water2"), mud = W("Sobek_Tile_Mud");

        // ---- 바닥 · 벽 ----
        Tiled(r, "Floor", floor, new Vector2(HubW * 0.5f, HubH * 0.5f), new Vector2(HubW, HubH), Color.white, -40, false);
        Tiled(r, "Floor_Aisle", floorB, new Vector2(23f, 19f), new Vector2(4f, 22f), Color.white, -39, false); // 가운데 길 (악어 비늘 판석)
        Tiled(r, "Wall_N", wall, new Vector2(HubW * 0.5f, HubH + 0.5f), new Vector2(HubW + 2f, 1f), Color.white, -38, true);
        Tiled(r, "Wall_S", top, new Vector2(HubW * 0.5f, -0.5f), new Vector2(HubW + 2f, 1f), Color.white, -38, true);
        Tiled(r, "Wall_W", top, new Vector2(-0.5f, HubH * 0.5f), new Vector2(1f, HubH + 2f), Color.white, -38, true);
        Tiled(r, "Wall_E", top, new Vector2(HubW + 0.5f, HubH * 0.5f), new Vector2(1f, HubH + 2f), Color.white, -38, true);
        // 전실 칸막이 (가운데 x 21~25만 뚫림)
        Tiled(r, "Wall_Ante_L", wall, new Vector2(10.5f, 7.5f), new Vector2(21f, 1f), Color.white, -38, true);
        Tiled(r, "Wall_Ante_R", wall, new Vector2(35.5f, 7.5f), new Vector2(21f, 1f), Color.white, -38, true);
        Label(r, "전실", new Vector2(23f, 6.4f), font);
        for (int i = 0; i < 4; i++) // 기둥 장식
        {
            Prop(r, $"Pillar_{i}", W("Sobek_Obj_Pillar"), new Vector2(18f + (i % 2) * 10f, i < 2 ? 8.2f : 27.2f), true);
        }

        // ---- 서브 퍼즐 4개 + 비석 (이야기 + 힌트) ----
        SobekBoard s1 = Board(r, "Board_S1", SobekDataSetup.S1, new Vector2(3f, 21f), theme, font, true, log);
        SobekBoard s2 = Board(r, "Board_S2", SobekDataSetup.S2, new Vector2(30f, 21f), theme, font, true, log);
        SobekBoard s3 = Board(r, "Board_S3", SobekDataSetup.S3, new Vector2(3f, 10f), theme, font, true, log);
        SobekBoard s4 = Board(r, "Board_S4", SobekDataSetup.S4, new Vector2(30f, 9f), theme, font, true, log);
        Stele(r, "Stele_S1", new Vector2(1f, 30.6f), "SOBEK_S1", panel, "");
        Stele(r, "Stele_S2", new Vector2(33f, 19.2f), "SOBEK_S2", panel, "");
        Stele(r, "Stele_S3", new Vector2(1f, 17.6f), "SOBEK_S3", panel, "");
        Stele(r, "Stele_S4", new Vector2(28.9f, 15.6f), "SOBEK_S4", panel, "");
        Stele(r, "Stele_Hall", new Vector2(26.5f, 3.2f), "SOBEK_HALL", panel, "");
        log.Add("비석 5개 (전실 + 서브 S1~S4): 이야기 + 퍼즐 힌트, 처음엔 렌즈 해독");

        // ---- 소베크의 입 (큰 문) ----
        var door = Sprite2D(r, "SobekHubDoor", W("Sobek_Obj_Door"), new Vector2(23f, 30.6f), new Vector2(3f, 2f), Color.white, 0, "Objects");
        door.AddComponent<BoxCollider2D>().size = new Vector2(3f, 2f);
        door.AddComponent<SobekHubDoor>();
        Label(door.transform, "소베크의 입", new Vector2(0f, 1.5f), font);
        Stele(r, "Stele_Door", new Vector2(19.6f, 29.4f), "SOBEK_DOOR", panel, "");
        var spawn = new GameObject("ReturnSpawn").transform;
        spawn.SetParent(r, false);
        spawn.position = new Vector3(23f, 28.6f, 0f);

        // ---- 소베크 석상 + 갈대 (단서 3) ----
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

        // ---- 수로 (물 -> 빠지면 진흙 바닥) ----
        Vector2 poolC = new Vector2(23f, 16f), poolS = new Vector2(10f, 10f);
        Tiled(r, "Channel_Mud", mud, poolC, poolS, Color.white, -37, false);
        var pool = Tiled(r, "FloodWater", water, poolC, poolS, Color.white, -30, true);
        Anim(pool, 1.5f, water, water2);
        var puddle = Prop(r, "Puddle", W("Sobek_Obj_Puddle"), new Vector2(26.5f, 13f), false);
        puddle.GetComponent<SpriteRenderer>().sortingLayerName = "Ground";
        puddle.GetComponent<SpriteRenderer>().sortingOrder = -29;

        // 단서 1 (전실 바닥) / 단서 2 (물 빠진 수로 진흙)
        var clue1 = Prop(r, "Clue1_FloodShard", W("Sobek_Obj_ClueShard"), new Vector2(19.5f, 3f), false);
        AddTrigger(clue1, 1f).gameObject.AddComponent<SobekClueItem>().EditorSetup(1, "", "",
            "범람 비석 조각", "나일이 넘치면 검은 흙이 남고, 땅은 다시 산다.",
            new[] { SobekStory.Khepri, "비석 조각이야. 이 신전 어딘가에 맞는 홈이 있을 거야." }, 0, false);
        var clue2 = Prop(r, "Clue2_MudShard", W("Sobek_Obj_ClueShard"), new Vector2(19.8f, 12.6f), false);
        clue2.GetComponent<SpriteRenderer>().color = new Color(0.75f, 0.62f, 0.48f);
        clue2.GetComponent<SpriteRenderer>().sortingOrder = -20;
        AddTrigger(clue2, 1f).gameObject.AddComponent<SobekClueItem>().EditorSetup(2, SobekIds.FlagDrainPlayed, "",
            "진흙 속 조각", "소베크는 강에 던져진 호루스의 두 손을 건져 올렸다.", new string[0], 0, false);

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
        SetArray(so, "waterBlockers", pool.GetComponent<Collider2D>());
        SetArrayMany(so, "showWhenDrained", puddle, exit);
        so.FindProperty("returnSpawn").objectReferenceValue = spawn;
        so.ApplyModifiedPropertiesWithoutUndo();
        root.AddComponent<SobekClueHud>().EditorSetup(new[] { U("Common_Icon_SobekClue1"), U("Common_Icon_SobekClue2"), U("Common_Icon_SobekClue3") }, font);

        Selection.activeGameObject = root;
        Save(scene, HubPath, log);
        Debug.Log("[SobekSceneBuilder] 허브 씬 완료\n- " + string.Join("\n- ", log));
    }

    // ================= 메인 (셰데트 신전 3구역 + 형판 공개·큐브) =================
    private static void BuildMain(bool askSave)
    {
        if (!Prepare(askSave, MainPath)) return;
        var log = new List<string>();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TMP_FontAsset font = FindFont();
        SobekTheme theme = SobekDataSetup.LoadTheme();

        BuildCommon(new Vector3(1.5f, 1.5f, 0f), font, 6f);
        AddStory(log);

        var root = new GameObject("SobekMain");
        // 구역 3개 (서로 멀리 떨어뜨려 두고 화면 전환으로 이동) + 판 둘레 벽 그림
        SobekBoard a = Board(root.transform, "Zone1_Flood", SobekDataSetup.MainA, new Vector2(0f, 0f), theme, font, false, log);
        SobekBoard b = Board(root.transform, "Zone2_DryMaze", SobekDataSetup.MainB, new Vector2(30f, 0f), theme, font, false, log);
        SobekBoard c = Board(root.transform, "Zone3_Tower", SobekDataSetup.MainC, new Vector2(60f, 0f), theme, font, false, log);
        foreach (SobekBoard z in new[] { a, b, c }) Frame(z, root.transform);
        var flow = root.AddComponent<SobekMainFlow>();
        flow.EditorSetup(new[] { a, b, c }, new[]
        {
            new[] { "아펩이 저승의 강을 삼키기 전, 소베크는 마지막 범람의 물을 이 신전에 가둬 두었다.", "물이 찬 신전을 지나 소베크의 입으로 들어가자." },
            new[] { "소베크의 입을 지나 물이 빠진 아래층으로 내려왔다.", "악어 무덤의 미궁… 수면에 비친 반대편 방을 오가야 한다." },
            new[] { "미궁 끝, 소용돌이 섬들이 소베크-라의 탑을 둘러싸고 있다.", "섬을 돌려 길을 이어 제단까지 가자." },
        });

        // 형판 공개 · 중앙 홀 큐브 연출 (바스테트와 같은 BastetEnding, 칸 = 소베크)
        var endGo = new GameObject("SobekEnding");
        endGo.transform.SetParent(root.transform, false);
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
        eso.FindProperty("exitMessage").stringValue = "물이 빠진 신전으로 돌아간다…";
        eso.ApplyModifiedPropertiesWithoutUndo();
        flow.EditorSetupEnding(ending);
        log.Add("메인 클리어: 소베크 대사(초상화) -> 형판 공개(악어 형판) -> 중앙 홀 큐브 소베크 칸 -> 허브(물 빠짐)");

        Selection.activeGameObject = root;
        Save(scene, MainPath, log);
        Debug.Log("[SobekSceneBuilder] 메인 씬 완료\n- " + string.Join("\n- ", log));
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

    // 메인 판 둘레를 신전 벽으로 감쌈 (판 밖이 검은 화면으로 안 보이게)
    private static void Frame(SobekBoard board, Transform parent)
    {
        if (board == null || board.Data == null) return;
        Vector2 o = board.transform.position;
        float w = board.Data.Width, h = board.Data.Height;
        Tiled(parent, board.name + "_Frame", W("Sobek_Tile_WallTop"), o + new Vector2(w * 0.5f, h * 0.5f), new Vector2(w + 6f, h + 6f), Color.white, -60, false);
        Tiled(parent, board.name + "_FrameTop", W("Sobek_Tile_Wall"), o + new Vector2(w * 0.5f, h + 0.5f), new Vector2(w + 2f, 1f), Color.white, -59, false);
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

    // ================= 판 하나 =================
    private static SobekBoard Board(Transform parent, string objName, string dataName, Vector2 origin, SobekTheme theme,
        TMP_FontAsset font, bool hub, List<string> log)
    {
        SobekBoardData data = SobekDataSetup.LoadBoard(dataName);
        var go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        go.transform.position = origin;
        var board = go.AddComponent<SobekBoard>(); // SobekBoardView 자동 부착
        if (data == null)
        {
            log.Add($"{objName}: 데이터 {dataName} 없음 (1번 메뉴 먼저)");
            return board;
        }
        board.EditorSetup(data, hub);
        var view = go.GetComponent<SobekBoardView>();
        SetObj(view, "theme", theme);
        SetObj(view, "labelFont", font);

        int w = data.Width, h = data.Height;
        if (hub)
        {
            // 판 위를 자유 이동으로 못 지나가게 막는 벽 (판 안에서는 칸 이동이라 상관없음)
            var block = go.AddComponent<BoxCollider2D>();
            block.size = new Vector2(w, h);
            block.offset = new Vector2(w * 0.5f, h * 0.5f);

            // 입구 돌 (E: 들어가기)
            Vector2Int e = data.Entrance;
            var ent = Sprite2D(go.transform, "Entrance", SobekDataSetup.LoadArt("Sobek_Obj_EntranceStone"),
                Vector2.zero, Vector2.one, Color.white, -15, "Ground");
            ent.transform.localPosition = new Vector3(e.x + 0.5f, h - 1 - e.y + 0.5f, 0f);
            var col = ent.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;
            ent.AddComponent<SobekBoardEntrance>().EditorSetup(board);
            Label(go.transform, data.DisplayName, new Vector2(w * 0.5f, h + 0.6f), font);
        }
        log.Add($"{objName} ({data.DisplayName}, {w}x{h}) 위치 {origin}");
        return board;
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
