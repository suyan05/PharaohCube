// 바스테트 실전 방 아이템 / 플래그 ID (문자열 오타 방지용 상수)
public static class BastetItemIds
{
    public const string Paw = "ITEM_BASTET_PAW";             // B1 발자국 보상: 은 발자국 인장
    public const string Eye = "ITEM_BASTET_EYE";             // B2 고양이 눈 보상: 호박 눈
    public const string Necklace = "ITEM_BASTET_NECKLACE";   // B3 목걸이 보상: 청금석 목걸이
    public const string Claw = "ITEM_BASTET_CLAW";           // B4 발톱 보상: 발톱 인장
    public const string Plate = "PLATE_BASTET";              // B5 메인 보상: 고양이 형판 (BastetSunMoonMemoryPuzzle가 지급)
    public const string Vessel = "ITEM_BASTET_VESSEL";       // [추가] 히든: 바스테트의 그릇 (팀 ItemCatalog 이름 규칙 ITEM_*)
    public const string ReedPen = "KEY_REEDPEN";             // [추가] 공물 기록함 -> 토트 통로 열쇠

    public const string FlagHallRead = "F_CLUE_BASTET_HALL_READ"; // 입구 비석 읽음
    public const string FlagMainDone = "F_BASTET_MAIN_DONE";      // B5 클리어
    public const string FlagIntroPlayed = "F_BASTET_INTRO_PLAYED";

    // [추가] 서브 퍼즐 클리어 기록 (보상 아이템을 봉인석에 넣어 사라져도 진행이 유지되게)
    public const string FlagB1Clear = "F_BASTET_B1_CLEAR";
    public const string FlagB2Clear = "F_BASTET_B2_CLEAR";
    public const string FlagB3Clear = "F_BASTET_B3_CLEAR";
    public const string FlagB4Clear = "F_BASTET_B4_CLEAR";
    public const string FlagShrineOpen = "F_BASTET_SHRINE_OPEN"; // 신상에 발톱 인장을 넣음

    public static string ClearFlag(int n) => n == 1 ? FlagB1Clear : n == 2 ? FlagB2Clear : n == 3 ? FlagB3Clear : n == 4 ? FlagB4Clear : "";
}
