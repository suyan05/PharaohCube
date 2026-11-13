// [추가] 바스테트 스토리(벽화 4장, 방 연출, 검은 고양이 길잡이, 이야기 비석) 플래그 모음
// - 문자열 오타 방지용 상수 + 플래그 읽기/쓰기 도우미
// - 스토리 기획서 B안 '라의 열두 시간' 바스테트의 방 (밤 3~4시) 기준
public static class BastetStoryIds
{
    public const int MuralCount = 4;

    // 벽화 n번을 봄 (1~4)
    public static string MuralFlag(int n) => $"F_BASTET_MURAL_{n}";
    // 벽화 4장을 모두 봄 -> 방 스토리 연출 시작
    public const string FlagMuralsAll = "F_BASTET_MURALS_ALL";
    // 방 스토리 연출을 다 봄 -> 검은 고양이 길잡이 등장
    public const string FlagRoomStory = "F_BASTET_ROOM_STORY";

    // 이야기 비석 n번을 해독함 (1~3)
    public static string StoryStele(int n) => $"F_BASTET_STORY_STELE_{n}";

    // BastetGoddess와 같은 플래그 (여신 소개 / 마무리 대사)
    public const string FlagGoddessIntro = "F_BASTET_GODDESS_INTRO";
    public const string FlagGoddessEnd = "F_BASTET_GODDESS_END";

    // 메인 클리어 후 고양이가 플레이어를 따라감 (중앙 홀에서 이어 쓸 수 있게 남겨 둠)
    public const string FlagCatFollow = "F_BASTET_CAT_FOLLOW";

    // ================= 도우미 =================
    public static bool Has(string flag) => !string.IsNullOrEmpty(flag) && GameManager.Instance != null && GameManager.Instance.HasFlag(flag);

    public static void Set(string flag)
    {
        if (string.IsNullOrEmpty(flag) || GameManager.Instance == null || GameManager.Instance.HasFlag(flag)) return;
        GameManager.Instance.SetFlag(flag);
    }

    public static int MuralsSeen()
    {
        int n = 0;
        for (int i = 1; i <= MuralCount; i++) if (Has(MuralFlag(i))) n++;
        return n;
    }
}
