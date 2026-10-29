using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 아이템 아이콘 자동 연결
// 메뉴: PharaohCube -> 아이템 아이콘 자동 연결
// - Resources/ItemCatalog 에셋의 각 아이템 Icon 칸에, 아래 표의 파일 이름과 같은 그림을 찾아 넣음
// - 그림이 Sprite가 아니면 Sprite (2D and UI)로 바꿔줌 (아이콘은 일러스트라 Bilinear)
// - 그림을 못 찾은 아이템은 건너뛰고 Console에 목록을 찍음 (나중에 그림 넣고 다시 누르면 됨)
public static class ItemIconLinker
{
    private const string CatalogPath = "Assets/Resources/ItemCatalog.asset";

    // 아이템 ID -> 아이콘 파일 이름 (아트 요청서 4.4 / 3.2 / 2.2)
    private static readonly Dictionary<string, string> IconNames = new Dictionary<string, string>
    {
        { "TOOL_BRUSH", "Common_Icon_Brush" },
        { "TOOL_LENS", "Common_Icon_LensConvex" },
        { "COL_SCARAB", "Common_Icon_Scarab" },
        { "ITEM_RA_MAP", "Common_Icon_RaMap" },
        { "ITEM_RA_EMBER", "Common_Icon_RaEmber" },
        { "ITEM_SISTRUM", "Common_Icon_Sistrum" },
        { "ITEM_TORCH", "Common_Icon_Torch" },
        { "PLATE_RA", "UI_Item_PlateRa" }
    };

    [MenuItem("PharaohCube/아이템 아이콘 자동 연결")]
    private static void Link()
    {
        try
        {
            ItemCatalog catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            if (catalog == null)
            {
                EditorUtility.DisplayDialog("아이콘 연결", $"{CatalogPath} 가 없어요.", "확인");
                return;
            }

            var so = new SerializedObject(catalog);
            int linked = LinkAll(so.FindProperty("items"), out List<string> missing);
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log($"[ItemIconLinker] 아이콘 {linked}개 연결 완료");
            if (missing.Count > 0) Debug.LogWarning($"[ItemIconLinker] 그림 없음 (건너뜀): {string.Join(", ", missing)}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ItemIconLinker] 아이콘 연결 오류: {e}");
        }
    }

    private static int LinkAll(SerializedProperty items, out List<string> missing)
    {
        missing = new List<string>();
        int linked = 0;
        for (int i = 0; i < items.arraySize; i++)
        {
            SerializedProperty item = items.GetArrayElementAtIndex(i);
            string id = item.FindPropertyRelative("id").stringValue;
            if (!IconNames.TryGetValue(id, out string file)) continue;

            Sprite icon = FindIcon(file);
            if (icon == null)
            {
                missing.Add($"{id}({file})");
                continue;
            }
            item.FindPropertyRelative("icon").objectReferenceValue = icon;
            linked++;
        }
        return linked;
    }

    // Assets 전체에서 파일 이름이 정확히 같은 PNG를 찾음 (Sprite가 아니면 Sprite로 바꿈)
    private static Sprite FindIcon(string fileName)
    {
        foreach (string guid in AssetDatabase.FindAssets(fileName + " t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) != fileName) continue;

            MakeSprite(path);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }

    private static void MakeSprite(string path)
    {
        if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
        if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }
}