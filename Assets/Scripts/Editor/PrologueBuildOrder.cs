using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 빌드 순서 맞추기: 발굴 현장 -> 스핑크스의 꿈 -> 임시 중앙 홀 -> 라의 방 -> (나머지는 원래 순서 그대로)
// 메뉴: PharaohCube -> Prologue -> 빌드 순서 맞추기 (임시 중앙 홀 만들기 때도 자동으로 부름)
// 씬 이동(SceneFlow)은 Build Profiles의 Scene List에 있는 씬만 불러올 수 있어서 필요함
public static class PrologueBuildOrder
{
    private static readonly string[] Front =
    {
        "Assets/Scenes/Prologue/Prologue_Dig.unity",
        "Assets/Scenes/Prologue/Prologue_Sphinx.unity",
        CentralRoomBuilder.ScenePath,
        "Assets/Scenes/Puzzle_Test/Puzzle_Test_Ra_Tilemap.unity"
    };

    [MenuItem("PharaohCube/Prologue/빌드 순서 맞추기")]
    public static void Apply()
    {
        var result = new List<EditorBuildSettingsScene>();
        foreach (string path in Front)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) result.Add(new EditorBuildSettingsScene(path, true));
            else Debug.LogWarning($"[PrologueBuildOrder] 씬 없음 (건너뜀): {path}");
        }

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (System.Array.IndexOf(Front, scene.path) < 0) result.Add(scene);
        }

        EditorBuildSettings.scenes = result.ToArray();
        Debug.Log("[PrologueBuildOrder] 빌드 순서: 발굴 현장 -> 스핑크스 -> 중앙 홀(임시) -> 라의 방 -> 나머지");
    }
}
