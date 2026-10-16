using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 메인 저울 실패 처벌(AnubisJudgmentPenalty)을 메뉴 한 번으로 설정 (에디터 전용)
// - AnubisJudgmentPenalty 오브젝트 생성 + 메인 제단(Altar_A5_Main) 연결
// - 되돌아갈 위치 Respawn_A1 생성 (지금 플레이어 시작 위치)
// - 메인 저울(AnubisPuzzleManager2D)의 Penalty 칸에 연결
// - 서브 퍼즐 변형 관리자(AnubisVariantManager) 추가
public static class AnubisPenaltySetup
{
    private const string PenaltyObjectName = "AnubisJudgmentPenalty";
    private const string RespawnName = "Respawn_A1";
    private const string MainAltarName = "Altar_A5_Main";

    [MenuItem("Tools/Anubis/진행 설정/실전: 심판 실패 처벌 적용")]
    private static void Apply()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (!EditorUtility.DisplayDialog("심판 실패 처벌",
                "메인 저울에서 실패하면 씬을 다시 불러오지 않고\n보상 4개 회수 + 서브 퍼즐을 A1부터 다시 잠그도록 설정합니다.\n\n현재 씬: " + scene,
                "적용", "취소"))
        {
            return;
        }

        var scale = Object.FindFirstObjectByType<AnubisPuzzleManager2D>(FindObjectsInactive.Include);
        if (scale == null)
        {
            Debug.LogError("[AnubisPenalty] 씬에 AnubisPuzzleManager2D(메인 저울)가 없음");
            return;
        }

        AnubisJudgmentPenalty penalty = FindOrCreatePenalty();
        Transform respawn = FindOrCreateRespawn();
        AnubisAltarStation station = FindStation();

        var so = new SerializedObject(penalty);
        so.FindProperty("mainAltarStation").objectReferenceValue = station;
        so.FindProperty("respawnPoint").objectReferenceValue = respawn;
        so.ApplyModifiedProperties();

        // [추가] 변형 관리자도 같은 오브젝트에 (새 게임마다 무작위, 실패하면 다음 변형)
        if (penalty.GetComponent<AnubisVariantManager>() == null)
            Undo.AddComponent<AnubisVariantManager>(penalty.gameObject);

        var scaleSo = new SerializedObject(scale);
        scaleSo.FindProperty("penalty").objectReferenceValue = penalty;
        scaleSo.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(penalty.gameObject.scene);
        Selection.activeGameObject = penalty.gameObject;
        Debug.Log($"[AnubisPenalty] 설정 완료. 메인 제단: {(station != null ? station.name : "없음")}, " +
                  $"되돌아갈 위치: {respawn.position}. 씬 저장(Ctrl+S)", penalty);
    }

    private static AnubisJudgmentPenalty FindOrCreatePenalty()
    {
        var existing = Object.FindFirstObjectByType<AnubisJudgmentPenalty>(FindObjectsInactive.Include);
        if (existing != null) return existing;

        var go = new GameObject(PenaltyObjectName);
        Undo.RegisterCreatedObjectUndo(go, "Create AnubisJudgmentPenalty");
        return Undo.AddComponent<AnubisJudgmentPenalty>(go);
    }

    // 지금 플레이어가 서 있는 시작 위치를 되돌아갈 곳으로 (없으면 원점)
    private static Transform FindOrCreateRespawn()
    {
        var found = GameObject.Find(RespawnName);
        if (found != null) return found.transform;

        var go = new GameObject(RespawnName);
        Undo.RegisterCreatedObjectUndo(go, "Create Respawn_A1");

        var player = Object.FindFirstObjectByType<PlayerController2D>(FindObjectsInactive.Include);
        go.transform.position = player != null ? player.transform.position : Vector3.zero;
        if (player == null) Debug.LogWarning("[AnubisPenalty] 플레이어를 못 찾아 Respawn_A1을 원점에 만듦 -> A1 제단 앞으로 옮기기");
        return go.transform;
    }

    private static AnubisAltarStation FindStation()
    {
        foreach (var st in Object.FindObjectsByType<AnubisAltarStation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (st.name == MainAltarName) return st;
        }
        Debug.LogWarning($"[AnubisPenalty] '{MainAltarName}'에 AnubisAltarStation이 없음 -> Main Altar Station 칸에 직접 연결");
        return null;
    }
}
