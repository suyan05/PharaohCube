using System;
using UnityEditor;
using UnityEngine;

// 월드 도트 그림 자동 임포트 설정 (세부 아트 요청서 0.2)
// 아래 폴더에 PNG를 "처음" 넣을 때만 자동으로:
//   Sprite / Single, PPU 32, Point 필터, 압축 없음, 밉맵 끔
//   피벗: 파일 이름에 _Obj_ 가 있으면 바닥 가운데 (물건이 바닥에 닿는 줄 = 맨 아래), 나머지(타일·FX)는 가운데
// 이미 들어와 있는 그림은 건드리지 않음 (손으로 바꾼 설정 유지)
// 주의: 이 파일은 꼭 이름이 Editor인 폴더 안에 있어야 함 (Assets/Scripts/Editor)
public class PixelArtImporter : AssetPostprocessor
{
    private static readonly string[] PixelFolders =
    {
        "Assets/image/Prologue/World/",
        "Assets/image/Hall/World/",
        "Assets/image/Common/World/"
    };

    private void OnPreprocessTexture()
    {
        try
        {
            if (!assetImporter.importSettingsMissing) return; // 처음 들어올 때만
            string path = assetPath.Replace('\\', '/');
            if (!IsPixelFolder(path)) return;

            Apply((TextureImporter)assetImporter, path);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PixelArtImporter] 임포트 설정 오류 ({assetPath}): {e}");
        }
    }

    private static bool IsPixelFolder(string path)
    {
        foreach (string folder in PixelFolders)
        {
            if (path.StartsWith(folder, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static void Apply(TextureImporter importer, string path)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        bool standing = path.Contains("_Obj_");
        settings.spriteAlignment = (int)(standing ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
    }
}
