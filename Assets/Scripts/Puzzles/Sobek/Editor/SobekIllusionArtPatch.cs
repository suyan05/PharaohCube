#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// [추가] 메뉴: Tools/Sobek/6. 착시 퍼즐에 플레이어 그림 넣기 (S1~S4, 메인)
// 착시 퍼즐 씬 5개를 차례로 열어 각 판(SobekIllusionBoard)의 캐릭터(흰 네모)를 플레이어 그림으로 바꾸고 저장
// - 플레이어 프리팹(PlayerSpriteAnimator)의 앞모습(downFrames)·뒷모습(upFrames)·걷기 순서를 그대로 씀
// - 씬을 새로 만들지 않음 (퍼즐 씬에서 직접 고친 내용은 그대로)
// 블록 무늬·물결·배경 그림은 Resources/Sobek/Illusion/ 에서 실행 중 자동으로 읽으므로 이 메뉴가 없어도 보임
public static class SobekIllusionArtPatch
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
    private const string SceneFolder = "Assets/Scenes/Puzzle_Test";

    [MenuItem("Tools/Sobek/6. 착시 퍼즐에 플레이어 그림 넣기 (S1~S4, 메인)", priority = 6)]
    private static void Menu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!PlayerFrames(out Sprite[] front, out Sprite[] back, out int[] pattern))
        {
            EditorUtility.DisplayDialog("플레이어 그림", $"{PlayerPrefabPath} 의 PlayerSpriteAnimator에서 걷기 그림을 찾지 못했습니다.", "확인");
            return;
        }

        string before = SceneManager.GetActiveScene().path;
        var log = new List<string>();
        string[] names = { SobekIds.SubScene1, SobekIds.SubScene2, SobekIds.SubScene3, SobekIds.SubScene4, SobekIds.MainScene };
        int boards = 0;
        foreach (string n in names)
        {
            string path = FindScene(n);
            if (path == null) { log.Add($"{n}: 씬 없음 (Tools/Sobek/5 먼저)"); continue; }
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int count = 0;
            foreach (SobekIllusionBoard b in Object.FindObjectsByType<SobekIllusionBoard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Apply(b, front, back, pattern);
                count++;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            log.Add($"{n}: 판 {count}개에 플레이어 그림");
            boards += count;
        }
        if (!string.IsNullOrEmpty(before)) EditorSceneManager.OpenScene(before, OpenSceneMode.Single);
        Debug.Log("[SobekIllusionArtPatch] 완료\n- " + string.Join("\n- ", log));
        EditorUtility.DisplayDialog("착시 퍼즐 그림", $"판 {boards}개의 캐릭터를 플레이어 그림으로 바꿨습니다.\n\n" + string.Join("\n", log) +
            "\n\n블록 무늬·물결·배경은 Play 하면 자동으로 보입니다.", "확인");
    }

    // 퍼즐 판 하나에 플레이어 그림 넣기 (착시 퍼즐 씬 만들기 메뉴에서도 부름)
    public static void Apply(SobekIllusionBoard b, Sprite[] front, Sprite[] back, int[] pattern)
    {
        if (b == null) return;
        Undo.RecordObject(b, "플레이어 그림");
        b.EditorSetAvatarFrames(front, back, pattern);
        if (b.EditorAvatar != null) EditorUtility.SetDirty(b.EditorAvatar);
        EditorUtility.SetDirty(b);
    }

    public static void ApplyFromPrefab(SobekIllusionBoard b)
    {
        if (PlayerFrames(out Sprite[] front, out Sprite[] back, out int[] pattern)) Apply(b, front, back, pattern);
    }

    // 플레이어 프리팹의 PlayerSpriteAnimator에서 앞(down)·뒤(up) 걷기 그림과 걷기 순서를 읽음
    public static bool PlayerFrames(out Sprite[] front, out Sprite[] back, out int[] pattern)
    {
        front = back = null; pattern = null;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null) return false;
        var anim = prefab.GetComponentInChildren<PlayerSpriteAnimator>(true);
        if (anim == null) return false;
        var so = new SerializedObject(anim);
        front = ReadSprites(so, "downFrames");
        back = ReadSprites(so, "upFrames");
        SerializedProperty p = so.FindProperty("walkPattern");
        if (p != null && p.isArray)
        {
            pattern = new int[p.arraySize];
            for (int i = 0; i < p.arraySize; i++) pattern[i] = p.GetArrayElementAtIndex(i).intValue;
        }
        return front != null && front.Length > 0;
    }

    private static Sprite[] ReadSprites(SerializedObject so, string prop)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null || !p.isArray) return null;
        var list = new List<Sprite>();
        for (int i = 0; i < p.arraySize; i++)
            if (p.GetArrayElementAtIndex(i).objectReferenceValue is Sprite s) list.Add(s);
        return list.ToArray();
    }

    private static string FindScene(string sceneName)
    {
        string direct = $"{SceneFolder}/{sceneName}.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(direct) != null) return direct;
        foreach (string guid in AssetDatabase.FindAssets($"{sceneName} t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName) return path;
        }
        return null;
    }
}
#endif
