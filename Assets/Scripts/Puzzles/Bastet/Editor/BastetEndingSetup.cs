#if UNITY_EDITOR
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// [추가] 메뉴: Tools/Bastet/★ 엔딩 연출 세팅 (일러스트 컷·큐브·소리)
// 지금 열린 바스테트 씬에 BastetEnding을 놓고 그림·글꼴을 연결 + 메인 퍼즐(BastetSunMoonWalk)에 엔딩 연결
// + 아이템 목록(ItemCatalog)의 고양이 형판(PLATE_BASTET) 아이콘이 비어 있으면 형판 그림으로 채움
// 여러 번 눌러도 안전 (있으면 연결만 다시)
public static class BastetEndingSetup
{
    private const string UiFolder = "Assets/image/Bastet/UI/";
    private const string CubeFolder = "Assets/image/Cube/";
    private const string CatalogPath = "Assets/Resources/ItemCatalog.asset";
    private static readonly string[] FaceFiles = { "Ra.jpg", "Bastet.jpg", "Thoth.jpg", "Horus.jpg", "Sobek.jpg", "Anubis.jpg" }; // BastetEnding 칸 순서

    [MenuItem("Tools/Bastet/★ 엔딩 연출 세팅 (일러스트 컷·큐브·소리)", priority = 7)]
    private static void Setup()
    {
        if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("엔딩 세팅", "Play를 멈춘 뒤 눌러 주세요.", "확인"); return; }
        var log = new StringBuilder("[BastetEndingSetup] 엔딩 연출 세팅\n");

        // 1) 그림·글꼴
        Sprite illust = LoadSprite(UiFolder + "UI_Portrait_Bastet_Glow.png");
        Sprite mural = LoadSprite(UiFolder + "Bastet_UI_Clue_B5_SunMoon.png");
        Sprite cube = LoadSprite(CubeFolder + "Cube.png");
        var faces = new Sprite[FaceFiles.Length];
        for (int i = 0; i < FaceFiles.Length; i++) faces[i] = LoadSprite(CubeFolder + FaceFiles[i]);
        Sprite plate = faces[1];
        TMP_FontAsset font = FindFont();
        log.AppendLine($"- 여신 그림: {Name(illust)} / 벽화: {Name(mural)} / 큐브: {Name(cube)} / 형판: {Name(plate)} / 글꼴: {(font != null ? font.name : "없음(기본)")}");

        // 2) 씬에 BastetEnding
        BastetEnding ending = Object.FindFirstObjectByType<BastetEnding>(FindObjectsInactive.Include);
        if (ending == null)
        {
            var go = new GameObject("BastetEnding");
            Undo.RegisterCreatedObjectUndo(go, "엔딩");
            ending = go.AddComponent<BastetEnding>();
            log.AppendLine("- BastetEnding 오브젝트 만듦");
        }
        Undo.RecordObject(ending, "엔딩 연결");
        ending.EditorSetup(illust, mural, plate, cube, faces, font, "");
        EditorUtility.SetDirty(ending);

        // 3) 메인 퍼즐에 연결
        var walk = Object.FindFirstObjectByType<BastetSunMoonWalk>(FindObjectsInactive.Include);
        if (walk != null)
        {
            var so = new SerializedObject(walk);
            so.FindProperty("ending").objectReferenceValue = ending;
            so.ApplyModifiedProperties();
            log.AppendLine($"- {walk.name}: 클리어하면 바로 다음 씬 대신 엔딩 연출");
        }
        else log.AppendLine("- 경고: BastetSunMoonWalk(메인 퍼즐)를 못 찾음");

        // 4) 고양이 형판 아이콘
        FillCatalogIcon(BastetItemIds.Plate, plate, log);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = ending.gameObject;
        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("엔딩 세팅", "엔딩 연출을 연결했습니다.\nCtrl+S로 씬을 저장한 뒤,\nPlay 중 BastetEnding 우클릭(⋮) → '테스트: 엔딩 바로 보기'로 확인할 수 있습니다.", "확인");
    }

    // 여러 장으로 잘린 그림(Bastet_0 등)이어도 첫 Sprite를 가져옴
    private static Sprite LoadSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;
        foreach (Object o in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
            if (o is Sprite sp) return sp;
        Debug.LogWarning($"[BastetEndingSetup] 그림 없음(또는 Sprite가 아님): {path}");
        return null;
    }

    private static void FillCatalogIcon(string itemId, Sprite icon, StringBuilder log)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
        if (catalog == null || icon == null) return;
        var so = new SerializedObject(catalog);
        SerializedProperty items = so.FindProperty("items");
        if (items == null) return;
        for (int i = 0; i < items.arraySize; i++)
        {
            SerializedProperty e = items.GetArrayElementAtIndex(i);
            if (e.FindPropertyRelative("id")?.stringValue != itemId) continue;
            SerializedProperty ic = e.FindPropertyRelative("icon");
            if (ic == null || ic.objectReferenceValue != null) { log.AppendLine($"- {itemId} 아이콘: 이미 있음"); return; }
            ic.objectReferenceValue = icon;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            log.AppendLine($"- {itemId} 아이콘: {icon.name} 연결 (획득 팝업에 형판 그림)");
            return;
        }
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

    private static string Name(Object o) => o != null ? o.name : "없음";
}
#endif
