#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

// [추가] 메뉴: Tools/Bastet/진행 상태 검사 (Play 중에 누르기)
// 막혔을 때 지금 상태를 Console에 한 번에 출력: 매니저, 플래그, 소지품, 퍼즐 풀림, 보상 담당, 봉인석, 석상 잠금
public static class BastetDebugReport
{
    private static readonly string[] Items = { "ITEM_TORCH", "ITEM_BASTET_PAW", "ITEM_BASTET_EYE", "ITEM_BASTET_NECKLACE", "ITEM_BASTET_CLAW", "PLATE_BASTET" };
    private static readonly string[] Flags = { "F_BASTET_B1_CLEAR", "F_BASTET_B2_CLEAR", "F_BASTET_B3_CLEAR", "F_BASTET_B4_CLEAR", "F_BASTET_MAIN_DONE",
        "F_BASTET_GATE_Gate_Room2_OPEN", "F_BASTET_GATE_Gate_Room3_OPEN", "F_BASTET_GATE_Gate_Room4_OPEN", "F_BASTET_GATE_Gate_T_OPEN" };

    [MenuItem("Tools/Bastet/진행 상태 검사 (Play 중에 누르기)", priority = 6)]
    private static void Run()
    {
        var sb = new StringBuilder("[BastetDebugReport] ===== 바스테트 진행 상태 =====\n");
        if (!Application.isPlaying) sb.AppendLine("※ Play 중이 아님 (막힌 순간에 Play 중에 누르면 정확함)");

        sb.AppendLine($"GameManager: {(GameManager.Instance != null ? GameManager.Instance.name : "없음!")} (씬에 {Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length}개)");
        sb.AppendLine($"ItemInventory: {(ItemInventory.Instance != null ? ItemInventory.Instance.name : "없음!")} (씬에 {Object.FindObjectsByType<ItemInventory>(FindObjectsSortMode.None).Length}개)");

        sb.Append("소지품: ");
        foreach (string id in Items) sb.Append($"{id}={(ItemInventory.Instance != null && ItemInventory.Instance.Has(id) ? "O" : "x")}  ");
        sb.AppendLine();
        sb.Append("플래그: ");
        foreach (string f in Flags) sb.Append($"{f.Replace("F_BASTET_", "")}={(GameManager.Instance != null && GameManager.Instance.HasFlag(f) ? "O" : "x")}  ");
        sb.AppendLine();

        foreach (BastetRoomProgress p in Object.FindObjectsByType<BastetRoomProgress>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            sb.AppendLine($"보상 담당 '{p.name}': 켜짐={p.isActiveAndEnabled}");
            var so = new SerializedObject(p);
            SerializedProperty arr = so.FindProperty("entries");
            for (int i = 0; i < arr.arraySize; i++)
            {
                SerializedProperty e = arr.GetArrayElementAtIndex(i);
                var pz = e.FindPropertyRelative("puzzle").objectReferenceValue as PuzzleBase;
                sb.AppendLine($"   {i + 1}. {(pz != null ? pz.name : "(비어 있음!)")} 풀림={(pz != null && pz.IsCleared ? "O" : "x")} 켜짐={(pz != null && pz.isActiveAndEnabled)} -> 보상 {e.FindPropertyRelative("rewardItemId").stringValue}");
            }
        }
        if (Object.FindObjectsByType<BastetRoomProgress>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0)
            sb.AppendLine("보상 담당(BastetRoomProgress)이 씬에 없음! -> Tools/Bastet/실전 방 세팅");

        foreach (BastetRoomGate g in Object.FindObjectsByType<BastetRoomGate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(g);
            sb.AppendLine($"봉인석 '{g.name}': 켜짐={g.isActiveAndEnabled} 열림={(g.IsOpen ? "O" : "x")} 필요={so.FindProperty("requiredItemId").stringValue}");
        }
        foreach (BastetPuzzleStarter st in Object.FindObjectsByType<BastetPuzzleStarter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(st);
            sb.AppendLine($"석상 '{st.name}': 켜짐={st.isActiveAndEnabled} 잠금 아이템='{so.FindProperty("requiredItemId").stringValue}' 퍼즐={(st.Target != null ? st.Target.name : "없음!")} 풀림={(st.Target != null && st.Target.IsCleared ? "O" : "x")}");
        }
        Debug.Log(sb.ToString());
        // [추가] 콘솔은 두 줄만 보이므로 전체 내용을 클립보드에 복사 -> 채팅에 Ctrl+V
        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        EditorUtility.DisplayDialog("진행 상태 검사", "검사 내용을 복사했습니다.\n채팅창에 Ctrl+V로 붙여 넣어 주세요.", "확인");
    }
}
#endif
