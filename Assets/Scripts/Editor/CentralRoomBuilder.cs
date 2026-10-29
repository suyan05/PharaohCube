using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

// [임시] 중앙 홀 씬을 통째로 만드는 메뉴 (중앙 홀 맵, 아트가 오기 전까지)
// 메뉴: PharaohCube -> Prologue -> 임시 중앙 홀 씬 만들기
// -> Assets/Scenes/Prologue/CentralRoom_Temp.unity 를 새로 만들고 저장 + 빌드 순서도 맞춤
//
// 맵 18 x 12칸 (왼쪽 아래 = 0, 0). 아래 가운데 계단으로 들어옴 -> 가운데 큐브, 왼쪽 횃불 걸이, 오른쪽 화로, 북쪽 문(라의 신전)
// 그림: Assets/image/Hall/World/Hall_* (임시 도트). 진짜 그림이 오면 같은 이름으로 덮어쓰기
public static class CentralRoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Prologue/CentralRoom_Temp.unity";
    private const string PlayerPrefab = "Assets/Prefabs/Player/Player.prefab";
    private const string ManagersPrefab = "Assets/Prefabs/Common/Managers.prefab";
    private const string TileFolder = "Assets/Tile/Hall";
    private const int MapW = 18;
    private const int MapH = 12;

    private static string layer = "Default";
    private static int order;

    [MenuItem("PharaohCube/Prologue/임시 중앙 홀 씬 만들기")]
    private static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog("중앙 홀", "CentralRoom_Temp 씬이 이미 있어요. 새로 만들어서 덮어쓸까요?", "덮어쓰기", "취소")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        try
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform player = CreatePlayerAndManagers();
            Tilemap floor = CentralRoomMap.Build(TileFolder, MapW, MapH, layer, order);
            CreateCamera(floor);
            CreateLightAndEvents();
            CentralRoomObjects.Build(MapH, layer, order);

            PrologueAssetFinder.EnsureFolder("Assets/Scenes/Prologue");
            EditorSceneManager.SaveScene(scene, ScenePath);
            PrologueBuildOrder.Apply();
            Debug.Log($"[CentralRoomBuilder] {ScenePath} 완료 (플레이어 {(player != null ? "있음" : "없음")})");
        }
        catch (Exception e)
        {
            Debug.LogError($"[CentralRoomBuilder] 만들기 오류: {e}");
        }
    }

    // ================= 플레이어, Managers =================
    private static Transform CreatePlayerAndManagers()
    {
        Instantiate(ManagersPrefab);
        GameObject player = Instantiate(PlayerPrefab);
        if (player == null) return null;

        player.transform.position = new Vector3(9f, 0.8f, 0f);
        SpriteRenderer sr = player.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            layer = sr.sortingLayerName;
            order = sr.sortingOrder;
        }

        PlayerTorchTemp torch = player.GetComponent<PlayerTorchTemp>();
        if (torch == null) torch = player.AddComponent<PlayerTorchTemp>();
        var so = new SerializedObject(torch);
        so.FindProperty("startWithTorch").boolValue = false; // 여기서 횃불을 만든다
        so.ApplyModifiedPropertiesWithoutUndo();
        return player.transform;
    }

    private static GameObject Instantiate(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"[CentralRoomBuilder] 프리팹 없음: {path}");
            return null;
        }
        return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    }

    // ================= 카메라, 조명, 입력 =================
    private static void CreateCamera(Tilemap floor)
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        go.transform.position = new Vector3(9f, 3f, -10f);

        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.035f, 0.05f);
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        CameraFollow2D follow = go.AddComponent<CameraFollow2D>();
        var so = new SerializedObject(follow);
        so.FindProperty("boundsTilemap").objectReferenceValue = floor;
        SerializedProperty pad = so.FindProperty("bottomPadding");
        if (pad != null) pad.floatValue = 3f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 어두운 홀 (큐브와 화로 빛이 돋보이게)
    private static void CreateLightAndEvents()
    {
        var lightGo = new GameObject("Global Light 2D");
        Light2D light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = new Color(0.72f, 0.68f, 0.85f);
        light.intensity = 0.25f;

        var events = new GameObject("EventSystem");
        events.AddComponent<EventSystem>();
        events.AddComponent<InputSystemUIInputModule>();
    }
}
