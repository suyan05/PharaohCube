using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// 라의 방 다듬기 메뉴 (Puzzle_Test_Ra_Tilemap 씬을 열고, "횃불 신전으로 바꾸기"를 한 뒤에 실행)
// 메뉴: PharaohCube -> Ra -> 라의 방 다듬기 (어둠, 글자, 히든 불씨)   (여러 번 눌러도 안전)
//
// 하는 것
// 1. 어둠: 전역 조명 낮 0.05 / 밤 0.07 + 내 횃불 시야를 조금 좁힘 (RaDarkness가 실행 중에도 한 번 더 맞춤)
// 2. 글자 겹침 정리: RaHudDirector 추가, 밤 알림 중복 제거 (라의 성소 알림만 남김)
// 3. 테스트용 하얀 네모(Square, TEST_SAND) 끄기
// 4. 히든: 서쪽 방 구석에 태양의 배 모형(RaSunBoat) + 라의 성소 밤 대사에 복선 한 줄
// 5. 아이템 아이콘: 라의 불씨, 횃불
public static class RaPolishSetup
{
    private const string UnlitPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
    private const float DayGlobal = 0.05f;
    private const float NightGlobal = 0.07f;

    [MenuItem("PharaohCube/Ra/라의 방 다듬기 (어둠, 글자, 히든 불씨)")]
    private static void Apply()
    {
        try
        {
            if (Object.FindAnyObjectByType<WallTorch>(FindObjectsInactive.Include) == null)
            {
                EditorUtility.DisplayDialog("라의 방", "벽 횃불(WallTorch)이 없어요.\n먼저 PharaohCube -> Ra -> 횃불 신전으로 바꾸기를 눌러주세요.", "확인");
                return;
            }

            GameObject root = RaTempleRoot();
            Darken(root);
            SetupHud(root);
            DisableTestSquare();
            RaSunBoatBuilder.Build();
            RaPolishExtras.AddShrineForeshadow();
            RaPolishExtras.LinkIcons();

            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("[RaPolishSetup] 완료 -> Ctrl+S로 씬 저장");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RaPolishSetup] 오류: {e}");
        }
    }

    private static GameObject RaTempleRoot()
    {
        GameObject root = GameObject.Find("RaTemple");
        if (root != null) return root;
        root = new GameObject("RaTemple");
        Undo.RegisterCreatedObjectUndo(root, "라의 신전");
        return root;
    }

    // ================= 1. 어둠 =================
    private static void Darken(GameObject root)
    {
        RaDarkness dark = root.GetComponent<RaDarkness>();
        if (dark == null) dark = Undo.AddComponent<RaDarkness>(root);
        Edit(dark, so => so.FindProperty("unlitMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(UnlitPath));

        foreach (Light2D light in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (light.lightType != Light2D.LightType.Global) continue;
            Edit(light, so => so.FindProperty("m_Intensity").floatValue = DayGlobal);
        }

        RaNightAtmosphere night = Object.FindAnyObjectByType<RaNightAtmosphere>(FindObjectsInactive.Include);
        if (night != null) Edit(night, so => so.FindProperty("nightGlobalIntensity").floatValue = NightGlobal);

        PlayerTorchTemp torch = Object.FindAnyObjectByType<PlayerTorchTemp>(FindObjectsInactive.Include);
        if (torch != null) Edit(torch, so =>
        {
            so.FindProperty("baseRadius").floatValue = 3.2f;
            so.FindProperty("intensity").floatValue = 1.25f;
        });
    }

    // ================= 2. 글자 정리 =================
    private static void SetupHud(GameObject root)
    {
        if (root.GetComponent<RaHudDirector>() == null) Undo.AddComponent<RaHudDirector>(root);

        RaTorchManager manager = root.GetComponent<RaTorchManager>();
        if (manager != null) Edit(manager, so => so.FindProperty("nightMessage").stringValue = "");
    }

    // ================= 3. 테스트 네모 =================
    private static void DisableTestSquare()
    {
        foreach (Inspectable insp in Object.FindObjectsByType<Inspectable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(insp);
            if (so.FindProperty("objectId").stringValue != "TEST_SAND") continue;
            Undo.RecordObject(insp.gameObject, "테스트 네모 끄기");
            insp.gameObject.SetActive(false);
            Debug.Log($"[RaPolishSetup] 테스트용 '{insp.name}'(TEST_SAND) 끔 (지우지는 않음)");
        }
    }

    // ================= 도우미 =================
    public static void Edit(Object target, Action<SerializedObject> change)
    {
        Undo.RecordObject(target, "라의 방 다듬기");
        var so = new SerializedObject(target);
        change(so);
        so.ApplyModifiedProperties();
    }
}
