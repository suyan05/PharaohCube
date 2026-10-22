#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 메뉴: Tools/Bastet/아이템 카탈로그에 바스테트 아이템 추가
// 팀 공용 Resources/ItemCatalog 에셋에 바스테트 아이템이 없으면 추가 (이미 있으면 건드리지 않음)
// - 획득 팝업(ItemPopup)이 이름·설명·아이콘을 여기서 읽음. 없으면 팝업에 ID가 그대로 나옴
// - 아이콘 그림(Common_Icon_*)이 프로젝트에 있으면 같이 연결, 없으면 비워 둠 (나중에 다시 눌러도 됨)
public static class BastetCatalogSetup
{
    private const string CatalogPath = "Assets/Resources/ItemCatalog.asset";

    private struct Def
    {
        public string id, name, desc, icon; public bool hidden;
        public Def(string i, string n, string d, string ic, bool h) { id = i; name = n; desc = d; icon = ic; hidden = h; }
    }

    // 이름·설명은 아트 요청서 5.1 아이콘 표 기준
    private static readonly Def[] Items =
    {
        new Def(BastetItemIds.Paw, "은 발자국 인장", "바스테트가 지나간 길을 기억한 증표", "Common_Icon_BastetPawSeal", false),
        new Def(BastetItemIds.Eye, "호박 눈", "뒤를 돌아보는 고양이의 눈빛이 담긴 구슬", "Common_Icon_BastetEyeBead", false),
        new Def(BastetItemIds.Necklace, "목걸이 구슬", "좌우가 꼭 맞는 바스테트의 목걸이 구슬", "Common_Icon_BastetNecklaceBead", false),
        new Def(BastetItemIds.Claw, "발톱 인장", "발톱 자국 세 줄이 새겨진 인장", "Common_Icon_BastetClawSeal", false),
        new Def(BastetItemIds.Plate, "고양이 형판", "큐브의 두 번째 면에 끼우는 바스테트의 형판", "Bastet_UI_Plate", false),
        new Def(BastetItemIds.ReedPen, "토트의 갈대 펜", "공물 기록함에서 나온 서기관의 펜. 토트의 방 통로를 연다", "Common_Icon_ReedPen", false),
        new Def(BastetItemIds.Vessel, "바스테트의 그릇", "불을 담는 고양이 귀 그릇. 횃불이 두 배 밝고 두 배 오래 탄다", "Common_Icon_BastetVessel", true),
    };

    [MenuItem("Tools/Bastet/아이템 카탈로그에 바스테트 아이템 추가")]
    public static void Run()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
        if (catalog == null)
        {
            EditorUtility.DisplayDialog("바스테트 아이템", $"{CatalogPath} 가 없습니다.\n팀 main을 받은 뒤 다시 실행해 주세요.", "확인");
            return;
        }

        var so = new SerializedObject(catalog);
        SerializedProperty list = so.FindProperty("items");
        var have = new HashSet<string>();
        for (int i = 0; i < list.arraySize; i++) have.Add(list.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue);

        int added = 0, iconLinked = 0;
        var log = new List<string>();
        foreach (Def d in Items)
        {
            if (have.Contains(d.id)) { log.Add($"{d.id} 이미 있음"); continue; }
            list.arraySize++;
            SerializedProperty e = list.GetArrayElementAtIndex(list.arraySize - 1);
            e.FindPropertyRelative("id").stringValue = d.id;
            e.FindPropertyRelative("displayName").stringValue = d.name;
            e.FindPropertyRelative("description").stringValue = d.desc;
            e.FindPropertyRelative("hidden").boolValue = d.hidden;
            Sprite icon = FindSprite(d.icon);
            e.FindPropertyRelative("icon").objectReferenceValue = icon;
            if (icon != null) iconLinked++;
            added++;
            log.Add($"{d.id} 추가 ({d.name}{(icon != null ? ", 아이콘 연결" : ", 아이콘 없음")})");
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log($"[BastetCatalogSetup] {added}개 추가, 아이콘 {iconLinked}개 연결\n- " + string.Join("\n- ", log), catalog);
    }

    // 파일 이름이 정확히 같은 Sprite 찾기 (없으면 null)
    private static Sprite FindSprite(string fileName)
    {
        foreach (string guid in AssetDatabase.FindAssets(fileName + " t:Sprite"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == fileName) return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }
}
#endif
