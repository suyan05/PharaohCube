#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 아누비스 튜토리얼 씬 한 번에 세팅
// Tools/Anubis/튜토리얼: 양피지 + 진짜 방 이동 연출 + 기둥 뒤로 지나가기
// 1) 튜토리얼용 쉬운 양피지 4장 (.asset) 만들기 - 튜토리얼 퍼즐(8/5/3 항아리, 4배 도르래, 4kg 심장, 숫자 마방진)에 맞춘 글
// 2) Managers에 양피지 수집/보기(AnubisScrollCollection, AnubisScrollViewer) 붙이기
// 3) 제단 4개 옆에 양피지 조각 2개씩 놓기 (위치는 Scene에서 옮기기)
// 4) 출구(Exit_ToAnubis)에 폰트 연결 -> 화면이 어두워지며 진짜 방으로
// 5) 이름이 Pillar로 시작하는 기둥에 PillarOcclusion 붙이기
// - 여러 번 눌러도 안전: 이미 있는 것은 새로 만들지 않음, 고친 양피지 글은 덮어쓰지 않음
public static class AnubisTutorialSetup
{
    private const string TutorialDataFolder = "Assets/Scripts/Puzzles/Anubis/Data/Tutorial";
    private const string RealDataFolder = "Assets/Scripts/Puzzles/Anubis/Data";
    private const string PiecesRootName = "Scrolls_Tutorial";

    // (파일 이름, ID, 제목, 조각 그림을 빌려올 실전 양피지, 옆에 둘 제단, 글)
    // 글은 빈 줄로 2문단 -> 조각 1개: 규칙 / 조각 2개: 목표 + 요령
    private static readonly (string file, string id, string title, string artFrom, string altar, string text)[] Scrolls =
    {
        ("Scroll_Tut1_Sand", "SCROLL_ANUBIS_TUT_1", "모래의 서 (연습)", "Scroll_Room1", "Altar_A1_Sand",
            "나트론의 모래는 항아리에서 항아리로만 옮겨진다.\n한 번 붓기 시작하면 받는 항아리가 가득 차거나\n붓는 항아리가 빌 때까지 멈추지 않는다.\n\n" +
            "여덟 되 항아리에 모래가 가득하다.\n다섯 되와 세 되 항아리를 써서\n여덟 되와 다섯 되 항아리에 네 되씩 나누어라.\n(요령: 작은 항아리로 덜어 내고, 덜어 낸 모래를 다시 옮겨라)"),

        ("Scroll_Tut2_Gear", "SCROLL_ANUBIS_TUT_2", "태엽의 서 (연습)", "Scroll_Room2", "Altar_A2_Gear",
            "첫 바퀴가 돌면 마지막 바퀴가 따라 돈다.\n마지막 바퀴의 이가 첫 바퀴보다 많을수록\n힘은 그 비율만큼 커진다. (이 24개 ÷ 이 12개 = 2배)\n\n" +
            "고정 도르래는 방향만 바꾸고,\n움직 도르래는 힘을 두 배로 만든다.\n한 근의 추로 네 근의 유물을 들어 올려라. (톱니 배율 × 도르래 배율 = 4)"),

        ("Scroll_Tut3_Lever", "SCROLL_ANUBIS_TUT_3", "지렛대의 서 (연습)", "Scroll_Room3", "Altar_A3_Lever",
            "지렛대는 무게만으로 기울지 않는다.\n(무게 × 축에서의 거리)가 양쪽에서 같을 때\n비로소 평형을 이룬다.\n\n" +
            "망자의 심장은 네 근, 왼쪽 두 번째 눈금에 걸려 있다.\n흑요석 추 하나는 두 근이다.\n추의 개수와 걸 눈금을 정해 양쪽 힘을 같게 하라. (4 × 2 = 8)"),

        ("Scroll_Tut4_Magic", "SCROLL_ANUBIS_TUT_4", "심판관의 서 (연습)", "Scroll_Room4", "Altar_A4_MagicSquare",
            "아홉 칸에 1부터 9까지 한 번씩만 놓는다.\n가로, 세로, 대각선 어느 줄을 더해도\n모두 15가 되어야 한다.\n\n" +
            "가운데는 언제나 5다.\n가운데를 사이에 두고 마주 보는 두 칸을 더하면 언제나 10이다.\n(실전에서는 숫자 대신 신의 상징이 놓인다)")
    };

    // 제단 기준 조각 위치 (왼쪽 아래, 오른쪽 아래)
    private static readonly Vector3[] PieceOffsets = { new Vector3(-1.6f, -1.4f, 0f), new Vector3(1.6f, -1.4f, 0f) };

    [MenuItem("Tools/Anubis/튜토리얼: 양피지 + 진짜 방 이동 연출 + 기둥 뒤로 지나가기")]
    public static void Apply()
    {
        var log = new List<string>();
        TMP_FontAsset font = FindFont();

        var datas = EnsureScrollAssets(log);
        EnsureManagers(log, font);
        EnsurePieces(log, datas);
        EnsureExit(log, font);
        ApplyPillars(log, FindPillars());

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[튜토리얼 세팅] 완료 (Ctrl+S로 씬 저장)\n- " + string.Join("\n- ", log));
    }

    // 실전 씬 등 다른 씬의 기둥에도 쓸 수 있게 따로 메뉴
    [MenuItem("Tools/Anubis/기둥 뒤로 지나가기 적용 (선택한 오브젝트, 없으면 Pillar로 시작하는 전부)")]
    public static void ApplyPillarsMenu()
    {
        var log = new List<string>();
        var targets = new List<GameObject>();
        foreach (GameObject go in Selection.gameObjects)
            if (go.GetComponent<SpriteRenderer>() != null) targets.Add(go);
        if (targets.Count == 0) targets = FindPillars();

        ApplyPillars(log, targets);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[기둥 뒤로 지나가기]\n- " + string.Join("\n- ", log));
    }

    // ================= 1. 튜토리얼 양피지 데이터 =================
    private static Dictionary<string, AnubisScrollData> EnsureScrollAssets(List<string> log)
    {
        EnsureFolder(TutorialDataFolder);
        var result = new Dictionary<string, AnubisScrollData>();

        foreach (var s in Scrolls)
        {
            string path = $"{TutorialDataFolder}/{s.file}.asset";
            var data = AssetDatabase.LoadAssetAtPath<AnubisScrollData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<AnubisScrollData>();
                AssetDatabase.CreateAsset(data, path);

                var so = new SerializedObject(data);
                so.FindProperty("scrollId").stringValue = s.id;
                so.FindProperty("title").stringValue = s.title;
                so.FindProperty("pieceCount").intValue = 2;          // 실전(5조각)보다 적게
                so.FindProperty("fullText").stringValue = s.text;
                so.FindProperty("stageCount").intValue = 2;          // 조각 1개 = 규칙, 2개 = 목표
                CopyPieceSprites(so, s.artFrom, 2);
                so.ApplyModifiedPropertiesWithoutUndo();
                log.Add($"양피지 '{s.title}' 만듦 ({path})");
            }
            result[s.altar] = data;
        }
        return result;
    }

    // 실전 양피지의 조각 그림 앞 n장을 빌려 씀 (아트가 따로 오면 Inspector에서 교체)
    private static void CopyPieceSprites(SerializedObject target, string realScrollName, int count)
    {
        var real = AssetDatabase.LoadAssetAtPath<AnubisScrollData>($"{RealDataFolder}/{realScrollName}.asset");
        if (real == null) return;

        var src = new SerializedObject(real).FindProperty("pieceSprites");
        var dst = target.FindProperty("pieceSprites");
        if (src == null || dst == null) return;

        int n = Mathf.Min(count, src.arraySize);
        dst.arraySize = n;
        for (int i = 0; i < n; i++)
            dst.GetArrayElementAtIndex(i).objectReferenceValue = src.GetArrayElementAtIndex(i).objectReferenceValue;
    }

    // ================= 2. 수집 + 보기 창 =================
    private static void EnsureManagers(List<string> log, TMP_FontAsset font)
    {
        GameObject managers = GameObject.Find("Managers");
        if (managers == null)
        {
            managers = new GameObject("Managers");
            Undo.RegisterCreatedObjectUndo(managers, "Managers");
        }

        if (Object.FindFirstObjectByType<AnubisScrollCollection>(FindObjectsInactive.Include) == null)
        {
            Undo.AddComponent<AnubisScrollCollection>(managers);
            log.Add("Managers에 AnubisScrollCollection 붙임");
        }

        var viewer = Object.FindFirstObjectByType<AnubisScrollViewer>(FindObjectsInactive.Include);
        if (viewer == null)
        {
            viewer = Undo.AddComponent<AnubisScrollViewer>(managers);
            log.Add("Managers에 AnubisScrollViewer 붙임 (H: 양피지 보기)");
        }
        var vso = new SerializedObject(viewer);
        SetObject(vso, "collection", Object.FindFirstObjectByType<AnubisScrollCollection>(FindObjectsInactive.Include));
        SetObject(vso, "font", font);
        vso.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================= 3. 조각 배치 =================
    private static void EnsurePieces(List<string> log, Dictionary<string, AnubisScrollData> datas)
    {
        GameObject root = GameObject.Find(PiecesRootName);
        if (root == null)
        {
            root = new GameObject(PiecesRootName);
            Undo.RegisterCreatedObjectUndo(root, "튜토리얼 양피지");
        }

        var defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        int made = 0;

        foreach (var pair in datas)
        {
            GameObject altar = FindInScene(pair.Key);
            if (altar == null)
            {
                log.Add($"제단 '{pair.Key}'를 못 찾아 조각을 건너뜀");
                continue;
            }

            AnubisScrollData data = pair.Value;
            for (int i = 0; i < data.PieceCount; i++)
            {
                string objName = $"{data.name}_Piece{i + 1}";
                if (root.transform.Find(objName) != null) continue; // 이미 있음 (옮겨 둔 위치 유지)

                var go = new GameObject(objName);
                Undo.RegisterCreatedObjectUndo(go, objName);
                go.transform.SetParent(root.transform, false);
                go.transform.position = altar.transform.position + PieceOffsets[i % PieceOffsets.Length];

                var sr = go.AddComponent<SpriteRenderer>();
                Sprite pieceSprite = data.GetPieceSprite(i);
                sr.sprite = pieceSprite != null ? pieceSprite : defaultSprite;
                sr.color = pieceSprite != null ? Color.white : new Color(0.93f, 0.85f, 0.62f, 1f);
                sr.sortingLayerName = "Objects";

                var col = go.AddComponent<CircleCollider2D>(); // 줍기 범위 (트리거라 지나갈 수 있음)
                col.isTrigger = true;
                col.radius = 0.4f;

                var piece = go.AddComponent<AnubisScrollPiece>();
                var so = new SerializedObject(piece);
                so.FindProperty("scroll").objectReferenceValue = data;
                so.FindProperty("pieceNumber").intValue = i + 1;
                so.ApplyModifiedPropertiesWithoutUndo();
                made++;
            }
        }
        log.Add($"양피지 조각 {made}개 새로 놓음 ('{PiecesRootName}' 아래, 제단 양옆 아래쪽. 벽에 걸리면 옮기기)");
    }

    // ================= 4. 출구 =================
    private static void EnsureExit(List<string> log, TMP_FontAsset font)
    {
        var exit = Object.FindFirstObjectByType<AnubisSceneExit>(FindObjectsInactive.Include);
        if (exit == null)
        {
            log.Add("출구(AnubisSceneExit)가 없음 -> 메인 쪽 통로에 Trigger 콜라이더 + AnubisSceneExit를 직접 놓기");
            return;
        }
        var so = new SerializedObject(exit);
        SetObject(so, "font", font);
        so.ApplyModifiedPropertiesWithoutUndo();
        log.Add($"출구 '{exit.name}': 화면이 어두워지며 '{so.FindProperty("nextSceneName").stringValue}'로 이동");
    }

    // ================= 5. 기둥 =================
    private static List<GameObject> FindPillars()
    {
        var list = new List<GameObject>();
        foreach (SpriteRenderer r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (r.gameObject.scene.IsValid() && r.name.StartsWith("Pillar")) list.Add(r.gameObject);
        }
        return list;
    }

    private static void ApplyPillars(List<string> log, List<GameObject> pillars)
    {
        foreach (GameObject go in pillars)
        {
            var occ = go.GetComponent<PillarOcclusion>();
            if (occ == null) occ = Undo.AddComponent<PillarOcclusion>(go);

            // 에디터에서도 바로 콜라이더를 받침 크기로 (Scene에서 확인 가능)
            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = Undo.AddComponent<BoxCollider2D>(go);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                Undo.RecordObject(col, "기둥 콜라이더");
                float ratio = new SerializedObject(occ).FindProperty("blockHeightRatio").floatValue;
                float widthRatio = new SerializedObject(occ).FindProperty("blockWidthRatio").floatValue;
                Bounds b = sr.sprite.bounds;
                float h = b.size.y * ratio;
                col.isTrigger = false;
                col.enabled = true;
                col.size = new Vector2(b.size.x * widthRatio, h);
                col.offset = new Vector2(b.center.x, b.min.y + h * 0.5f);
            }
        }
        log.Add($"기둥 {pillars.Count}개에 PillarOcclusion (아래 25%만 막힘, 뒤로 지나가면 가려짐)");
    }

    // ================= 도우미 =================
    private static GameObject FindInScene(string name)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name && t.gameObject.scene.IsValid()) return t.gameObject;
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
