using System;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

// [추가] 진짜 중앙 홀(CentralRoom_Tilemap)에 진행을 붙이는 메뉴
// 메뉴: PharaohCube -> Hall -> 중앙 홀(타일맵) 연결하기
// - 맵(Grid, 큐브, 화로, 횃불 걸이, 신 그림)은 그대로 두고 아래만 추가 (여러 번 눌러도 중복으로 안 생김)
//   1) Player, Managers 프리팹 / 카메라 따라가기 / 어두운 전체 조명 / EventSystem / 벽 충돌
//   2) HallSystems: HallTempDirector + HallCubeInsert + HallDoorBoard
//   3) 신전 문 5개 (라 남쪽, 바스테트 서북, 토트 동북, 호루스 서남, 소베크 동남). 아누비스는 소베크를 지나 내려감
// - 문 위치가 그림과 안 맞으면 HallDoors 아래 Door_* 오브젝트를 끌어서 옮기면 됨 (메뉴를 다시 눌러도 위치 유지)
// - 빌드 목록: 임시 홀(CentralRoom_Temp) 자리에 이 씬을 넣고 임시 홀은 끔
public static class CentralRoomTilemapSetup
{
    public const string ScenePath = "Assets/Scenes/Prologue/CentralRoom_Tilemap.unity";
    private const string PlayerPrefab = "Assets/Prefabs/Player/Player.prefab";
    private const string ManagersPrefab = "Assets/Prefabs/Common/Managers.prefab";
    private const string FontPath = "Assets/Fonts/Fonts/Galmuri11 SDF.asset";
    private static readonly string[] CubeFaces = { "Ra", "Bastet", "Thoth", "Horus", "Sobek", "Anubis" };

    // 서는 곳: 큐브(0, 10) 앞
    private static readonly Vector2 ArrivePoint = new Vector2(0f, 6.6f);
    private static readonly Vector2 CubeFront = new Vector2(0f, 8.2f);

    private struct DoorSpec
    {
        public string god, scene, prompt;
        public Vector2 pos, walk;
        public DoorSpec(string god, string scene, string prompt, Vector2 pos, Vector2 walk)
        {
            this.god = god; this.scene = scene; this.prompt = prompt; this.pos = pos; this.walk = walk;
        }
    }

    private static readonly DoorSpec[] Doors =
    {
        new DoorSpec("라", "Puzzle_Test_Ra_Tilemap", "라의 신전으로", new Vector2(0f, -12.4f), new Vector2(0f, -13.4f)),
        new DoorSpec("바스테트", "Puzzle_Test_Bastet_Tliemap", "바스테트의 신전으로", new Vector2(-16.4f, 3f), new Vector2(-17.4f, 3f)),
        new DoorSpec("토트", "Puzzle_Thoth_Tilemap", "토트의 신전으로", new Vector2(16.4f, 3f), new Vector2(17.4f, 3f)),
        new DoorSpec("호루스", "Puzzle_Horus_Tilemap", "호루스의 신전으로", new Vector2(-16.4f, -5f), new Vector2(-17.4f, -5f)),
        new DoorSpec("소베크", "Sobek_Hub", "소베크의 신전으로", new Vector2(16.4f, -5f), new Vector2(17.4f, -5f))
    };

    [MenuItem("PharaohCube/Hall/중앙 홀(타일맵) 연결하기")]
    private static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var log = new StringBuilder();
        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Tilemap ground = FindTilemap("Ground");
            AddBasics(ground, log);
            HallTempDirector director = AddSystems(log);
            HallDoorBoard board = director != null ? director.GetComponent<HallDoorBoard>() : null;
            if (board != null) LinkDoors(director, board, log);
            PlayerController2D player = Object.FindFirstObjectByType<PlayerController2D>();
            CentralRoomTilemapDecor.Build(player != null ? player.transform : null, log);
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            CentralRoomTilemapBuildList.Apply(log);
        }
        catch (Exception e)
        {
            log.AppendLine($"오류: {e.Message}");
            Debug.LogError($"[CentralRoomTilemapSetup] 오류: {e}");
        }
        Debug.Log("[CentralRoomTilemapSetup]\n" + log);
        EditorUtility.DisplayDialog("중앙 홀(타일맵)", log.ToString(), "확인");
    }

    // ================= 1) 기본 =================
    private static void AddBasics(Tilemap ground, StringBuilder log)
    {
        if (Object.FindFirstObjectByType<GameManager>() == null) Spawn(ManagersPrefab, Vector2.zero, log);
        if (Object.FindFirstObjectByType<PlayerController2D>() == null)
        {
            GameObject player = Spawn(PlayerPrefab, ArrivePoint, log);
            if (player != null) PrepareTorch(player);
        }
        SetupCamera(ground, log);
        SetupLightAndEvents(log);
        SetupWallCollider(log);
    }

    private static GameObject Spawn(string path, Vector2 pos, StringBuilder log)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            log.AppendLine($"- 경고: 프리팹 없음 {path}");
            return null;
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = pos;
        log.AppendLine($"- {go.name} 추가");
        return go;
    }

    // 중앙 홀에서 횃불을 처음 얻음 (임시 홀과 같은 설정)
    private static void PrepareTorch(GameObject player)
    {
        PlayerTorchTemp torch = player.GetComponent<PlayerTorchTemp>();
        if (torch == null) torch = player.AddComponent<PlayerTorchTemp>();
        var so = new SerializedObject(torch);
        SerializedProperty start = so.FindProperty("startWithTorch");
        if (start != null) start.boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetupCamera(Tilemap ground, StringBuilder log)
    {
        Camera cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            log.AppendLine("- 경고: 카메라가 없음");
            return;
        }
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.035f, 0.05f);
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        CameraFollow2D follow = cam.GetComponent<CameraFollow2D>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow2D>();
        var so = new SerializedObject(follow);
        so.FindProperty("boundsTilemap").objectReferenceValue = ground;
        so.ApplyModifiedPropertiesWithoutUndo();
        log.AppendLine("- 카메라: 플레이어 따라가기 + 바닥 타일맵 경계");
    }

    private static void SetupLightAndEvents(StringBuilder log)
    {
        if (!HasGlobalLight())
        {
            Light2D light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.color = new Color(0.72f, 0.68f, 0.85f);
            light.intensity = 0.25f;
            log.AppendLine("- 어두운 전체 조명 추가");
        }
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>();
        }
    }

    private static bool HasGlobalLight()
    {
        foreach (Light2D l in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (l.lightType == Light2D.LightType.Global) return true;
        }
        return false;
    }

    // 벽 타일맵에 충돌이 없어서 벽을 뚫고 지나감 -> 충돌 추가
    private static void SetupWallCollider(StringBuilder log)
    {
        Tilemap wall = FindTilemap("Wall");
        if (wall == null || wall.GetComponent<TilemapCollider2D>() != null) return;
        wall.gameObject.AddComponent<TilemapCollider2D>();
        log.AppendLine("- 벽 충돌(TilemapCollider2D) 추가");
    }

    // ================= 2) 진행 =================
    private static HallTempDirector AddSystems(StringBuilder log)
    {
        HallTempDirector director = Object.FindFirstObjectByType<HallTempDirector>(FindObjectsInactive.Include);
        if (director == null) director = new GameObject("HallSystems").AddComponent<HallTempDirector>();
        HallCubeInsert insert = GetOrAdd<HallCubeInsert>(director.gameObject);
        GetOrAdd<HallDoorBoard>(director.gameObject);
        LinkCubeArt(insert, log);
        LinkProps(director, insert, log);
        return director;
    }

    private static void LinkCubeArt(HallCubeInsert insert, StringBuilder log)
    {
        var faces = new Sprite[CubeFaces.Length];
        for (int i = 0; i < faces.Length; i++)
        {
            faces[i] = LoadFirstSprite($"Assets/image/Cube/{CubeFaces[i]}.jpg");
            if (faces[i] == null) log.AppendLine($"- 경고: image/Cube/{CubeFaces[i]}.jpg 없음");
        }
        var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(FontPath);
        insert.EditorSetup(LoadFirstSprite("Assets/image/Cube/Cube.png"), faces, font);
        EditorUtility.SetDirty(insert);
    }

    private static void LinkProps(HallTempDirector director, HallCubeInsert insert, StringBuilder log)
    {
        HallFlame cube = FindFlame("Cube", 4f, new Color(1f, 0.85f, 0.5f));
        HallFlame brazier = FindFlame("Brazier", 6f, new Color(1f, 0.7f, 0.35f));
        var so = new SerializedObject(director);
        so.FindProperty("cube").objectReferenceValue = cube;
        so.FindProperty("brazier").objectReferenceValue = brazier;
        so.FindProperty("rack").objectReferenceValue = FindComp<Inspectable>("TorchRack");
        so.FindProperty("brazierInspect").objectReferenceValue = FindComp<Inspectable>("Brazier");
        so.FindProperty("cubeInsert").objectReferenceValue = insert;
        so.FindProperty("doorBoard").objectReferenceValue = director.GetComponent<HallDoorBoard>();
        so.FindProperty("arrivePoint").vector2Value = ArrivePoint;
        so.FindProperty("walkPoint").vector2Value = CubeFront;
        so.FindProperty("returnPoint").vector2Value = ArrivePoint;
        so.FindProperty("returnWalkPoint").vector2Value = CubeFront;
        so.FindProperty("objDoor").stringValue = "남쪽 라의 문으로 태양의 신전에 들어가자";
        Transform sun = FindComp<Transform>("Ra"); // 남쪽 바닥의 태양 문장 -> 라의 불이 여기서 나옴
        so.FindProperty("fireSource").objectReferenceValue = sun;
        SetLine(so, "lines.voice", 1, "???", "태양의 문장에 남은 라의 불을 가져가라.");
        SetLine(so, "lines.toDoor", 0, "나", "남쪽, 태양 문장 너머가 첫 번째 신전... 태양의 신, 라.");
        so.ApplyModifiedPropertiesWithoutUndo();
        log.AppendLine($"- 진행: 큐브 {(cube != null ? "O" : "X")}, 화로 {(brazier != null ? "O" : "X")}, 횃불 걸이 {(FindComp<Inspectable>("TorchRack") != null ? "O" : "X")}");
    }

    // 큐브, 화로의 빛이 비어 있으면 점광원을 달아 줌
    private static HallFlame FindFlame(string name, float radius, Color color)
    {
        HallFlame flame = FindComp<HallFlame>(name);
        if (flame == null) return null;
        var so = new SerializedObject(flame);
        SerializedProperty light = so.FindProperty("lightSource");
        if (light != null && light.objectReferenceValue == null)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(flame.transform, false);
            go.transform.localPosition = new Vector3(0f, 1f, 0f);
            Light2D l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = color;
            l.pointLightOuterRadius = radius;
            light.objectReferenceValue = l;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return flame;
    }

    // ================= 3) 신전 문 =================
    private static void LinkDoors(HallTempDirector director, HallDoorBoard board, StringBuilder log)
    {
        Transform root = FindOrCreate("HallDoors", null);
        var entries = new HallDoorBoard.Entry[Doors.Length];
        for (int i = 0; i < Doors.Length; i++)
        {
            SceneDoor door = MakeDoor(root, Doors[i]);
            CentralRoomTilemapDecor.AddDoorGlow(door, i);
            entries[i] = new HallDoorBoard.Entry { god = Doors[i].god, door = door };
        }
        board.EditorSetup(entries);
        EditorUtility.SetDirty(board);

        var so = new SerializedObject(director);
        so.FindProperty("door").objectReferenceValue = entries[0].door.transform; // 처음 열리는 문 = 라
        so.ApplyModifiedPropertiesWithoutUndo();
        log.AppendLine($"- 신전 문 {Doors.Length}개 (HallDoors 아래, 위치는 끌어서 조정 가능)");
    }

    private static SceneDoor MakeDoor(Transform root, DoorSpec spec)
    {
        string name = "Door_" + CubeFaces[Array.IndexOf(GodNames(), spec.god)];
        Transform t = root.Find(name);
        bool fresh = t == null;
        if (fresh)
        {
            t = new GameObject(name).transform;
            t.SetParent(root, false);
            t.position = spec.pos;
            BoxCollider2D box = t.gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.6f, 1.6f);
            box.isTrigger = true;
        }
        SceneDoor door = GetOrAdd<SceneDoor>(t.gameObject);
        var so = new SerializedObject(door);
        so.FindProperty("promptText").stringValue = spec.prompt;
        so.FindProperty("nextScene").stringValue = spec.scene;
        if (fresh) so.FindProperty("walkInto").vector2Value = spec.walk;
        so.ApplyModifiedPropertiesWithoutUndo();
        return door;
    }

    private static string[] GodNames() => HallCubeInsert.GodNames;

    // ================= 도우미 =================
    // 씬에 저장된 대사 한 줄 바꾸기 (HallTempLines 기본값을 바꿔도 이미 만든 씬에는 안 들어가서)
    private static void SetLine(SerializedObject so, string path, int index, string speaker, string text)
    {
        SerializedProperty arr = so.FindProperty(path);
        if (arr == null || !arr.isArray) return;
        if (arr.arraySize <= index) arr.arraySize = index + 1;
        SerializedProperty line = arr.GetArrayElementAtIndex(index);
        line.FindPropertyRelative("speaker").stringValue = speaker;
        line.FindPropertyRelative("text").stringValue = text;
    }

    private static Tilemap FindTilemap(string name)
    {
        foreach (Tilemap t in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (t.name == name) return t;
        }
        return null;
    }

    private static T FindComp<T>(string objectName) where T : Component
    {
        foreach (T c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.gameObject.name.Trim() == objectName) return c;
        }
        return null;
    }

    private static Transform FindOrCreate(string name, Transform parent)
    {
        GameObject found = GameObject.Find(name);
        if (found != null) return found.transform;
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static Sprite LoadFirstSprite(string path)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path)) if (o is Sprite s) return s;
        return null;
    }
}
