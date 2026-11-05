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
    public const string FlagMainADone = "F_SOBEK_MAIN_A_DONE";    // 범람기 구역 통과
    public const string FlagMainDone = "F_SOBEK_MAIN_DONE";       // 소베크-라 제단 도착
    public const string FlagDrainPlayed = "F_SOBEK_DRAIN_PLAYED"; // 허브 물 빠짐 연출 봄
    public const string FlagPlateGiven = "F_SOBEK_PLATE_GIVEN";

    // 씬 이름 (Build Profiles -> Scene List에 있어야 함)
    public const string HubScene = "Sobek_Hub";
    public const string MainScene = "Sobek_Main";
    public const string AnubisScene = "Puzzle_Test_Anubis_Tutorial";
}
