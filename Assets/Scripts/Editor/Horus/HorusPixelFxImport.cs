using System;
using UnityEditor;
using UnityEngine;

// 픽셀 이펙트 그림(Assets/Resources/Horus/PixelFx/*.png)을 넣으면 자동으로 도트용 스프라이트로 가져옴
// - 32px = 1칸, Point(또렷하게), 압축 없음
// - HO_Flame_* 는 기준점을 아래 가운데로 (불꽃 밑동을 횃대 테두리에 맞추기 쉽게)
// - HO_Sconce_Base 는 원래 횃대 그림과 같은 가운데 기준점
public class HorusPixelFxImport : AssetPostprocessor
{
    private const string Folder = "Assets/Resources/Horus/PixelFx/";

    private void OnPreprocessTexture()
    {
        try
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.StartsWith(Folder, StringComparison.Ordinal)) return;
            var imp = (TextureImporter)assetImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 32f;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            SetPivot(imp, System.IO.Path.GetFileName(path).StartsWith("HO_Flame_", StringComparison.Ordinal));
        }
        catch (Exception e)
        {
            Debug.LogError($"[픽셀 이펙트 가져오기] {assetPath}: {e.Message}");
        }
    }

    private static void SetPivot(TextureImporter imp, bool bottom)
    {
        var settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)(bottom ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
        imp.SetTextureSettings(settings);
    }
}
