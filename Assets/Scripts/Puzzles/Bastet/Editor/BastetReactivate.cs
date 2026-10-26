#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// [추가] 메뉴: Tools/Bastet/꺼진 바스테트 오브젝트 다시 켜기
// 맵 작업 중에 꺼 둔(체크 해제) 바스테트 오브젝트를 한 번에 다시 켬
// (오브젝트는 지워지지 않고 꺼져만 있음 -> 켜면 퍼즐·벽 횃불·비석·여신·출구가 그대로 돌아옴)
public static class BastetReactivate
{
    // 바스테트가 동작하려면 켜져 있어야 하는 오브젝트 (자식은 각자 원래 상태 유지)
    private static readonly string[] Names =
    {
        "Bastet_MapFit", "Hall_Center", "Room1_Footprint", "Room2_Eye", "Room3_Necklace", "Room4_Claw", "Room5_Shrine",
        "ZoneGuide", "BastetSystems", "BastetStoryCanvas", "BastetRoomUI",
        "Slate_Bastet_Hall", "Stele_B1", "Stele_B2", "Stele_B3", "Stele_B4", "Stele_B5", "Exit_ToThoth"
    };

    // ZoneGuide는 오브젝트만 켬 (컴포넌트는 '횃불로 방 밝히기'가 꺼 둔 상태 그대로)
    [MenuItem("Tools/Bastet/꺼진 바스테트 오브젝트 다시 켜기", priority = 5)]
    private static void Run()
    {
        var targets = new HashSet<string>(Names);
        var log = new StringBuilder();
        int n = 0;
        var scene = EditorSceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!targets.Contains(t.name) || t.gameObject.activeSelf) continue;
                Undo.RecordObject(t.gameObject, "바스테트 다시 켜기");
                t.gameObject.SetActive(true);
                log.AppendLine($"- {t.name}");
                n++;
            }
        }
        if (n > 0) EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[BastetReactivate] 다시 켠 오브젝트 {n}개\n{log}");
        EditorUtility.DisplayDialog("바스테트 다시 켜기", n > 0 ? $"{n}개를 다시 켰습니다.\n\n{log}\nPlay로 확인한 뒤 Ctrl+S" : "꺼진 바스테트 오브젝트가 없습니다.", "확인");
    }
}
#endif
