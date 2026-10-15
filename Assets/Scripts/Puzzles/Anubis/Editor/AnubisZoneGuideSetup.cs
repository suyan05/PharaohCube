using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 아누비스 실전 씬에 "다음 제단 안내"(AnubisZoneGuide)를 메뉴 한 번으로 설정 (에디터 전용)
// - A2~A5 구역 4개를 만들고: 해금 아이템 = 앞 퍼즐 보상, 카메라 대상 = 다음 제단, 불빛 = 제단의 Light 2D
// - 제단 잠금(AnubisAltarLock)의 불빛 제어와 해금 문구는 끔 (안내 연출이 대신 켜고 안내하니까 중복 방지)
// - 반드시 "실전: 제단 순차 해금 적용" 다음에 실행
public static class AnubisZoneGuideSetup
{
    private const string GuideObjectName = "AnubisZoneGuide";

    private static readonly string[] ZoneNames =
    {
        "A2 톱니 구역", "A3 레버 구역", "A4 마방진 구역", "A5 심판의 저울"
    };

    [MenuItem("Tools/Anubis/진행 설정/실전: 다음 제단 안내 연출 적용")]
    private static void Apply()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (!EditorUtility.DisplayDialog("다음 제단 안내 연출",
                "A1을 풀면 A2 제단으로 카메라가 이동하며 불이 켜지는 연출을 A2~A5에 설정합니다.\n" +
                "제단 잠금의 불빛 제어와 해금 문구는 꺼집니다.\n\n현재 씬: " + scene, "적용", "취소"))
        {
            return;
        }

        AnubisZoneGuide guide = FindOrCreateGuide();
        var zones = new List<AnubisZoneGuide.GuideZone>();

        // 0번(A1)은 처음부터 열려 있으니 1번(A2)부터
        for (int i = 1; i < AnubisProgressionSetup.AltarNames.Length; i++)
        {
            GameObject altar = FindByName(AnubisProgressionSetup.AltarNames[i]);
            if (altar == null) continue;

            DisableLockLightAndMessage(altar);
            zones.Add(MakeZone(i, altar));
        }

        Undo.RecordObject(guide, "다음 제단 안내 설정");
        guide.SetZonesForEditor(zones.ToArray());
        EditorUtility.SetDirty(guide);
        EditorSceneManager.MarkSceneDirty(guide.gameObject.scene);

        Selection.activeGameObject = guide.gameObject;
        Debug.Log($"[AnubisZoneGuide] 구역 {zones.Count}개 설정 완료. 방 조명 / 막힌 통로는 각 구역의 Turn On / Turn Off에 직접 넣고 씬 저장(Ctrl+S)", guide);
    }

    private static AnubisZoneGuide.GuideZone MakeZone(int altarIndex, GameObject altar)
    {
        return new AnubisZoneGuide.GuideZone
        {
            zoneName = ZoneNames[altarIndex - 1],
            unlockItem = AnubisProgressionSetup.RealRequired[altarIndex],
            unlockMessage = AnubisProgressionSetup.RealUnlockMessage[altarIndex],
            focusPoint = altar.transform,
            turnOn = new GameObject[0],
            turnOff = new GameObject[0],
            fadeLights = altar.GetComponentsInChildren<Light2D>(true)
        };
    }

    // 제단 잠금이 불을 바로 켜버리면 연출 때 번쩍임이 안 보여서 끔
    private static void DisableLockLightAndMessage(GameObject altar)
    {
        var lk = altar.GetComponent<AnubisAltarLock>();
        if (lk == null)
        {
            Debug.LogWarning($"[AnubisZoneGuide] {altar.name}에 AnubisAltarLock이 없음 -> '실전: 제단 순차 해금 적용'을 먼저 실행", altar);
            return;
        }

        var so = new SerializedObject(lk);
        so.FindProperty("controlOwnLights").boolValue = false;
        so.FindProperty("unlockMessage").stringValue = "";
        so.ApplyModifiedProperties();
    }

    private static AnubisZoneGuide FindOrCreateGuide()
    {
        var existing = Object.FindFirstObjectByType<AnubisZoneGuide>(FindObjectsInactive.Include);
        if (existing != null) return existing;

        var go = new GameObject(GuideObjectName);
        Undo.RegisterCreatedObjectUndo(go, "Create AnubisZoneGuide");
        return Undo.AddComponent<AnubisZoneGuide>(go);
    }

    private static GameObject FindByName(string objName)
    {
        foreach (var tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tr.name == objName) return tr.gameObject;
        }
        Debug.LogWarning($"[AnubisZoneGuide] '{objName}' 오브젝트가 없어 건너뜀");
        return null;
    }
}