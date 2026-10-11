using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 양피지 조각 오브젝트 자동 생성 (에디터 전용)
// 사용법: Project 창에서 양피지 데이터(Scroll_Room1 등)를 선택 → Tools > Anubis > 양피지 > 선택한 양피지의 조각 만들기
// Scene 뷰 화면 중앙 주변에 조각이 원형으로 생성됨 → 원하는 곳으로 옮기기

public static class AnubisScrollPieceTool
{
    [MenuItem("Tools/Anubis/양피지/선택한 양피지의 조각 만들기")]
    private static void CreatePieces()
    {
        var data = Selection.activeObject as AnubisScrollData;
        if (data == null)
        {
            EditorUtility.DisplayDialog("양피지 조각 만들기", "Project 창에서 양피지 데이터(Scroll_Room1 등)를 먼저 클릭하세요.", "확인");
            return;
        }

        Vector3 center = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        center.z = 0f;

        var parent = new GameObject($"Scrolls_{data.name}");
        Undo.RegisterCreatedObjectUndo(parent, "Create Scroll Pieces");
        parent.transform.position = center;

        var defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        for (int i = 0; i < data.PieceCount; i++)
        {
            var go = new GameObject($"ScrollPiece_{i + 1}");
            go.transform.SetParent(parent.transform, false);
            float angle = i * Mathf.PI * 2f / data.PieceCount;
            go.transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 1.5f;

            // 그림 (조각 그림이 없으면 양피지 색 사각형)

            var sr = go.AddComponent<SpriteRenderer>();
            Sprite pieceSprite = data.GetPieceSprite(i);
            sr.sprite = pieceSprite != null ? pieceSprite : defaultSprite;
            sr.color = pieceSprite != null ? Color.white : new Color(0.93f, 0.85f, 0.62f, 1f);
            sr.sortingLayerName = "Objects";

            // 줍기 범위 (Trigger라 플레이어가 지나갈 수 있음)

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;

            var piece = go.AddComponent<AnubisScrollPiece>();
            var so = new SerializedObject(piece);
            so.FindProperty("scroll").objectReferenceValue = data;
            so.FindProperty("pieceNumber").intValue = i + 1;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Selection.activeGameObject = parent;
        EditorSceneManager.MarkSceneDirty(parent.scene);
        Debug.Log($"[AnubisScroll] {data.Title} 조각 {data.PieceCount}개 생성. Scene 뷰에서 위치를 옮기세요.");
    }
}