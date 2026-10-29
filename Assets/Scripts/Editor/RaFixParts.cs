using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// RaFixSetup 도우미: 벽 횃불 플래그, 기둥 충돌, 성소 화로
public static class RaFixParts
{
    // 퍼즐 장치 이름 -> (낮 자동 점등, 밤 자동 점등)
    private static readonly (string station, string day, string night)[] Stations =
    {
        ("OBJ_005_ShadowDial", "F_RA_P1_DONE", "F_RA_NIGHT_P1_DONE"),
        ("OBJ_003_CircuitPanel (1)", "F_RA_CIRCUIT_1", ""),
        ("OBJ_007_EclipseTable_P3", "F_RA_P3_DONE", "F_RA_NIGHT_P3_DONE"),
        ("OBJ_009_CardBench_P4", "F_RA_P4_DONE", "F_RA_NIGHT_P4_DONE"),
        ("OBJ_010_SunAltar_P5", "F_RA_P5_DONE", "F_RA_NIGHT_P5_DONE")
    };

    private static readonly Vector3 BrazierPos = new Vector3(0.4f, -0.5f, 0f);

    // ================= 벽 횃불: 실제로 있는 방의 퍼즐 플래그로 =================
    // (LightObject_P3 아래 횃불이 사실 남서쪽 방에 있는 등, 그룹 이름과 실제 방이 달라서 엉뚱한 때 켜지던 문제)
    public static void RemapTorches()
    {
        var log = new List<string>();
        foreach (WallTorch t in Object.FindObjectsByType<WallTorch>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            bool center = t.transform.parent != null && t.transform.parent.name.EndsWith("_P0");
            var pick = center ? ("가운데", "", "") : Nearest(t.transform.position);
            RaPolishSetup.Edit(t, so =>
            {
                so.FindProperty("dayAutoFlag").stringValue = pick.Item2;
                so.FindProperty("nightAutoFlag").stringValue = pick.Item3;
                so.FindProperty("holdRange").floatValue = 2.4f;
            });
            WidenReach(t);
            log.Add($"{t.name}({(Vector2)t.transform.position}) -> {pick.Item1}");
        }
        Debug.Log("[RaFixSetup] 벽 횃불 방 맞춤:\n" + string.Join("\n", log));
    }

    private static (string, string, string) Nearest(Vector3 pos)
    {
        (string, string, string) best = ("가운데", "", "");
        float bestDist = float.MaxValue;
        foreach (var s in Stations)
        {
            GameObject go = GameObject.Find(s.station);
            if (go == null) continue;
            float d = (go.transform.position - pos).sqrMagnitude;
            if (d >= bestDist) continue;
            bestDist = d;
            best = (s.station, s.day, s.night);
        }
        return best;
    }

    // ================= 벽 횃불 빛 자리 =================
    // 남서쪽 방 횃불의 빛이 서쪽 방에, 서쪽 방 횃불의 빛이 남서쪽 방에 가 있었음 (빛 오브젝트가 서로 바뀌어 붙어 있음)
    // -> 횃불에서 1.5칸 넘게 떨어진 빛은 그 횃불 바로 아래(바닥 쪽)로 옮김
    public static void FixTorchLights()
    {
        foreach (WallTorch t in Object.FindObjectsByType<WallTorch>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var light = new SerializedObject(t).FindProperty("lightSource").objectReferenceValue as Light2D;
            if (light == null) continue;
            Vector3 want = t.transform.position + new Vector3(0f, -0.8f, 0f);
            if (Vector2.Distance(light.transform.position, t.transform.position) <= 1.5f) continue;
            Undo.RecordObject(light.transform, "횃불 빛 자리");
            Debug.Log($"[RaFixSetup] {t.name}의 빛이 {(Vector2)light.transform.position}에 있어서 {(Vector2)want}로 옮김");
            light.transform.position = want;
        }
    }

    // ================= 비석 <- 그 방 벽 횃불 =================
    // 규칙 통일: 비석은 "그 방의 벽 횃불"을 밝혀야 읽힘 (손에 든 횃불, 옆 방 불빛은 안 셈)
    public static void LinkSteleTorches()
    {
        WallTorch[] torches = Object.FindObjectsByType<WallTorch>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ClueStele stele in Object.FindObjectsByType<ClueStele>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string room = Nearest(stele.transform.position).Item1;
            var mine = new List<WallTorch>();
            foreach (WallTorch t in torches) if (StationOf(t) == room) mine.Add(t);
            RaPolishSetup.Edit(stele, so =>
            {
                SerializedProperty arr = so.FindProperty("roomTorches");
                arr.arraySize = mine.Count;
                for (int i = 0; i < mine.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = mine[i];
            });
            Debug.Log($"[RaFixSetup] 비석 {stele.name} -> {room} 벽 횃불 {mine.Count}개");
        }
    }

    private static string StationOf(WallTorch t)
    {
        bool center = t.transform.parent != null && t.transform.parent.name.EndsWith("_P0");
        return center ? "가운데" : Nearest(t.transform.position).Item1;
    }

    // ================= 시작 위치 (기둥과 겹치면 옆으로) =================
    private static readonly Vector3 SafeStart = new Vector3(2f, 2.6f, 0f);

    public static void MovePlayerStart()
    {
        PlayerController2D pc = Object.FindAnyObjectByType<PlayerController2D>(FindObjectsInactive.Include);
        if (pc == null || !OnPillar(pc.transform.position)) return;
        Undo.RecordObject(pc.transform, "시작 위치");
        pc.transform.position = SafeStart;
        Debug.Log($"[RaFixSetup] 시작 위치가 기둥과 겹쳐서 {SafeStart}로 옮김");

        RaTempleEntry entry = Object.FindAnyObjectByType<RaTempleEntry>(FindObjectsInactive.Include);
        WallTorch near = NearestTorch(SafeStart);
        if (entry != null && near != null) RaPolishSetup.Edit(entry, so => so.FindProperty("firstTorch").objectReferenceValue = near);
    }

    private static bool OnPillar(Vector3 pos)
    {
        foreach (SpriteRenderer sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!sr.name.StartsWith("Pillar")) continue;
            Bounds b = sr.bounds;
            b.Expand(0.6f);
            if (b.Contains(new Vector3(pos.x, pos.y, b.center.z))) return true;
        }
        return false;
    }

    private static WallTorch NearestTorch(Vector3 pos)
    {
        WallTorch best = null;
        float bestDist = float.MaxValue;
        foreach (WallTorch t in Object.FindObjectsByType<WallTorch>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            float d = (t.transform.position - pos).sqrMagnitude;
            if (d >= bestDist) continue;
            bestDist = d;
            best = t;
        }
        return best;
    }

    // 벽 위쪽에 달린 횃불도 아래 바닥에서 닿게: 조사 상자를 아래로 길게 (월드 기준 1.4 x 2.8, 1.1 아래로)
    private static void WidenReach(WallTorch t)
    {
        BoxCollider2D box = t.GetComponent<BoxCollider2D>();
        if (box == null) return;
        Undo.RecordObject(box, "횃불 조사 범위");
        Vector3 s = t.transform.lossyScale;
        float sx = Mathf.Max(0.01f, Mathf.Abs(s.x)), sy = Mathf.Max(0.01f, Mathf.Abs(s.y));
        box.size = new Vector2(1.4f / sx, 2.8f / sy);
        box.offset = new Vector2(0f, -1.1f / sy);
        box.isTrigger = true;
    }

    // ================= 기둥: 밑동에 충돌 =================
    public static void Pillars()
    {
        int count = 0;
        foreach (SpriteRenderer sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!sr.name.StartsWith("Pillar") || sr.sprite == null) continue;
            BoxCollider2D box = sr.GetComponent<BoxCollider2D>();
            if (box == null) box = Undo.AddComponent<BoxCollider2D>(sr.gameObject);
            Bounds b = sr.sprite.bounds; // 그림 기준 (크기 배율은 Unity가 알아서 곱함)
            Undo.RecordObject(box, "기둥 충돌");
            box.isTrigger = false;
            box.size = new Vector2(b.size.x * 0.8f, b.size.y * 0.28f);
            box.offset = new Vector2(b.center.x, b.min.y + b.size.y * 0.14f);
            count++;
        }
        Debug.Log($"[RaFixSetup] 기둥 {count}개 밑동 충돌 상자");
    }

    // ================= 성소 화로 =================
    public static void Brazier()
    {
        if (Object.FindAnyObjectByType<RaBrazier>(FindObjectsInactive.Include) != null) return;

        var go = new GameObject("RaBrazier (성소 화로)");
        GameObject parent = GameObject.Find("Interactables");
        if (parent != null) go.transform.SetParent(parent.transform, false);
        go.transform.position = BrazierPos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrologueAssetFinder.PixelSprite("Hall_Obj_Brazier_On", true);
        CopySorting(sr);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, 0.45f);
        box.offset = new Vector2(0f, 0.22f);

        Light2D light = MakeLight(go.transform);
        RaBrazier brazier = go.AddComponent<RaBrazier>();
        RaPolishSetup.Edit(brazier, so => so.FindProperty("fireLight").objectReferenceValue = light);
        Undo.RegisterCreatedObjectUndo(go, "성소 화로");
        Debug.Log($"[RaFixSetup] 성소 화로 배치 {BrazierPos} (옮기려면 씬에서 끌기)");
    }

    private static Light2D MakeLight(Transform parent)
    {
        var lg = new GameObject("FireLight");
        lg.transform.SetParent(parent, false);
        lg.transform.localPosition = new Vector3(0f, 1f, 0f);
        Light2D light = lg.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(1f, 0.68f, 0.32f);
        light.pointLightInnerRadius = 0.6f;
        light.pointLightOuterRadius = 3.5f;
        light.intensity = 1.3f;
        RaPolishExtras.CopyLightLayers(light);
        return light;
    }

    private static void CopySorting(SpriteRenderer sr)
    {
        GameObject refGo = GameObject.Find("OBJ_007_EclipseTable_P3");
        SpriteRenderer src = refGo != null ? refGo.GetComponent<SpriteRenderer>() : null;
        if (src == null) return;
        sr.sortingLayerID = src.sortingLayerID;
        sr.sortingOrder = src.sortingOrder;
        sr.sharedMaterial = src.sharedMaterial;
    }
}
