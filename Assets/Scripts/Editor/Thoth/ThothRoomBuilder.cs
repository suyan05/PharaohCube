using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// 토트의 방 (달의 서고) 씬 통째로 만들기
// 메뉴: PharaohCube -> Thoth -> 토트의 방 씬 만들기
// -> Assets/Scenes/Puzzle_Test/Puzzle_Thoth_Tilemap.unity 를 새로 만들고 빌드 목록에 추가
// 1) Managers + 플레이어 + 카메라(맵 따라가기) + 달빛 전역 조명 + EventSystem
// 2) 맵 타일 (ThothRoomMap) -> 물건, 퍼즐, 연출 감독 (ThothRoomObjects)
// 3) ItemCatalog에 토트 아이템 추가 (ThothItemSetup)
// 예전 테스트 씬(Puzzle_Test_Thoth)과 그 스크립트는 그대로 둠
public static class ThothRoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Puzzle_Test/Puzzle_Thoth_Tilemap.unity";
    private const string PlayerPrefab = "Assets/Prefabs/Player/Player.prefab";
    private const string ManagersPrefab = "Assets/Prefabs/Common/Managers.prefab";

    [MenuItem("PharaohCube/Thoth/토트의 방 씬 만들기")]
    private static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog("토트의 방", "Puzzle_Thoth_Tilemap 씬이 이미 있어요. 새로 만들어서 덮어쓸까요?\n(씬에서 손으로 고친 건 사라져요)", "덮어쓰기", "취소")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        try
        {
            ThothArtImport.ReimportAll();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string layer = CreatePlayerAndManagers();
            Tilemap ground = ThothRoomMap.Build(layer, 0, out Tilemap wall);
            CreateCamera(wall);
            CreateLightAndEvents();
            ThothRoomObjects.Build(layer);
            int items = ThothItemSetup.Apply();
            EditorSceneManager.SaveScene(scene, ScenePath);
            HorusRoomSetup.AddToBuildSettings(ScenePath);
            EditorUtility.DisplayDialog("토트의 방", $"{ScenePath}\n만들었어요 (아이템 {items}개 등록). Play로 확인해 주세요.", "확인");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ThothRoomBuilder] 만들기 오류: {e}");
        }
    }

    // 플레이어의 정렬 레이어를 돌려줌 (타일맵, 물건을 같은 레이어에 맞춤)
    private static string CreatePlayerAndManagers()
    {
        Instantiate(ManagersPrefab);
        GameObject player = Instantiate(PlayerPrefab);
        if (player == null) return "Default";
        player.transform.position = ThothRoomObjects.PlayerStart;
        PlayerTorchTemp torch = player.GetComponent<PlayerTorchTemp>();
        if (torch != null)
        {
            var so = new SerializedObject(torch);
            SerializedProperty start = so.FindProperty("startWithTorch");
            if (start != null) start.boolValue = false; // 달빛 서고라 횃불 없이 시작
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        SpriteRenderer sr = player.GetComponentInChildren<SpriteRenderer>();
        return sr != null ? sr.sortingLayerName : "Default";
    }

    private static GameObject Instantiate(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"[ThothRoomBuilder] 프리팹 없음: {path}");
            return null;
        }
        return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    }

    private static void CreateCamera(Tilemap bounds)
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        go.transform.position = new Vector3(ThothRoomObjects.PlayerStart.x, ThothRoomObjects.PlayerStart.y + 2f, -10f);
        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.04f, 0.07f);
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        go.AddComponent<AudioListener>();
        CameraFollow2D follow = go.AddComponent<CameraFollow2D>();
        var so = new SerializedObject(follow);
        so.FindProperty("boundsTilemap").objectReferenceValue = bounds;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 달빛 서고: 푸르스름하고 어둑하게 (천창 달빛과 금빛 벽화가 돋보이게)
    private static void CreateLightAndEvents()
    {
        var lightGo = new GameObject("Global Light 2D");
        Light2D light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = new Color(0.72f, 0.8f, 1f);
        light.intensity = 0.62f;

        var events = new GameObject("EventSystem");
        events.AddComponent<EventSystem>();
        events.AddComponent<InputSystemUIInputModule>();
    }
}
