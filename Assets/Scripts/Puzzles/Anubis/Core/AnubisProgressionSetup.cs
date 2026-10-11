using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 제단 5곳의 잠금(AnubisAltarLock)을 메뉴 한 번으로 설정 (에디터 전용)
// - 실전: A1 열림 → A2~A5는 앞 퍼즐 보상을 얻으면 차례대로 열림
// - 튜토리얼: A1~A4 열림, A5(메인)만 항상 봉인
public static class AnubisProgressionSetup
{
    private static readonly string[] AltarNames =
    {
        "Altar_A1_Sand", "Altar_A2_Gear", "Altar_A3_Lever", "Altar_A4_MagicSquare", "Altar_A5_Main"
    };

    // 실전: 각 제단을 여는 아이템 
    private static readonly string[] RealRequired =
    {
        "", AnubisItemIds.GoldJar, AnubisItemIds.ClayPot, AnubisItemIds.Obsidian, AnubisItemIds.BronzeBowl
    };

    private static readonly string[] RealUnlockMessage =
    {
        "",
        "어딘가에서 불꽃이 피어오릅니다.\n두 번째 시험의 제단이 깨어났습니다.",
        "어딘가에서 불꽃이 피어오릅니다.\n세 번째 시험의 제단이 깨어났습니다.",
        "어딘가에서 불꽃이 피어오릅니다.\n네 번째 시험의 제단이 깨어났습니다.",
        "네 개의 시험을 모두 통과했습니다.\n심판의 저울에 불이 들어왔습니다."
    };

    [MenuItem("Tools/Anubis/진행 설정/실전: 제단 순차 해금 적용")]
    private static void ApplyReal()
    {
        if (!Confirm("실전 순차 해금", "A1만 열고, A2~A5는 앞 퍼즐을 풀어야 열리게 설정합니다.")) return;
        for (int i = 0; i < AltarNames.Length; i++)
            Apply(AltarNames[i], false, RealRequired[i],
                  "제단이 봉인되어 있습니다.\n앞선 시험을 먼저 통과하십시오.", RealUnlockMessage[i]);
        Finish("실전 순차 해금");
    }

    [MenuItem("Tools/Anubis/진행 설정/튜토리얼: 메인 제단 봉인 적용")]
    private static void ApplyTutorial()
    {
        if (!Confirm("튜토리얼 설정", "A1~A4는 열고, A5(메인 저울)는 항상 봉인합니다.")) return;
        for (int i = 0; i < 4; i++) Apply(AltarNames[i], false, "", "", "");
        Apply(AltarNames[4], true, "",
              "튜토리얼에서는 심판의 저울이 봉인되어 있습니다.\n네 개의 연습을 마치면 진짜 신전으로 가는 문이 열립니다.", "");
        Finish("튜토리얼 설정");
    }

    private static void Apply(string altarName, bool alwaysLocked, string requiredItem, string lockedMsg, string unlockMsg)
    {
        var altar = FindByName(altarName);
        if (altar == null) return;

        var lk = altar.GetComponent<AnubisAltarLock>();
        if (lk == null) lk = Undo.AddComponent<AnubisAltarLock>(altar);

        var so = new SerializedObject(lk);
        so.FindProperty("alwaysLocked").boolValue = alwaysLocked;
        so.FindProperty("requiredItem").stringValue = requiredItem;
        if (!string.IsNullOrEmpty(lockedMsg)) so.FindProperty("lockedMessage").stringValue = lockedMsg;
        so.FindProperty("unlockMessage").stringValue = unlockMsg;
        so.ApplyModifiedProperties();

        string state = alwaysLocked ? "항상 봉인" : string.IsNullOrEmpty(requiredItem) ? "처음부터 열림" : $"'{requiredItem}' 획득 시 열림";
        Debug.Log($"[AnubisProgression] {altarName}: {state}", altar);
    }

    // 비활성 포함 이름으로 찾기. 같은 이름이 여러 개면 경고
    private static GameObject FindByName(string objName)
    {
        GameObject found = null;
        int count = 0;
        foreach (var tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tr.name != objName) continue;
            count++;
            if (found == null) found = tr.gameObject;
        }
        if (count == 0) Debug.LogWarning($"[AnubisProgression] '{objName}' 오브젝트가 없어 건너뜀");
        if (count > 1) Debug.LogWarning($"[AnubisProgression] '{objName}'가 {count}개 있음. 하나만 남기세요.", found);
        return found;
    }

    private static bool Confirm(string title, string message)
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return EditorUtility.DisplayDialog(title, $"{message}\n\n현재 씬: {scene}", "적용", "취소");
    }

    private static void Finish(string label)
    {
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"[AnubisProgression] {label} 완료. Ctrl+S로 저장하세요.");
    }
}