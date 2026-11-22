#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// [추가] 메뉴: Tools/Sobek/7. 허브 보강 (벽화, 벽, 카메라, 순서 잠금, 숨은 통로)
// 지금 Sobek_Hub 씬을 새로 만들지 않고 그 위에 덧붙임 (직접 옮긴 것·고친 것은 그대로, 여러 번 실행해도 됨)
//  1) 전실 위쪽 벽(Wall_Ante_L/R)을 2칸 높이로 (아래로 1칸 늘림)
//  2) 그 벽에 소베크 벽화 4장 (Mural_Sobek_1~4, 물의 주인 / 태초의 알 / 강을 삼킨 뱀 / 소베크의 입)
//  3) 카메라 영역: 본당(CameraZone_Hub) + 숨은 방(CameraZone_Lower) -> 본당에서 숨은 방이 보이지 않음
//  4) 순서 잠금: S2~S4 다리 안쪽 끝에 통나무(Lock_S2~S4) + 섬을 덮는 어둠(Fog_S2~S4)
//  5) 수위계 옆 계단(StairsDown): 처음 내려갈 때 통로 "페트수코스의 길"을 걸은 뒤 숨은 방 도착
//  6) 허브 첫 대사에 벽화·순서 안내 추가 (예전 기본 대사일 때만)
//  [추가] 숨은 방 도착 지점(Lower_Spawn)이 방 밖이면 방 안으로 옮김
public static class SobekHubUpgrade
{
    private const string WorldFolder = "Assets/image/Sobek/World";
    private const string StoryFolder = "Assets/image/Sobek/Story";
    private const string BubblePath = "Assets/image/ui/UI_Prompt_New.png";
    private const float HubCenterX = 23f;

    // 벽화 자리 (전실 위쪽 벽, 왼쪽 벽 x 0~21 / 오른쪽 벽 x 25~46, 기둥 x 18·28은 피함)
    private static readonly float[] MuralX = { 6f, 13.5f, 32.5f, 40f };
    private static readonly int[] MuralNotes = { 40, 41, 42, 43 };

    private struct LockInfo { public string id, island, item, flag, key; }
    private static readonly LockInfo[] Locks =
    {
        new LockInfo { id = "S2", island = "악어 등 다리", item = SobekIds.AmuletRotate, flag = SobekIds.FlagS1Clear, key = "소용돌이 섬의 부적" },
        new LockInfo { id = "S3", island = "범람하는 나일", item = SobekIds.AmuletLift, flag = SobekIds.FlagS2Clear, key = "악어 등 다리의 부적" },
        new LockInfo { id = "S4", island = "두 개의 수면", item = SobekIds.AmuletWater, flag = SobekIds.FlagS3Clear, key = "범람하는 나일의 부적" },
    };

    [MenuItem("Tools/Sobek/7. 허브 보강 (벽화, 벽, 카메라, 순서 잠금, 숨은 통로)", priority = 7)]
    private static void Menu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != SobekIds.HubScene)
        {
            string path = FindScene(SobekIds.HubScene);
            if (path == null) { EditorUtility.DisplayDialog("허브 보강", $"{SobekIds.HubScene} 씬을 찾지 못했습니다.", "확인"); return; }
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        var log = new StringBuilder("[SobekHubUpgrade] 허브 보강\n");
        GameObject rootGo = GameObject.Find("SobekHub");
        if (rootGo == null) { EditorUtility.DisplayDialog("허브 보강", "SobekHub 오브젝트가 없습니다. (Tools/Sobek/2 로 만든 허브가 필요)", "확인"); return; }
        Transform root = rootGo.transform;

        RaiseAnteWall(log);
        MakeMurals(root, log);
        MakeCameraZones(root, log);
        MakeLocks(root, log);
        SetupStairs(log);
        FixLowerSpawn(log);
        UpdateIntro(log);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("허브 보강", log.ToString().Replace("[SobekHubUpgrade] ", "") + "\n씬을 저장했습니다. Play 해서 확인해 주세요.", "확인");
    }

    // ================= 1) 전실 벽 2칸 =================
    private static void RaiseAnteWall(StringBuilder log)
    {
        foreach (string n in new[] { "Wall_Ante_L", "Wall_Ante_R" })
        {
            GameObject go = Find(n);
            if (go == null) { log.AppendLine($"- {n} 없음 (건너뜀)"); continue; }
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            Undo.RecordObject(sr, "벽 높이");
            Undo.RecordObject(go.transform, "벽 높이");
            Vector2 size = sr.drawMode != SpriteDrawMode.Simple ? sr.size : (Vector2)sr.bounds.size;
            float top = go.transform.position.y + size.y * 0.5f;          // 윗선(본당 쪽)은 그대로
            const float h = 2f;
            if (sr.drawMode == SpriteDrawMode.Simple) sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(size.x, h);
            go.transform.position = new Vector3(go.transform.position.x, top - h * 0.5f, go.transform.position.z);
            var col = go.GetComponent<BoxCollider2D>();
            if (col != null) { Undo.RecordObject(col, "벽 높이"); col.size = new Vector2(size.x, h); col.offset = Vector2.zero; }
            log.AppendLine($"- {n}: 높이 {size.y:0.#} -> {h}칸 (y {top - h:0.#} ~ {top:0.#})");
        }
    }

    // ================= 2) 벽화 4장 =================
    private static void MakeMurals(Transform root, StringBuilder log)
    {
        GameObject wall = Find("Wall_Ante_L");
        float bottom = wall != null ? wall.transform.position.y - wall.GetComponent<SpriteRenderer>().size.y * 0.5f : 6f;
        Transform parent = Child(root, "Murals_Ante");
        Sprite bubble = LoadSprite(BubblePath);
        for (int i = 1; i <= SobekMuralScenes.Count; i++)
        {
            GameObject go = Child(parent, $"Mural_Sobek_{i}").gameObject;
            go.transform.position = new Vector3(MuralX[i - 1], bottom, 0f);
            var sr = GetOrAdd<SpriteRenderer>(go);
            Sprite art = LoadSprite($"{StoryFolder}/Sobek_Mural{i}.png");
            if (art != null && (sr.sprite == null || sr.sprite.name.StartsWith("Sobek_Mural"))) sr.sprite = art;
            sr.sortingLayerName = "Objects";
            sr.sortingOrder = -2;
            float w = sr.sprite != null ? sr.sprite.bounds.size.x : 4f;
            float h = sr.sprite != null ? sr.sprite.bounds.size.y : 2f;
            var col = GetOrAdd<BoxCollider2D>(go);
            col.isTrigger = true;                    // 벽에 걸린 그림: 길은 안 막고, 아래 바닥에 서서 E
            col.size = new Vector2(w * 0.9f, 1.4f);
            col.offset = new Vector2(0f, -0.1f);
            SpriteRenderer b = Bubble(go.transform, new Vector3(0f, h + 0.5f, 0f), bubble);
            var m = GetOrAdd<SobekMural>(go);
            m.EditorSetup(i, MuralNotes[i - 1], sr, b);
            EditorUtility.SetDirty(m);
            if (art == null) log.AppendLine($"- 경고: {StoryFolder}/Sobek_Mural{i}.png 없음 (python3 Tools/SobekArt/sobek_murals.py)");
        }
        // 벽화 보기 화면 (바스테트와 같은 것, 글꼴 Galmuri11)
        GameObject viewer = Child(root, "BastetMuralViewer").gameObject;
        var v = GetOrAdd<BastetMuralViewer>(viewer);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fonts/Galmuri11 SDF.asset");
        v.EditorSetup(font);
        EditorUtility.SetDirty(v);
        log.AppendLine($"- 벽화 4장: 전실 위쪽 벽 (x {string.Join(", ", MuralX)}), 노트 40~43");
    }

    // ================= 3) 카메라 영역 =================
    private static void MakeCameraZones(Transform root, StringBuilder log)
    {
        // 본당 + 전실 전체 (벽 안쪽). 숨은 방은 오른쪽 바깥이라 이 영역에 있는 동안은 안 보임
        Bounds hub = new Bounds(new Vector3(HubCenterX, 16f, 0f), new Vector3(46f, 32f, 0f));
        GameObject floor = Find("Floor_Ante"), hall = Find("FloodWater");
        if (floor != null && hall != null)
        {
            Bounds b = floor.GetComponent<Renderer>().bounds;
            b.Encapsulate(hall.GetComponent<Renderer>().bounds);
            hub = b;
        }
        Zone(root, "CameraZone_Hub", hub, new Vector4(1f, 1f, 1f, 1.5f), false, log);

        GameObject lowerFloor = Find("Lower_Floor");
        if (lowerFloor != null)
            Zone(root, "CameraZone_Lower", lowerFloor.GetComponent<Renderer>().bounds, new Vector4(1f, 1f, 1f, 1.5f), true, log);
        else log.AppendLine("- Lower_Floor 없음 -> 숨은 방 카메라 영역 건너뜀");

        // 맵 경계 타일맵이 없으면 카메라는 영역만 따름 (확인용 로그)
        Camera main = Camera.main;
        if (main != null && main.GetComponent<CameraFollow2D>() == null) log.AppendLine("- 경고: Main Camera에 CameraFollow2D 없음");
    }

    private static void Zone(Transform root, string objName, Bounds b, Vector4 margin, bool fit, StringBuilder log)
    {
        GameObject go = Child(root, objName).gameObject;
        go.transform.position = new Vector3(b.center.x, b.center.y, 0f);
        var box = GetOrAdd<BoxCollider2D>(go);
        box.isTrigger = true;
        var zone = GetOrAdd<CameraRoomZone>(go);
        zone.EditorSetup(new Vector2(b.size.x, b.size.y), margin);
        var so = new SerializedObject(zone);
        so.FindProperty("fitWholeRoom").boolValue = fit;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(zone);
        log.AppendLine($"- {objName}: {b.size.x:0.#} x {b.size.y:0.#}칸 (가운데 {b.center.x:0.#}, {b.center.y:0.#})");
    }

    // ================= 4) 순서 잠금 =================
    private static void MakeLocks(Transform root, StringBuilder log)
    {
        Transform parent = Child(root, "BridgeLocks");
        Sprite logArt = LoadSprite($"{WorldFolder}/Sobek_Obj_LogBlock.png");
        Sprite fogArt = LoadSprite($"{WorldFolder}/Sobek_Fx_Fog.png");
        if (logArt == null || fogArt == null) log.AppendLine("- 경고: 통나무/어둠 그림 없음 (python3 Tools/SobekArt/sobek_illusion_art.py)");

        foreach (LockInfo info in Locks)
        {
            GameObject bridge = Find("Path_Bridge_" + info.id), island = Find("Path_Island_" + info.id);
            if (bridge == null || island == null) { log.AppendLine($"- {info.id}: 다리/섬(Path_Bridge_{info.id}, Path_Island_{info.id}) 없음 -> 건너뜀"); continue; }
            Bounds bb = bridge.GetComponent<Renderer>().bounds, ib = island.GetComponent<Renderer>().bounds;
            bool left = bb.center.x < HubCenterX;
            float logX = left ? bb.max.x - 0.7f : bb.min.x + 0.7f;   // 다리 안쪽(가운데 길 쪽) 끝

            // 통나무
            GameObject lockGo = Child(parent, "Lock_" + info.id).gameObject;
            lockGo.transform.position = new Vector3(logX, bb.center.y, 0f);
            lockGo.transform.rotation = Quaternion.identity;
            var sr = GetOrAdd<SpriteRenderer>(lockGo);
            if (logArt != null) sr.sprite = logArt;
            sr.sortingLayerName = "Objects";
            sr.sortingOrder = 81;
            sr.color = Color.white;
            sr.enabled = true;
            var col = GetOrAdd<BoxCollider2D>(lockGo);
            col.isTrigger = false;
            col.size = new Vector2(1.1f, bb.size.y + 0.2f);
            col.offset = Vector2.zero;
            col.enabled = true;

            // 어둠 (섬 + 다리, 통나무까지)
            GameObject fogGo = Child(parent, "Fog_" + info.id).gameObject;
            float x0 = left ? ib.min.x - 1.3f : logX - 0.1f, x1 = left ? logX + 0.1f : ib.max.x + 1.3f;
            float y0 = ib.min.y - 1.2f, y1 = ib.max.y + 1.2f;
            fogGo.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f);
            var fsr = GetOrAdd<SpriteRenderer>(fogGo);
            if (fogArt != null) fsr.sprite = fogArt;
            fsr.drawMode = SpriteDrawMode.Sliced;
            fsr.size = new Vector2(x1 - x0, y1 - y0);
            fsr.color = new Color(0.02f, 0.02f, 0.04f, 0.92f);
            fsr.sortingLayerName = "Objects";
            fsr.sortingOrder = 80;
            fsr.enabled = true;

            var lk = GetOrAdd<SobekBridgeLock>(lockGo);
            lk.EditorSetup(info.id, info.island, info.item, info.flag, info.key, sr, fsr, new Vector2(0f, -1.8f));
            EditorUtility.SetDirty(lk);
            log.AppendLine($"- Lock_{info.id}: 통나무 x {logX:0.#} ({info.key} -> {info.island}), 어둠 {x1 - x0:0.#} x {y1 - y0:0.#}칸");
        }
    }

    // ================= 5) 숨은 방 통로 =================
    private static void SetupStairs(StringBuilder log)
    {
        GameObject go = Find("StairsDown");
        SobekStairs st = go != null ? go.GetComponentInChildren<SobekStairs>(true) : null;
        if (st == null) { log.AppendLine("- StairsDown(SobekStairs) 없음 -> 통로 연결 건너뜀"); return; }
        Undo.RecordObject(st, "통로");
        st.EditorSetCorridor(true);
        EditorUtility.SetDirty(st);
        log.AppendLine("- StairsDown: 처음 내려갈 때 통로 '페트수코스의 길' -> 숨은 방 도착");
    }

    // ================= 5-2) 숨은 방 도착 지점 =================
    // 숨은 방(HiddenRoom_Lower)을 옮기면 Lower_Spawn만 방 밖(왼쪽 벽 너머)에 남는 문제 -> 바닥 안, 올라가는 계단 오른쪽으로
    private static void FixLowerSpawn(StringBuilder log)
    {
        GameObject spawn = Find("Lower_Spawn"), floor = Find("Lower_Floor"), up = Find("Lower_StairsUp");
        if (spawn == null || floor == null) return;
        Bounds b = floor.GetComponent<Renderer>().bounds;
        Vector3 p = spawn.transform.position;
        Bounds inner = b; inner.Expand(new Vector3(-1.5f, -1.5f, 0f));
        if (inner.Contains(new Vector3(p.x, p.y, b.center.z))) { log.AppendLine("- Lower_Spawn: 숨은 방 안에 있음 (그대로)"); return; }
        Vector3 target = up != null ? up.transform.position + new Vector3(1.8f, 0.6f, 0f) : new Vector3(b.min.x + 2f, b.min.y + 1.5f, 0f);
        target.x = Mathf.Clamp(target.x, inner.min.x, inner.max.x);
        target.y = Mathf.Clamp(target.y, inner.min.y, inner.max.y);
        Undo.RecordObject(spawn.transform, "숨은 방 도착 지점");
        spawn.transform.position = new Vector3(target.x, target.y, 0f);
        log.AppendLine($"- Lower_Spawn: 방 밖({p.x:0.#}, {p.y:0.#}) -> 방 안({target.x:0.#}, {target.y:0.#})");
    }

    // ================= 6) 첫 대사 =================
    private static void UpdateIntro(StringBuilder log)
    {
        SobekHubState state = Object.FindFirstObjectByType<SobekHubState>(FindObjectsInactive.Include);
        if (state == null) return;
        var so = new SerializedObject(state);
        SerializedProperty p = so.FindProperty("introLines");
        if (p == null) return;
        bool old = false;
        for (int i = 0; i < p.arraySize; i++) if (p.GetArrayElementAtIndex(i).stringValue.Contains("섬마다 시험의 문이 있어")) old = true;
        if (!old) { log.AppendLine("- 허브 첫 대사: 직접 고친 대사라 그대로 둠"); return; }
        string[] lines =
        {
            SobekStory.Khepri, "여기는 소베크의 신전, 셰데트야. 신전이 온통 물에 잠겨 있어.",
            SobekStory.Me, "물 위로 좁은 길과 섬 네 개만 떠 있다... 저 큰 문은 악어 입 모양이다. 홈이 네 개 비어 있다.",
            SobekStory.Khepri, "먼저 전실 벽의 벽화를 살펴봐. 이 신전에 무슨 일이 있었는지 알 수 있을 거야.",
            SobekStory.Khepri, "섬마다 시험이 있어. 다리를 막은 통나무는 앞 섬의 부적으로 치울 수 있어. 소용돌이 섬부터 가자.",
        };
        p.arraySize = lines.Length;
        for (int i = 0; i < lines.Length; i++) p.GetArrayElementAtIndex(i).stringValue = lines[i];
        so.ApplyModifiedProperties();
        log.AppendLine("- 허브 첫 대사: 벽화 보기 + 소용돌이 섬부터 안내 추가");
    }

    // ================= 도우미 =================
    private static GameObject Find(string objName)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == objName && t.gameObject.scene.IsValid()) return t.gameObject;
        return null;
    }

    private static Transform Child(Transform parent, string objName)
    {
        Transform t = parent.Find(objName);
        if (t != null) return t;
        GameObject found = Find(objName); // 다른 곳으로 옮겨 놓았으면 그것을 씀
        if (found != null) return found.transform;
        var go = new GameObject(objName);
        Undo.RegisterCreatedObjectUndo(go, objName);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }

    private static SpriteRenderer Bubble(Transform parent, Vector3 localPos, Sprite sprite)
    {
        Transform bt = parent.Find("NewBubble");
        if (bt == null)
        {
            var b = new GameObject("NewBubble");
            Undo.RegisterCreatedObjectUndo(b, "말풍선");
            b.transform.SetParent(parent, false);
            bt = b.transform;
        }
        bt.localPosition = localPos;
        var bsr = GetOrAdd<SpriteRenderer>(bt.gameObject);
        if (sprite != null) bsr.sprite = sprite;
        bsr.sortingLayerName = "Objects";
        bsr.sortingOrder = 5;
        return bsr;
    }

    private static Sprite LoadSprite(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path)) if (o is Sprite sp) return sp;
        return null;
    }

    private static string FindScene(string sceneName)
    {
        foreach (string guid in AssetDatabase.FindAssets($"{sceneName} t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName) return path;
        }
        return null;
    }
}
#endif
