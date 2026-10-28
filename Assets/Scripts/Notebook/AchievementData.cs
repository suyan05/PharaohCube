using System.Collections.Generic;

// 고고학 수첩 업적(유물 카드) 목록 - 기획서 "[기획·아트] 고고학 수첩 업적 시스템" 2장 그대로 24개
// - 조건은 플래그로 판정 (AchievementSystem이 0.5초마다 확인). 조건이 비어 있는 것은 각 방 담당이
//   해당 순간에 AchievementSystem.Unlock("ACH_...") 한 줄을 부르면 됨
// - [확인 필요] 바스테트, 토트, 호루스, 소베크, 아누비스, 엔딩 업적의 플래그 이름 (각 담당자)
public enum AchGrade { Papyrus, Bronze, Gold }

public class AchievementDef
{
    public string id, chapter, title, relic, text, hint;
    public AchGrade grade;
    public string[] anyFlags = new string[0];   // 하나라도 켜지면
    public string[] allFlags = new string[0];   // 전부 켜지면
    public string[] noneFlags = new string[0];  // 이게 켜져 있으면 안 됨
    public string[] allItems = new string[0];   // 이 아이템을 전부 가지면
    public bool allOthers;                      // 다른 업적 전부
}

public static class AchievementData
{
    public static readonly string[] Chapters = { "프롤로그", "라", "바스테트", "토트", "호루스", "소베크", "아누비스", "특별" };

    // [확인 필요] 다른 방 히든 아이템 ID (바스테트 그릇만 확정)
    public static readonly string[] HiddenItems = { "ITEM_RA_EMBER", "ITEM_BASTET_VESSEL", "ITEM_THOTH_TABLET", "ITEM_HORUS_BLESSING", "ITEM_SOBEK_TOOTH" };

    private static List<AchievementDef> all;

    public static List<AchievementDef> All
    {
        get
        {
            if (all != null) return all;
            all = new List<AchievementDef>();
            AddPrologueAndRa(all);
            AddOtherRooms(all);
            AddSpecial(all);
            return all;
        }
    }

    private static void AddPrologueAndRa(List<AchievementDef> l)
    {
        l.Add(Any("ACH_PRO_BRUSH", "프롤로그", "첫 붓질", "채색 토기 조각", AchGrade.Papyrus, "모든 발굴은 붓 한 번에서 시작된다.", "모래 아래를 살펴보자", "F_PRO_OBJ_SAND_01_DONE", "F_PRO_OBJ_SAND_02_DONE", "F_PRO_OBJ_SAND_03_DONE"));
        l.Add(Any("ACH_PRO_SCARAB", "프롤로그", "아침의 풍뎅이", "청록 스카라브 부적", AchGrade.Papyrus, "해를 굴려 올리는 풍뎅이. 이집트인은 매일 아침 태양이 다시 태어난다고 믿었다.", "발굴 구역 어딘가", "F_PRO_SCARAB_FOUND"));
        l.Add(Any("ACH_PRO_RIDDLE", "프롤로그", "지평선의 수수께끼", "꿈의 비석", AchGrade.Bronze, "스핑크스 앞발 사이에 선 비석. 한 왕자가 꾼 꿈이 새겨져 있다.", "꿈속의 문지기", "F_PRO_STELE_DECODED"));
        l.Add(Any("ACH_RA_P1", "라", "그림자를 읽는 자", "그림자 시계", AchGrade.Papyrus, "해가 높을수록 그림자는 짧아진다. 고대 이집트인은 그림자로 시간을 재었다.", "라의 방 동쪽", "F_RA_P1_DONE"));
        l.Add(Any("ACH_RA_P2", "라", "빛의 길", "청동 손거울", AchGrade.Papyrus, "광을 낸 청동 거울. 태양 원반을 닮은 둥근 거울은 빛과 생명을 뜻했다.", "라의 방 서쪽", "F_RA_CIRCUIT_4"));
        l.Add(Any("ACH_RA_P3", "라", "세 개의 태양", "날개 달린 태양 원반", AchGrade.Papyrus, "신전 문 위에서 악한 것을 막던 날개 달린 태양.", "라의 방 북서쪽", "F_RA_P3_DONE"));
        l.Add(Any("ACH_RA_P4", "라", "하루의 순서", "채색 벽화 조각", AchGrade.Papyrus, "무덤 벽에 그려진 하루. 해는 동쪽에서 태어나 서쪽에 눕는다.", "라의 방 남서쪽", "F_RA_P4_DONE"));
        l.Add(Any("ACH_RA_P5", "라", "태양의 다섯 걸음", "태양의 배 모형", AchGrade.Bronze, "라는 밤마다 배를 타고 저승의 강을 건넌다.", "라의 방 북쪽", "F_RA_P5_DONE"));
        AchievementDef night = Any("ACH_RA_NIGHT", "라", "밤의 시련", "숫양 머리 밤 태양 부적", AchGrade.Bronze, "밤의 라는 숫양의 머리를 하고 어둠을 건넌다.", "해가 진 뒤의 라의 방");
        night.allFlags = new[] { "F_RA_NIGHT_P1_DONE", "F_RA_NIGHT_P3_DONE", "F_RA_NIGHT_P4_DONE", "F_RA_NIGHT_P5_DONE" };
        l.Add(night);
        l.Add(Any("ACH_RA_CLEAR", "라", "라의 형판", "황금 태양 형판", AchGrade.Bronze, "큐브의 첫 면. 태양의 신이 시험을 통과한 자에게 내어준 금판.", "라의 시련을 끝내자", "F_RA_PLATE_GIVEN"));
    }

    private static void AddOtherRooms(List<AchievementDef> l)
    {
        // [추가] 바스테트 (플래그: BastetItemIds - F_BASTET_B1~B4_CLEAR, F_BASTET_MAIN_DONE)
        l.Add(Any("ACH_BAS_B1", "바스테트", "고양이의 발걸음", "고양이 발자국 부조", AchGrade.Papyrus, "모래 위에 찍힌 작은 발자국. 고양이는 지나온 길을 잊지 않는다고 믿었다.", "발자국의 방", "F_BASTET_B1_CLEAR"));
        l.Add(Any("ACH_BAS_B2", "바스테트", "거꾸로 읽는 눈", "호박 고양이 눈 부적", AchGrade.Papyrus, "어둠에서도 빛나는 고양이의 눈. 밤을 지키는 눈으로 여겨 부적으로 지녔다.", "눈의 방", "F_BASTET_B2_CLEAR"));
        l.Add(Any("ACH_BAS_B3", "바스테트", "거울 같은 목걸이", "청금석 우세크 목걸이", AchGrade.Papyrus, "가슴을 덮는 넓은 목걸이. 왼쪽과 오른쪽이 꼭 같아야 아름답다고 여겼다.", "목걸이의 방", "F_BASTET_B3_CLEAR"));
        l.Add(Any("ACH_BAS_B4", "바스테트", "발톱의 문양", "발톱 긁힘 석판", AchGrade.Papyrus, "규칙대로 새긴 발톱 무늬. 반복되는 무늬는 영원을 뜻했다.", "발톱의 방", "F_BASTET_B4_CLEAR"));
        l.Add(Any("ACH_BAS_CLEAR", "바스테트", "고양이 여신의 축복", "청동 고양이상", AchGrade.Bronze, "금 귀걸이를 한 청동 고양이. 바스테트에게 바친 제물이다.", "해와 달의 기억을 풀자", "F_BASTET_MAIN_DONE"));
        AchievementDef vessel = Any("ACH_BAS_VESSEL", "바스테트", "향과 기름의 그릇", "설화석고 향유 그릇", AchGrade.Bronze, "바스테트는 향과 기름 그릇을 지키는 여신. 이 그릇에 담긴 불은 오래 탄다.", "???");
        vessel.allItems = new[] { "ITEM_BASTET_VESSEL" };
        l.Add(vessel);
        l.Add(Any("ACH_THO_1", "토트", "서기관의 손", "서기관 팔레트", AchGrade.Papyrus, "갈대 펜과 먹 두 개(검정, 빨강)를 담는 나무 팔레트. 글을 아는 자는 귀한 대접을 받았다.", "지식의 방 첫 관문"));
        l.Add(Any("ACH_THO_2", "토트", "달의 셈", "따오기 상", AchGrade.Papyrus, "달을 세는 신 토트의 새. 따오기의 부리는 초승달을 닮았다.", "지식의 방 두 번째 관문"));
        l.Add(Any("ACH_THO_CLEAR", "토트", "신성문자 해독가", "로제타석", AchGrade.Bronze, "세 가지 글자로 같은 내용을 새긴 돌. 이 돌 덕분에 상형문자를 다시 읽게 됐다.", "지식의 방을 끝내자"));
        l.Add(Any("ACH_HOR_STAR", "호루스", "별을 잇는 자", "덴데라 천장 별자리판", AchGrade.Papyrus, "신전 천장에 새겨진 하늘 지도. 별은 신들의 길이었다.", "밤하늘의 방"));
        l.Add(Any("ACH_HOR_CLEAR", "호루스", "호루스의 눈", "우자트 부적", AchGrade.Bronze, "상처에서 다시 온전해진 눈. 지키고 되살리는 힘의 상징.", "매의 방을 끝내자"));
        l.Add(Any("ACH_SOB_CLEAR", "소베크", "나일의 주인", "악어 신 소베크 상", AchGrade.Bronze, "나일의 물과 비옥함을 다스리던 악어 머리의 신.", "물의 방을 끝내자"));
        l.Add(Any("ACH_ANU_CLEAR", "아누비스", "마음의 무게", "심장의 무게 달기 파피루스", AchGrade.Bronze, "죽은 자의 심장을 깃털과 달아본다. 가벼우면 영원한 삶으로, 무거우면 암무트에게.", "저울 앞에 서라"));
    }

    private static void AddSpecial(List<AchievementDef> l)
    {
        AchievementDef gifts = Any("ACH_SP_GIFTS", "특별", "신들의 선물", "황금 가슴 장식(펙토랄)", AchGrade.Gold, "다섯 신의 상징을 엮어 만든 가슴 장식.", "???");
        gifts.allItems = HiddenItems;
        l.Add(gifts);
        l.Add(Any("ACH_SP_TWICE", "특별", "두 번의 판결", "암무트 부조", AchGrade.Gold, "죽은 자의 심장을 삼키는 짐승. 악어 머리, 사자 몸, 하마 다리.", "???"));
        l.Add(Any("ACH_SP_MUMMY", "특별", "영원한 잠", "사람 모양 관", AchGrade.Gold, "눈을 뜨지 못한 자는 이 안에서 영원히 잠든다.", "???"));
        l.Add(Any("ACH_SP_TRUE", "특별", "진실의 깃털", "마아트의 깃털", AchGrade.Gold, "진실의 깃털일까, 그냥 새의 깃털일까.", "???"));
        AchievementDef nohint = Any("ACH_SP_NOHINT", "특별", "스스로 읽은 자", "오벨리스크", AchGrade.Gold, "태양의 첫 빛을 향해 선 돌기둥. 인도 없이 하늘을 가리킨다.", "???");
        nohint.allFlags = new[] { "F_RA_PLATE_GIVEN" };
        nohint.noneFlags = new[] { "F_HINT_USED" };
        l.Add(nohint);
        AchievementDef legend = Any("ACH_SP_ALL", "특별", "전설의 발굴자", "황금 가면", AchGrade.Gold, "모래 아래 잠든 왕의 얼굴. 모든 발굴자가 꿈꾸는 발견.", "???");
        legend.allOthers = true;
        l.Add(legend);
    }

    private static AchievementDef Any(string id, string chapter, string title, string relic, AchGrade grade, string text, string hint, params string[] flags)
    {
        return new AchievementDef { id = id, chapter = chapter, title = title, relic = relic, grade = grade, text = text, hint = hint, anyFlags = flags };
    }
}
