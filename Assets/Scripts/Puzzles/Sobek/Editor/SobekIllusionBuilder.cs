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

// 메뉴: Tools/Sobek/5. 착시 퍼즐 씬 만들기 (서브 S1~S4 + 메인)
// 퍼즐 데이터 7개(SobekIllusion_S1~S4, M1~M3) + 머티리얼 + 씬 5개를 처음부터 만들고 Build Profiles 씬 목록에 추가
// - 서브: 씬 하나에 판 하나 (풀면 부적 -> 허브의 그 섬 게이트 앞으로)
// - 메인: 씬 하나에 구역 3개, 지금 구역만 보이게 (풀면 형판 공개·큐브 -> 허브에서 물 빠짐)
// 퍼즐 배치는 SobekIllusionDesigns (설계 도구 + BFS 검증 값)
public static class SobekIllusionBuilder
{
    private const string Folder = SobekDataSetup.DataRoot + "/Illusion";
    private const string BlockMatPath = Folder + "/Mat_SobekIllusion_Block.mat";
    private const string MarkerMatPath = Folder + "/Mat_SobekIllusion_Marker.mat";
    private const string SceneFolder = "Assets/Scenes/Puzzle_Test";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";


    private class Def
    {
        public string key, title, intro, design, clearText;
        public float camSize = 9f;
    }

    private static readonly Def[] Subs =
    {
        new Def { key = "S1", title = "소용돌이 섬", design = SobekIllusionDesigns.S1,
            intro = "청록 돌을 밟으면 섬의 팔이 돈다. 신전을 돌려 끊어진 길이 맞닿는 자리를 찾자.",
            clearText = "소용돌이가 잦아들고, 섬의 팔이 제단을 가리킨다." },
        new Def { key = "S2", title = "악어 등 다리", design = SobekIllusionDesigns.S2,
            intro = "주황 돌을 밟을 때마다 악어 등 다리가 한 걸음씩 헤엄친다.",
            clearText = "악어가 등을 내어 주었다." },
        new Def { key = "S3", title = "범람하는 나일", design = SobekIllusionDesigns.S3,
            intro = "파랑 돌이 물높이를 바꾼다. 물이 오르면 뗏목도 오르고, 낮은 길은 잠긴다.",
            clearText = "나일의 물이 제자리를 찾았다." },
        new Def { key = "S4", title = "두 개의 수면", design = SobekIllusionDesigns.S4,
            intro = "장치는 없다. 오직 보는 자리만이 길을 잇는다.",
            clearText = "물 위의 길과 물에 비친 길이 하나가 되었다." },
    };

    private static readonly Def[] Mains =
    {
        new Def { key = "M1", title = "범람기", design = SobekIllusionDesigns.M1, camSize = 9.5f,
            clearText = "범람의 물길이 열렸다." },
        new Def { key = "M2", title = "건기의 미궁", design = SobekIllusionDesigns.M2, camSize = 9.5f,
            clearText = "미궁의 다리가 이어졌다." },
        new Def { key = "M3", title = "소베크-라의 탑", design = SobekIllusionDesigns.M3, camSize = 9.5f,
            clearText = "소베크-라의 제단에 닿았다!" },
    };

    private static readonly string[][] MainLines =
    {
        new[] { "소베크의 입 안쪽, 범람기의 신전. 물과 섬이 함께 움직인다.", "네 섬에서 배운 것을 모두 떠올리자." },
        new[] { "물이 빠진 아래층, 건기의 미궁. 악어 등 다리와 소용돌이 섬이 길을 막는다." },
        new[] { "소베크-라의 탑. 섬, 악어, 물, 그리고 보는 자리… 모두 맞아야 제단에 닿는다." },
    };

    [MenuItem("Tools/Sobek/5. 착시 퍼즐 씬 만들기 (서브 S1~S4 + 메인)", priority = 5)]
    public static void BuildMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!EditorUtility.DisplayDialog("착시 퍼즐", "Sobek_S1~S4, Sobek_Main 씬을 새로 만듭니다.\n그 씬들에서 직접 고친 내용은 사라집니다. 계속할까요?", "만들기", "취소"))
            return;
        BuildAll();
    }

    // 전체 한 번에(0번 메뉴)에서도 부름 (물어보지 않음)
    public static void BuildAll()
    {
        var log = new List<string>();
        SobekDataSetup.EnsureFolder(Folder);
        SobekDataSetup.EnsureFolder(SceneFolder);
        Material block = EnsureMaterial(BlockMatPath, false, log);
        Material marker = EnsureMaterial(MarkerMatPath, true, log);
        if (block == null || marker == null) return;

        string[] scenes = { SobekIds.SubScene1, SobekIds.SubScene2, SobekIds.SubScene3, SobekIds.SubScene4 };
        string[] rewards = { SobekIds.AmuletRotate, SobekIds.AmuletLift, SobekIds.AmuletWater, SobekIds.AmuletMirror };
        string[] flags = { SobekIds.FlagS1Clear, SobekIds.FlagS2Clear, SobekIds.FlagS3Clear, SobekIds.FlagS4Clear };
        for (int i = 0; i < Subs.Length; i++)
        {
            SobekIllusionLevel lv = EnsureLevel(Subs[i], log);
            BuildSubScene(scenes[i], Subs[i], lv, rewards[i], flags[i], block, marker, log);
        }

        var levels = new List<SobekIllusionLevel>();
        foreach (Def d in Mains) levels.Add(EnsureLevel(d, log));
        AssetDatabase.SaveAssets();
        BuildMainScene(levels, block, marker, log);
        Debug.Log("[SobekIllusionBuilder] 완료\n- " + string.Join("\n- ", log));
    }

    // ================= 데이터 =================
    private static SobekIllusionLevel EnsureLevel(Def d, List<string> log)
    {
        string path = $"{Folder}/SobekIllusion_{d.key}.asset";
        var level = AssetDatabase.LoadAssetAtPath<SobekIllusionLevel>(path);
        bool created = level == null;
        if (created) level = ScriptableObject.CreateInstance<SobekIllusionLevel>();
        level.EditorParse(d.title, d.intro ?? "", d.design, d.camSize);
        if (created) AssetDatabase.CreateAsset(level, path);
        else EditorUtility.SetDirty(level);

        SobekIllusionModel m = level.BuildModel();
        var sol = m.Solve(level.StartYaw, out int states);
        int ill = 0, plates = 0;
        if (sol != null) foreach (var s in sol) { if (s.action.StartsWith("illusion")) ill++; if (s.action.EndsWith("+plate")) plates++; }
        log.Add($"{path}: {(sol == null ? "<color=red>정답 없음!</color>" : $"정답 {sol.Count - 1}수 (착시 길 {ill} · 발판 {plates}, 상태 {states}개)")}");
        return level;
    }

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
        return mat;
    }

    // ================= 씬 =================
    private static void BuildSubScene(string sceneName, Def d, SobekIllusionLevel level, string reward, string flag, Material block, Material marker, List<string> log)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TMP_FontAsset font = SobekSceneBuilder.FindFontPublic();
        SobekIllusionCamera rig = Common(font, level);

        var root = new GameObject("SobekPuzzle");
        SobekIllusionBoard board = Board(root.transform, "Board_" + d.key, level, rig, block, marker, font, "", d.clearText);
        var flow = root.AddComponent<SobekIllusionFlow>();
        flow.EditorSetup(new[] { board }, new[] { new[] { level.Intro } }, $"F_SOBEK_{d.key}_STAGE_", reward, flag, false, null, d.key);

        Save(scene, sceneName, log);
    }

    private static void BuildMainScene(List<SobekIllusionLevel> levels, Material block, Material marker, List<string> log)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TMP_FontAsset font = SobekSceneBuilder.FindFontPublic();
        SobekIllusionCamera rig = Common(font, levels[0]);

        var root = new GameObject("SobekPuzzle");
        var boards = new SobekIllusionBoard[levels.Count];
        for (int i = 0; i < levels.Count; i++)
        {
            boards[i] = Board(root.transform, $"Stage{i + 1}_{Mains[i].key}", levels[i], rig, block, marker, font, $"구역 {i + 1}/{levels.Count} · ", Mains[i].clearText);
            boards[i].gameObject.SetActive(i == 0); // 지금 구역만 보이게 (실행 중에는 SobekIllusionFlow가 켜고 끔)
        }
        BastetEnding ending = SobekSceneBuilder.AddSobekEnding(root.transform, font, log);
        var flow = root.AddComponent<SobekIllusionFlow>();
        flow.EditorSetup(boards, MainLines, "F_SOBEK_MAIN_STAGE_", "", "", true, ending, "");
        log.Add("메인: 구역 3개 (범람기 = 섬+물 / 건기의 미궁 = 악어 다리+섬 / 소베크-라의 탑 = 섬+악어 다리+물), 지금 구역만 보임");

        Save(scene, SobekIds.MainScene, log);
    }

    // 매니저 · 조명 · 대사 · 카메라 (착시 씬 공통)
    private static SobekIllusionCamera Common(TMP_FontAsset font, SobekIllusionLevel first)
    {
        new GameObject("GameManager_Sobek").AddComponent<GameManager>();
        var managers = new GameObject("Managers");
        managers.AddComponent<ItemInventory>();
        var toast = managers.AddComponent<AnubisToast>();
        SetObj(toast, "font", font);
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
        SobekSceneBuilder.AddStoryPublic();

        // 2D 조명 (캐릭터·제단 스프라이트가 Sprite-Lit이라 전역 조명 필요. 블록은 조명 안 받음)
        var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;

        var rigGo = new GameObject("IllusionCameraRig");
        rigGo.transform.position = first.Center();
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(rigGo.transform, false);
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = first.BackgroundColor;
        camGo.AddComponent<AudioListener>();
        var rig = rigGo.AddComponent<SobekIllusionCamera>();
        rig.EditorSetup(cam, first.CameraSize);
        rig.SetYawImmediate(first.StartYaw);
        return rig;
    }

    private static SobekIllusionBoard Board(Transform parent, string objName, SobekIllusionLevel level, SobekIllusionCamera rig, Material block, Material marker, TMP_FontAsset font, string prefix, string clearText)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        var board = go.AddComponent<SobekIllusionBoard>();
        SpriteRenderer avatar = MakeSprite(go.transform, "Avatar", PlayerSprite(), 20);
        SpriteRenderer goal = MakeSprite(go.transform, "GoalAltar", SobekDataSetup.World("Sobek_Obj_Altar"), 10);
        board.EditorSetup(level, rig, block, marker, avatar, goal, font, prefix, clearText);
        return board;
    }

    private static void Save(Scene scene, string sceneName, List<string> log)
    {
        string path = $"{SceneFolder}/{sceneName}.unity";
        EditorSceneManager.SaveScene(scene, path);
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == path))
        {
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            log.Add($"Build Profiles 씬 목록에 추가: {path}");
        }
        log.Add($"씬 저장: {path}");
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
