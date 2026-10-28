using UnityEngine;
using UnityEngine.SceneManagement;

// [추가] 바스테트 방 소리 모음 (팀 공용 GameAudio 사용 -> Resources/Audio/<이름>.wav)
// 이름만 여기 모아 두고, 퍼즐 코드에서는 BastetSfx.Correct(2) 처럼 부름
// 더 좋은 소리가 오면 같은 이름 파일로 덮어쓰면 코드 수정 없이 바뀜
public static class BastetSfx
{
    // 소리 파일 이름 (Assets/Resources/Audio/)
    public const string PlatePress = "sfx_bastet_plate_press";   // 발판이 '턱' 눌림
    public const string PlateShow = "sfx_bastet_plate_show";     // 보여 주기 (발판/석상 빛남)
    public const string CorrectName = "sfx_bastet_correct";      // 맞게 밟음/누름
    public const string WrongName = "sfx_bastet_wrong";          // 틀림 (하악)
    public const string RoundClearName = "sfx_bastet_round_clear"; // 단계 통과
    public const string EyeName = "sfx_bastet_eye";              // 석상 눈 빛남
    public const string NightName = "sfx_bastet_night";          // 해가 짐
    public const string ClearName = "sfx_bastet_clear";          // 퍼즐 클리어
    public const string PlateMount = "sfx_bastet_plate_mount";   // 형판이 큐브에 박힘
    public const string EndingChime = "sfx_bastet_ending_chime"; // 엔딩 일러스트 컷
    public const string GateOpen = "sfx_rumble";                 // 봉인석 열림 (팀 공용 쿠르릉)
    public const string MusicTemple = "bgm_bastet_temple";
    public const string MusicEnding = "bgm_bastet_ending";

    // 음 높이: 번호마다 다르게 (히자즈 음계 비율) -> 어느 발판인지 소리로도 구분
    private static readonly float[] Scale = { 1f, 1.0667f, 1.25f, 1.3333f, 1.5f, 1.6f, 1.7778f, 2f };

    public static void Press() => GameAudio.PlayThrottled(PlatePress, 0.08f, 0.9f);
    public static void Show(int index) => GameAudio.Play(PlateShow, 0.7f, Pitch(index) * 0.75f);
    public static void Eye(int index) => GameAudio.Play(EyeName, 0.6f, Pitch(index) * 0.75f);
    public static void Correct(int step) => GameAudio.Play(CorrectName, 0.85f, Pitch(step) * 0.75f); // 단계가 갈수록 높아짐
    public static void Wrong() => GameAudio.Play(WrongName, 0.9f);
    public static void RoundClear() => GameAudio.Play(RoundClearName, 0.85f);
    public static void Night() => GameAudio.Play(NightName, 0.9f);
    public static void Clear() => GameAudio.Play(ClearName, 1f);
    public static void Mount() => GameAudio.Play(PlateMount, 1f);
    public static void Chime() => GameAudio.Play(EndingChime, 0.9f);
    public static void Gate() => GameAudio.Play(GateOpen, 0.8f);

    private static float Pitch(int i) => Scale[Mathf.Abs(i) % Scale.Length];

    // ================= 바스테트 씬 배경음악 =================
    // 팀 GameAudioDirector가 씬별 음악을 깔고, 바스테트 씬은 여기서 깜 (팀 코드는 수정 안 함)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= HandleScene;
        SceneManager.sceneLoaded += HandleScene;
        HandleScene(SceneManager.GetActiveScene(), LoadSceneMode.Single); // Play로 바로 연 씬
    }

    private static void HandleScene(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || !scene.name.Contains("Bastet")) return;
        GameAudio.Music(MusicTemple);
        Debug.Log($"[BastetSfx] {scene.name}: 배경음악 {MusicTemple}");
    }
}
