using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// RaPolishSetup 도우미 (라의 성소 복선, 아이콘, 조명 설정)
public static class RaPolishExtras
{
    private const string CatalogPath = "Assets/Resources/ItemCatalog.asset";
    private const string Foreshadow = "나의 불은 해가 진 뒤에도 배 안에서 꺼지지 않는다. 어둠 속에서 그것을 찾거라.";
    private const int NightPortrait = 2; // RaPortrait.Night

    private static readonly Dictionary<string, string> Icons = new Dictionary<string, string>
    {
        { "ITEM_RA_EMBER", "Assets/image/Common/Icon/Common_Icon_RaEmber.png" },
        { "ITEM_TORCH", "Assets/image/Common/Icon/Common_Icon_Torch.png" }
    };

    // ================= 라의 성소 밤 대사에 복선 한 줄 =================
    public static void AddShrineForeshadow()
    {
        RaShrine shrine = Object.FindAnyObjectByType<RaShrine>(FindObjectsInactive.Include);
        if (shrine == null) return;

        var so = new SerializedObject(shrine);
        SerializedProperty lines = so.FindProperty("nightLines");
        if (lines == null || !lines.isArray || HasText(lines, Foreshadow)) return;

        Undo.RecordObject(shrine, "라의 성소 복선");
        int i = lines.arraySize;
        lines.InsertArrayElementAtIndex(i);
        SerializedProperty line = lines.GetArrayElementAtIndex(i);
        line.FindPropertyRelative("portrait").enumValueIndex = NightPortrait;
        line.FindPropertyRelative("text").stringValue = Foreshadow;
        so.ApplyModifiedProperties();
        Debug.Log("[RaPolishSetup] 라의 성소 밤 대사에 불씨 복선 추가");
    }

    private static bool HasText(SerializedProperty lines, string text)
    {
        for (int i = 0; i < lines.arraySize; i++)
        {
            SerializedProperty t = lines.GetArrayElementAtIndex(i).FindPropertyRelative("text");
            if (t != null && t.stringValue == text) return true;
        }
        return false;
    }

    // ================= 아이템 아이콘 (비어 있을 때만 채움) =================
    public static void LinkIcons()
    {
        ItemCatalog catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
        if (catalog == null) return;

        var so = new SerializedObject(catalog);
        SerializedProperty items = so.FindProperty("items");
        for (int i = 0; i < items.arraySize; i++)
        {
            SerializedProperty item = items.GetArrayElementAtIndex(i);
            if (!Icons.TryGetValue(item.FindPropertyRelative("id").stringValue, out string path)) continue;
            SerializedProperty icon = item.FindPropertyRelative("icon");
            if (icon.objectReferenceValue != null) continue;
            icon.objectReferenceValue = IconSprite(path);
        }
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    private static Sprite IconSprite(string path)
    {
        if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return null;
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ================= 조명 =================
    // 밤이 되면 RaNightAtmosphere가 모든 빛을 푸르게 바꾸는데, 배 안 불씨는 주황 그대로 두기
    public static void ExcludeFromNight(Light2D light)
    {
        RaNightAtmosphere night = Object.FindAnyObjectByType<RaNightAtmosphere>(FindObjectsInactive.Include);
        if (night == null || light == null) return;

        var so = new SerializedObject(night);
        SerializedProperty list = so.FindProperty("excludeLights");
        if (list == null) return;
        Undo.RecordObject(night, "불씨 빛 밤 제외");
        int i = list.arraySize;
        list.InsertArrayElementAtIndex(i);
        list.GetArrayElementAtIndex(i).objectReferenceValue = light;
        so.ApplyModifiedProperties();
    }

    // 새로 만든 빛이 벽 횃불 빛과 같은 정렬 레이어를 비추도록 (안 맞추면 바닥이 안 밝아질 수 있음)
    public static void CopyLightLayers(Light2D target)
    {
        WallTorch torch = Object.FindAnyObjectByType<WallTorch>(FindObjectsInactive.Include);
        Light2D source = torch != null ? torch.GetComponentInChildren<Light2D>(true) : null;
        if (source == null || target == null) return;

        SerializedProperty from = new SerializedObject(source).FindProperty("m_ApplyToSortingLayers");
        var toSo = new SerializedObject(target);
        SerializedProperty to = toSo.FindProperty("m_ApplyToSortingLayers");
        if (from == null || to == null) return;

        to.arraySize = from.arraySize;
        for (int i = 0; i < from.arraySize; i++) to.GetArrayElementAtIndex(i).intValue = from.GetArrayElementAtIndex(i).intValue;
        toSo.ApplyModifiedPropertiesWithoutUndo();
    }
}
