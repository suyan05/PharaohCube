using System.Collections.Generic;
using System.Text;
using UnityEditor;

// [추가] 빌드 목록에서 임시 중앙 홀(CentralRoom_Temp) 자리에 진짜 중앙 홀(CentralRoom_Tilemap)을 넣음
// - 스핑크스 다음 번호 = 진짜 중앙 홀 (SceneFlow.LoadNext가 번호 순서로 감)
// - 임시 홀은 목록 끝에 꺼 둔 채로 남김 (지우지 않음)
public static class CentralRoomTilemapBuildList
{
    private const string TempPath = "Assets/Scenes/Prologue/CentralRoom_Temp.unity";

    [MenuItem("PharaohCube/Hall/빌드 목록에 진짜 중앙 홀 넣기")]
    private static void Menu()
    {
        var log = new StringBuilder();
        Apply(log);
        EditorUtility.DisplayDialog("빌드 목록", log.ToString(), "확인");
    }

    public static void Apply(StringBuilder log)
    {
        string main = CentralRoomTilemapSetup.ScenePath;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(main) == null)
        {
            log.AppendLine($"- 경고: {main} 씬이 없음");
            return;
        }
        var result = new List<EditorBuildSettingsScene>();
        EditorBuildSettingsScene temp = null;
        bool placed = false;
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            if (s.path == main) continue;
            if (s.path == TempPath)
            {
                temp = s;
                result.Add(new EditorBuildSettingsScene(main, true));
                placed = true;
                continue;
            }
            result.Add(s);
        }
        if (!placed) result.Insert(System.Math.Min(4, result.Count), new EditorBuildSettingsScene(main, true));
        if (temp != null) result.Add(new EditorBuildSettingsScene(TempPath, false));
        EditorBuildSettings.scenes = result.ToArray();
        log.AppendLine("- 빌드 목록: 스핑크스 다음 = CentralRoom_Tilemap (임시 홀은 끝에 꺼 둠)");
    }
}
