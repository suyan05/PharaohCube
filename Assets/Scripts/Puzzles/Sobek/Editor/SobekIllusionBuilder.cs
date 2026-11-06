#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// 메뉴: Tools/Sobek/5. 착시 퍼즐 1 만들기 (Sobek_Illusion1)
// 데이터(SobekIllusion_1) + 머티리얼 2개 + 테스트 씬을 처음부터 만들고 Build Profiles 씬 목록에 추가
// 퍼즐 배치는 파이썬 검증 도구(BFS)로 확인한 값: 착시 길 5번 + 각도 4개 모두 써야 풀림
public static class SobekIllusionBuilder
{
    private const string Folder = SobekDataSetup.DataRoot + "/Illusion";
    private const string LevelPath = Folder + "/SobekIllusion_1.asset";
    private const string BlockMatPath = Folder + "/Mat_SobekIllusion_Block.mat";
    private const string MarkerMatPath = Folder + "/Mat_SobekIllusion_Marker.mat";
    private const string ScenePath = "Assets/Scenes/Puzzle_Test/" + SobekIds.IllusionScene1 + ".unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";

    [MenuItem("Tools/Sobek/5. 착시 퍼즐 1 만들기 (Sobek_Illusion1)", priority = 5)]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (System.IO.File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("착시 퍼즐", $"{ScenePath} 가 이미 있습니다.\n새로 만들면 그 씬에서 고친 내용이 사라집니다. 덮어쓸까요?", "덮어쓰기", "취소"))
            return;

        var log = new List<string>();
        SobekDataSetup.EnsureFolder(Folder);
        SobekDataSetup.EnsureFolder("Assets/Scenes/Puzzle_Test");

        SobekIllusionLevel level = EnsureLevel(log);
        Material block = EnsureMaterial(BlockMatPath, false, log);
        Material marker = EnsureMaterial(MarkerMatPath, true, log);
        if (block == null || marker == null) return;
        AssetDatabase.SaveAssets();

        BuildScene(level, block, marker, log);
        Debug.Log("[SobekIllusionBuilder] 완료\n- " + string.Join("\n- ", log));
    }

    // ================= 데이터 =================
    // 끊어진 수로: 물가 부두(시작, 높이 0)에서 소베크의 제단(높이 5)까지
    // 정답 (15수): 135° -> 45° -> 135° -> 225° -> 315° -> 225° 순서로 착시 길 5번
    private static SobekIllusionLevel EnsureLevel(List<string> log)
    {
        var level = AssetDatabase.LoadAssetAtPath<SobekIllusionLevel>(LevelPath);
        bool created = level == null;
        if (created) level = ScriptableObject.CreateInstance<SobekIllusionLevel>();

        var segs = new List<SobekIllusionLevel.Segment>
        {
            Seg(4, 0, -1, true, 2),   // 시작 부두
            Seg(2, 2, 2, false, 3),   // 낮은 수로
            Seg(0, 2, -2, true, 2),   // 갈림 수로
            Seg(0, 3, -4, true, 2),   // 뒤쪽 계단참
            Seg(-4, 4, 1, true, 4),   // 높은 수로
            Seg(4, 5, -3, false, 4),  // 제단 길 (끝 = 제단)
        };
        level.EditorSet("착시의 수로", "신전을 돌려 보자. 끊어진 길도 보는 자리에 따라 이어진다.",
            segs, new Vector3Int(4, 0, -1), new Vector3Int(4, 5, 0), 2, 8.5f);
        if (created) AssetDatabase.CreateAsset(level, LevelPath);
        else EditorUtility.SetDirty(level);

        var sol = level.BuildGraph().Solve(level.Start, level.Goal, level.StartYaw, out int states);
        log.Add($"{LevelPath} -> 정답 {(sol == null ? "없음!" : (sol.Count - 1) + "수")} (상태 {states}개)");
        return level;
    }

    private static SobekIllusionLevel.Segment Seg(int x, int y, int z, bool alongX, int len) =>
        new SobekIllusionLevel.Segment { start = new Vector3Int(x, y, z), alongX = alongX, length = len, pillar = true };

    private static Material EnsureMaterial(string path, bool transparent, List<string> log)
    {
        Shader sh = Shader.Find("Sobek/IllusionUnlit");
        if (sh == null)
        {
            Debug.LogError("[SobekIllusionBuilder] Sobek/IllusionUnlit 셰이더가 없음 (SobekIllusionUnlit.shader 확인)");
            return null;
        }
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(sh);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = sh;
        mat.SetColor("_Color", Color.white);
        mat.SetFloat("_SrcBlend", transparent ? (float)UnityEngine.Rendering.BlendMode.SrcAlpha : (float)UnityEngine.Rendering.BlendMode.One);
        mat.SetFloat("_DstBlend", transparent ? (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : (float)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetFloat("_ZWrite", transparent ? 0f : 1f);
        mat.renderQueue = transparent ? 3000 : 2000;
        EditorUtility.SetDirty(mat);
        log.Add($"머티리얼: {path}");
        return mat;
    }

    // ================= 씬 =================
    private static void BuildScene(SobekIllusionLevel level, Material block, Material marker, List<string> log)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TMP_FontAsset font = FindFont();

        // 매니저 (다른 소베크 씬과 같은 구성)
        new GameObject("GameManager_Sobek").AddComponent<GameManager>();
        var managers = new GameObject("Managers");
        managers.AddComponent<ItemInventory>();
        var toast = managers.AddComponent<AnubisToast>();
        SetObj(toast, "font", font);
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        // 2D 조명 (캐릭터·제단 스프라이트가 Sprite-Lit이라 전역 조명 필요. 블록은 조명 안 받음)
        var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;

        // 카메라 (회전 중심 + 자식 카메라)
        var rigGo = new GameObject("IllusionCameraRig");
        rigGo.transform.position = level.Center();
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(rigGo.transform, false);
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = level.BackgroundColor;
        camGo.AddComponent<AudioListener>();
        var rig = rigGo.AddComponent<SobekIllusionCamera>();
        rig.EditorSetup(cam, level.CameraSize);
        rig.SetYawImmediate(level.StartYaw);

        // 퍼즐
        var boardGo = new GameObject("SobekIllusion");
        var board = boardGo.AddComponent<SobekIllusionBoard>();

        SpriteRenderer avatar = MakeSprite(boardGo.transform, "Avatar", PlayerSprite(), 20);
        SpriteRenderer goal = MakeSprite(boardGo.transform, "GoalAltar", SobekDataSetup.World("Sobek_Obj_Altar"), 10);
        board.EditorSetup(level, rig, block, marker, avatar, goal, font);

        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == ScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            log.Add($"Build Profiles 씬 목록에 추가: {ScenePath}");
        }
        log.Add($"씬 저장: {ScenePath} (Play 하면 바로 테스트)");
    }

    private static SpriteRenderer MakeSprite(Transform parent, string name, Sprite sprite, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        if (sprite == null) Debug.LogWarning($"[SobekIllusionBuilder] {name} 그림 없음 -> Inspector에서 Sprite를 넣어 주세요");
        return sr;
    }

    // 플레이어 프리팹의 그림을 빌려 씀 (없으면 비워 둠)
    private static Sprite PlayerSprite()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null) return null;
        var sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
        return sr != null ? sr.sprite : null;
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
}
#endif
