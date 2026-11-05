using UnityEditor;
using UnityEngine;

// ItemCatalog에 호루스 아이템 넣기 (HorusArtSetup이 부름)
// - 같은 ID가 이미 있으면 아이콘만 바꾸고, 이름/설명은 비어 있을 때만 채움 (다른 사람이 고친 글은 그대로)
// - 없으면 맨 끝에 새로 추가
public static class HorusItemSetup
{
    private const string CatalogPath = "Assets/Resources/ItemCatalog.asset";

    private static readonly string[,] Items =
    {
        // ID, 이름, 설명, 아이콘
        { HorusItemIds.Feather, "매의 깃털", "깃털로 먼지를 털면 반쪽 날개가 드러난다.", "Object/Object_05" },
        { HorusItemIds.StarFrag1, "별 조각", "성도판의 꺼진 별에 끼우면 빛이 들어온다.", "Object/Object_04" },
        { HorusItemIds.StarFrag2, "별 조각", "성도판의 꺼진 별에 끼우면 빛이 들어온다.", "Object/Object_04" },
        { HorusItemIds.StarFrag3, "별 조각", "성도판의 꺼진 별에 끼우면 빛이 들어온다.", "Object/Object_04" },
        { HorusItemIds.SilverThread, "은빛 실", "은빛 실은 찢긴 것을 다시 잇는다.", "Object/Object_03" },
        { HorusItemIds.MoonEye, "달의 눈", "장착대에 끼우면 이후 긋는 선이 은빛이 된다.", "Object/Object_02" },
        { HorusItemIds.SealFalcon, "매 인장", "세트의 봉인은 매의 인장만이 연다.", "Object/Object_01" },
        { HorusItemIds.PlateHorus, "하늘 형판", "하늘의 형판을 큐브의 면에 장착하라.", "H5/H5_08" }
    };

    public static int Apply()
    {
        ItemCatalog catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
        if (catalog == null)
        {
            Debug.LogWarning($"[HorusItemSetup] {CatalogPath} 가 없어서 아이템은 건너뜀");
            return 0;
        }
        var so = new SerializedObject(catalog);
        SerializedProperty items = HorusArtSetup.Prop(so, "items");
        if (items == null) return 0;
        for (int i = 0; i < Items.GetLength(0); i++) Upsert(items, i);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        return Items.GetLength(0);
    }

    private static void Upsert(SerializedProperty items, int row)
    {
        SerializedProperty item = FindItem(items, Items[row, 0]);
        if (item == null)
        {
            items.arraySize++;
            item = items.GetArrayElementAtIndex(items.arraySize - 1);
            item.FindPropertyRelative("id").stringValue = Items[row, 0];
            item.FindPropertyRelative("displayName").stringValue = "";
            item.FindPropertyRelative("description").stringValue = "";
            item.FindPropertyRelative("hidden").boolValue = false;
        }
        FillIfEmpty(item.FindPropertyRelative("displayName"), Items[row, 1]);
        FillIfEmpty(item.FindPropertyRelative("description"), Items[row, 2]);
        Sprite icon = HorusArtSetup.Art(Items[row, 3]);
        if (icon != null) item.FindPropertyRelative("icon").objectReferenceValue = icon;
    }

    private static SerializedProperty FindItem(SerializedProperty items, string id)
    {
        for (int i = 0; i < items.arraySize; i++)
        {
            SerializedProperty e = items.GetArrayElementAtIndex(i);
            if (e.FindPropertyRelative("id").stringValue == id) return e;
        }
        return null;
    }

    private static void FillIfEmpty(SerializedProperty p, string value)
    {
        if (p != null && string.IsNullOrEmpty(p.stringValue)) p.stringValue = value;
    }
}
