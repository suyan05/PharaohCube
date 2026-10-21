using System;
using System.Collections.Generic;
using UnityEngine;

// 아이템 하나의 정보 (획득 팝업, 소지품 바, 지도, 수첩이 같이 씀)
[Serializable]
public class ItemDefinition
{
    [Tooltip("ItemInventory에 쓰는 ID (예: TOOL_BRUSH)")]
    public string id = "";
    public string displayName = "";
    [TextArea(1, 3)] public string description = "";
    [Tooltip("Common_Icon_* 128x128")]
    public Sprite icon;
    [Tooltip("켜면 히든 팝업 (금 테두리 + 빛살)")]
    public bool hidden;
}

// 게임 전체 아이템 목록 (에셋 1개)
// 만들기: Project 우클릭 -> Create -> PharaohCube -> Item Catalog
// Resources 폴더에 "ItemCatalog" 이름으로 두면 코드가 알아서 찾음
[CreateAssetMenu(fileName = "ItemCatalog", menuName = "PharaohCube/Item Catalog")]
public class ItemCatalog : ScriptableObject
{
    private const string ResourcePath = "ItemCatalog";
    private static ItemCatalog cached;

    [SerializeField] private List<ItemDefinition> items = DefaultItems();

    // Resources/ItemCatalog 에셋. 없으면 코드 기본 목록으로 임시 생성
    public static ItemCatalog Main
    {
        get
        {
            if (cached != null) return cached;

            cached = Resources.Load<ItemCatalog>(ResourcePath);
            if (cached == null)
            {
                Debug.LogWarning("[ItemCatalog] Resources/ItemCatalog 에셋이 없음 -> 코드 기본 목록 사용 (아이콘 없음)");
                cached = CreateInstance<ItemCatalog>();
            }
            return cached;
        }
    }

    public bool TryGet(string id, out ItemDefinition def)
    {
        def = null;
        if (string.IsNullOrEmpty(id) || items == null) return false;

        foreach (ItemDefinition item in items)
        {
            if (item == null || item.id != id) continue;
            def = item;
            return true;
        }
        return false;
    }

    // 목록에 없으면 ID를 이름으로 쓰는 임시 정보
    public ItemDefinition GetOrFallback(string id)
    {
        if (TryGet(id, out ItemDefinition def)) return def;

        Debug.LogWarning($"[ItemCatalog] '{id}'가 목록에 없음 -> 이름 대신 ID 표시");
        return new ItemDefinition { id = id, displayName = id };
    }

    // 기획서 [총정리] + 프롤로그 + 기존 라의 방 아이템
    private static List<ItemDefinition> DefaultItems()
    {
        return new List<ItemDefinition>
        {
            Make("TOOL_BRUSH", "발굴용 붓", "길게 눌러 모래를 털어낼 수 있다", false),
            Make("TOOL_LENS", "볼록렌즈", "상형문자에 비추면 읽을 수 있다", false),
            Make("COL_SCARAB", "청록 스카라브", "수집품. 쇠똥구리는 아침의 태양을 뜻한다", false),
            Make("PLATE_RA", "태양 형판", "큐브의 첫 면에 장착하라", false),
            Make("ITEM_RA_MAP", "라의 지도", "불에 비추면 숨은 것의 자리가 떠오른다 (M키)", false),
            Make("ITEM_RA_EMBER", "라의 불씨", "해가 진 뒤에도 꺼지지 않는 불. 횃불을 얻으면 함께 타오른다", true),
            Make("ITEM_SISTRUM", "시스트럼", "고양이 여신의 악기", false),
            Make("ITEM_TORCH", "횃불", "어둠을 밝힌다. 화로에서 다시 붙일 수 있다", false)
        };
    }

    private static ItemDefinition Make(string id, string name, string desc, bool hidden)
    {
        return new ItemDefinition { id = id, displayName = name, description = desc, hidden = hidden };
    }
}