// 토트의 방 대사 모음 (문구는 여기서만 고치면 됨)
// 이야기: 밤의 여섯째 시간, 달의 서고. 토트는 글자를 만든 서기관이자 달을 세는 신
//   벽화 A '글자를 만든 토트' -> 글자의 뜻과 읽는 방향을 배움
//   벽화 B '달과의 내기' -> 토트가 달과 내기해 다섯 날을 얻음. 그 밤의 달은 왼쪽 반달
//   벽화 C '되찾은 눈' -> 토트가 호루스의 눈을 고침. 눈 아래 다섯 글자 = 지혜의 문 주문
public static class ThothLines
{
    public const string Me = "나";
    public const string Khepri = "케프리";
    public const string Thoth = "토트";
    public const string Mural = "벽화";
    public const string Stele = "비석";

    // ================= 목표 =================
    public const string GoalA = "서쪽 서고의 부서진 벽화를 되살리자";
    public const string GoalB = "동쪽 서고의 벽화를 되살리자";
    public const string GoalDesk = "동쪽 서고의 서기관 책상에서 기록을 확인하자";
    public const string GoalDial = "홀 가운데 달 시계를 맞추자";
    public const string GoalC = "달빛 속 조각을 모아 북쪽 벽감의 벽화를 되살리자";
    public const string GoalSpell = "지혜의 문 앞 글자판에서 주문을 읊자";
    public const string GoalExit = "지혜의 문으로 중앙 홀에 돌아가자";

    public static string MissingPieces(string fresco, int owned)
    {
        return $"벽화 '{ThothFrescoData.Title(fresco)}' 조각 {owned}/{ThothFrescoData.PieceCount} - 서고 어딘가에 나머지가 있다";
    }

    // ================= 시작 =================
    public static DialogueLine[] Intro => ThothState.Lines(
        Khepri, "여기는 토트의 서고. 글자와 달을 다스리는 신, 따오기 머리의 서기관이 머물던 곳이야.",
        Khepri, "밤의 여섯째 시간... 벽화들이 산산이 부서졌어. 그림은 기억을 품고 있으니, 조각을 맞추면 이야기가 돌아올 거야.",
        Me, "벽화 조각 맞추기구나. 서쪽 서고부터 가 보자.");

    // ================= 벽화 =================
    public static DialogueLine[] WallFirst(string fresco) => ThothState.Lines(
        Me, "벽화가 무너져서 조각이 바닥에 흩어져 있어.",
        Me, "아홉 조각 중 여섯... 나머지는 이 근처 어딘가에 있겠지.");

    public static DialogueLine[] AfterA => ThothState.Lines(
        Me, "따오기는 토트, 저울은 진실, 갈대 펜은 기록, 달은 달, 눈은 눈... 적어 두자.",
        Khepri, "동쪽 서고에도 무너진 벽화가 있어. 저기 따오기 상도 너를 보고 있네.");

    public static DialogueLine[] AfterB => ThothState.Lines(
        Me, "내기를 한 밤에 달은 왼쪽 절반만 빛났다...",
        Khepri, "서기관의 책상에 그 밤의 기록이 남아 있어. 벽화와 맞춰 봐.");

    public static DialogueLine[] AfterC => ThothState.Lines(
        Me, "눈 아래에 다섯 글자가 줄지어 있어. 이게 문을 여는 주문이야.",
        Me, "글은 새가 바라보는 쪽에서 시작된다고 했지...",
        Khepri, "지혜의 문 앞 다섯 글자판에 차례로 서서 주문을 읊어. 틀리면 처음부터야.");

    public static DialogueLine[] Replay(string fresco) => ThothState.Lines(Me, $"벽화 '{ThothFrescoData.Title(fresco)}'... 다시 볼까.");

    // ================= 서기관 책상 =================
    public const string DeskTitle = "서기관의 책상 - 그 밤의 기록";
    public const string DeskHelp = "벽화 '달과의 내기'와 다른 기록 하나를 골라 지우자 (클릭)   E: 닫기";
    public static readonly string[] Records =
    {
        "내기가 끝나자 달은 빛을 잃기 시작했다.\n그래서 달은 지금도 차고 기운다.",
        "토트는 일흔둘 중 하나의 빛을 모아\n다섯 날을 만들었다.\n해에도 달에도 속하지 않은 날.",
        "내기를 한 밤,\n달은 둥근 보름달이었다."
    };
    public const int FalseRecord = 2;
    public const string DeskWrong = "이 기록은 벽화와 맞아. 벽화를 다시 떠올려 보자.";

    public static DialogueLine[] DeskDrawer => ThothState.Lines(Me, "책상 서랍에 벽화 조각이 하나 끼어 있었어.");
    public static DialogueLine[] DeskNeedB => ThothState.Lines(
        Me, "그 밤의 기록 세 장... 무슨 밤인지 아직 모르겠어.",
        Me, "동쪽 벽화를 먼저 되살리자.");
    public static DialogueLine[] DeskSolved => ThothState.Lines(
        Me, "보름달이 아니라 왼쪽이 빛나는 반달이었어. 기록을 바로잡았다.",
        Khepri, "홀 가운데 달 시계가 깨어났어. 달이 차고 기우는 차례를 맞추고, 바늘로 내기의 밤을 가리켜 봐.");
    public static DialogueLine[] DeskAgain => ThothState.Lines(Me, "바로잡은 기록. 내기의 밤, 달은 왼쪽 반달이었다.");

    // ================= 달 시계 =================
    public const string DialTitle = "달의 시계";
    public const string DialHelp = "달 그림을 골라 빈 구멍에 끼우자. 맨 위는 빛이 없는 밤.   < > 바늘 돌리기   E: 닫기";
    public static string DialWrong(int right) => $"달 그림 {right}/7개가 제자리에 있다.";
    public const string DialNeedle = "달의 차례는 맞았어. 바늘은 '내기의 밤'의 달을 가리켜야 해.";
    public const string DialNotFull = "아직 빈 구멍이 있다.";
    public static DialogueLine[] DialLocked => ThothState.Lines(
        Me, "달 그림 여덟 칸짜리 시계야. 맨 위만 깜깜한 달이 끼워져 있어.",
        Me, "무엇을 가리켜야 할지 아직 모르겠어. 서고의 기록부터 보자.");
    public static DialogueLine[] DialAgain => ThothState.Lines(Me, "달 시계가 내기의 밤을 가리키고 있다.");
    public static DialogueLine[] DialReveal => ThothState.Lines(
        Khepri, "달빛이 숨은 벽감을 열었어!",
        Me, "빛 속에 조각이 떨어져 있어. 시계 가운데서도 뭔가 굴러 나왔고.");

    // ================= 비석, 따오기, 서가 =================
    public static DialogueLine[] SteleLines => ThothState.Lines(
        Stele, "달은 오른쪽부터 차서, 둥글어진 뒤, 오른쪽부터 닳는다.",
        Stele, "첫 칸은 빛이 없는 밤. 달은 해가 지나는 쪽(시계 방향)으로 자란다.");
    public static DialogueLine[] IbisLocked => ThothState.Lines(
        Me, "토트의 새, 따오기 상. 부리가 서쪽 서고를 가리키고 있어.");
    public static DialogueLine[] IbisGive => ThothState.Lines(
        Me, "따오기 부리 끝에 벽화 조각이 걸려 있었어. 서쪽 벽화를 되살리니 고개를 숙였나 봐.");
    public static DialogueLine[] IbisAfter => ThothState.Lines(Me, "따오기 상. 이제 동쪽 서고를 바라보고 있다.");

    // ================= 주문 =================
    public static DialogueLine[] PlateSleeping => ThothState.Lines(
        Me, "글자가 새겨진 바닥판. 아직 빛이 잠들어 있어.",
        Me, "벽화를 모두 되살리면 깨어날 것 같아.");
    public const string SpellWrong = "글자의 빛이 꺼졌다... 순서가 틀렸어.";

    // ================= 끝 =================
    public static DialogueLine[] GateClosed => ThothState.Lines(Me, "지혜의 문. 굳게 닫혀 있다. 문 앞 바닥에 글자판 다섯 개.");
    public static DialogueLine[] FinaleOpen => ThothState.Lines(Khepri, "다섯 글자가 이어졌어... 문이 열린다!");
    public static DialogueLine[] FinaleThoth => ThothState.Lines(
        Thoth, "부서진 것을 맞추는 자여. 나는 토트, 신들의 서기관이다.",
        Thoth, "글은 잊힌 것을 기억하게 하고, 달은 잃어버린 날을 돌려준다.",
        Thoth, "네가 되살린 이야기만큼, 이 지혜를 가져가라.",
        Thoth, "그리고 이것은 내가 고친 호루스의 눈. 하늘의 신전에 이 눈을 기다리는 자리가 있다.");
    public static DialogueLine[] FinaleAfter => ThothState.Lines(
        Me, "세 번째 형판... 이제 중앙 홀로 돌아가자.");

    // ================= 숨은 방 =================
    public static DialogueLine[] FragShelf => ThothState.Lines(Me, "책장 위에 달 모양 금 조각이 있어.");
    public static DialogueLine[] FragDust => ThothState.Lines(Me, "먼지 밑에서 달 모양 금 조각이 나왔어.");
    public static DialogueLine[] FragDial => ThothState.Lines(Me, "시계 가운데서 달 모양 금 조각이 굴러 나왔어.");
    public static DialogueLine[] NicheLow(int n) => ThothState.Lines(
        Me, $"벽에 달 모양 홈이 세 개 파여 있어. (달 조각 {n}/3)",
        Me, "초승달, 반달, 보름달... 모양에 맞는 조각을 찾아야 해.");
    public static DialogueLine[] NicheOpen => ThothState.Lines(Me, "세 조각을 끼우니... 벽이 안쪽으로 열린다!");
    public static DialogueLine[] NicheSeen => ThothState.Lines(Me, "토트의 책이 잠든 물길로 이어진 벽. 읽지 말고 기억만 하자.");
    public static DialogueLine[] VaultAfter => ThothState.Lines(
        Me, "돌아 나오는 길에 작은 점토판을 주웠어. 책의 첫 줄만 옮겨 적혀 있다.");
}
