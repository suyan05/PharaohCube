using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// [추가] 신 일러스트 6장을 한 번에 연결하는 메뉴
// 메뉴: PharaohCube -> Hall -> 신 일러스트 연결
// - Assets/image/Illustration/{Ra, Bastet, Thoth, Horus, Sobek, Anubis}.png 의 첫 스프라이트
//   -> Assets/Resources/GodIllustrations.asset (없으면 만듦)
// - 그림이 스프라이트로 안 가져와져 있으면 Sprite로 바꿔서 다시 가져옴
// - 임시 일러스트를 진짜 그림으로 바꾸면: 같은 이름으로 덮어쓰고 이 메뉴만 다시 누르면 됨
public static class GodIllustrationLinker
{
    private const string Folder = "Assets/image/Illustration";
    private const string AssetPath = "Assets/Resources/GodIllustrations.asset";
    private static readonly string[] Files = { "Ra", "Bastet", "Thoth", "Horus", "Sobek", "Anubis" };

    [MenuItem("PharaohCube/Hall/신 일러스트 연결")]
    private static void Run()
    {
        var log = new StringBuilder();
        try
        {
            GodIllustration set = LoadOrCreate();
            var sprites = new Sprite[GodIllustration.Count];
            for (int i = 0; i < Files.Length; i++) sprites[i] = LoadSprite($"{Folder}/{Files[i]}.png", log);
            set.EditorSetup(sprites);
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            log.AppendLine($"- {AssetPath} 저장");
        }
        catch (Exception e)
        {
            log.AppendLine($"오류: {e.Message}");
            Debug.LogError($"[GodIllustrationLinker] 오류: {e}");
        }
        Debug.Log("[GodIllustrationLinker]\n" + log);
        EditorUtility.DisplayDialog("신 일러스트", log.ToString(), "확인");
    }

    private static GodIllustration LoadOrCreate()
    {
        var set = AssetDatabase.LoadAssetAtPath<GodIllustration>(AssetPath);
        if (set != null) return set;
        set = ScriptableObject.CreateInstance<GodIllustration>();
        AssetDatabase.CreateAsset(set, AssetPath);
        return set;
    }

    private static Sprite LoadSprite(string path, StringBuilder log)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            log.AppendLine($"- 없음: {path}");
            return null;
        }
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
            log.AppendLine($"- {path}: Sprite로 다시 가져옴");
        }
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (o is Sprite s)
            {
                log.AppendLine($"- {s.name} 연결");
                return s;
            }
        }
        log.AppendLine($"- 스프라이트 없음: {path}");
        return null;
    }
}
