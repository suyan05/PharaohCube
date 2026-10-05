// 호루스의 방 아이템 ID 모음 (호루스 기획서 4.1). 라의 방 ItemIds와 같은 역할
public static class HorusItemIds
{
    public const string Feather = "ITEM_FEATHER";             // 매의 깃털 (H2-1 보상 -> H1 사용)
    public const string StarFrag1 = "HO_STAR_FRAG_1";         // 별 조각 1 (H1 보상 -> 성도 D1)
    public const string SilverThread = "ITEM_SILVER_THREAD";  // 은빛 실 (H2-2 보상 -> H3 사용)
    public const string StarFrag2 = "HO_STAR_FRAG_2";         // 별 조각 2 (H3 보상 -> 성도 D2)
    public const string MoonEye = "ITEM_MOON_EYE";            // 달의 눈 (H3 보상 -> 장착대)
    public const string SealFalcon = "ITEM_SEAL_FALCON";      // 매 인장 (H2-3 보상 -> H4 사용)
    public const string StarFrag3 = "HO_STAR_FRAG_3";         // 별 조각 3 (H4 보상 -> 성도 D3)

    public const string PlateHorus = "PLATE_HORUS";           // 호루스 형판 (H5 보상 -> 큐브 장착). F5 목록 제외

    public static readonly string[] All =
    {
        Feather, StarFrag1, SilverThread, StarFrag2, MoonEye, SealFalcon, StarFrag3
    };
}