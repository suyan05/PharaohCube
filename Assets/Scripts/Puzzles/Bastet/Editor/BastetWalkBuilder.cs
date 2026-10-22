#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 메뉴: Tools/Bastet/걷는 퍼즐/...
// 걸어서 푸는 B1(발자국), B2(고양이 눈 석상), B5(해와 달) 임시 오브젝트를 Scene 뷰 가운데에 만들고 연결
// 그림은 나중에 SpriteRenderer의 Sprite만 바꾸면 됨 (발자국: Plate / Glow, 석상: Body / Eyes)
public static class BastetWalkBuilder
{
    private const string ObjectLayer = "Objects";
    private const string DataPath = "Assets/Scripts/Puzzles/Bastet/Data/BastetSunMoonData.asset";

    // B1 발자국 7개 (석상 기준 위치, 그림 예시처럼 흩어진 배치, 간격 2칸 이상)
    private static readonly Vector2[] FootprintOffsets =
    {
        new Vector2(-3.8f, 0.4f), new Vector2(-1.7f, 1.4f), new Vector2(0.6f, 1.4f), new Vector2(3.2f, 0.6f),
        new Vector2(-2.6f, -1.6f), new Vector2(0.9f, -1.0f), new Vector2(3.0f, -1.8f)
    };

    [MenuItem("Tools/Bastet/걷는 퍼즐/B1 발자국 만들기")]
    public static void BuildB1() => BuildB1At(Pivot());

    // [추가] 위치를 받아 만들기 (5개 방 배치 메뉴에서 사용)
    public static GameObject BuildB1At(Vector3 c)
    {
        GameObject root = NewRoot("Walk_B1_Footprint", c);
        var puzzle = root.AddComponent<BastetFootprintWalk>();

        GameObject starter = MakeStatue("PawStatue", root.transform, new Vector3(0f, 3.2f, 0f), new Vector2(1.6f, 2.2f),
            new Color(0.62f, 0.38f, 0.7f), out _, out _, out Transform paw, true);

        var plates = new BastetFootprintPlate[FootprintOffsets.Length];
        for (int i = 0; i < plates.Length; i++)
            plates[i] = MakePlate($"Footprint_{i}", root.transform, FootprintOffsets[i]);

        puzzle.EditorSetPlates(plates);
        LinkStarter(starter, puzzle, paw);
        Finish(root, "B1 발자국: 석상(PawStatue) 1 + 발자국 7. 정답 순서는 BastetFootprintWalk의 Sequence (Gizmo 선으로 확인)");
        return root;
    }

    [MenuItem("Tools/Bastet/걷는 퍼즐/B2 고양이 눈 석상 만들기")]
    public static void BuildB2() => BuildB2At(Pivot());

    // [추가] 위치를 받아 만들기 (5개 방 배치 메뉴에서 사용)
    public static GameObject BuildB2At(Vector3 c)
    {
        GameObject root = NewRoot("Walk_B2_EyeStatues", c);
        var puzzle = root.AddComponent<BastetEyeStatueRing>();

        GameObject starter = MakeStatue("BigCatStatue", root.transform, new Vector3(0f, 3.2f, 0f), new Vector2(1.6f, 2.2f),
            new Color(0.62f, 0.38f, 0.7f), out _, out _, out _, false);

        // 작은 석상 6개: 아래로 휜 반원, 간격 2.5칸
        var statues = new BastetEyeStatue[6];
        for (int i = 0; i < statues.Length; i++)
        {
            float x = -6.25f + i * 2.5f;
            float y = -0.4f - x * x * 0.035f;
            GameObject s = MakeStatue($"SmallCat_{i}", root.transform, new Vector3(x, y, 0f), new Vector2(0.9f, 1.3f),
                new Color(0.5f, 0.32f, 0.55f), out SpriteRenderer eyes, out Light2D eyeLight, out _, false);
            statues[i] = s.AddComponent<BastetEyeStatue>();
            statues[i].EditorSetup(eyes, eyeLight, s.transform);
        }

        puzzle.EditorSetStatues(statues);
        LinkStarter(starter, puzzle, null);
        Finish(root, "B2 고양이 눈: 큰 석상 1 + 작은 석상 6 (간격 2.5칸). 순서는 BastetEyeStatueRing의 Forward Steps");
        return root;
    }

    [MenuItem("Tools/Bastet/걷는 퍼즐/B5 해와 달의 기억 (메인) 만들기")]
    public static void BuildB5() => BuildB5At(Pivot());

    // [추가] 위치를 받아 만들기 (5개 방 배치 메뉴에서 사용)
    public static GameObject BuildB5At(Vector3 c)
    {
        GameObject root = NewRoot("Walk_B5_SunMoon", c);
        var puzzle = root.AddComponent<BastetSunMoonWalk>();

        GameObject starter = MakeStatue("BastetShrine", root.transform, new Vector3(0f, 5.2f, 0f), new Vector2(2f, 2.8f),
            new Color(0.85f, 0.65f, 0.3f), out _, out _, out _, false);

        // 석상 6개 원형 (위쪽은 신상 자리라 비움), 발자국은 석상 안쪽(가운데 쪽)
        float[] angles = { 150f, 195f, 240f, 300f, 345f, 30f };
        var plates = new BastetFootprintPlate[6];
        var statues = new BastetEyeStatue[6];
        for (int i = 0; i < 6; i++)
        {
            float a = angles[i] * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            GameObject s = MakeStatue($"RingCat_{i}", root.transform, dir * 4.6f, new Vector2(0.9f, 1.3f),
                new Color(0.5f, 0.32f, 0.55f), out SpriteRenderer eyes, out Light2D eyeLight, out _, false);
            statues[i] = s.AddComponent<BastetEyeStatue>();
            statues[i].EditorSetup(eyes, eyeLight, s.transform);
            plates[i] = MakePlate($"RingStep_{i}", root.transform, dir * 3.1f);
        }

        GameObject door = FindInScene("Door_North_Exit");
        puzzle.EditorSetup(FindOrCreateData(), plates, statues, door);
        LinkStarter(starter, puzzle, null);
        Finish(root, "B5 해와 달: 신상 1 + 석상 6 + 발자국 6" + (door != null ? ", Door_North_Exit 연결" : ", 출구 문(Door_North_Exit) 없음"));
        return root;
    }

    [MenuItem("Tools/Bastet/걷는 퍼즐/플레이어에 임시 횃불 붙이기")]
    public static void AddTorchToPlayer()
    {
        var player = Object.FindFirstObjectByType<PlayerController2D>(FindObjectsInactive.Include);
        if (player == null) { EditorUtility.DisplayDialog("임시 횃불", "씬에 PlayerController2D(플레이어)가 없습니다.", "확인"); return; }
        if (player.GetComponent<PlayerTorchTemp>() == null) Undo.AddComponent<PlayerTorchTemp>(player.gameObject);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        Selection.activeGameObject = player.gameObject;
        Debug.Log("[BastetWalkBuilder] 플레이어에 PlayerTorchTemp 붙임. 방이 밝으면 효과가 안 보이니 Global Light 2D 밝기를 0.3~0.4로");
    }

    [MenuItem("Tools/Bastet/걷는 퍼즐/테스트용 화로 만들기")]
    public static void AddBrazier()
    {
        GameObject go = MakeBrazierAt("TestBrazier", null, Pivot());
        Finish(go, "테스트용 화로 (E로 횃불 다시 붙이기). 실제 게임에서는 중앙 홀 화로가 이 역할");
    }

    // [추가] 화로 (E로 횃불 다시 붙이기)
    internal static GameObject MakeBrazierAt(string name, Transform parent, Vector3 worldPos)
    {
        GameObject go = MakeBox(name, parent, Vector3.zero, new Vector2(0.8f, 0.8f), new Color(0.9f, 0.45f, 0.15f), 0);
        go.transform.position = worldPos;
        go.AddComponent<BoxCollider2D>();
        go.AddComponent<TorchRelightPoint>();
        Undo.RegisterCreatedObjectUndo(go, "화로");
        return go;
    }

    // ================= 만들기 도우미 =================
    private static GameObject NewRoot(string name, Vector3 pos)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, name);
        go.transform.position = pos;
        return go;
    }

    // 밟는 발자국: 바닥 판 + 빛(Glow, Light2D) + Trigger
    private static BastetFootprintPlate MakePlate(string name, Transform parent, Vector2 local)
    {
        Sprite knob = Knob();
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = knob;
        sr.color = new Color(0.28f, 0.22f, 0.32f);
        sr.sortingLayerName = ObjectLayer;
        sr.sortingOrder = -6; // 플레이어보다 아래 (바닥)
        go.transform.localScale = new Vector3(0.9f, 0.6f, 1f);

        var glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(go.transform, false);
        var glow = glowGo.AddComponent<SpriteRenderer>();
        glow.sprite = knob;
        glow.sortingLayerName = ObjectLayer;
        glow.sortingOrder = -5;
        glow.enabled = false;
        Light2D light = AddPointLight(glowGo, 1.4f);

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;

        var plate = go.AddComponent<BastetFootprintPlate>();
        plate.EditorSetup(glow, light);
        return plate;
    }

    // 석상: 몸통(네모) + 눈(빛) + (선택) 앞발
    private static GameObject MakeStatue(string name, Transform parent, Vector3 local, Vector2 size, Color color,
        out SpriteRenderer eyes, out Light2D eyeLight, out Transform paw, bool withPaw)
    {
        GameObject go = MakeBox(name, parent, Vector3.zero, size, color, 0);
        go.transform.localPosition = local;
        var col = go.AddComponent<BoxCollider2D>(); // 단단한 석상 (지나갈 수 없음, E 범위 감지)
        col.size = Vector2.one;

        var eyeGo = new GameObject("Eyes");
        eyeGo.transform.SetParent(go.transform, false);
        eyeGo.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        eyeGo.transform.localScale = new Vector3(0.6f, 0.15f, 1f);
        eyes = eyeGo.AddComponent<SpriteRenderer>();
        eyes.sprite = Knob();
        eyes.sortingLayerName = ObjectLayer;
        eyes.sortingOrder = 6;
        eyes.enabled = false;
        eyeLight = AddPointLight(eyeGo, 1.6f);

        paw = null;
        if (withPaw)
        {
            GameObject p = MakeBox("Paw", go.transform, new Vector3(0.45f, -0.35f, 0f), new Vector2(0.35f, 0.18f),
                color * 0.8f, 7);
            paw = p.transform;
        }
        return go;
    }

    internal static GameObject MakeBox(string name, Transform parent, Vector3 local, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color = color;
        sr.sortingLayerName = ObjectLayer;
        sr.sortingOrder = order;
        return go;
    }

    internal static Light2D AddPointLight(GameObject go, float radius)
    {
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.pointLightOuterRadius = radius;
        l.pointLightInnerRadius = radius * 0.3f;
        l.intensity = 1.2f;
        l.enabled = false; // 퍼즐이 켬
        return l;
    }

    private static void LinkStarter(GameObject starterGo, BastetWalkPuzzleBase puzzle, Transform paw)
    {
        var starter = starterGo.AddComponent<BastetPuzzleStarter>();
        starter.EditorSetup(puzzle, paw);
    }

    private static void Finish(GameObject root, string msg)
    {
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log($"[BastetWalkBuilder] {msg}\n- Scene 뷰 가운데에 만들었음. 루트를 옮겨 방에 배치 후 Ctrl+S", root);
    }

    internal static Vector3 Pivot()
    {
        Vector3 p = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        p.z = 0f;
        return p;
    }

    internal static Sprite Knob() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

    private static BastetSunMoonData FindOrCreateData()
    {
        string[] guids = AssetDatabase.FindAssets("t:BastetSunMoonData");
        if (guids.Length > 0) return AssetDatabase.LoadAssetAtPath<BastetSunMoonData>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (!AssetDatabase.IsValidFolder("Assets/Scripts/Puzzles/Bastet/Data")) AssetDatabase.CreateFolder("Assets/Scripts/Puzzles/Bastet", "Data");
        var d = ScriptableObject.CreateInstance<BastetSunMoonData>();
        AssetDatabase.CreateAsset(d, DataPath);
        AssetDatabase.SaveAssets();
        return d;
    }

    internal static GameObject FindInScene(string name)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name && t.gameObject.scene.IsValid()) return t.gameObject;
        return null;
    }
}
#endif
