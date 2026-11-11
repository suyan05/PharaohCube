using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬이 열릴 때 숨은 방 입구를 자동으로 달아 줌 (배치 메뉴를 다시 돌리거나 씬을 손으로 고치지 않아도 됨)
// - 라의 방: 태양의 배 모형(RaSunBoat)이 있는 씬이면 그 자리에 뚜껑돌(RaHiddenHatch)을 만듦
// - 호루스의 방: 매의 회랑(HorusHiddenGallery) 안의 둥지(OBJ_HG1_Nest)에 HorusNestVault를 붙임
public static class HiddenVaultSpawner
{
    private const string NestName = "OBJ_HG1_Nest";
    private const string HatchName = "OBJ_RA_HiddenHatch";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Attach();

    private static void Attach()
    {
        try
        {
            AttachRa();
            AttachHorus();
        }
        catch (Exception e)
        {
            Debug.LogError($"[숨은 방] 입구 달기 오류: {e}");
        }
    }

    private static void AttachRa()
    {
        RaSunBoat boat = UnityEngine.Object.FindFirstObjectByType<RaSunBoat>(FindObjectsInactive.Include);
        if (boat == null || UnityEngine.Object.FindFirstObjectByType<RaHiddenHatch>(FindObjectsInactive.Include) != null) return;
        var go = new GameObject(HatchName);
        go.transform.position = boat.transform.position + new Vector3(0f, 0.2f, 0f);
        go.AddComponent<RaHiddenHatch>();
    }

    private static void AttachHorus()
    {
        HorusHiddenGallery gallery = UnityEngine.Object.FindFirstObjectByType<HorusHiddenGallery>(FindObjectsInactive.Include);
        if (gallery == null) return;
        foreach (Transform t in gallery.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != NestName || t.GetComponent<HorusNestVault>() != null) continue;
            t.gameObject.AddComponent<HorusNestVault>();
            return;
        }
    }
}
