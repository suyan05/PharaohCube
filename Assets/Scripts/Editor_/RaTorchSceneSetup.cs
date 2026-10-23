using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// RaTorchSetup이 쓰는 씬 수정 도우미 (조명, 입장 연출, 플레이어 횃불, 목표 문구)
// 남의 스크립트(RaZoneLighting, RaNightAtmosphere, RaObjectiveGuide 등)는 코드를 고치지 않고 Inspector 값만 바꿈
public static class RaTorchSceneSetup
{
    private const float DayGlobal = 0.22f;
    private const float NightGlobal = 0.3f;

    // ================= RaZoneLighting: 횃불 켜기 빼기 =================
    public static void StripZoneLighting()
    {
        RaZoneLighting zone = Object.FindAnyObjectByType<RaZoneLighting>(FindObjectsInactive.Include);
        if (zone == null) return;

        Undo.RecordObject(zone, "구역 조명 정리");
        var so = new SerializedObject(zone);
        SerializedProperty zones = so.FindProperty("zones");
        for (int i = 0; i < zones.arraySize; i++)
        {
            SerializedProperty z = zones.GetArrayElementAtIndex(i);
            z.FindPropertyRelative("turnOn").arraySize = 0;
            z.FindPropertyRelative("fadeLights").arraySize = 0;
            SerializedProperty msg = z.FindPropertyRelative("unlockMessage");
            msg.stringValue = msg.stringValue.Replace("횃불에 불이 붙었다", "문이 열렸다");
        }
        so.ApplyModifiedProperties();
    }

    // ================= 어둡게 =================
    public static void DarkenLights()
    {
        foreach (Light2D light in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (light.lightType != Light2D.LightType.Global) continue;
            Undo.RecordObject(light, "신전 어둡게");
            light.intensity = DayGlobal;
        }

        RaNightAtmosphere night = Object.FindAnyObjectByType<RaNightAtmosphere>(FindObjectsInactive.Include);
        if (night == null) return;
        Undo.RecordObject(night, "밤 어둡게");
        var so = new SerializedObject(night);
        SerializedProperty p = so.FindProperty("nightGlobalIntensity");
        if (p != null) p.floatValue = NightGlobal;
        so.ApplyModifiedProperties();
    }

    // ================= 플레이어 횃불 =================
    public static Transform SetupPlayerTorch()
    {
        PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
        if (player == null)
        {
            Debug.LogWarning("[RaTorchSetup] 씬에 플레이어가 없음");
            return null;
        }

        PlayerTorchTemp torch = player.GetComponent<PlayerTorchTemp>();
        if (torch == null) torch = Undo.AddComponent<PlayerTorchTemp>(player.gameObject);

        var so = new SerializedObject(torch);
        so.FindProperty("startWithTorch").boolValue = true;   // 이 씬만 바로 Play해도 횃불 들고 시작
        so.FindProperty("burnSeconds").floatValue = 600f;     // 라의 방에는 화로가 없어서 넉넉하게 (켜진 벽 횃불에서 다시 붙일 수 있음)
        so.ApplyModifiedProperties();
        return player.transform;
    }

    // ================= 입장 연출 =================
    public static void SetupEntry(WallTorch firstTorch)
    {
        RaIntroCutscene oldIntro = Object.FindAnyObjectByType<RaIntroCutscene>(FindObjectsInactive.Include);
        if (oldIntro != null)
        {
            Undo.RecordObject(oldIntro, "예전 입장 연출 끄기");
            oldIntro.enabled = false;
        }

        GameObject root = GameObject.Find("RaTemple");
        if (root == null)
        {
            root = new GameObject("RaTemple");
            Undo.RegisterCreatedObjectUndo(root, "라의 신전 연출");
        }

        RaTempleEntry entry = root.GetComponent<RaTempleEntry>();
        if (entry == null) entry = Undo.AddComponent<RaTempleEntry>(root);
        if (root.GetComponent<RaTorchManager>() == null) Undo.AddComponent<RaTorchManager>(root);

        var so = new SerializedObject(entry);
        so.FindProperty("firstTorch").objectReferenceValue = firstTorch;
        so.ApplyModifiedProperties();
    }

    // ================= 목표 문구 =================
    public static void SetupObjectiveGuide()
    {
        RaObjectiveGuide guide = Object.FindAnyObjectByType<RaObjectiveGuide>(FindObjectsInactive.Include);
        if (guide == null) return;

        Undo.RecordObject(guide, "목표 문구");
        var so = new SerializedObject(guide);
        SerializedProperty steps = so.FindProperty("steps");
        if (steps.arraySize == 0 || steps.GetArrayElementAtIndex(0).FindPropertyRelative("doneFlag").stringValue != "F_RA_FIRST_TORCH_LIT")
        {
            steps.InsertArrayElementAtIndex(0);
            SerializedProperty first = steps.GetArrayElementAtIndex(0);
            first.FindPropertyRelative("doneFlag").stringValue = "F_RA_FIRST_TORCH_LIT";
            first.FindPropertyRelative("objective").stringValue = "벽 횃불에 내 불을 옮겨 붙이자\n(벽 횃불 앞에서 E 길게)";
        }
        ReplaceObjective(steps, "F_RA_NIGHT_P1_DONE", "횃불이 모두 꺼졌다. 다시 밝히며\n동쪽 방 밤의 그림자를 읽자");
        so.ApplyModifiedProperties();
    }

    private static void ReplaceObjective(SerializedProperty steps, string doneFlag, string text)
    {
        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty s = steps.GetArrayElementAtIndex(i);
            if (s.FindPropertyRelative("doneFlag").stringValue == doneFlag) s.FindPropertyRelative("objective").stringValue = text;
        }
    }
}
