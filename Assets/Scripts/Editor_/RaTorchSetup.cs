using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// 라의 방을 "어두운 신전 + 내 횃불로 벽 횃불 붙이기"로 바꾸는 메뉴 (Puzzle_Test_Ra_Tilemap 씬을 열고 실행)
// 메뉴: PharaohCube -> Ra -> 횃불 신전으로 바꾸기   (여러 번 눌러도 안전, Ctrl+Z로 되돌리기 가능)
//
// 하는 것
// 1. Light_Manager 아래 벽 횃불(Light_off_0...) 9개에 WallTorch + 조사용 트리거 (그 방 퍼즐 완료 플래그로 자동 점등도)
// 2. RaZoneLighting: 횃불 켜기는 빼고 문 열기(Turn Off)와 카메라 연출만 남김, 알림 문구를 "문이 열렸다"로
// 3. 전역 조명 어둡게 (낮 0.22 / 밤 0.3)
// 4. 예전 입장 연출(RaIntroCutscene) 끄고 새 입장 연출(RaTempleEntry) + RaTorchManager
// 5. 플레이어에 횃불(PlayerTorchTemp) - 이 씬만 바로 Play해도 횃불을 들고 시작
// 6. 목표 문구(RaObjectiveGuide) 맨 앞에 "벽 횃불 붙이기" 단계 추가, 밤 첫 단계 문구 수정
public static class RaTorchSetup
{
    private const string LightOnPath = "Assets/image/Test/Object/Light_on.png";

    // 방 이름 -> (낮 자동 점등 플래그, 밤 자동 점등 플래그)
    private static readonly Dictionary<string, string[]> RoomFlags = new Dictionary<string, string[]>
    {
        { "LightObject_P0", new[] { "", "" } },
        { "LightObject_P1", new[] { "F_RA_P1_DONE", "F_RA_NIGHT_P1_DONE" } },
        { "LightObject_P2", new[] { "F_RA_CIRCUIT_4", "" } },
        { "LightObject_P3", new[] { "F_RA_P3_DONE", "F_RA_NIGHT_P3_DONE" } },
        { "LightObject_P4", new[] { "F_RA_P4_DONE", "F_RA_NIGHT_P4_DONE" } },
        { "LightObject_P5", new[] { "F_RA_P5_DONE", "F_RA_NIGHT_P5_DONE" } }
    };

    [MenuItem("PharaohCube/Ra/횃불 신전으로 바꾸기")]
    private static void Apply()
    {
        try
        {
            GameObject manager = GameObject.Find("Light_Manager");
            if (manager == null)
            {
                EditorUtility.DisplayDialog("라의 방", "Light_Manager가 없어요. Puzzle_Test_Ra_Tilemap 씬을 열고 실행해주세요.", "확인");
                return;
            }

            List<WallTorch> torches = SetupWallTorches(manager.transform);
            RaTorchSceneSetup.StripZoneLighting();
            RaTorchSceneSetup.DarkenLights();
            Transform player = RaTorchSceneSetup.SetupPlayerTorch();
            RaTorchSceneSetup.SetupEntry(Nearest(torches, player));
            RaTorchSceneSetup.SetupObjectiveGuide();

            EditorSceneManager.MarkSceneDirty(manager.scene);
            Debug.Log($"[RaTorchSetup] 완료: 벽 횃불 {torches.Count}개 -> Ctrl+S로 씬 저장");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RaTorchSetup] 오류: {e}");
        }
    }

    // ================= 벽 횃불 =================
    private static List<WallTorch> SetupWallTorches(Transform manager)
    {
        var result = new List<WallTorch>();
        Sprite onSprite = LoadFirstSprite(LightOnPath);
        foreach (Transform room in manager)
        {
            if (!RoomFlags.TryGetValue(room.name, out string[] flags)) continue;
            int index = 0;
            foreach (Transform child in room)
            {
                if (!child.name.StartsWith("Light_off")) continue;
                result.Add(SetupTorch(child.gameObject, $"{room.name.Replace("LightObject_", "")}_{index++}", flags, onSprite));
            }
        }
        return result;
    }

    private static WallTorch SetupTorch(GameObject go, string id, string[] flags, Sprite onSprite)
    {
        BoxCollider2D box = go.GetComponent<BoxCollider2D>();
        if (box == null) box = Undo.AddComponent<BoxCollider2D>(go);
        box.isTrigger = true;

        WallTorch torch = go.GetComponent<WallTorch>();
        if (torch == null) torch = Undo.AddComponent<WallTorch>(go);

        Light2D light = go.GetComponentInChildren<Light2D>(true);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        var so = new SerializedObject(torch);
        so.FindProperty("torchId").stringValue = id;
        so.FindProperty("target").objectReferenceValue = sr;
        so.FindProperty("offSprite").objectReferenceValue = sr != null ? sr.sprite : null;
        so.FindProperty("onSprite").objectReferenceValue = onSprite;
        so.FindProperty("lightSource").objectReferenceValue = light;
        if (light != null && light.intensity > 0.1f) so.FindProperty("litIntensity").floatValue = light.intensity;
        so.FindProperty("dayAutoFlag").stringValue = flags[0];
        so.FindProperty("nightAutoFlag").stringValue = flags[1];
        so.ApplyModifiedProperties();
        return torch;
    }

    private static WallTorch Nearest(List<WallTorch> torches, Transform player)
    {
        WallTorch best = null;
        float bestDist = float.MaxValue;
        Vector3 from = player != null ? player.position : Vector3.zero;
        foreach (WallTorch t in torches)
        {
            float d = (t.transform.position - from).sqrMagnitude;
            if (d >= bestDist) continue;
            bestDist = d;
            best = t;
        }
        return best;
    }

    private static Sprite LoadFirstSprite(string path)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (o is Sprite s) return s;
        }
        Debug.LogWarning($"[RaTorchSetup] 켜진 횃불 그림 없음: {path} (꺼진 그림 그대로 + 빛만 켜짐)");
        return null;
    }
}
