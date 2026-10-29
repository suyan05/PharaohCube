using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// 라의 방 2차 고치기 (Puzzle_Test_Ra_Tilemap 씬을 열고 실행, 여러 번 눌러도 안전)
// 메뉴: PharaohCube -> Ra -> 라의 방 고치기 (2차: 어둠, 횃불 시간, 기둥, 비석, 밤 그림)
//
// 0. 1차 메뉴(라의 방 다듬기)를 먼저 자동으로 한 번 실행
// 1. 어둠: 낮 0.03 / 밤 0 (밤은 불빛만), 내 횃불 반경 3칸 + 60초 (어두운 곳에서만 줄어듦)
// 2. 시스템: RaTorchTimer(횃불 시간), RaTorchGaugeUI(왼쪽 위 게이지), RaDayToNight(낮을 다 풀면 라가 먼저 말 걸기)
// 3. 벽 횃불: 실제 있는 방 기준으로 자동 점등 플래그 다시 맞춤 + 벽 위 횃불도 닿게 조사 범위 넓힘
// 4. 기둥 4개: 밑동에 충돌 상자 (앞으로 못 지나감)
// 5. 성소 화로: 신전 가운데 꺼지지 않는 불 (횃불 다시 붙이기 + 밝은 곳)
// 6. 단서 그림: 밤 P3, P4, P5 비석에 밤 벽화 넣기 / 일식 석탁 밤 그림(달이 해를 삼킨 문양) 넣기
// 7. 퍼즐 화면 파피루스 배경판: P1 그림자 시계, P4 카드 벤치 (흰 글자는 진한 갈색으로)
public static class RaFixSetup
{
    private const string FirstMenu = "PharaohCube/Ra/라의 방 다듬기 (어둠, 글자, 히든 불씨)";

    [MenuItem("PharaohCube/Ra/라의 방 고치기 (2차: 어둠, 횃불 시간, 기둥, 비석, 밤 그림)")]
    private static void Apply()
    {
        try
        {
            if (Object.FindAnyObjectByType<WallTorch>(FindObjectsInactive.Include) == null)
            {
                EditorUtility.DisplayDialog("라의 방", "벽 횃불(WallTorch)이 없어요.\n먼저 PharaohCube -> Ra -> 횃불 신전으로 바꾸기를 눌러주세요.", "확인");
                return;
            }

            EditorApplication.ExecuteMenuItem(FirstMenu);
            GameObject root = GameObject.Find("RaTemple");
            if (root == null) root = new GameObject("RaTemple");

            Systems(root);
            Darkness(root);
            RaFixParts.RemapTorches();
            RaFixParts.FixTorchLights();
            RaFixParts.LinkSteleTorches();
            RaFixParts.MovePlayerStart();
            RaFixParts.Pillars();
            RaFixParts.Brazier();
            RaFixArt.ClueArt();
            RaFixArt.EclipseNightArt();
            RaFixArt.PuzzlePapyrus();
            ObjectiveText();

            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("[RaFixSetup] 2차 완료 -> Ctrl+S로 씬 저장 꼭!");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RaFixSetup] 오류: {e}");
        }
    }

    private static void Systems(GameObject root)
    {
        Add<RaTorchTimer>(root);
        Add<RaTorchGaugeUI>(root);
        Add<RaDayToNight>(root);
        Add<RaHudDirector>(root);
        Add<RaDarkness>(root);
    }

    private static void Add<T>(GameObject root) where T : Component
    {
        if (Object.FindAnyObjectByType<T>(FindObjectsInactive.Include) == null) Undo.AddComponent<T>(root);
    }

    private static void Darkness(GameObject root)
    {
        RaDarkness dark = Object.FindAnyObjectByType<RaDarkness>(FindObjectsInactive.Include);
        if (dark != null) RaPolishSetup.Edit(dark, so =>
        {
            so.FindProperty("dayIntensity").floatValue = 0.03f;
            so.FindProperty("nightIntensity").floatValue = 0f;
        });

        RaNightAtmosphere night = Object.FindAnyObjectByType<RaNightAtmosphere>(FindObjectsInactive.Include);
        if (night != null) RaPolishSetup.Edit(night, so => so.FindProperty("nightGlobalIntensity").floatValue = 0f);

        PlayerTorchTemp torch = Object.FindAnyObjectByType<PlayerTorchTemp>(FindObjectsInactive.Include);
        if (torch != null) RaPolishSetup.Edit(torch, so =>
        {
            so.FindProperty("baseRadius").floatValue = 3f;
            so.FindProperty("burnSeconds").floatValue = 60f;
        });
    }

    // 낮을 다 풀면 라가 먼저 부르므로 "성소로 돌아가자" 대신
    private static void ObjectiveText()
    {
        RaObjectiveGuide guide = Object.FindAnyObjectByType<RaObjectiveGuide>(FindObjectsInactive.Include);
        if (guide == null) return;
        RaPolishSetup.Edit(guide, so =>
        {
            SerializedProperty steps = so.FindProperty("steps");
            for (int i = 0; i < steps.arraySize; i++)
            {
                SerializedProperty s = steps.GetArrayElementAtIndex(i);
                if (s.FindPropertyRelative("doneFlag").stringValue != "F_RA_NIGHT_OPEN") continue;
                s.FindPropertyRelative("objective").stringValue = "라의 목소리가 들린다... (제단 화면을 닫자)";
            }
        });
    }
}
