using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// 호루스의 방 타일맵 씬 만들기 (라의 방 Puzzle_Test_Ra_Tilemap처럼)
// 메뉴: PharaohCube -> Horus -> 호루스 타일맵 씬 만들기
// 1) Puzzle_Horus_Test를 복사해서 Puzzle_Horus_Tilemap을 만듦 (테스트 씬은 그대로 남음)
// 2) 임시 바닥/벽 사각형을 지우고 라의 방 타일로 바닥/벽을 깖 (HorusRoomMap, 작업 도면 그대로)
// 3) 플레이어, 따라오는 카메라, 어두운 밤 조명, 입구 전실/매의 회랑/별화로/석상을 놓음 (HorusRoomObjects)
// 4) 호루스 그림 연결 (HorusArtSetup), 빌드 세팅에 추가
// 퍼즐, 단서, 문 같은 기존 오브젝트와 그 연결은 그대로 씀 (좌표도 기획서 3.2 그대로)
public static class HorusRoomBuilder
{
    public const string SourcePath = "Assets/Scenes/Puzzle_Test/Puzzle_Horus_Test.unity";
    public const string ScenePath = "Assets/Scenes/Puzzle_Test/Puzzle_Horus_Tilemap.unity";

    [MenuItem("PharaohCube/Horus/호루스 타일맵 씬 만들기")]
    private static void Build()
    {
        if (!Confirm() || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        try
        {
            Scene scene = CopyAndOpen();
            if (!scene.IsValid()) return;
            Sprite square = HorusRoomSetup.TakeSprite("Floor");
            Sprite circle = HorusRoomSetup.TakeSprite("Pillar_NW");
            HorusRoomSetup.RemovePlaceholders();
            string layer = HorusRoomSetup.CreatePlayer(out int order);
            Tilemap ground = HorusRoomMap.Build(layer, order);
            HorusRoomSetup.SetupCamera(ground);
            HorusRoomSetup.SetupNightLight();
            HorusRoomObjects.Build(square, circle, layer);
            HorusArtSetup.Apply();
            HorusWorldPuzzles.Apply(); // H4, H5를 몸으로 푸는 퍼즐로 (그림 크기를 맞춘 뒤에)
            HorusRoomSetup.FinishSorting(layer);
            HorusRoomSetup.SetupObjectiveGuide();
            HorusRoomSetup.FixPuzzleFlags();
            EditorSceneManager.SaveScene(scene);
            HorusRoomSetup.AddToBuildSettings(ScenePath);
            EditorUtility.DisplayDialog("호루스 타일맵", $"{ScenePath}\n만들었어요. Play로 확인해 주세요.", "확인");
        }
        catch (Exception e)
        {
            Debug.LogError($"[HorusRoomBuilder] 만들기 오류: {e}");
        }
    }

    private static bool Confirm()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourcePath) == null)
        {
            EditorUtility.DisplayDialog("호루스 타일맵", $"원본 씬이 없어요: {SourcePath}", "확인");
            return false;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) return true;
        return EditorUtility.DisplayDialog("호루스 타일맵",
            "Puzzle_Horus_Tilemap 씬이 이미 있어요.\n테스트 씬에서 새로 만들어 덮어쓸까요? (타일맵 씬에서 손으로 고친 건 사라져요)", "덮어쓰기", "취소");
    }

    private static Scene CopyAndOpen()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) AssetDatabase.DeleteAsset(ScenePath);
        if (!AssetDatabase.CopyAsset(SourcePath, ScenePath))
        {
            Debug.LogError($"[HorusRoomBuilder] 씬 복사 실패: {SourcePath} -> {ScenePath}");
            return default(Scene);
        }
        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }
}
