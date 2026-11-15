using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// [추가] 바스테트 씬이 열릴 때 숨은 방(향실) 입구를 자동으로 달아 줌
// - 씬에 BastetIncenseVault가 이미 있으면(메뉴로 저장해 둔 경우) 아무것도 안 함
// - 없으면 목걸이의 방(Room3_Necklace) 위쪽 벽 오른쪽에 만듦
// (사랑님의 HiddenVaultSpawner와 같은 방식, 그 파일은 안 고침)
public static class BastetVaultSpawner
{
    public const string RoomName = "Room3_Necklace";
    public const string VaultName = "OBJ_BASTET_IncenseVault";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Attach();

    private static void Attach()
    {
        try
        {
            if (UnityEngine.Object.FindFirstObjectByType<BastetIncenseVault>(FindObjectsInactive.Include) != null) return;
            BastetRoomArea room = FindRoom();
            if (room == null) return; // 바스테트 씬이 아님
            var go = new GameObject(VaultName);
            go.transform.position = DefaultPosition(room);
            go.AddComponent<BastetIncenseVault>();
            Debug.Log($"[바스테트 향실] 입구 자동 생성: {RoomName} 위쪽 벽 {go.transform.position}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[바스테트 향실] 입구 달기 오류: {e}");
        }
    }

    public static BastetRoomArea FindRoom()
    {
        foreach (BastetRoomArea r in UnityEngine.Object.FindObjectsByType<BastetRoomArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (r.name == RoomName) return r;
        return null;
    }

    // 방 위쪽 벽(바닥 바로 위) 오른쪽 1/4 지점 (가운데 비석과 겹치지 않게)
    public static Vector3 DefaultPosition(BastetRoomArea room)
    {
        Vector2 half = room.Size * 0.5f;
        return room.transform.position + new Vector3(half.x * 0.5f, half.y + 0.6f, 0f);
    }
}
