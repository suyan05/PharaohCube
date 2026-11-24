using UnityEditor;
using UnityEngine;

// ItemCatalog에 토트의 방 아이템 넣기 (ThothRoomBuilder가 부름)
// - 같은 ID가 이미 있으면 이름/설명/아이콘은 비어 있을 때만 채움 (다른 사람이 고친 것은 그대로)
// - 없으면 맨 끝에 새로 추가
// [확인 필요] PLATE_THOTH, ITEM_THOTH_TABLET은 다른 담당자 목록(치트, 수첩)에도 있는 ID -> 이미 있으면 그대로 둠
public static class ThothItemSetup
{
    private const string CatalogPath = "Assets/Resources/ItemCatalog.asset";
    private const string Art = "Assets/Resources/Thoth/";

    private static readonly string[,] Items =
    {
        // ID, 이름, 설명, 아이콘(Resources/Thoth/ 밑), 숨은 유물(1)
        { ThothIds.Notes, "토트의 여백 메모", "글자와 뜻: 따오기 = 토트, 저울 = 진실, 갈대 펜 = 기록, 달 = 달, 눈 = 눈. 글은 새가 바라보는 쪽에서 시작된다.", "UI/UI_Notes", "0" },
        { ThothIds.PieceItem("A"), "벽화 조각", "벽화 '글자를 만든 토트'의 조각. 서쪽 서고의 벽화 앞에서 맞추자.", "UI/UI_PieceIcon_A", "0" },
        { ThothIds.PieceItem("B"), "벽화 조각", "벽화 '달과의 내기'의 조각. 동쪽 서고의 벽화 앞에서 맞추자.", "UI/UI_PieceIcon_B", "0" },
        { ThothIds.PieceItem("C"), "벽화 조각", "벽화 '되찾은 눈'의 조각. 북쪽 벽감의 벽화 앞에서 맞추자.", "UI/UI_PieceIcon_C", "0" },
        { ThothIds.MoonFrag(1), "달 조각 (초승달)", "달 모양 금 조각. 어딘가의 달 모양 홈에 꼭 맞을 것 같다.", "World/TH_MoonFrag_1", "0" },
        { ThothIds.MoonFrag(2), "달 조각 (반달)", "달 모양 금 조각. 어딘가의 달 모양 홈에 꼭 맞을 것 같다.", "World/TH_MoonFrag_2", "0" },
        { ThothIds.MoonFrag(3), "달 조각 (보름달)", "달 모양 금 조각. 어딘가의 달 모양 홈에 꼭 맞을 것 같다.", "World/TH_MoonFrag_3", "0" },
        { ThothIds.Plate, "지혜의 형판", "토트의 지혜가 새겨진 형판. 큐브의 세 번째 면에 끼울 수 있다.", "UI/UI_Template", "0" },
        { ThothIds.Tablet, "토트의 석판", "토트의 책 첫 줄이 새겨진 석판. 심판의 저울이 세 번 기울어도, 단 한 번 기록을 고쳐 쓸 수 있다", "UI/UI_Notes", "1" }
    };

    public static int Apply()
    {
        ItemCatalog catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
        if (catalog == null)
        {
            Debug.LogWarning($"[ThothItemSetup] {CatalogPath} 가 없어서 아이템은 건너뜀");
            return 0;
        }
        var so = new SerializedObject(catalog);
        SerializedProperty items = so.FindProperty("items");
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
            item.FindPropertyRelative("icon").objectReferenceValue = null;
            item.FindPropertyRelative("hidden").boolValue = Items[row, 4] == "1";
        }
        FillIfEmpty(item.FindPropertyRelative("displayName"), Items[row, 1]);
        FillIfEmpty(item.FindPropertyRelative("description"), Items[row, 2]);
        SerializedProperty icon = item.FindPropertyRelative("icon");
        if (icon != null && icon.objectReferenceValue == null)
        {
            icon.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Art + Items[row, 3] + ".png");
        }
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
