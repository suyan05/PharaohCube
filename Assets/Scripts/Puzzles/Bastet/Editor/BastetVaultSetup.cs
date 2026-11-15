#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// [추가] 메뉴: Tools/Bastet/숨은 방 입구 배치
// - 목걸이의 방(Room3_Necklace) 위쪽 벽에 향실 입구(BastetIncenseVault)를 씬에 저장해 둠
// - 저장해 두면 Scene 뷰에서 위치를 직접 옮길 수 있음 (안 해도 게임이 열릴 때 자동으로 만들어짐)
// - 이미 있으면 선택만 해 줌
public static class BastetVaultSetup
{
    [MenuItem("Tools/Bastet/숨은 방 입구 배치", priority = 22)]
    private static void Place()
    {
        BastetIncenseVault existing = Object.FindFirstObjectByType<BastetIncenseVault>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing.gameObject);
            EditorUtility.DisplayDialog("숨은 방 입구", $"이미 있습니다: {existing.name}\nScene 뷰에서 위치를 옮긴 뒤 Ctrl+S", "확인");
            return;
        }

        BastetRoomArea room = BastetVaultSpawner.FindRoom();
        if (room == null)
        {
            EditorUtility.DisplayDialog("숨은 방 입구", $"씬에 {BastetVaultSpawner.RoomName} 가 없습니다.\n바스테트 씬을 열고 다시 실행해 주세요.", "확인");
            return;
        }

        var go = new GameObject(BastetVaultSpawner.VaultName);
        Undo.RegisterCreatedObjectUndo(go, "숨은 방 입구");
        go.transform.SetParent(room.transform, true);
        go.transform.position = BastetVaultSpawner.DefaultPosition(room);
        go.AddComponent<BastetIncenseVault>();
        Selection.activeGameObject = go;
        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"[BastetVaultSetup] 향실 입구 배치: {room.name} 위쪽 벽 {go.transform.position}", go);
        EditorUtility.DisplayDialog("숨은 방 입구",
            $"{room.name} 위쪽 벽에 입구를 놓았습니다.\n\n- 노란 테두리(기즈모)가 입구 자리\n- 비석을 모두 해독해야 게임에서 보임\n- 위치를 옮긴 뒤 Ctrl+S", "확인");
    }
}
#endif
