#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// 메뉴: Tools/Bastet/튜토리얼 만들기 
// 지금 열려 있는 임시 맵 씬에 바스테트 튜토리얼을 한 번에 세팅
//  1) GameManager / Managers(소지품, 토스트, 아래 안내 문구) / EventSystem
//  2) 튜토리얼 데이터 4개 (Bastet/Data/Tutorial)
//  3) 제단 Altar_T1~T4 (씬에 같은 이름이 있으면 그 오브젝트에 붙임, 없으면 임시 상자 생성)
//  4) 석문 Gate_T (보상 4개 필요) / 출구 Exit_ToBastet (화면 어두워지며 실전 방으로)
//  5) Build Profiles 씬 목록에 현재 씬 + 실전 씬 추가
// 여러 번 눌러도 안전 (이미 있는 것은 위치를 건드리지 않고 값만 갱신)
public static class BastetTutorialSetup
{
    private const string DataFolder = "Assets/Scripts/Puzzles/Bastet/Data/Tutorial";
    private const string RealScenePath = "Assets/Scenes/Puzzle_Test/Puzzle_Test_Bastet.unity";
    private const string RealSceneName = "Puzzle_Test_Bastet";

    public static readonly string[] RewardIds =
    {
        "ITEM_BASTET_TUT_1", "ITEM_BASTET_TUT_2", "ITEM_BASTET_TUT_3", "ITEM_BASTET_TUT_4"
    };

    private static readonly string[] AltarNames = { "Altar_T1", "Altar_T2", "Altar_T3", "Altar_T4" };

    [MenuItem("Tools/Bastet/튜토리얼 만들기 (열린 씬)")]
    public static void Apply()
    {
        var log = new List<string>();
        TMP_FontAsset font = FindFont();
        if (font == null) log.Add("경고: MonoplexKR-Regular SDF 폰트를 못 찾음 (한글이 □로 보일 수 있음)");

        EnsureGameManager(log);
        EnsureManagers(log, font);
        EnsureEventSystem(log);
        BastetTutorialData[] datas = EnsureDataAssets(log);
        EnsureAltars(log, datas, font);
        EnsureGate(log);
        EnsureExit(log, font);
        EnsureBuildScenes(log);

        if (Object.FindFirstObjectByType<PlayerController2D>(FindObjectsInactive.Include) == null)
            log.Add("경고: 씬에 PlayerController2D(플레이어)가 없음. Player 프리팹을 넣고 Tag = Player 확인");

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[BastetTutorialSetup] 완료\n- " + string.Join("\n- ", log));
    }

    // ================= 1. 매니저 =================
    private static void EnsureGameManager(List<string> log)
    {
        if (Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include) != null) return;
        var go = new GameObject("GameManager_BastetTest");
        Undo.RegisterCreatedObjectUndo(go, "GameManager");
        go.AddComponent<GameManager>();
        log.Add("GameManager_BastetTest 만듦 (최상위)");
    }

    private static void EnsureManagers(List<string> log, TMP_FontAsset font)
    {
        GameObject managers = GameObject.Find("Managers");
        if (managers == null)
        {
            managers = new GameObject("Managers");
            Undo.RegisterCreatedObjectUndo(managers, "Managers");
            log.Add("Managers 만듦");
        }

        if (Object.FindFirstObjectByType<ItemInventory>(FindObjectsInactive.Include) == null)
        {
            Undo.AddComponent<ItemInventory>(managers);
            log.Add("Managers에 ItemInventory 붙임");
        }

        var toast = Object.FindFirstObjectByType<AnubisToast>(FindObjectsInactive.Include);
        if (toast == null)
        {
            toast = Undo.AddComponent<AnubisToast>(managers);
            log.Add("Managers에 AnubisToast 붙임 (가운데 알림)");
        }
        var tso = new SerializedObject(toast);
        SetObject(tso, "font", font);
        var start = tso.FindProperty("startMessage");
        if (start != null && string.IsNullOrEmpty(start.stringValue))
            start.stringValue = "바스테트의 시험장. 네 개의 제단에서 시험을 통과하면 석문이 열린다.";
        tso.ApplyModifiedPropertiesWithoutUndo();

        var prompt = Object.FindFirstObjectByType<AnubisPromptUI>(FindObjectsInactive.Include);
        if (prompt == null)
        {
            prompt = Undo.AddComponent<AnubisPromptUI>(managers);
            log.Add("Managers에 AnubisPromptUI 붙임 (아래 'E: …' 안내)");
        }
        var pso = new SerializedObject(prompt);
        SetObject(pso, "font", font);
        pso.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureEventSystem(List<string> log)
    {
        var es = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (es == null)
        {
            var go = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(go, "EventSystem");
            es = go.AddComponent<EventSystem>();
            log.Add("EventSystem 만듦");
        }
        var legacy = es.GetComponent<StandaloneInputModule>();
        if (legacy != null) { Undo.DestroyObjectImmediate(legacy); log.Add("StandaloneInputModule -> InputSystemUIInputModule 교체"); }
        if (es.GetComponent<InputSystemUIInputModule>() == null) Undo.AddComponent<InputSystemUIInputModule>(es.gameObject);
    }

    // ================= 2. 데이터 =================
    private static BastetTutorialData[] EnsureDataAssets(List<string> log)
    {
        EnsureFolder(DataFolder);
        var result = new BastetTutorialData[4];

        result[0] = GetOrCreate("BastetTut_1_Footprint", log, so =>
        {
            Fill(so, BastetTutorialData.Mode.Footprint, "시험 1. 고양이 발자국",
                "빛나는 발자국을 잘 봐 두어라. 불이 꺼지면 같은 순서로 밟는다.\n길을 벗어나면 처음부터 다시 보여 준다. ('시작' 칸부터)",
                "발자국을 끝까지 따라갔다! 은빛 방울을 얻었다.", RewardIds[0]);
            so.FindProperty("gridSize").intValue = 3;
            SetInts(so, "sequence", 6, 3, 4, 1);   // 왼쪽 아래 -> 왼쪽 가운데 -> 가운데 -> 위 가운데
        });

        result[1] = GetOrCreate("BastetTut_2_Eye", log, so =>
        {
            Fill(so, BastetTutorialData.Mode.EyeReverse, "시험 2. 고양이의 눈",
                "눈이 빛나는 순서를 기억해라.\n고양이는 뒤를 돌아본다: 마지막에 빛난 눈부터 거꾸로 누른다.",
                "고양이가 고개를 끄덕였다! 호박 눈알을 얻었다.", RewardIds[1]);
            so.FindProperty("buttonCount").intValue = 4;
            SetInts(so, "sequence", 0, 2, 3);       // 정답 입력: 4 -> 3 -> 1
        });

        result[2] = GetOrCreate("BastetTut_3_Necklace", log, so =>
        {
            Fill(so, BastetTutorialData.Mode.Necklace, "시험 3. 목걸이 거울",
                "가운데 금빛 선은 거울이다. 오른쪽 조각은 왼쪽 짝이 거울에 비친 모습이어야 한다.\n짝: 바깥 1번↔4번, 안쪽 2번↔3번. 위·아래는 그대로, 왼쪽·오른쪽은 반대로.",
                "목걸이가 좌우로 빛난다! 청금석 구슬을 얻었다.", RewardIds[2]);
            SetInts(so, "startRotations", 1, 0, 1, 2);
            so.FindProperty("firstPieceTurnsSecond").boolValue = true;
        });

        result[3] = GetOrCreate("BastetTut_4_Claw", log, so =>
        {
            Fill(so, BastetTutorialData.Mode.ClawPattern, "시험 4. 발톱 무늬",
                "발톱 무늬는 왼쪽과 오른쪽이 거울처럼 같다.\n아랫줄은 윗줄보다 발톱이 하나씩 더 깊다. 파란 빈칸을 눌러 채워라.",
                "무늬가 이어졌다! 발톱 인장을 얻었다.", RewardIds[3]);
            SetInts(so, "pattern", 1, 2, 2, 1, 2, 3, 3, 2);
            so.FindProperty("columns").intValue = 4;
            SetInts(so, "blanks", 1, 6);            // 정답: 1번 칸 = //, 6번 칸 = ///
        });

        return result;
    }

    private static BastetTutorialData GetOrCreate(string file, List<string> log, System.Action<SerializedObject> fill)
    {
        string path = $"{DataFolder}/{file}.asset";
        var data = AssetDatabase.LoadAssetAtPath<BastetTutorialData>(path);
        if (data != null) return data; // 이미 있으면 사용자가 고친 값을 유지

        data = ScriptableObject.CreateInstance<BastetTutorialData>();
        AssetDatabase.CreateAsset(data, path);
        var so = new SerializedObject(data);
        fill(so);
        so.ApplyModifiedPropertiesWithoutUndo();
        log.Add($"데이터 생성: {path}");
        return data;
    }

    private static void Fill(SerializedObject so, BastetTutorialData.Mode mode, string title, string rule, string success, string reward)
    {
        so.FindProperty("mode").enumValueIndex = (int)mode;
        so.FindProperty("title").stringValue = title;
        so.FindProperty("ruleText").stringValue = rule;
        so.FindProperty("successMessage").stringValue = success;
        so.FindProperty("rewardItemId").stringValue = reward;
    }

    private static void SetInts(SerializedObject so, string prop, params int[] values)
    {
        var p = so.FindProperty(prop);
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).intValue = values[i];
    }

    // ================= 3. 제단 =================
    private static void EnsureAltars(List<string> log, BastetTutorialData[] datas, TMP_FontAsset font)
    {
        Vector3 pivot = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        pivot.z = 0f;

        for (int i = 0; i < AltarNames.Length; i++)
        {
            GameObject altar = FindInScene(AltarNames[i]);
            bool created = false;
            if (altar == null)
            {
                // 임시 제단: 화면 가운데 기준 가로로 4개 (나중에 맵에 맞게 옮기기)
                altar = MakePlaceholder(AltarNames[i], pivot + new Vector3(-4.5f + i * 3f, 2f, 0f), new Color(0.85f, 0.7f, 0.4f), new Vector2(1f, 1f));
                created = true;
            }

            if (altar.GetComponent<Collider2D>() == null) Undo.AddComponent<BoxCollider2D>(altar);
            var puzzle = altar.GetComponent<BastetTutorialPuzzle>();
            if (puzzle == null) puzzle = Undo.AddComponent<BastetTutorialPuzzle>(altar);
            if (altar.GetComponent<BastetTutorialAltar>() == null) Undo.AddComponent<BastetTutorialAltar>(altar);
            FixSortingLayer(altar, log);

            Undo.RecordObject(puzzle, "튜토리얼 데이터");
            puzzle.EditorSetup(datas[i], font);
            EditorUtility.SetDirty(puzzle);
            log.Add($"{AltarNames[i]}: {(created ? "임시 상자 생성 (위치 옮기기)" : "기존 오브젝트에 붙임")} -> {datas[i].Title}");
        }
    }

    // ================= 4. 석문 / 출구 =================
    private static void EnsureGate(List<string> log)
    {
        GameObject gate = FindInScene("Gate_T");
        Vector3 pivot = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        pivot.z = 0f;
        if (gate == null)
        {
            gate = MakePlaceholder("Gate_T", pivot + new Vector3(0f, 5f, 0f), new Color(0.45f, 0.4f, 0.35f), new Vector2(3f, 1f));
            log.Add("Gate_T: 임시 석문 생성 (메인/출구로 가는 길목에 옮기기)");
        }
        var col = gate.GetComponent<Collider2D>();
        if (col == null) col = Undo.AddComponent<BoxCollider2D>(gate);
        col.isTrigger = false; // 닫혀 있을 때 길을 막음
        FixSortingLayer(gate, log);

        var g = gate.GetComponent<AnubisItemGate>();
        if (g == null) g = Undo.AddComponent<AnubisItemGate>(gate);
        var so = new SerializedObject(g);
        var req = so.FindProperty("requiredItems");
        req.arraySize = RewardIds.Length;
        for (int i = 0; i < RewardIds.Length; i++) req.GetArrayElementAtIndex(i).stringValue = RewardIds[i];
        so.FindProperty("lockedMessage").stringValue = "네 개의 시험을 모두 통과해야 석문이 열립니다.";
        so.FindProperty("openMessage").stringValue = "바스테트의 석문이 열립니다!";
        so.ApplyModifiedPropertiesWithoutUndo();
        log.Add("Gate_T: 보상 4개(ITEM_BASTET_TUT_1~4) 모이면 열림");
    }

    private static void EnsureExit(List<string> log, TMP_FontAsset font)
    {
        GameObject exit = FindInScene("Exit_ToBastet");
        Vector3 pivot = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        pivot.z = 0f;
        if (exit == null)
        {
            exit = MakePlaceholder("Exit_ToBastet", pivot + new Vector3(0f, 7f, 0f), new Color(0.3f, 0.6f, 0.9f, 0.5f), new Vector2(2f, 1f));
            log.Add("Exit_ToBastet: 임시 출구 생성 (석문 뒤쪽에 옮기기)");
        }
        var col = exit.GetComponent<Collider2D>();
        if (col == null) col = Undo.AddComponent<BoxCollider2D>(exit);
        col.isTrigger = true; // 밟으면 이동
        FixSortingLayer(exit, log);

        var e = exit.GetComponent<AnubisSceneExit>();
        if (e == null) e = Undo.AddComponent<AnubisSceneExit>(exit);
        var so = new SerializedObject(e);
        so.FindProperty("nextSceneName").stringValue = RealSceneName;
        so.FindProperty("exitMessage").stringValue = "바스테트의 진짜 신전으로 들어갑니다...";
        SetObject(so, "font", font);
        var items = so.FindProperty("itemsToRemoveOnExit");
        items.arraySize = RewardIds.Length; // 튜토리얼 보상은 실전으로 가져가지 않음
        for (int i = 0; i < RewardIds.Length; i++) items.GetArrayElementAtIndex(i).stringValue = RewardIds[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        log.Add($"Exit_ToBastet: 화면이 어두워지며 '{RealSceneName}'로 이동");
    }

    // ================= 5. Build Profiles 씬 목록 =================
    private static void EnsureBuildScenes(List<string> log)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        string current = EditorSceneManager.GetActiveScene().path;
        bool changed = false;
        foreach (string path in new[] { current, RealScenePath })
        {
            if (string.IsNullOrEmpty(path))
            {
                log.Add("경고: 현재 씬이 저장되지 않음. 먼저 Puzzle_Test_Bastet_Tutorial 이름으로 저장 후 다시 실행");
                continue;
            }
            if (!File.Exists(path)) { log.Add($"경고: {path} 없음"); continue; }
            if (scenes.Exists(s => s.path == path)) continue;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            changed = true;
            log.Add($"Build Profiles 씬 목록에 추가: {path}");
        }
        if (changed) EditorBuildSettings.scenes = scenes.ToArray();
    }

    private const string ObjectLayer = "Objects";

    private static void FixSortingLayer(GameObject go, List<string> log)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sortingLayerName != "Default") return;
        Undo.RecordObject(sr, "정렬 레이어");
        sr.sortingLayerName = ObjectLayer;
        log.Add($"{go.name}: Sorting Layer Default -> {ObjectLayer}");
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
        sr.sortingLayerName = ObjectLayer;
        sr.sortingOrder = 5;
        go.AddComponent<BoxCollider2D>();
        return go;
    }

    private static GameObject FindInScene(string objName)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == objName && t.gameObject.scene.IsValid()) return t.gameObject;
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
}
#endif