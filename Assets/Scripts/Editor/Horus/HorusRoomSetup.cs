using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

// 호루스 타일맵 씬 준비 단계들 (HorusRoomBuilder가 순서대로 부름)
public static class HorusRoomSetup
{
    private const string PlayerPrefab = "Assets/Prefabs/Player/Player.prefab";
    private static readonly Vector3 PlayerStart = new Vector3(12f, -7.3f, 0f); // 입구 전실 아래쪽 (작업 도면 HA0)

    private static readonly string[] PlaceholderWalls = { "Floor", "Wall_North", "Wall_South", "Wall_West", "Wall_East" };
    private static readonly string[] FloorDecals = { "Plaza", "StarPathSpine", "Path_East", "Path_West", "Path_SouthWest", "Path_SouthEast" };
    private static readonly string[] SortRoots = { "Interactables", "Puzzles", "Environment", "HorusRoom", "HorusGallery" };

    // 지우기 전에 임시 사각형/원 그림을 챙겨 둠 (기본 도형용)
    public static Sprite TakeSprite(string objName)
    {
        GameObject go = HorusArtSetup.Find(objName);
        SpriteRenderer sr = go != null ? go.GetComponent<SpriteRenderer>() : null;
        return sr != null ? sr.sprite : null;
    }

    // 임시 바닥/벽 사각형 삭제 (타일맵이 대신함). 길 표시는 남겨서 '바닥 위 빛'으로만 씀
    public static void RemovePlaceholders()
    {
        foreach (string n in PlaceholderWalls)
        {
            GameObject go = HorusArtSetup.Find(n);
            if (go != null) Object.DestroyImmediate(go);
        }
        foreach (string n in FloorDecals)
        {
            GameObject go = HorusArtSetup.Find(n);
            if (go == null) continue;
            SetHint(go, YSortHint.Mode.AlwaysBelow);
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && (n == "Plaza" || n == "StarPathSpine")) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.18f);
        }
    }

    // 플레이어를 놓고, 플레이어의 정렬 레이어를 돌려줌 (타일맵, 물건을 같은 레이어에 맞춤)
    public static string CreatePlayer(out int order)
    {
        order = 0;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        if (prefab == null)
        {
            Debug.LogWarning($"[HorusRoomSetup] 플레이어 프리팹 없음: {PlayerPrefab}");
            return "Default";
        }
        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.transform.position = PlayerStart;
        PlayerTorchTemp torch = player.GetComponent<PlayerTorchTemp>();
        if (torch == null) torch = player.AddComponent<PlayerTorchTemp>();
        var so = new SerializedObject(torch);
        SerializedProperty start = so.FindProperty("startWithTorch");
        if (start != null) start.boolValue = false; // 횃불은 라의 방에서 가져오거나 전실 횃불 제단에서 얻음
        so.ApplyModifiedPropertiesWithoutUndo();
        SpriteRenderer sr = player.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return "Default";
        order = sr.sortingOrder;
        return sr.sortingLayerName;
    }

    public static void SetupCamera(Tilemap ground)
    {
        Camera cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            Debug.LogWarning("[HorusRoomSetup] Main Camera가 없음");
            return;
        }
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.transform.position = new Vector3(PlayerStart.x, PlayerStart.y + 2f, -10f);
        CameraFollow2D follow = cam.GetComponent<CameraFollow2D>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow2D>();
        var so = new SerializedObject(follow);
        SerializedProperty bounds = so.FindProperty("boundsTilemap");
        if (bounds != null) bounds.objectReferenceValue = ground;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 밤하늘 신전 (기획서 3.3: 라의 방보다 어둡게). 별화로, 횃불, 퍼즐 빛이 돋보이게
    public static void SetupNightLight()
    {
        Light2D global = null;
        foreach (Light2D l in HorusArtSetup.All<Light2D>())
        {
            if (l.lightType == Light2D.LightType.Global) global = l;
        }
        if (global == null) global = new GameObject("Global Light 2D").AddComponent<Light2D>();
        global.lightType = Light2D.LightType.Global;
        global.color = new Color(0.55f, 0.62f, 0.9f);
        global.intensity = 0.32f;
    }

    // 물건 그림을 플레이어와 같은 정렬 레이어로 (YSortSystem은 플레이어 레이어만 앞뒤 정렬함)
    public static void FinishSorting(string layer)
    {
        foreach (string rootName in SortRoots)
        {
            GameObject root = HorusArtSetup.Find(rootName);
            if (root == null) continue;
            foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true)) sr.sortingLayerName = layer;
        }
        GameObject floorPlate = HorusArtSetup.Find("H4_StarPathLock"); // 바닥에 깔린 별판
        if (floorPlate != null) SetHint(floorPlate, YSortHint.Mode.AlwaysBelow);
    }

    // 목표 안내: 바닥 길 표시는 끔 (매 길잡이가 안내). 목표 문구 HUD는 그대로
    public static void SetupObjectiveGuide()
    {
        HorusObjectiveGuide guide = Object.FindFirstObjectByType<HorusObjectiveGuide>(FindObjectsInactive.Include);
        if (guide == null) return;
        var so = new SerializedObject(guide);
        SetColor(so, "pathDim", new Color(0.24f, 0.48f, 0.84f, 0f));
        SetColor(so, "pathLit", new Color(0.24f, 0.48f, 0.84f, 0f)); // 반짝이는 길 대신 매 길잡이 + 바닥 금 별 행렬길이 안내
        SetColor(so, "targetLocked", new Color(0.55f, 0.5f, 0.6f, 1f)); // 그림이 들어가서 예전처럼 거의 검게 하지 않음
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 퍼즐 완료 플래그가 씬에서 비어 있으면 채움 (H1이 비어 있어서 목표 안내/매/노트 3이 멈췄었음)
    public static void FixPuzzleFlags()
    {
        SetClearFlag<MirrorWingPuzzle>("F_HO_H1_DONE");
        SetClearFlag<WedjatRestorePuzzle>("F_HO_H3_DONE");
        SetClearFlag<StarPathLockPuzzle>("F_HO_H4_DONE");
        SetClearFlag<SkyAlignPuzzle>("F_HO_H5_DONE");
    }

    private static void SetClearFlag<T>(string flag) where T : PuzzleBase
    {
        T puzzle = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (puzzle == null) return;
        var so = new SerializedObject(puzzle);
        SerializedProperty p = so.FindProperty("clearFlag");
        if (p == null || !string.IsNullOrEmpty(p.stringValue)) return;
        p.stringValue = flag;
        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[HorusRoomSetup] {typeof(T).Name} 완료 플래그가 비어 있어서 {flag}로 채움");
    }

    public static void AddToBuildSettings(string path)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene s in scenes)
        {
            if (s.path != path) continue;
            s.enabled = true;
            EditorBuildSettings.scenes = scenes.ToArray();
            return;
        }
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ================= 도우미 =================
    private static void SetHint(GameObject go, YSortHint.Mode mode)
    {
        YSortHint hint = go.GetComponent<YSortHint>();
        if (hint == null) hint = go.AddComponent<YSortHint>();
        hint.mode = mode;
        EditorUtility.SetDirty(hint);
    }

    private static void SetColor(SerializedObject so, string field, Color c)
    {
        SerializedProperty p = HorusArtSetup.Prop(so, field);
        if (p != null) p.colorValue = c;
    }
}
