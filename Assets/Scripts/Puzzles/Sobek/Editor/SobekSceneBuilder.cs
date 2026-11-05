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
// 허브 맵은 임시 모래 바닥 + 벽 (나중에 수연님 타일맵으로 바꿔도 판·문 오브젝트는 그대로 옮겨 쓰면 됨)
public static class SobekSceneBuilder
{
    private const string SceneFolder = "Assets/Scenes/Puzzle_Test";
    private const string HubPath = SceneFolder + "/" + SobekIds.HubScene + ".unity";
    private const string MainPath = SceneFolder + "/" + SobekIds.MainScene + ".unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";

    // 허브 크기 (유닛 = 칸)
    private const float HubW = 40f, HubH = 28f;

    [MenuItem("Tools/Sobek/0. 전체 한 번에 (데이터 -> 허브 -> 메인)", priority = 0)]
    public static void All()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SobekDataSetup.Run();
        BuildMain(false);
        BuildHub(false); // 마지막에 허브를 열어 둠
    }

    [MenuItem("Tools/Sobek/2. 허브 씬 만들기 (Sobek_Hub)", priority = 2)]
    public static void BuildHubMenu() => BuildHub(true);

    [MenuItem("Tools/Sobek/3. 메인 씬 만들기 (Sobek_Main)", priority = 3)]
    public static void BuildMainMenu() => BuildMain(true);

    // ================= 허브 =================
    private static void BuildHub(bool askSave)
    {
        if (!Prepare(askSave, HubPath)) return;
        var log = new List<string>();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TMP_FontAsset font = FindFont();
        SobekTheme theme = SobekDataSetup.LoadTheme();

        GameObject player = BuildCommon(new Vector3(20f, 3f, 0f), font, 7f);

        // 바닥 + 벽
        var root = new GameObject("SobekHub");
        Sprite floor = SobekDataSetup.LoadArt("Sobek_Tile_Floor");
        Sprite wall = SobekDataSetup.LoadArt("Sobek_Tile_Wall");
        Sprite water = SobekDataSetup.LoadArt("Sobek_Tile_Water");
        Tiled(root.transform, "Floor", floor, new Vector2(HubW * 0.5f, HubH * 0.5f), new Vector2(HubW, HubH), new Color(0.90f, 0.82f, 0.64f), -40, false);
        Tiled(root.transform, "Wall_S", wall, new Vector2(HubW * 0.5f, -0.5f), new Vector2(HubW + 2f, 1f), Color.white, -39, true);
        Tiled(root.transform, "Wall_N", wall, new Vector2(HubW * 0.5f, HubH + 0.5f), new Vector2(HubW + 2f, 1f), Color.white, -39, true);
        Tiled(root.transform, "Wall_W", wall, new Vector2(-0.5f, HubH * 0.5f), new Vector2(1f, HubH), Color.white, -39, true);
        Tiled(root.transform, "Wall_E", wall, new Vector2(HubW + 0.5f, HubH * 0.5f), new Vector2(1f, HubH), Color.white, -39, true);

        // 서브 퍼즐 4개 (판 왼쪽 아래 모서리 위치)
        Board(root.transform, "Board_S1", SobekDataSetup.S1, new Vector2(3f, 19f), theme, font, true, log);
        Board(root.transform, "Board_S2", SobekDataSetup.S2, new Vector2(16f, 19f), theme, font, true, log);
        Board(root.transform, "Board_S3", SobekDataSetup.S3, new Vector2(29f, 19f), theme, font, true, log);
        Board(root.transform, "Board_S4", SobekDataSetup.S4, new Vector2(3f, 6f), theme, font, true, log);

        // 큰 문 (소베크의 입)
        var door = Sprite2D(root.transform, "SobekHubDoor", SobekDataSetup.LoadArt("Sobek_Obj_Door"), new Vector2(20f, 13f), new Vector2(3f, 2f), Color.white, 0, "Objects");
        var doorCol = door.AddComponent<BoxCollider2D>();
        doorCol.size = new Vector2(3f, 2f);
        door.AddComponent<SobekHubDoor>();
        Label(door.transform, "소베크의 입", new Vector2(0f, 1.6f), font);

        // 물 (메인을 깨면 빠짐) + 그 밑의 아누비스 문
        var pool = Tiled(root.transform, "FloodWater", water, new Vector2(34f, 6f), new Vector2(12f, 12f), Color.white, -30, true);
        var anubisExit = Sprite2D(root.transform, "Exit_ToAnubis", SobekDataSetup.LoadArt("Sobek_Obj_Altar"), new Vector2(36f, 4f), new Vector2(2f, 2f), new Color(0.55f, 0.45f, 0.85f), -29, "Ground");
        var exitCol = anubisExit.AddComponent<BoxCollider2D>();
        exitCol.isTrigger = true;
        exitCol.size = new Vector2(2f, 2f);
        anubisExit.AddComponent<SobekSceneExit>();
        Label(anubisExit.transform, "아누비스의 문", new Vector2(0f, 1.4f), font);

        var spawn = new GameObject("ReturnSpawn").transform;
        spawn.SetParent(root.transform, false);
        spawn.position = new Vector3(20f, 11f, 0f);

        var state = root.AddComponent<SobekHubState>();
        var so = new SerializedObject(state);
        SetArray(so, "waterRenderers", pool.GetComponent<SpriteRenderer>());
        SetArray(so, "waterBlockers", pool.GetComponent<Collider2D>());
        SetArray(so, "showWhenDrained", anubisExit);
        so.FindProperty("returnSpawn").objectReferenceValue = spawn;
        so.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = root;
        Save(scene, HubPath, log);
        Debug.Log("[SobekSceneBuilder] 허브 씬 완료\n- " + string.Join("\n- ", log));
    }

    // ================= 메인 =================
    private static void BuildMain(bool askSave)
    {
        if (!Prepare(askSave, MainPath)) return;
        var log = new List<string>();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TMP_FontAsset font = FindFont();
        SobekTheme theme = SobekDataSetup.LoadTheme();

        BuildCommon(new Vector3(1.5f, 1.5f, 0f), font, 6f);

        var root = new GameObject("SobekMain");
        SobekBoard a = Board(root.transform, "Board_MainA_Flood", SobekDataSetup.MainA, new Vector2(0f, 0f), theme, font, false, log);
        SobekBoard b = Board(root.transform, "Board_MainB_Dry", SobekDataSetup.MainB, new Vector2(30f, 0f), theme, font, false, log);
        var flow = root.AddComponent<SobekMainFlow>();
        flow.EditorSetup(a, b);

        Selection.activeGameObject = root;
        Save(scene, MainPath, log);
        Debug.Log("[SobekSceneBuilder] 메인 씬 완료\n- " + string.Join("\n- ", log));
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
