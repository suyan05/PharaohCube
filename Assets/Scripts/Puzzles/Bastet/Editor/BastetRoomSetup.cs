#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 메뉴: Tools/Bastet/실전 방 세팅 (제단 잠금, 비석 "!", 힌트, 목표, 소지품, 토트 출구)
// 지금 열린 실전 씬(Puzzle_Test_Bastet_Tilemap)에 아누비스 방과 같은 진행 구조를 한 번에 붙임
//  1) 매니저 (GameManager, ItemInventory, 알림, 아래 안내 문구, EventSystem)
//  2) 퍼즐 찾기 (B1~B5를 컴포넌트 종류로 찾음 -> 팝업 이름도 원래대로 정리)
//  3) 제단 B1~B5: 없으면 임시 상자 생성, 순서 잠금 (B1 -> B2 -> B3 -> B4 -> B5)
//  4) 보상 지급 (BastetRoomProgress), 목표 띠, 소지품 바, 입장 연출
//  5) 비석 단서 + "!" (입구, B1은 처음부터 / 나머지는 앞 퍼즐을 풀면)
//  6) 퍼즐 화면 힌트 버튼
//  7) 토트 출구 (B5를 풀면 Door_North_Exit가 사라지고 그 뒤 출구로 이동)
// 여러 번 눌러도 안전: 이미 있는 오브젝트는 위치를 그대로 두고 연결만 다시 함, 고친 문구(.asset)는 덮어쓰지 않음
public static class BastetRoomSetup
{
    private const string DataRoot = "Assets/Scripts/Puzzles/Bastet/Data";
    private const string ClueFolder = DataRoot + "/Clues";
    private const string HintFolder = DataRoot + "/Hints";
    private const string RoomRootName = "BastetRoomUI";
    private const string StoryCanvasName = "BastetStoryCanvas";
    private const string ExitName = "Exit_ToThoth";
    private const string DoorName = "Door_North_Exit";
    private const string NextSceneName = "Puzzle_Test_Anubis_Tutorial"; // [수정] 토트 씬 완성 전까지 아누비스로
    private const string NextScenePath = "Assets/Scenes/Puzzle_Test/Puzzle_Test_Anubis_Tutorial.unity";
    private const string ObjectLayer = "Objects";

    // 퍼즐 5개 정보 (번호, 이름 꼬리, 팝업 이름, 제단 안내, 잠금 아이템, 보상, 비석 ID, 비석 제목, 비석 본문)
    private class PuzzleInfo
    {
        public int number;
        public string suffix, popupName, prompt, requiredItem, reward, flag, toast;
        public string clueId, clueTitle, clueBody;
        public Vector3 placeholderOffset;
        public PuzzleBase puzzle;
        public GameObject panel;
        public GameObject altar;
    }

    [MenuItem("Tools/Bastet/실전 방 세팅 (제단 잠금, 비석 \"!\", 힌트, 목표, 토트 출구)")]
    public static void Apply()
    {
        var log = new List<string>();
        TMP_FontAsset font = FindFont();

        EnsureManagers(log, font);
        List<PuzzleInfo> infos = FindPuzzles(log);
        if (infos == null) return;

        EnsureAltars(log, infos);
        CluePanel cluePanel = EnsureCluePanel(log);
        GameObject room = EnsureRoomRoot(log, font, infos);
        EnsureSteles(log, infos, cluePanel);
        EnsureHints(log, infos);
        EnsureExit(log, font);
        EnsureBuildScenes(log);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = room;
        Debug.Log("[BastetRoomSetup] 완료 (Ctrl+S로 씬 저장)\n- " + string.Join("\n- ", log));
    }

    // ================= 1. 매니저 =================
    private static void EnsureManagers(List<string> log, TMP_FontAsset font)
    {
        if (Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include) == null)
        {
            var gm = new GameObject("GameManager_BastetRoom");
            Undo.RegisterCreatedObjectUndo(gm, "GameManager");
            gm.AddComponent<GameManager>(); // Managers 밖 최상위 (DontDestroyOnLoad 때문)
            log.Add("GameManager_BastetRoom 만듦");
        }

        GameObject managers = GameObject.Find("Managers");
        if (managers == null)
        {
            managers = new GameObject("Managers");
            Undo.RegisterCreatedObjectUndo(managers, "Managers");
        }
        if (Object.FindFirstObjectByType<ItemInventory>(FindObjectsInactive.Include) == null)
        {
            Undo.AddComponent<ItemInventory>(managers);
            log.Add("Managers에 ItemInventory 붙임");
        }

        var toast = Object.FindFirstObjectByType<AnubisToast>(FindObjectsInactive.Include);
        if (toast == null) toast = Undo.AddComponent<AnubisToast>(managers);
        var tso = new SerializedObject(toast);
        SetObject(tso, "font", font);
        SetString(tso, "startMessage", ""); // 튜토리얼 시작 문구가 복사돼 왔으면 지움 (입장 연출이 안내)
        tso.ApplyModifiedPropertiesWithoutUndo();

        var prompt = Object.FindFirstObjectByType<AnubisPromptUI>(FindObjectsInactive.Include);
        if (prompt == null) prompt = Undo.AddComponent<AnubisPromptUI>(managers);
        var pso = new SerializedObject(prompt);
        SetObject(pso, "font", font);
        pso.ApplyModifiedPropertiesWithoutUndo();

        var es = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (es == null)
        {
            var go = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(go, "EventSystem");
            es = go.AddComponent<EventSystem>();
        }
        var legacy = es.GetComponent<StandaloneInputModule>();
        if (legacy != null) Undo.DestroyObjectImmediate(legacy);
        if (es.GetComponent<InputSystemUIInputModule>() == null) Undo.AddComponent<InputSystemUIInputModule>(es.gameObject);
    }

    // ================= 2. 퍼즐 찾기 =================
    private static List<PuzzleInfo> FindPuzzles(List<string> log)
    {
        var infos = new List<PuzzleInfo>
        {
            new PuzzleInfo
            {
                number = 1, suffix = "Footprint", popupName = "Popup_Bastet1_Footprint", prompt = "고양이 발자국 제단 조사",
                requiredItem = "", reward = BastetItemIds.Paw, toast = "발자국을 끝까지 따라갔다! 은 발자국 인장을 얻었다.",
                clueId = "BASTET_B1", clueTitle = "고양이 발자국",
                clueBody = "바스테트는 지나온 길을 잊지 않는다.\n고양이 석상이 앞발을 내리치면 발자국이 빛난다.\n끝까지 지켜본 뒤, 같은 순서로 밟아라. 길을 벗어나면 횃불이 꺼진다.",
                placeholderOffset = new Vector3(-6f, -2f, 0f),
                puzzle = (PuzzleBase)Find<BastetFootprintWalk>() ?? Find<BastetFootprintPuzzle>() // [수정] 걷는 버전 우선
            },
            new PuzzleInfo
            {
                number = 2, suffix = "EyeStatue", popupName = "Popup_Bastet2_EyeSequence", prompt = "고양이 눈 제단 조사",
                requiredItem = BastetItemIds.Paw, reward = BastetItemIds.Eye, toast = "고양이의 시선을 되감았다! 호박 눈을 얻었다.",
                clueId = "BASTET_B2", clueTitle = "고양이의 눈",
                clueBody = "고양이는 뒤를 돌아본다.\n작은 석상들의 눈이 빛난 순서를 기억하고, 마지막에 빛난 석상부터 거꾸로 찾아가라.\n틀리면 석상이 횃불을 삼킨다.",
                placeholderOffset = new Vector3(-6f, 3f, 0f),
                puzzle = (PuzzleBase)Find<BastetEyeStatueRing>() ?? Find<BastetEyeSequencePuzzle>() // [수정] 걷는 버전 우선
            },
            new PuzzleInfo
            {
                number = 3, suffix = "Necklace", popupName = "Popup_Bastet3_Necklace", prompt = "고양이 목걸이 제단 조사",
                requiredItem = BastetItemIds.Eye, reward = BastetItemIds.Necklace, toast = "목걸이가 좌우로 빛난다! 청금석 목걸이를 얻었다.",
                clueId = "BASTET_B3", clueTitle = "목걸이 거울",
                clueBody = "바스테트의 목걸이는 가운데 선을 거울로 삼는다.\n짝: 1↔5, 2↔6, 3↔7, 4↔8. 위아래는 그대로, 왼쪽 오른쪽은 반대로.\n조각 하나를 돌리면 이웃한 조각도 함께 돈다.",
                placeholderOffset = new Vector3(6f, 3f, 0f),
                puzzle = Find<BastetNecklacePuzzle>()
            },
            new PuzzleInfo
            {
                number = 4, suffix = "ClawPattern", popupName = "Popup_Bastet4_ClawPattern", prompt = "고양이 발톱 벽화 조사",
                requiredItem = BastetItemIds.Necklace, reward = BastetItemIds.Claw, toast = "벽화의 무늬가 이어졌다! 발톱 인장을 얻었다.",
                clueId = "BASTET_B4", clueTitle = "발톱 벽화",
                clueBody = "벽화의 발톱 무늬에는 두 가지 규칙이 있다.\n한 줄 안에서는 왼쪽과 오른쪽이 거울처럼 같다.\n아래 줄로 내려갈수록 발톱이 하나씩 깊어지고, 가장 깊은 다음은 다시 하나로 돌아간다.",
                placeholderOffset = new Vector3(6f, -2f, 0f),
                puzzle = Find<BastetClawPatternPuzzle>()
            },
            new PuzzleInfo
            {
                number = 5, suffix = "SacredMain", popupName = "Popup_Bastet5_SunMoon", prompt = "바스테트 신상 조사",
                requiredItem = BastetItemIds.Claw, reward = "", flag = BastetItemIds.FlagMainDone,
                toast = "새벽이 열렸다! 북쪽 문이 열린다.",
                clueId = "BASTET_B5", clueTitle = "해와 달의 기억",
                clueBody = "낮의 바스테트는 라의 눈. 태양의 배를 따라 걸은 발자국을 기억하라.\n밤이 오면 수호자는 지나온 길을 거슬러 돌아간다.\n마지막에 빛난 눈부터, 거꾸로.",
                placeholderOffset = new Vector3(0f, 7f, 0f),
                puzzle = (PuzzleBase)Find<BastetSunMoonWalk>() ?? Find<BastetSunMoonMemoryPuzzle>() // [수정] 걷는 버전 우선
            }
        };

        // B5: 해와 달이 없으면 옛 메인(신성한 반복)이라도 연결
        if (infos[4].puzzle == null)
        {
            infos[4].puzzle = Find<BastetMainPuzzle>();
            infos[4].popupName = "Popup_Bastet5_Main";
            if (infos[4].puzzle != null) log.Add("경고: 해와 달의 기억이 없어 옛 B5(BastetMainPuzzle)에 연결. Tools/Bastet/B5 해와 달의 기억 UI 만들기를 먼저 실행 추천");
        }

        // [추가] B1~B4도 클리어 플래그를 켬 (아이템을 봉인석에 넣어 사라져도 목표·비석 진행이 유지되게)
        foreach (PuzzleInfo info in infos)
            if (info.number <= 4 && string.IsNullOrEmpty(info.flag)) info.flag = BastetItemIds.ClearFlag(info.number);

        var missing = new List<string>();
        foreach (PuzzleInfo info in infos)
        {
            if (info.puzzle == null) { missing.Add($"B{info.number}"); continue; }

            info.panel = FindPanel(info.puzzle);
            if (info.puzzle is BastetWalkPuzzleBase) { log.Add($"B{info.number}: 걷는 퍼즐 '{info.puzzle.name}' 사용"); continue; } // [추가] 팝업 없음
            // 팝업 이름이 바뀌어 있으면 (예: Altar_Bastet3_Necklace) 원래 이름으로
            if (info.panel.name != info.popupName && info.panel.GetComponentInParent<Canvas>(true) != null)
            {
                log.Add($"팝업 이름 정리: '{info.panel.name}' -> '{info.popupName}'");
                Undo.RecordObject(info.panel, "팝업 이름");
                info.panel.name = info.popupName;
            }
        }

        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog("퍼즐을 못 찾음",
                $"씬에서 {string.Join(", ", missing)} 퍼즐 컴포넌트를 찾지 못했습니다.\nPuzzle_Test_Bastet의 Canvas(팝업)를 먼저 복사해 오세요.", "확인");
            return null;
        }
        return infos;
    }

    private static GameObject FindPanel(PuzzleBase puzzle)
    {
        var so = new SerializedObject(puzzle);
        SerializedProperty p = so.FindProperty("puzzleUIRoot") ?? so.FindProperty("puzzleRoot");
        if (p != null && p.objectReferenceValue is GameObject go) return go;
        return puzzle.gameObject;
    }

    // ================= 3. 제단 =================
    private static void EnsureAltars(List<string> log, List<PuzzleInfo> infos)
    {
        Vector3 pivot = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        pivot.z = 0f;
        var existing = Object.FindObjectsByType<BastetAltarInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        // [추가] 5개 방 모드(봉인석이 있음): 방 입구 봉인석이 잠금을 맡고 아이템은 넣으면 사라지므로, B2~B4 석상/제단 잠금은 끔
        bool gated = Object.FindObjectsByType<BastetRoomGate>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0;
        if (gated) log.Add("5개 방 모드: B2~B5 잠금 = 방 입구 봉인석 (아이템 넣으면 사라짐)");

        foreach (PuzzleInfo info in infos)
        {
            // [추가] 걷는 퍼즐: 제단 대신 시작 석상(BastetPuzzleStarter)에 잠금을 걸고, 비석은 석상 옆에
            if (info.puzzle is BastetWalkPuzzleBase walk)
            {
                BastetPuzzleStarter starter = null;
                foreach (var st in Object.FindObjectsByType<BastetPuzzleStarter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (st.Target == walk) { starter = st; break; }
                if (starter == null)
                {
                    log.Add($"경고: B{info.number} 걷는 퍼즐의 시작 석상이 없음 -> Tools/Bastet/걷는 퍼즐 메뉴로 다시 만들기");
                    info.altar = walk.gameObject;
                    continue;
                }
                Undo.RecordObject(starter, "시작 석상 잠금");
                string lockItem = gated && info.number >= 2 ? "" : info.requiredItem; // [수정] 5개 방 모드: 방 입구 봉인석이 잠금 (B5 = 북쪽 봉인석에 발톱 인장)
                starter.EditorSetLock(lockItem,
                    info.number == 5 ? "신상이 아직 잠들어 있다. 발톱 인장을 가져와 바치자." : "석상이 아직 잠들어 있다. 앞의 시험을 먼저 통과하자.");
                starter.EditorSetConsume(false, ""); // [수정] 아이템은 봉인석에서 넣음
                EditorUtility.SetDirty(starter);
                info.altar = starter.gameObject;
                string lt = string.IsNullOrEmpty(lockItem) ? "처음부터 열림 (방 봉인석이 잠금)" : $"{lockItem} 필요";
                log.Add($"{starter.name} (B{info.number} 걷는 퍼즐 시작 석상): {lt}");
                continue;
            }

            // 같은 퍼즐을 여는 제단이 이미 있으면 그대로 씀
            BastetAltarInteractable altar = null;
            foreach (var a in existing)
                if (a.TargetPuzzle == info.puzzle && a.gameObject.scene.IsValid()) { altar = a; break; }

            GameObject go = altar != null ? altar.gameObject : FindInScene($"Altar_Bastet{info.number}_{info.suffix}");
            bool created = false;
            if (go == null)
            {
                go = MakePlaceholder($"Altar_Bastet{info.number}_{info.suffix}", pivot + info.placeholderOffset,
                    new Color(0.85f, 0.7f, 0.4f), new Vector2(1f, 1f));
                created = true;
            }
            else if (go.GetComponentInParent<Canvas>(true) != null)
            {
                // 이름만 Altar인 팝업이면 제단으로 쓰지 않음
                go = MakePlaceholder($"Altar_Bastet{info.number}_{info.suffix}", pivot + info.placeholderOffset,
                    new Color(0.85f, 0.7f, 0.4f), new Vector2(1f, 1f));
                created = true;
            }

            if (go.GetComponent<Collider2D>() == null) Undo.AddComponent<BoxCollider2D>(go);
            if (altar == null) altar = go.GetComponent<BastetAltarInteractable>();
            if (altar == null) altar = Undo.AddComponent<BastetAltarInteractable>(go);

            Undo.RecordObject(altar, "제단 연결");
            altar.EditorSetup(info.prompt, info.puzzle, info.panel, gated ? "" : info.requiredItem, // [수정] 5개 방 모드는 봉인석이 잠금
                info.number == 5 ? "신상이 아직 잠들어 있다. 네 개의 흔적을 먼저 모으자." : "제단이 아직 잠들어 있다. 앞의 시험을 먼저 통과하자.");
            EditorUtility.SetDirty(altar);
            FixSortingLayer(go);

            // 팝업은 처음에 꺼 둠
            if (info.panel.activeSelf) { Undo.RecordObject(info.panel, "팝업 끄기"); info.panel.SetActive(false); }

            info.altar = go;
            string lockText = string.IsNullOrEmpty(info.requiredItem) ? "처음부터 열림" : $"{info.requiredItem} 필요";
            log.Add($"{go.name}: {(created ? "임시 상자 생성 (맵에 옮기기)" : "기존 제단 사용")} -> {info.panel.name}, {lockText}");
        }
    }

    // ================= 4. 비석 화면 =================
    private static CluePanel EnsureCluePanel(List<string> log)
    {
        var cluePanel = Object.FindFirstObjectByType<CluePanel>(FindObjectsInactive.Include);
        if (cluePanel != null) return cluePanel;

        GameObject canvasGo = GameObject.Find(StoryCanvasName);
        if (canvasGo == null)
        {
            canvasGo = new GameObject(StoryCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "비석 캔버스");
            canvasGo.layer = 5;
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 65; // 퍼즐(50), 알림(55)보다 위
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        RectTransform rt = CreateStretchChild(canvasGo.transform, "Clue_Overlay");
        cluePanel = rt.gameObject.AddComponent<CluePanel>();
        var so = new SerializedObject(cluePanel);
        SetObject(so, "frameSprite", FindSprite("UI_Journal_Frame"));
        so.ApplyModifiedPropertiesWithoutUndo();
        log.Add("비석 화면: BastetStoryCanvas/Clue_Overlay (CluePanel) 만듦");
        return cluePanel;
    }

    // ================= 5. 방 UI (보상, 목표, 소지품, 입장 연출) =================
    private static GameObject EnsureRoomRoot(List<string> log, TMP_FontAsset font, List<PuzzleInfo> infos)
    {
        GameObject room = GameObject.Find(RoomRootName);
        if (room == null)
        {
            room = new GameObject(RoomRootName);
            Undo.RegisterCreatedObjectUndo(room, "바스테트 방 UI");
        }

        // 보상 지급
        var progress = GetOrAdd<BastetRoomProgress>(room);
        var entries = new BastetRoomProgress.Entry[infos.Count];
        for (int i = 0; i < infos.Count; i++)
        {
            entries[i] = new BastetRoomProgress.Entry
            {
                puzzle = infos[i].puzzle, rewardItemId = infos[i].reward, flag = infos[i].flag, toast = infos[i].toast
            };
        }
        Undo.RecordObject(progress, "보상 연결");
        progress.EditorSetEntries(entries);
        EditorUtility.SetDirty(progress);
        log.Add("보상: B1 발자국 인장 -> B2 호박 눈 -> B3 청금석 목걸이 -> B4 발톱 인장 -> B5 클리어 플래그");

        // 목표 띠 (아누비스 방과 같은 컴포넌트, 단계만 바스테트용)
        var guide = GetOrAdd<AnubisObjectiveGuide>(room);
        var gso = new SerializedObject(guide);
        SetObject(gso, "font", font);
        var steps = new (string item, string flag, string text)[]
        {
            ("", BastetItemIds.FlagHallRead, "입구의 비석을 읽어 보자 (\"!\")"),
            ("", BastetItemIds.FlagB1Clear, "발자국의 방에서 고양이 석상의 시험을 통과하자"),          // [수정] 플래그로 진행
            ("", BastetItemIds.FlagB2Clear, "발자국 인장을 눈의 방 봉인석에 넣고 시험을 통과하자"),
            ("", BastetItemIds.FlagB3Clear, "호박 눈을 목걸이의 방 봉인석에 넣고 시험을 통과하자"),
            ("", BastetItemIds.FlagB4Clear, "목걸이 구슬을 발톱의 방 봉인석에 넣고 시험을 통과하자"),
            ("", BastetItemIds.FlagMainDone, "발톱 인장을 북쪽 봉인석에 넣고 신상의 방에서 해와 달의 시험을 풀자"),
            ("", "", "북쪽 문이 열렸다. 아누비스의 신전으로 가자")
        };
        SerializedProperty stepsProp = gso.FindProperty("steps");
        stepsProp.arraySize = steps.Length;
        for (int i = 0; i < steps.Length; i++)
        {
            SerializedProperty s = stepsProp.GetArrayElementAtIndex(i);
            s.FindPropertyRelative("doneItemId").stringValue = steps[i].item;
            s.FindPropertyRelative("doneFlag").stringValue = steps[i].flag;
            s.FindPropertyRelative("objective").stringValue = steps[i].text;
        }
        gso.ApplyModifiedPropertiesWithoutUndo();
        log.Add($"목표 띠: {steps.Length}단계");

        // 소지품 바 (아이콘은 그림이 오면 Inspector에서 넣기)
        var hud = GetOrAdd<InventoryHUD>(room);
        var items = new (string id, string label)[]
        {
            (BastetItemIds.Paw, "은 발자국 인장"), (BastetItemIds.Eye, "호박 눈"),
            (BastetItemIds.Necklace, "청금석 목걸이"), (BastetItemIds.Claw, "발톱 인장"), (BastetItemIds.Plate, "고양이 형판")
        };
        var hso = new SerializedObject(hud);
        SerializedProperty list = hso.FindProperty("entries");
        if (list != null)
        {
            list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                SerializedProperty e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("itemId").stringValue = items[i].id;
                e.FindPropertyRelative("displayName").stringValue = items[i].label;
            }
            hso.ApplyModifiedPropertiesWithoutUndo();
        }

        // 입장 연출 (맵 줌아웃)
        var intro = GetOrAdd<RaIntroCutscene>(room);
        var iso = new SerializedObject(intro);
        SetString(iso, "playedFlag", BastetItemIds.FlagIntroPlayed);
        SetString(iso, "overviewMessage", "바스테트의 신전. 불이 켜진 발자국의 방부터 가 보자. 시험을 통과할 때마다 다음 방에 불이 켜진다.");
        iso.ApplyModifiedPropertiesWithoutUndo();

        if (Camera.main == null || Camera.main.GetComponent<CameraFollow2D>() == null)
            log.Add("경고: Main Camera에 CameraFollow2D가 없어 입장 연출이 건너뛰어짐 (Main Camera에 추가 추천)");

        return room;
    }

    // ================= 6. 비석 단서 + "!" =================
    private static void EnsureSteles(List<string> log, List<PuzzleInfo> infos, CluePanel panel)
    {
        EnsureFolder(ClueFolder);
        Sprite steleSprite = FindSprite("Slate");

        // 입구 비석: 플레이어 근처
        ClueData hall = EnsureClueData("BASTET_HALL", "바스테트의 신전",
            "고양이 여신 바스테트는 낮에는 라의 눈으로 태양을 지키고, 밤에는 아펩을 막는 수호자가 된다.\n" +
            "네 제단의 시험을 차례로 통과해 그녀의 흔적(발자국, 눈, 목걸이, 발톱)을 모아라.\n흔적이 모두 모이면 가운데 신상이 깨어난다.");
        var player = Object.FindFirstObjectByType<PlayerController2D>(FindObjectsInactive.Include);
        Vector3 hallPos = (player != null ? player.transform.position : Vector3.zero) + new Vector3(-1.6f, 1.2f, 0f);
        SetupStele("Slate_Bastet_Hall", hallPos, hall, "", panel, steleSprite);

        // 제단 옆 비석: B1은 처음부터, 나머지는 앞 퍼즐 보상을 얻으면 "!"
        foreach (PuzzleInfo info in infos)
        {
            ClueData data = EnsureClueData(info.clueId, info.clueTitle, info.clueBody);
            Vector3 pos = info.altar.transform.position + new Vector3(-1.8f, 0.3f, 0f);
            // [수정] 앞 퍼즐 클리어 플래그로 "!" (보상을 봉인석에 넣어 사라져도 유지)
            string prevFlag = info.number >= 2 ? BastetItemIds.ClearFlag(info.number - 1) : "";
            if (info.number == 5) prevFlag = BastetItemIds.FlagB4Clear;
            SetupStele($"Stele_B{info.number}", pos, data, "", panel, steleSprite, prevFlag);
        }
        log.Add($"비석 6개 (입구 + B1~B5), \"!\" 순서: 입구·B1 -> B2 -> B3 -> B4 -> B5 (문구: {ClueFolder})");
    }

    private static void SetupStele(string objName, Vector3 pos, ClueData data, string unlockItem, CluePanel panel, Sprite sprite, string unlockFlag = "")
    {
        GameObject go = FindInScene(objName);
        if (go == null)
        {
            go = new GameObject(objName);
            Undo.RegisterCreatedObjectUndo(go, objName);
            go.transform.position = pos; // 처음 만들 때만 위치 지정 (옮긴 위치는 유지)
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (sprite == null) { sr.color = new Color(0.55f, 0.5f, 0.45f); go.transform.localScale = new Vector3(0.7f, 1f, 1f); }
            sr.sortingLayerName = ObjectLayer;
            sr.sortingOrder = 5;
        }
        FixSortingLayer(go);

        if (go.GetComponent<Collider2D>() == null)
        {
            var col = Undo.AddComponent<BoxCollider2D>(go);
            col.isTrigger = true; // 비석은 지나갈 수 있게
        }

        if (go.GetComponent<BastetClueStele>() != null) return; // [추가] 이미 바스테트 비석(라의 방 방식)으로 바꿨으면 그대로

        var stele = go.GetComponent<AnubisClueStele>();
        if (stele == null) stele = Undo.AddComponent<AnubisClueStele>(go);
        SpriteRenderer bubble = EnsureBubble(go);

        var so = new SerializedObject(stele);
        SetObject(so, "data", data);
        SetObject(so, "panel", panel);
        SetObject(so, "newBubble", bubble);
        SetString(so, "unlockItemId", unlockItem);
        SetString(so, "unlockFlag", unlockFlag); // [추가]
        SetString(so, "lockedMessage", "글자가 아직 흐릿하다. 앞의 시험을 먼저 통과하자.");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static ClueData EnsureClueData(string id, string title, string body)
    {
        string path = $"{ClueFolder}/Clue_{id}.asset";
        var data = AssetDatabase.LoadAssetAtPath<ClueData>(path);
        if (data != null) return data; // 고친 문구 보호

        data = ScriptableObject.CreateInstance<ClueData>();
        AssetDatabase.CreateAsset(data, path);
        var so = new SerializedObject(data);
        SetString(so, "clueId", id);
        so.FindProperty("day.title").stringValue = title;
        so.FindProperty("day.body").stringValue = body;
        so.FindProperty("day.illustrationTint").colorValue = Color.white;
        so.ApplyModifiedPropertiesWithoutUndo();
        return data;
    }

    // ================= 7. 힌트 버튼 =================
    private static void EnsureHints(List<string> log, List<PuzzleInfo> infos)
    {
        EnsureFolder(HintFolder);
        var texts = new Dictionary<int, string[]>
        {
            [1] = new[]
            {
                "처음 칸과 마지막 칸은 하늘색으로 빛난다. 시작과 끝을 먼저 기억하자.",
                "칸 번호보다 '위, 아래, 왼쪽, 오른쪽' 방향으로 외우면 쉽다. 경로는 이웃한 칸으로만 이어진다.",
                "같은 칸을 두 번 밟는 구간이 있다. 틀리면 처음부터 다시 보여 주니, [다시 보기]로 끝까지 지켜보자."
            },
            [2] = new[]
            {
                "함께 빛난 눈은 한 묶음이다. 묶음 안의 눈은 아무 순서로 눌러도 된다.",
                "정답은 빛난 순서의 정반대. 마지막에 빛난 묶음부터 누른다.",
                "보여 준 뒤 분홍빛으로 거꾸로 한 번 더 되감아 준다. 그 순서가 바로 누를 순서다."
            },
            [3] = new[]
            {
                "짝: 1↔5, 2↔6, 3↔7, 4↔8. 위·아래 방향은 같고, 왼쪽·오른쪽 방향은 반대여야 거울이다.",
                "한 조각을 누르면 연결된 조각도 90도 돈다. 같은 조각을 4번 누르면 원래대로 돌아온다.",
                "확인 버튼을 누르면 틀린 짝이 빨갛게 표시된다. 한 짝씩 고쳐 나가자."
            },
            [4] = new[]
            {
                "각 줄은 좌우 대칭이다. 1번째 칸 = 4번째 칸, 2번째 칸 = 3번째 칸.",
                "세로로 한 줄 내려갈 때마다 / -> // -> /// -> / 순서로 한 단계씩 깊어진다.",
                "빈칸의 바로 위 칸을 보고 한 단계 깊게, 그다음 좌우 짝과 같은지 확인하자."
            },
            [5] = new[]
            {
                "해 단계는 라운드마다 앞의 순서에 하나씩 더해진다. 새로 붙는 마지막 하나만 더 외우면 된다.",
                "석상마다 색이 다르다. 번호 대신 색으로 외워 보자.",
                "달 단계는 6개의 눈이 한 번씩 빛난다. 마지막에 빛난 눈부터 거꾸로 누른다.",
                "같은 단계에서 3번 틀리면 순서가 새로 섞인다. 서두르지 말고 끝까지 본 뒤 누르자."
            }
        };

        Sprite normal = FindSprite("UI_Hint_Normal");
        Sprite hover = FindSprite("UI_Hint_Hover");
        Sprite disabled = FindSprite("UI_Hint_Disabled");

        foreach (PuzzleInfo info in infos)
        {
            if (info.puzzle is BastetWalkPuzzleBase) continue; // [추가] 걷는 퍼즐은 화면이 없어 힌트 버튼 없음 (비석이 힌트)
            var data = EnsureHintData($"B{info.number}", texts[info.number]);

            Transform child = info.panel.transform.Find("HintButton");
            RectTransform rt = child != null ? (RectTransform)child : CreateStretchChild(info.panel.transform, "HintButton");
            rt.SetAsLastSibling(); // 팝업 안 다른 UI보다 위

            var provider = rt.GetComponent<AnubisPuzzleHints>();
            if (provider == null) provider = Undo.AddComponent<AnubisPuzzleHints>(rt.gameObject);
            var pso = new SerializedObject(provider);
            SetObject(pso, "data", data);
            SetString(pso, "doneItemId", info.number == 5 ? BastetItemIds.Plate : info.reward);
            SetString(pso, "doneFlag", info.flag ?? "");
            pso.ApplyModifiedPropertiesWithoutUndo();

            var button = rt.GetComponent<PuzzleHintButton>();
            if (button == null) button = Undo.AddComponent<PuzzleHintButton>(rt.gameObject);
            var bso = new SerializedObject(button);
            SetObject(bso, "provider", provider);
            SetObject(bso, "normalSprite", normal);
            SetObject(bso, "hoverSprite", hover);
            SetObject(bso, "disabledSprite", disabled);
            // B1~B4 팝업(1200x800) 오른쪽 위 / B5(화면 전체) 닫기 버튼 왼쪽
            bool main = info.number == 5;
            SetVector2(bso, "buttonPos", main ? new Vector2(420f, 385f) : new Vector2(520f, 320f));
            SetVector2(bso, "messagePos", main ? new Vector2(0f, -480f) : new Vector2(0f, -350f));
            bso.ApplyModifiedPropertiesWithoutUndo();
        }
        log.Add($"힌트 버튼 5개 (문구: {HintFolder}, 45초 쿨타임)");
    }

    private static AnubisHintData EnsureHintData(string id, string[] hints)
    {
        string path = $"{HintFolder}/Hint_Bastet_{id}.asset";
        var data = AssetDatabase.LoadAssetAtPath<AnubisHintData>(path);
        if (data != null) return data;
        data = ScriptableObject.CreateInstance<AnubisHintData>();
        data.EditorSetHints(hints);
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    // ================= 8. 토트 출구 =================
    private static void EnsureExit(List<string> log, TMP_FontAsset font)
    {
        GameObject exit = FindInScene(ExitName);
        if (exit == null)
        {
            GameObject door = FindInScene(DoorName);
            Vector3 pos = door != null ? door.transform.position + new Vector3(0f, 1.2f, 0f)
                : (SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero) + new Vector3(0f, 9f, 0f);
            pos.z = 0f;
            exit = MakePlaceholder(ExitName, pos, new Color(0.3f, 0.6f, 0.9f, 0.5f), new Vector2(2f, 1f));
            log.Add($"{ExitName}: 임시 출구 생성 ({(door != null ? DoorName + " 바로 뒤" : "위치 확인")})");
        }
        else FixSortingLayer(exit);

        var col = exit.GetComponent<Collider2D>();
        if (col == null) col = Undo.AddComponent<BoxCollider2D>(exit);
        col.isTrigger = true;

        var e = exit.GetComponent<AnubisSceneExit>();
        if (e == null) e = Undo.AddComponent<AnubisSceneExit>(exit);
        var so = new SerializedObject(e);
        SetString(so, "nextSceneName", NextSceneName);
        SetString(so, "exitMessage", "아누비스의 신전으로 향합니다..."); // [수정]
        SetObject(so, "font", font);
        SerializedProperty items = so.FindProperty("itemsToRemoveOnExit");
        if (items != null)
        {
            // 서브 퍼즐 보상은 다음 방에 필요 없음 (고양이 형판은 남김)
            string[] ids = { BastetItemIds.Paw, BastetItemIds.Eye, BastetItemIds.Necklace, BastetItemIds.Claw };
            items.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++) items.GetArrayElementAtIndex(i).stringValue = ids[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        // [추가] B5를 풀면 이 출구로 자동 이동하도록 연결
        var b5 = Find<BastetSunMoonWalk>();
        if (b5 != null)
        {
            Undo.RecordObject(b5, "출구 연결");
            b5.EditorSetExit(e, NextSceneName, "아누비스의 신전으로 향합니다...");
            EditorUtility.SetDirty(b5);
            log.Add($"B5 클리어 -> 보상 팝업을 닫으면 자동으로 '{NextSceneName}' 이동");
        }

        if (FindInScene(DoorName) == null)
            log.Add($"경고: '{DoorName}'(B5를 풀면 사라지는 문)이 없음 -> 출구가 처음부터 열려 있음. Puzzle_Test_Bastet에서 복사해 오기");
        log.Add($"{ExitName}: 밟으면 화면이 어두워지며 '{NextSceneName}'로 이동");
    }

    private static void EnsureBuildScenes(List<string> log)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        string current = EditorSceneManager.GetActiveScene().path;
        bool changed = false;
        foreach (string path in new[] { current, NextScenePath })
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path) || scenes.Exists(s => s.path == path)) continue;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            changed = true;
            log.Add($"Build Profiles 씬 목록에 추가: {path}");
        }
        if (changed) EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ================= 도우미 =================
    private static T Find<T>() where T : Object
    {
        foreach (T t in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var c = t as Component;
            if (c != null && c.gameObject.scene.IsValid()) return t;
        }
        return null;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c != null) return c;
        T existing = Find<T>();
        if (existing != null) return existing;
        return Undo.AddComponent<T>(go);
    }

    private static GameObject MakePlaceholder(string objName, Vector3 pos, Color color, Vector2 size)
    {
        var go = new GameObject(objName);
        Undo.RegisterCreatedObjectUndo(go, objName);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color = color;
        sr.sortingLayerName = ObjectLayer; // Default면 바닥 타일맵에 가려짐
        sr.sortingOrder = 5;
        go.AddComponent<BoxCollider2D>();
        return go;
    }

    private static void FixSortingLayer(GameObject go)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sortingLayerName != "Default") return;
        Undo.RecordObject(sr, "정렬 레이어");
        sr.sortingLayerName = ObjectLayer;
    }

    // 머리 위 "!" 말풍선 (라/아누비스 방과 같은 UI_Prompt_New)
    private static SpriteRenderer EnsureBubble(GameObject owner)
    {
        Transform t = owner.transform.Find("NewBubble");
        SpriteRenderer sr;
        if (t != null) sr = t.GetComponent<SpriteRenderer>();
        else
        {
            var go = new GameObject("NewBubble");
            Undo.RegisterCreatedObjectUndo(go, "말풍선");
            go.transform.SetParent(owner.transform, false);
            sr = go.AddComponent<SpriteRenderer>();
        }
        if (sr == null) return null;

        sr.sprite = FindSprite("UI_Prompt_New");
        var parentSr = owner.GetComponent<SpriteRenderer>();
        float top = 1.2f;
        if (parentSr != null)
        {
            sr.sortingLayerID = parentSr.sortingLayerID;
            sr.sortingOrder = parentSr.sortingOrder + 1;
            if (parentSr.sprite != null) top = parentSr.sprite.bounds.max.y + 0.35f;
        }
        sr.transform.localPosition = new Vector3(0f, top, 0f);
        sr.enabled = false; // 스크립트가 켬
        return sr;
    }

    private static RectTransform CreateStretchChild(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, name);
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static GameObject FindInScene(string name)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name && t.gameObject.scene.IsValid()) return t.gameObject;
        return null;
    }

    private static Sprite FindSprite(string fileName)
    {
        foreach (string guid in AssetDatabase.FindAssets($"{fileName} t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) != fileName) continue;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite s) return s;
        }
        return null;
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

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static void SetObject(SerializedObject so, string prop, Object value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p != null && value != null) p.objectReferenceValue = value;
    }

    private static void SetString(SerializedObject so, string prop, string value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p != null) p.stringValue = value;
    }

    private static void SetVector2(SerializedObject so, string prop, Vector2 value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p != null) p.vector2Value = value;
    }
}
#endif
