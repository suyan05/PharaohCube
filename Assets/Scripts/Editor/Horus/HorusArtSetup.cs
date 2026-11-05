using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// 호루스 그림 자동 연결
// 메뉴: PharaohCube -> Horus -> 호루스 그림 연결 (지금 열린 씬)
// - Assets/image/Horus 의 그림을 퍼즐 화면(H1~H5)과 방 안 물건에 넣음
// - ItemCatalog에 호루스 아이템 6종(이름, 설명, 아이콘)을 넣음
// - 여러 번 눌러도 됨 (같은 걸 다시 덮어쓰기만 함). 그림 파일이 없으면 그 칸만 건너뜀
// - 호루스 타일맵 씬 만들기(HorusRoomBuilder)도 마지막에 이걸 부름
public static class HorusArtSetup
{
    public const string ArtRoot = "Assets/image/Horus/";

    [MenuItem("PharaohCube/Horus/호루스 그림 연결 (지금 열린 씬)")]
    private static void Menu()
    {
        int count = Apply();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("호루스 그림 연결", $"{count}곳 연결했어요. 씬을 저장해 주세요 (Ctrl + S).", "확인");
    }

    // 연결한 곳 수를 돌려줌
    public static int Apply()
    {
        int count = 0;
        try
        {
            count += HorusUiArt.Apply();
            count += HorusWorldArt.Apply();
            count += HorusItemSetup.Apply();
            Debug.Log($"[HorusArtSetup] 그림 연결 {count}곳 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"[HorusArtSetup] 그림 연결 오류: {e}");
        }
        return count;
    }

    // ================= 그림 찾기 =================
    // "H2/H2_01" -> Assets/image/Horus/H2/H2_01.png. "Assets/..."로 시작하면 그 경로 그대로
    // 그림 하나가 여러 조각으로 잘려 있으면(Multiple) 가장 큰 조각을 씀
    public static Sprite Art(string name)
    {
        string path = name.StartsWith("Assets/") ? name : ArtRoot + name + ".png";
        MakeSprite(path);
        Sprite best = null;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (o is Sprite s && (best == null || Area(s) > Area(best))) best = s;
        }
        if (best == null) Debug.LogWarning($"[HorusArtSetup] 그림 없음: {path}");
        return best;
    }

    private static float Area(Sprite s) => s.rect.width * s.rect.height;

    // 새로 넣은 그림이 일반 텍스처로 들어와 있으면 Sprite로 바꿈 (예: H4_03_Mosaic)
    private static void MakeSprite(string path)
    {
        if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
        if (importer.textureType == TextureImporterType.Sprite) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    // ================= 씬에서 찾기 (꺼진 오브젝트 포함) =================
    public static GameObject Find(string objName)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == objName) return t.gameObject;
        }
        return null;
    }

    public static List<T> All<T>() where T : Component
    {
        return new List<T>(Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None));
    }

    // ================= SerializedObject 칸 채우기 (칸 이름이 없으면 경고만) =================
    public static SerializedProperty Prop(SerializedObject so, string field)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null) Debug.LogWarning($"[HorusArtSetup] {so.targetObject.name}: '{field}' 칸이 없음");
        return p;
    }

    public static void SetSprite(SerializedObject so, string field, string art)
    {
        SerializedProperty p = Prop(so, field);
        if (p != null) p.objectReferenceValue = Art(art);
    }

    public static void SetFloat(SerializedObject so, string field, float value)
    {
        SerializedProperty p = Prop(so, field);
        if (p != null) p.floatValue = value;
    }

    public static void SetVector(SerializedObject so, string field, Vector2 value)
    {
        SerializedProperty p = Prop(so, field);
        if (p != null) p.vector2Value = value;
    }

    public static void SetSpriteList(SerializedObject so, string field, string[] arts)
    {
        SerializedProperty p = Prop(so, field);
        if (p == null) return;
        p.arraySize = arts.Length;
        for (int i = 0; i < arts.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = Art(arts[i]);
    }
}
