using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 배치 도구들이 그림을 찾는 공용 도우미
// - Sprite("Pro_Obj_SphinxHead"): Assets 전체에서 파일 이름이 같은 그림을 찾음 (대소문자 무시)
//   Sprite Mode가 Multiple이어도 첫 조각을 씀. 못 찾으면 비슷한 이름 후보를 Console에 같이 알려줌
// - TileSprite: 위와 같은데, 그림이 32px가 아니면 PPU를 그림 가로 크기로 바꿔서 1칸에 딱 맞게 함
// - Exists: 그림이 있는지만 확인 (경고 없이)
public static class PrologueAssetFinder
{
    public static Sprite Sprite(string fileName)
    {
        try
        {
            string path = FindPath(fileName);
            Sprite sprite = path != null ? LoadSprite(path) : null;
            if (sprite == null) WarnMissing(fileName, path);
            return sprite;
        }
        catch (Exception e)
        {
            Debug.LogError($"[PrologueAssetFinder] 그림 찾기 오류 ({fileName}): {e}");
            return null;
        }
    }

    public static bool Exists(string fileName)
    {
        string path = FindPath(fileName);
        return path != null && LoadSprite(path) != null;
    }

    public static Sprite TileSprite(string fileName)
    {
        Sprite sprite = Sprite(fileName);
        if (sprite == null) return null;

        int width = Mathf.RoundToInt(sprite.rect.width);
        if (width == 32 || Mathf.Approximately(sprite.pixelsPerUnit, width)) return sprite;

        string path = AssetDatabase.GetAssetPath(sprite);
        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            Debug.Log($"[PrologueAssetFinder] {fileName}이 {width}px -> PPU {width}로 바꿔서 1칸에 맞춤");
            importer.spritePixelsPerUnit = width;
            importer.SaveAndReimport();
        }
        return LoadSprite(path);
    }

    public static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }

    // ================= 내부 =================
    private static string FindPath(string fileName)
    {
        foreach (string guid in AssetDatabase.FindAssets(fileName + " t:Texture2D", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.Equals(Path.GetFileNameWithoutExtension(path), fileName, StringComparison.OrdinalIgnoreCase)) return path;
        }
        return null;
    }

    // Single이면 그 그림, Multiple이면 첫 조각. 그림 설정이 Sprite가 아니면 null
    private static Sprite LoadSprite(string path)
    {
        Sprite single = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (single != null) return single;

        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (o is Sprite s) return s;
        }
        return null;
    }

    private static void WarnMissing(string fileName, string foundPath)
    {
        if (foundPath != null)
        {
            Debug.LogWarning($"[PrologueAssetFinder] {foundPath} 는 있는데 Sprite가 아님 -> Texture Type을 Sprite (2D and UI)로 바꾸고 Apply");
            return;
        }

        string key = KeyWord(fileName);
        var similar = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets(key + " t:Texture2D", new[] { "Assets" }))
        {
            similar.Add(Path.GetFileName(AssetDatabase.GUIDToAssetPath(guid)));
            if (similar.Count >= 5) break;
        }
        string hint = similar.Count > 0 ? $" / 비슷한 이름: {string.Join(", ", similar)}" : "";
        Debug.LogWarning($"[PrologueAssetFinder] 그림 없음: {fileName}.png{hint}");
    }

    // "Pro_Obj_SphinxHead_EyeGlow" -> "SphinxHead" (후보 찾기용 핵심 단어)
    private static string KeyWord(string fileName)
    {
        string[] parts = fileName.Split('_');
        return parts.Length >= 3 ? parts[2] : fileName;
    }
}
