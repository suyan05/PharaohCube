// 소베크 방 아이템 / 플래그 / 씬 이름 (문자열 오타 방지용 상수)
public static class SobekIds
{
    // 서브 퍼즐 보상: 악어 부적 4개 (허브의 큰 문에 넣음)
    public const string AmuletRotate = "ITEM_SOBEK_AMULET_ROTATE"; // S1 소용돌이 섬
    public const string AmuletLift = "ITEM_SOBEK_AMULET_LIFT";     // S2 악어 등 다리
    public const string AmuletWater = "ITEM_SOBEK_AMULET_WATER";   // S3 범람하는 나일
    public const string AmuletMirror = "ITEM_SOBEK_AMULET_MIRROR"; // S4 두 개의 수면
    public const string Plate = "PLATE_SOBEK";                     // 메인 보상: 악어 형판
    public const string Tooth = "ITEM_SOBEK_TOOTH";                // 히든: 소베크의 이빨 (업적 목록에 이미 있음)

    public static readonly string[] Amulets = { AmuletRotate, AmuletLift, AmuletWater, AmuletMirror };

    // 진행 플래그
    public const string FlagS1Clear = "F_SOBEK_S1_CLEAR";
    public const string FlagS2Clear = "F_SOBEK_S2_CLEAR";
    public const string FlagS3Clear = "F_SOBEK_S3_CLEAR";
    public const string FlagS4Clear = "F_SOBEK_S4_CLEAR";
    public const string FlagDoorOpen = "F_SOBEK_DOOR_OPEN";       // 큰 문에 부적 4개를 넣음
    public const string FlagMainADone = "F_SOBEK_MAIN_A_DONE";    // 첫 구역(범람기) 통과
    public const int MainZoneA = 0;
    public const string FlagMainDone = "F_SOBEK_MAIN_DONE";       // 소베크-라 제단 도착
    public const string FlagDrainPlayed = "F_SOBEK_DRAIN_PLAYED"; // 허브 물 빠짐 연출 봄
    public const string FlagPlateGiven = "F_SOBEK_PLATE_GIVEN";

    // 숨은 방 · 단서 · 악어 (통합 길잡이 기획서 8.5 플래그 규칙: {ROOM} = SO)
    public const string Room = "SO";
    public static string ClueGot(int n) => $"F_SO_CLUE_{n}_GOT";   // 단서 n 주움
    public static string ClueSet(int n) => $"F_SO_CLUE_{n}_SET";   // 단서 n을 수위계 기둥에 끼움
    public const string FlagSocketSeen = "F_SO_SOCKET_SEEN";       // 물에 잠긴 수위계 기둥을 봄
    public const string FlagGuideMet = "F_SO_GUIDE_MET";           // 악어 첫 만남
    public static string GuideLegDone(int n) => $"F_SO_GUIDE_LEG{n}_DONE";
    public const string FlagHiddenOpen = "F_SO_HIDDEN_OPEN";       // 숨은 방(물 빠진 하층) 열림
    public const string FlagToothGot = "HIDDEN_SobekTooth_GOT";    // 히든 아이템 획득 (HiddenItemService 규칙)
    public const string FlagTabletReady = "F_SO_TABLET_READY";     // 형판을 받음 -> 중앙 홀 받침에서 악어 석판
    public const string FlagHubIntro = "F_SO_HUB_INTRO";           // 허브 첫 입장 대사 봄
    public const string FlagEndingDone = "F_SOBEK_ENDING_DONE";    // 형판 공개·큐브 연출 끝

    // 씬 이름 (Build Profiles -> Scene List에 있어야 함)
    public const string HubScene = "Sobek_Hub";
    public const string MainScene = "Sobek_Main";
    // 착시 퍼즐 씬 (허브 섬 게이트 -> 여기, Tools/Sobek/5가 만듦)
    public const string SubScene1 = "Sobek_S1", SubScene2 = "Sobek_S2", SubScene3 = "Sobek_S3", SubScene4 = "Sobek_S4";
    public const string AnubisScene = "Puzzle_Test_Anubis_Tutorial";
    public const string CentralHallScene = "CentralRoom_Temp"; // 기획서: 신전을 깨면 중앙 홀로 돌아감 (지금 빌드 목록의 중앙 홀 [확인 필요])
}
