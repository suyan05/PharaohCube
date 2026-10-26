#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// [추가] 메뉴: Tools/Bastet/B3·B4 팝업 퍼즐 되살리기
// 맵 수정 중 지워진 B3(목걸이)·B4(발톱 벽화) 팝업을 원본 씬(Puzzle_Test_Bastet)에서 복사해 옴
//  1) 지금 씬에 B3/B4 퍼즐이 없으면 원본 씬을 잠깐 열어 팝업을 복사 -> 같은 Canvas 아래에 붙임
//  2) "실전 방 세팅"을 다시 실행해 제단 Puzzle UI Panel, BastetRoomProgress 보상 칸, 힌트 버튼을 다시 연결
// 여러 번 눌러도 안전 (이미 있으면 복사하지 않음)
public static class BastetPopupRestore
{
    private const string SourceScenePath = "Assets/Scenes/Puzzle_Test/Puzzle_Test_Bastet.unity";
    private static readonly string[] PopupNames = { "Popup_Bastet3_Necklace", "Popup_Bastet4_ClawPattern" };

    [MenuItem("Tools/Bastet/B3·B4 팝업 퍼즐 되살리기", priority = 5)]
    private static void Restore()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("팝업 되살리기", "Play를 멈춘 뒤 눌러 주세요.", "확인");
            return;
        }

        Scene target = SceneManager.GetActiveScene();
        var log = new StringBuilder("[BastetPopupRestore] B3·B4 팝업 되살리기\n");

        bool needNecklace = Object.FindFirstObjectByType<BastetNecklacePuzzle>(FindObjectsInactive.Include) == null;
        bool needClaw = Object.FindFirstObjectByType<BastetClawPatternPuzzle>(FindObjectsInactive.Include) == null;

        if (needNecklace || needClaw)
        {
            Transform canvas = FindPopupCanvas(); // [수정] 없으면 아래에서 원본 Canvas째 복사
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
            {
                EditorUtility.DisplayDialog("팝업 되살리기", $"원본 씬을 찾지 못했습니다:\n{SourceScenePath}", "확인");
                return;
            }

            // 원본 씬을 잠깐 같이 열기 (지금 씬은 그대로)
            Scene source = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
            try
            {
                // [추가] Canvas까지 지워졌으면 원본 Canvas를 통째로 복사하고 B3·B4 팝업만 남김
                if (canvas == null)
                {
                    canvas = CopyCanvas(source, target, log);
                    if (canvas == null) return;
                }
                if (needNecklace && canvas.Find(PopupNames[0]) == null) Copy(source, PopupNames[0], canvas, log);
                if (needClaw && canvas.Find(PopupNames[1]) == null) Copy(source, PopupNames[1], canvas, log);
            }
            finally
            {
                EditorSceneManager.CloseScene(source, true); // 원본 씬은 저장하지 않고 닫음
                SceneManager.SetActiveScene(target);
            }
        }
        else
        {
            log.AppendLine("- B3·B4 퍼즐이 이미 있음 -> 복사 안 함, 연결만 다시");
        }

        // 제단·보상·힌트 다시 연결
        BastetRoomSetup.Apply();
        EditorSceneManager.MarkSceneDirty(target);
        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("팝업 되살리기", "B3·B4 팝업을 연결했습니다.\nCtrl+S로 씬을 저장한 뒤 Play로 확인해 주세요.", "확인");
    }

    // 원본 씬에서 이름으로 찾아 복사 (안쪽 참조는 복사본끼리 자동 연결됨)
    private static void Copy(Scene source, string popupName, Transform canvas, StringBuilder log)
    {
        GameObject src = FindInScene(source, popupName);
        if (src == null)
        {
            log.AppendLine($"- {popupName}: 원본 씬에 없음");
            return;
        }
        GameObject copy = Object.Instantiate(src, canvas, false);
        copy.name = popupName;
        copy.SetActive(false); // 팝업은 처음에 꺼 둠
        Undo.RegisterCreatedObjectUndo(copy, "팝업 되살리기");
        log.AppendLine($"- {popupName}: 복사 -> {canvas.name} 아래");
    }

    // [추가] 원본 씬의 "Canvas"(설정 포함)를 지금 씬으로 복사 -> B3·B4 팝업 외 자식은 지움
    private static Transform CopyCanvas(Scene source, Scene target, StringBuilder log)
    {
        GameObject src = null;
        foreach (GameObject root in source.GetRootGameObjects())
            if (root.name == "Canvas" && root.GetComponent<Canvas>() != null) { src = root; break; }
        if (src == null)
        {
            EditorUtility.DisplayDialog("팝업 되살리기", "원본 씬에서도 Canvas를 찾지 못했습니다.", "확인");
            return null;
        }
        GameObject copy = Object.Instantiate(src);
        copy.name = "Canvas";
        SceneManager.MoveGameObjectToScene(copy, target);
        for (int i = copy.transform.childCount - 1; i >= 0; i--)
        {
            Transform c = copy.transform.GetChild(i);
            if (System.Array.IndexOf(PopupNames, c.name) < 0) Object.DestroyImmediate(c.gameObject); // B1·B2·B5는 걷는 퍼즐이라 필요 없음
            else c.gameObject.SetActive(false);
        }
        copy.SetActive(true);
        Undo.RegisterCreatedObjectUndo(copy, "Canvas 되살리기");
        log.AppendLine("- Canvas가 없어서 원본 Canvas 복사 (B3·B4 팝업만 남김)");
        return copy.transform;
    }

    // 다른 Bastet 팝업이 들어 있는 Canvas
    private static Transform FindPopupCanvas()
    {
        foreach (var p in Object.FindObjectsByType<PuzzleBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!p.gameObject.scene.IsValid() || !p.name.StartsWith("Popup_Bastet")) continue;
            Canvas c = p.GetComponentInParent<Canvas>(true);
            if (c != null) return c.rootCanvas.transform;
        }
        GameObject go = GameObject.Find("Canvas");
        return go != null ? go.transform : null;
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
        return null;
    }
}
#endif
