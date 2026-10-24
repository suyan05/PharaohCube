#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// [추가] 메뉴: Tools/Bastet/카메라 방 영역 만들기
// 퍼즐 방(B1~B5) 가이드마다 자식 "CameraZone" (가운데 홀은 기본 카메라)(CameraRoomZone + 트리거)을 만듦
// -> 방에 들어가면 카메라가 방 밖(파란 빈 화면)을 보여 주지 않고, 큰 방(B5 16x7)은 방 전체가 보이게 줌 아웃
// + Main Camera의 CameraFollow2D 맵 경계(Bounds Tilemap)가 비어 있으면 Ground 타일맵을 넣음 (통로에서도 맵 밖 안 보임)
public static class BastetCameraZones
{
    [MenuItem("Tools/Bastet/카메라 방 영역 만들기")]
    private static void Menu()
    {
        var log = new StringBuilder("[BastetCameraZones] 카메라 방 영역\n");
        int n = Build(log);
        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("카메라 방 영역", n > 0 ? $"방 {n}개에 카메라 영역을 만들었습니다.\nPlay 해서 방마다 화면을 확인한 뒤 Ctrl+S" :
            "방 가이드(BastetRoomArea)를 찾지 못했습니다.\n먼저 Tools/Bastet/★ 지금 맵(타일)에 맞춰 배치를 실행해 주세요.", "확인");
    }

    public static int Build(StringBuilder log)
    {
        int count = 0;
        foreach (BastetRoomArea area in Object.FindObjectsByType<BastetRoomArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // [수정] 가운데 홀은 넓어서 전체를 보여 주면 너무 작아짐 -> 기본 카메라 (영역 없음)
            if (area.name == "Hall_Center")
            {
                Transform old = area.transform.Find("CameraZone");
                if (old != null) { Undo.DestroyObjectImmediate(old.gameObject); log.AppendLine("- Hall_Center: 카메라 영역 지움 (기본 카메라)"); }
                continue;
            }
            Transform t = area.transform.Find("CameraZone");
            if (t == null)
            {
                var go = new GameObject("CameraZone");
                Undo.RegisterCreatedObjectUndo(go, "CameraZone");
                go.transform.SetParent(area.transform, false);
                t = go.transform;
            }
            t.localPosition = Vector3.zero;
            var box = t.GetComponent<BoxCollider2D>();
            if (box == null) box = Undo.AddComponent<BoxCollider2D>(t.gameObject);
            box.isTrigger = true;
            var zone = t.GetComponent<CameraRoomZone>();
            if (zone == null) zone = Undo.AddComponent<CameraRoomZone>(t.gameObject);
            // 트리거 = 방 바닥 크기 (통로 입구에서 바로 바뀌지 않게 가로·세로 0.5칸씩 안쪽)
            Vector2 size = area.Size - new Vector2(0.5f, 0.5f);
            zone.EditorSetup(size, new Vector4(1.25f, 1.25f, 1.25f, 2.5f));
            EditorUtility.SetDirty(zone);
            count++;
            log.AppendLine($"- {area.name}: 카메라 영역 {size.x:0.#} x {size.y:0.#}칸");
        }

        // 맵 경계
        Camera main = Camera.main;
        CameraFollow2D follow = main != null ? main.GetComponent<CameraFollow2D>() : null;
        if (follow != null)
        {
            var so = new SerializedObject(follow);
            SerializedProperty p = so.FindProperty("boundsTilemap");
            if (p != null && p.objectReferenceValue == null)
            {
                Tilemap ground = null;
                foreach (Tilemap tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
                    if (tm.name == "Ground") { ground = tm; break; }
                if (ground != null)
                {
                    p.objectReferenceValue = ground;
                    so.ApplyModifiedProperties();
                    log.AppendLine("- Main Camera 맵 경계 = Ground 타일맵");
                }
            }
        }
        else log.AppendLine("- 경고: Main Camera에 CameraFollow2D 없음");

        if (count > 0) EditorSceneManager.MarkAllScenesDirty();
        return count;
    }
}
#endif
