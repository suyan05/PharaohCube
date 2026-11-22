using System;
using UnityEditor;
using UnityEngine;

// 토트의 방 그림(Assets/Resources/Thoth/...)을 넣으면 자동으로 알맞게 가져옴 (빌더를 안 돌려도 적용)
// - World/  : 맵 타일, 물건 (32px = 1칸, Point = 도트 그대로, 압축 없음)
// - UI/, Fresco/ : 퍼즐 화면 그림, 벽화 조각 (매끈하게 Bilinear, 100px = 1유닛, 압축 없음 -> 조각 테두리가 뭉개지지 않게)
// 아트팀 원본(Assets/image/Thoth)은 건드리지 않고 복사본만 씀
public class ThothArtImport : AssetPostprocessor
{
    private const string Root = "Assets/Resources/Thoth/";

    private void OnPreprocessTexture()
    {
        try
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.StartsWith(Root, StringComparison.Ordinal)) return;
            var imp = (TextureImporter)assetImporter;
            bool world = path.StartsWith(Root + "World/", StringComparison.Ordinal);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = world ? 32f : 100f;
            imp.filterMode = world ? FilterMode.Point : FilterMode.Bilinear;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.maxTextureSize = 2048;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
        }
        catch (Exception e)
        {
            Debug.LogError($"[토트 그림 가져오기] {assetPath}: {e.Message}");
        }
    }

    // 이미 들어와 있던 그림에 설정을 다시 적용 (빌더가 처음에 부름)
    public static void ReimportAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Thoth" });
        foreach (string g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            TextureImporter imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp == null || imp.textureType == TextureImporterType.Sprite && imp.spriteImportMode == SpriteImportMode.Single) continue;
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
        }
    }
}
