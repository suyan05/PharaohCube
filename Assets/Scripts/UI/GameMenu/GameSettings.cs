using System;
using System.Collections.Generic;
using UnityEngine;

// [추가] 게임 설정 값 (소리 / 화면 / 언어) - PlayerPrefs에 저장, 바꾸면 바로 적용
// 시작 화면과 게임 중 설정 창(SettingsWindow)이 같이 씀
public enum GameLanguage { Korean = 0, English = 1 }

public static class GameSettings
{
    private const string KeyMaster = "PC_Set_Master";
    private const string KeyBgm = "PC_Set_Bgm";
    private const string KeySfx = "PC_Set_Sfx";
    private const string KeyResW = "PC_Set_ResW";
    private const string KeyResH = "PC_Set_ResH";
    private const string KeyFull = "PC_Set_Fullscreen";
    private const string KeyVSync = "PC_Set_VSync";
    private const string KeyLang = "PC_Set_Lang";

    // 기본값
    private const float DefaultMaster = 1f, DefaultBgm = 0.7f, DefaultSfx = 0.8f;

    public static float Master { get; private set; } = DefaultMaster;   // 전체 음량 (AudioListener)
    public static float Bgm { get; private set; } = DefaultBgm;         // 배경음 (SoundManager)
    public static float Sfx { get; private set; } = DefaultSfx;         // 효과음 (SoundManager)
    public static Vector2Int Resolution { get; private set; }
    public static bool Fullscreen { get; private set; } = true;
    public static bool VSync { get; private set; } = true;
    public static GameLanguage Language { get; private set; } = GameLanguage.Korean;

    public static event Action AudioChanged;     // 음량이 바뀜
    public static event Action LanguageChanged;  // 언어가 바뀜 (LocalizedText가 글자 갱신)

    private static bool loaded;
    private static List<Vector2Int> resolutions;

    // ================= 불러오기 / 저장 =================
    public static void Load()
    {
        if (loaded) return;
        loaded = true;
        Master = PlayerPrefs.GetFloat(KeyMaster, DefaultMaster);
        Bgm = PlayerPrefs.GetFloat(KeyBgm, DefaultBgm);
        Sfx = PlayerPrefs.GetFloat(KeySfx, DefaultSfx);
        Fullscreen = PlayerPrefs.GetInt(KeyFull, 1) == 1;
        VSync = PlayerPrefs.GetInt(KeyVSync, 1) == 1;
        Language = (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt(KeyLang, 0), 0, 1);
        int w = PlayerPrefs.GetInt(KeyResW, 0), h = PlayerPrefs.GetInt(KeyResH, 0);
        Resolution = w > 0 && h > 0 ? new Vector2Int(w, h) : CurrentScreenSize();
    }

    private static void Save()
    {
        PlayerPrefs.SetFloat(KeyMaster, Master);
        PlayerPrefs.SetFloat(KeyBgm, Bgm);
        PlayerPrefs.SetFloat(KeySfx, Sfx);
        PlayerPrefs.SetInt(KeyResW, Resolution.x);
        PlayerPrefs.SetInt(KeyResH, Resolution.y);
        PlayerPrefs.SetInt(KeyFull, Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(KeyVSync, VSync ? 1 : 0);
        PlayerPrefs.SetInt(KeyLang, (int)Language);
        PlayerPrefs.Save();
    }

    // 게임 시작 때 한 번 (GameMenuBootstrap)
    public static void ApplyAll()
    {
        Load();
        ApplyAudio();
        ApplyDisplay();
    }

    // ================= 소리 =================
    public static void SetMaster(float v) { Load(); Master = Mathf.Clamp01(v); ApplyAudio(); Save(); }
    public static void SetBgm(float v) { Load(); Bgm = Mathf.Clamp01(v); ApplyAudio(); Save(); }
    public static void SetSfx(float v) { Load(); Sfx = Mathf.Clamp01(v); ApplyAudio(); Save(); }

    private static void ApplyAudio()
    {
        AudioListener.volume = Master; // 팀원 소리(CineAudio 등)도 전체 음량은 같이 따라감
        AudioChanged?.Invoke();
    }

    // ================= 화면 =================
    public static void SetResolution(Vector2Int r) { Load(); Resolution = r; ApplyDisplay(); Save(); }
    public static void SetFullscreen(bool on) { Load(); Fullscreen = on; ApplyDisplay(); Save(); }
    public static void SetVSync(bool on) { Load(); VSync = on; ApplyDisplay(); Save(); }

    private static void ApplyDisplay()
    {
        QualitySettings.vSyncCount = VSync ? 1 : 0;
#if !UNITY_EDITOR
        // 에디터 Game 창에는 적용되지 않음 (빌드에서만)
        Screen.SetResolution(Resolution.x, Resolution.y, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
#endif
    }

    // 고를 수 있는 해상도 (모니터가 지원하는 것 중 16:9 위주, 중복 제거)
    public static List<Vector2Int> Resolutions
    {
        get
        {
            if (resolutions != null) return resolutions;
            resolutions = new List<Vector2Int>();
            foreach (UnityEngine.Resolution r in Screen.resolutions)
            {
                var v = new Vector2Int(r.width, r.height);
                if (v.x < 1024 || v.y < 576 || resolutions.Contains(v)) continue;
                resolutions.Add(v);
            }
            if (resolutions.Count == 0) // 에디터 등에서 목록이 비면 기본 목록
            {
                resolutions.Add(new Vector2Int(1280, 720));
                resolutions.Add(new Vector2Int(1600, 900));
                resolutions.Add(new Vector2Int(1920, 1080));
                resolutions.Add(new Vector2Int(2560, 1440));
            }
            resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return resolutions;
        }
    }

    public static int ResolutionIndex()
    {
        List<Vector2Int> list = Resolutions;
        int best = 0;
        for (int i = 0; i < list.Count; i++)
            if (Mathf.Abs(list[i].x - Resolution.x) + Mathf.Abs(list[i].y - Resolution.y) <
                Mathf.Abs(list[best].x - Resolution.x) + Mathf.Abs(list[best].y - Resolution.y)) best = i;
        return best;
    }

    private static Vector2Int CurrentScreenSize()
    {
        UnityEngine.Resolution r = Screen.currentResolution;
        return r.width > 0 ? new Vector2Int(r.width, r.height) : new Vector2Int(1920, 1080);
    }

    // ================= 언어 =================
    public static void SetLanguage(GameLanguage lang)
    {
        Load();
        if (Language == lang) return;
        Language = lang;
        Save();
        LanguageChanged?.Invoke();
    }

    // ================= 기본값 =================
    public static void ResetToDefault()
    {
        Load();
        Master = DefaultMaster; Bgm = DefaultBgm; Sfx = DefaultSfx;
        VSync = true; Fullscreen = true; Resolution = CurrentScreenSize();
        ApplyAudio();
        ApplyDisplay();
        Save();
        Debug.Log("[GameSettings] 기본값으로 되돌림 (언어는 유지)");
    }
}
