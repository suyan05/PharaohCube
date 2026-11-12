using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬이 열리면 호루스 횃대(HO_Sconce_On / HO_Sconce_Off 그림)를 찾아 움직이는 픽셀 불꽃을 자동으로 붙임
// - 빌더 메뉴를 다시 안 돌려도 됨. 그림 이름으로 찾기 때문에 다른 방에는 안 붙음
// - 그림은 Resources/Horus/PixelFx/ 에서 읽음 (HO_Flame_0~7, HO_Sconce_Base)
public static class HorusPixelFlameAutoAttach
{
    private const string Folder = "Horus/PixelFx/";
    private const string OnName = "HO_Sconce_On";
    private const string OffName = "HO_Sconce_Off";
    private const int FrameCount = 8;

    private static Sprite[] frames;
    private static Sprite baseSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttachAll();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AttachAll();

    private static void AttachAll()
    {
        try
        {
            if (!LoadArt()) return;
            SpriteRenderer[] all = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (SpriteRenderer sr in all)
            {
                if (!IsSconce(sr) || sr.GetComponent<HorusPixelFlame>() != null) continue;
                sr.gameObject.AddComponent<HorusPixelFlame>().Setup(frames, baseSprite);
            }
            HorusRuntimeTune.Apply(); // [추가] 비석 표식, 석상 방향, 달의 눈 자리, 꺼진 횃대, 방 밝기
        }
        catch (Exception e)
        {
            Debug.LogError($"[픽셀 불꽃] 자동 붙이기 실패: {e.Message}");
        }
    }

    private static bool IsSconce(SpriteRenderer sr)
    {
        if (sr == null || sr.sprite == null) return false;
        string n = sr.sprite.name;
        return n == OnName || n == OffName;
    }

    private static bool LoadArt()
    {
        if (frames != null && baseSprite != null) return true;
        var list = new Sprite[FrameCount];
        for (int i = 0; i < FrameCount; i++)
        {
            list[i] = Resources.Load<Sprite>($"{Folder}HO_Flame_{i}");
            if (list[i] == null) { Debug.LogWarning($"[픽셀 불꽃] 그림 없음: {Folder}HO_Flame_{i}"); return false; }
        }
        baseSprite = Resources.Load<Sprite>($"{Folder}HO_Sconce_Base");
        if (baseSprite == null) { Debug.LogWarning($"[픽셀 불꽃] 그림 없음: {Folder}HO_Sconce_Base"); return false; }
        frames = list;
        return true;
    }
}
