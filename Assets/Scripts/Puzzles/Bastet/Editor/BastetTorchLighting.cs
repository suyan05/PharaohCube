#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// [추가] 메뉴: Tools/Bastet/★ 횃불로 방 밝히기 (라의 방 방식)
// 라의 방과 같은 방식으로 바꿈
//  1) 방 불이 자동으로 켜지지 않음: ZoneGuide(아누비스 구역 안내)와 방 불빛(RoomLight_L/R)을 끔
//  2) 방마다 벽 횃불(WallTorch, 라의 방 것 그대로) 2개 -> 내 횃불이 켜져 있으면 E 길게 -> 그 방이 밝아짐
//     켜진 벽 횃불 근처 = 밝은 곳 (횃불 시간이 안 줄어듦), 켜진 벽 횃불에서 E = 꺼진 내 횃불 다시 붙이기
//  3) BastetSystems: 어둠(BastetDarkness, 라의 방보다 조금 밝게) + 횃불 시간(RaTorchTimer) + 왼쪽 위 게이지(RaTorchGaugeUI)
//     라의 방 전용(RaDarkness 완전 어둠, RaDayToNight 낮->밤)은 꺼 둔 채로 넣어서 자동으로 붙지 않게 함
//  4) B5 밤 단계 밝기를 바스테트 어둠보다 어둡게
// 여러 번 눌러도 중복으로 만들지 않음
public static class BastetTorchLighting
{
    private const string OffPath = "Assets/image/Test/Object/Light_off.png";
    private const string OnPath = "Assets/image/Test/Object/Light_on.png";
    private const string SortingLayer = "Objects";

    [MenuItem("Tools/Bastet/★ 횃불로 방 밝히기 (라의 방 방식)", priority = 3)]
    private static void Menu()
    {
        var log = new StringBuilder("[BastetTorchLighting] 횃불로 방 밝히기\n");
        int n = Build(log);
        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("횃불로 방 밝히기", n > 0
            ? $"벽 횃불 {n}개를 만들었습니다.\nPlay -> 내 횃불을 켠 채로 벽 횃불 앞에서 E 길게.\n확인 후 Ctrl+S"
            : "방 가이드(BastetRoomArea)를 찾지 못했습니다.\n먼저 Tools/Bastet/★ 지금 맵(타일)에 맞춰 배치를 실행해 주세요.", "확인");
    }

    public static int Build(StringBuilder log)
    {
        BastetRoomArea[] areas = Object.FindObjectsByType<BastetRoomArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (areas.Length == 0) return 0;

        DisableAutoLights(log);
        Sprite off = FirstSprite(OffPath), on = FirstSprite(OnPath);
        if (off == null || on == null) log.AppendLine($"- 경고: 벽 횃불 그림 없음 ({OffPath}) -> 네모로 표시");

        int count = 0;
        foreach (BastetRoomArea area in areas)
        {
            Vector2 size = area.Size;
            float top = size.y * 0.5f + 0.45f;              // 위쪽 벽 앞면 (3/4 탑다운)
            float x = Mathf.Round(size.x * 0.3f * 2f) / 2f; // 가운데(출구) 피해서 양옆
            float radius = Mathf.Clamp(Mathf.Max(size.x * 0.55f, size.y * 0.75f), 4f, 9f);
            string room = area.name;
            count += Torch(area.transform, room, 0, new Vector2(-x, top), radius, off, on);
            count += Torch(area.transform, room, 1, new Vector2(x, top), radius, off, on);
            if (room == "Hall_Center") // 넓은 가운데 홀은 아래쪽 양옆에 2개 더
            {
                count += Torch(area.transform, room, 2, new Vector2(-size.x * 0.5f + 0.6f, -size.y * 0.15f), radius, off, on);
                count += Torch(area.transform, room, 3, new Vector2(size.x * 0.5f - 0.6f, -size.y * 0.15f), radius, off, on);
            }
            log.AppendLine($"- {room}: 벽 횃불 (빛 반경 {radius:0.#}칸)");
        }

        EnsureSystems(log);
        DarkenB5Night(log);
        EditorSceneManager.MarkAllScenesDirty();
        return count;
    }

    // ================= 1) 자동 불빛 끄기 =================
    private static void DisableAutoLights(StringBuilder log)
    {
        foreach (AnubisZoneGuide g in Object.FindObjectsByType<AnubisZoneGuide>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (g.GetComponentInParent<BastetRoomArea>() == null && g.transform.root.name != "Bastet_MapFit") continue;
            Undo.RecordObject(g, "자동 불빛 끄기");
            g.enabled = false;
            log.AppendLine($"- {g.name}: 방 자동 불빛(ZoneGuide) 끔");
        }
        int lights = 0;
        foreach (BastetRoomArea area in Object.FindObjectsByType<BastetRoomArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (string n in new[] { "RoomLight_L", "RoomLight_R" })
            {
                Transform t = area.transform.Find(n);
                if (t == null || !t.gameObject.activeSelf) continue;
                Undo.RecordObject(t.gameObject, "방 불빛 끄기");
                t.gameObject.SetActive(false);
                lights++;
            }
        }
        if (lights > 0) log.AppendLine($"- 방 불빛(RoomLight) {lights}개 끔 (벽 횃불이 대신함)");
    }

    // ================= 2) 벽 횃불 =================
    private static int Torch(Transform room, string roomName, int index, Vector2 local, float radius, Sprite off, Sprite on)
    {
        string name = $"WallTorch_{index}";
        Transform t = room.Find(name);
        bool created = t == null;
        if (created)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, name);
            go.transform.SetParent(room, false);
            t = go.transform;
        }
        t.localPosition = new Vector3(local.x, local.y, 0f);

        var sr = t.GetComponent<SpriteRenderer>();
        if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(t.gameObject);
        sr.sprite = off != null ? off : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.sortingLayerName = SortingLayer;
        sr.sortingOrder = 2;

        var box = t.GetComponent<BoxCollider2D>();
        if (box == null) box = Undo.AddComponent<BoxCollider2D>(t.gameObject);
        box.isTrigger = true;                        // 길은 막지 않고 E 조사만
        box.size = new Vector2(1f, 1.4f);
        box.offset = new Vector2(0f, -0.4f);         // 벽 아래(플레이어 쪽)까지

        Transform lt = t.Find("Light");
        if (lt == null)
        {
            var lgo = new GameObject("Light");
            Undo.RegisterCreatedObjectUndo(lgo, "Light");
            lgo.transform.SetParent(t, false);
            lt = lgo.transform;
        }
        lt.localPosition = new Vector3(0f, -0.6f, 0f); // 빛 중심을 방 안쪽으로 살짝
        var light = lt.GetComponent<Light2D>();
        if (light == null) light = Undo.AddComponent<Light2D>(lt.gameObject);
        light.lightType = Light2D.LightType.Point;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * 0.25f;
        light.color = new Color(1f, 0.74f, 0.45f);
        light.intensity = 1.1f;
        lt.gameObject.SetActive(false); // 처음엔 꺼짐 (플레이어가 붙임)

        var wt = t.GetComponent<WallTorch>();
        if (wt == null) wt = Undo.AddComponent<WallTorch>(t.gameObject);
        var so = new SerializedObject(wt);
        so.FindProperty("torchId").stringValue = $"BASTET_{roomName}_{index}"; // 켜진 상태 저장 플래그 F_RA_TORCH_BASTET_..._D
        so.FindProperty("target").objectReferenceValue = sr;
        so.FindProperty("offSprite").objectReferenceValue = off;
        so.FindProperty("onSprite").objectReferenceValue = on;
        so.FindProperty("lightSource").objectReferenceValue = light;
        so.FindProperty("litIntensity").floatValue = 1.1f;
        so.FindProperty("brightRadius").floatValue = radius * 0.7f;
        so.FindProperty("dayAutoFlag").stringValue = "";   // 자동으로 켜지지 않음
        so.FindProperty("nightAutoFlag").stringValue = "";
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(wt);
        return 1;
    }

    // ================= 3) 시스템 =================
    private static void EnsureSystems(StringBuilder log)
    {
        GameObject sys = BastetWalkBuilder.FindInScene("BastetSystems");
        if (sys == null)
        {
            sys = new GameObject("BastetSystems");
            Undo.RegisterCreatedObjectUndo(sys, "BastetSystems");
        }
        if (sys.GetComponent<BastetDarkness>() == null) Undo.AddComponent<BastetDarkness>(sys);
        var timer = sys.GetComponent<RaTorchTimer>();
        if (timer == null) timer = Undo.AddComponent<RaTorchTimer>(sys);
        var so = new SerializedObject(timer);
        so.FindProperty("outMessage").stringValue = "횃불이 꺼져간다... 켜진 벽 횃불이나 중앙 홀 제단에서 다시 붙이자.";
        so.ApplyModifiedPropertiesWithoutUndo();
        if (sys.GetComponent<RaTorchGaugeUI>() == null) Undo.AddComponent<RaTorchGaugeUI>(sys);
        if (sys.GetComponent<RaHudDirector>() == null) Undo.AddComponent<RaHudDirector>(sys);

        // 라의 방 전용은 꺼진 채로 (RaRuntimeBootstrap이 "없네?" 하고 켜진 것을 붙이지 않게)
        Transform raOnly = sys.transform.Find("RaOnly (바스테트에서는 꺼 둠)");
        if (raOnly == null)
        {
            var go = new GameObject("RaOnly (바스테트에서는 꺼 둠)");
            Undo.RegisterCreatedObjectUndo(go, "RaOnly");
            go.transform.SetParent(sys.transform, false);
            raOnly = go.transform;
        }
        if (raOnly.GetComponent<RaDarkness>() == null) Undo.AddComponent<RaDarkness>(raOnly.gameObject);
        if (raOnly.GetComponent<RaDayToNight>() == null) Undo.AddComponent<RaDayToNight>(raOnly.gameObject);
        raOnly.gameObject.SetActive(false);
        log.AppendLine("- BastetSystems: 어둠(BastetDarkness 0.12) + 횃불 시간 + 왼쪽 위 게이지 (라의 방 완전 어둠·낮밤 전환은 꺼 둠)");
    }

    // ================= 4) B5 밤 =================
    private static void DarkenB5Night(StringBuilder log)
    {
        var b5 = Object.FindFirstObjectByType<BastetSunMoonWalk>(FindObjectsInactive.Include);
        if (b5 == null) return;
        var so = new SerializedObject(b5);
        SerializedProperty p = so.FindProperty("nightIntensity");
        if (p == null) return;
        p.floatValue = 0.03f;
        so.ApplyModifiedProperties();
        log.AppendLine("- B5 밤 단계 밝기 0.03 (바스테트 어둠 0.12보다 어둡게)");
    }

    private static Sprite FirstSprite(string path)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite s) return s;
        return null;
    }
}
#endif
