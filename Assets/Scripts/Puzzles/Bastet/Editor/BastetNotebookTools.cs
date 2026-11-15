#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

// [추가] 고고학 수첩 테스트 도구
// 1) 수첩 카드 그림 점검: 업적마다 그림이 연결됐는지, 파일을 실제로 읽을 수 있는지 Console에 출력
// 2) 수첩 기록 지우기: 테스트 플레이로 쌓인 유물 카드(PlayerPrefs)를 지움 (씬을 바로 Play로 테스트할 때 새 게임처럼)
public static class BastetNotebookTools
{
    [MenuItem("Tools/Bastet/수첩 카드 그림 점검", priority = 20)]
    private static void CheckArt()
    {
        var log = new StringBuilder();
        int ok = 0, noEntry = 0, broken = 0;
        foreach (AchievementDef a in AchievementData.All)
        {
            if (!NotebookCardArt.HasEntry(a.id)) { noEntry++; log.AppendLine($"- 그림 미연결: {a.id} ({a.relic})"); continue; }
            if (NotebookCardArt.Of(a.id) != null) ok++;
            else { broken++; log.AppendLine($"- 파일을 못 읽음: {a.id} ({a.relic})"); }
        }
        string head = $"그림 있음 {ok}개 / 미연결 {noEntry}개 / 못 읽음 {broken}개";
        Debug.Log($"[BastetNotebookTools] 수첩 카드 그림 점검: {head}\n{log}");
        EditorUtility.DisplayDialog("수첩 카드 그림 점검", head + "\n\n" + log, "확인");
    }

    [MenuItem("Tools/Bastet/수첩 기록 지우기 (테스트용)", priority = 21)]
    private static void ClearRecords()
    {
        if (!EditorUtility.DisplayDialog("수첩 기록 지우기", "지금까지 얻은 유물 카드와 메모를 모두 지울까요?\n(테스트 플레이로 쌓인 기록 정리용)", "지우기", "취소")) return;
        AchievementSystem.ClearForNewGame(); // Play 중이 아니어도 PlayerPrefs 기록을 지움
        NotebookNotes.Clear();
        Debug.Log("[BastetNotebookTools] 수첩 기록 지움 -> 다음 Play부터 모든 카드가 ??? 로 시작");
    }
}
#endif
