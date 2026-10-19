using UnityEngine;

// B5 "해와 달의 기억" 퍼즐 데이터 (규칙, 속도, 색, 그림, 소리, 문구)
// 코드 수정 없이 Inspector에서 난이도와 연출을 바꾸기 위해 ScriptableObject로 분리
[CreateAssetMenu(fileName = "BastetSunMoonData", menuName = "Bastet/메인 퍼즐 데이터 (해와 달의 기억)")]
public class BastetSunMoonData : ScriptableObject
{
    [Header("규칙")]
    [Tooltip("고양이 석상 버튼 수 (화면에 만든 버튼 수보다 많으면 버튼 수만큼만 씀)")]
    [Range(3, 8)]
    [SerializeField] private int buttonCount = 6;

    [Tooltip("해 단계 라운드 수. 같은 순서가 1개 -> 2개 -> ... 이 수까지 늘어남")]
    [Range(1, 8)]
    [SerializeField] private int sunRounds = 4;

    [Tooltip("같은 발자국이 연달아 두 번 나오지 않게")]
    [SerializeField] private bool noRepeatInRow = true;

    [Tooltip("켜면 해 단계에서 틀릴 때 1라운드부터 다시. 끄면 틀린 라운드만 다시 보여 줌")]
    [SerializeField] private bool sunFailRestartsFromRoundOne = false;

    [Tooltip("같은 단계에서 이 횟수만큼 틀리면 순서를 새로 섞음 (외워서 찍기 방지)")]
    [Range(1, 10)]
    [SerializeField] private int failsBeforeNewSequence = 3;

    [Header("속도 (초)")]
    [Tooltip("라운드 시작 전 잠깐 쉬는 시간")]
    [Range(0.2f, 2f)] [SerializeField] private float beforeShowDelay = 0.7f;
    [Tooltip("1라운드에서 버튼 하나가 켜져 있는 시간")]
    [Range(0.2f, 1.5f)] [SerializeField] private float showOnTime = 0.6f;
    [Tooltip("라운드가 오를 때마다 켜짐 시간을 이만큼 줄임 (뮤지컬 메모리처럼 점점 빨라짐)")]
    [Range(0f, 0.2f)] [SerializeField] private float speedUpPerRound = 0.07f;
    [Tooltip("켜짐 시간이 이보다 짧아지지 않음")]
    [Range(0.15f, 1f)] [SerializeField] private float minShowOnTime = 0.35f;
    [Tooltip("버튼과 버튼 사이 꺼져 있는 시간")]
    [Range(0.05f, 1f)] [SerializeField] private float showGapTime = 0.25f;
    [Tooltip("플레이어가 누른 버튼이 반짝이는 시간")]
    [Range(0.05f, 0.6f)] [SerializeField] private float pressFlashTime = 0.18f;
    [Tooltip("틀렸을 때 빨갛게 깜빡이는 시간")]
    [Range(0.2f, 2f)] [SerializeField] private float failFlashTime = 0.6f;
    [Tooltip("해 원반이 달 원반으로 뒤집히는 시간")]
    [Range(0.4f, 3f)] [SerializeField] private float phaseChangeTime = 1.4f;

    [Header("버튼 색 (석상마다 다른 색 = 뮤지컬 메모리의 색 버튼)")]
    [SerializeField]
    private Color[] buttonColors =
    {
        new Color(0.91f, 0.70f, 0.23f), // 금 #E8B23A
        new Color(0.71f, 0.76f, 0.29f), // 고양이 눈 연두 #B5C24A
        new Color(0.85f, 0.55f, 0.48f), // 밝은 장밋빛 #D98C7A
        new Color(0.94f, 0.86f, 0.76f), // 향유 크림 #EFDCC2
        new Color(0.75f, 0.54f, 0.31f), // 청동 밝은 면 #C08A4E
        new Color(0.24f, 0.48f, 0.84f), // 밤빛 파랑 #3E7BD6
        new Color(0.91f, 0.70f, 0.23f),
        new Color(0.71f, 0.76f, 0.29f)
    };
    [Tooltip("켜진 발자국/눈에 버튼 색을 입힘. 아트가 색까지 칠해져 오면 끄기")]
    [SerializeField] private bool tintWithButtonColor = true;
    [Tooltip("켜면 달 단계 눈도 버튼 색. 끄면 모든 눈이 달빛 크림색 (위치만으로 기억 = 더 어려움)")]
    [SerializeField] private bool moonUsesButtonColors = true;
    [SerializeField] private Color moonEyeColor = new Color(0.94f, 0.86f, 0.76f);   // #EFDCC2
    [SerializeField] private Color offColor = new Color(0.37f, 0.23f, 0.21f);       // #5E3A36
    [SerializeField] private Color wrongColor = new Color(0.75f, 0.22f, 0.17f);     // #C0392B
    [SerializeField] private Color goldColor = new Color(0.91f, 0.70f, 0.23f);      // #E8B23A
    [SerializeField] private Color sunColor = new Color(0.91f, 0.70f, 0.23f);       // 해 원반
    [SerializeField] private Color moonColor = new Color(0.94f, 0.86f, 0.76f);      // 달 원반

    [Header("그림 (선택, 비우면 색 네모로 표시)")]
    [SerializeField] private Sprite footprintOnSprite;
    [SerializeField] private Sprite footprintOffSprite;
    [SerializeField] private Sprite eyeOnSprite;
    [SerializeField] private Sprite eyeOffSprite;
    [SerializeField] private Sprite sunSprite;
    [SerializeField] private Sprite moonSprite;

    [Header("[추가] 그림: 레이아웃 가이드 규격 (선택)")]
    [Tooltip("석상 버튼 1장 (340 x 400, 꺼진 상태). 넣으면 석상 네모 대신 이 그림")]
    [SerializeField] private Sprite statueSprite;
    [Tooltip("퍼즐 화면 배경 1장 (1300 x 880). 석상/원반/진행 표시 자리는 비워 둔 그림")]
    [SerializeField] private Sprite frameSprite;
    [Tooltip("진행 표시 시스트럼 1장 (128 x 128, 밝은 무채색). 꺼짐/해/달 색은 코드가 입힘")]
    [SerializeField] private Sprite progressSprite;
    [Tooltip("켜면 눈 빛/발자국 빛 그림이 석상과 같은 340 x 400 캔버스(나머지 투명)라고 보고, 석상 전체 크기로 겹쳐 놓음. 꺼진 상태는 투명")]
    [SerializeField] private bool glowIsFullCanvas = true;

    [Header("소리 (선택)")]
    [Tooltip("버튼 소리 1개. 버튼마다 pitches 값으로 음 높이를 바꿔서 냄")]
    [SerializeField] private AudioClip toneClip;
    [SerializeField] private float[] pitches = { 0.80f, 0.90f, 1.00f, 1.12f, 1.26f, 1.34f, 1.50f, 1.68f };
    [SerializeField] private AudioClip wrongClip;
    [SerializeField] private AudioClip phaseChangeClip;
    [SerializeField] private AudioClip clearClip;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.8f;

    [Header("문구 ({0} = 라운드 번호)")]
    [TextArea(2, 3)] [SerializeField] private string startMessage = "낮의 바스테트는 라의 눈.\n태양의 배를 따라 걸은 발자국을 기억하라.";
    [TextArea(1, 2)] [SerializeField] private string sunShowMessage = "발자국 {0}개가 빛난다. 잘 보아라.";
    [TextArea(1, 2)] [SerializeField] private string sunInputMessage = "보인 순서대로 석상을 눌러라. ({0}개)";
    [TextArea(1, 2)] [SerializeField] private string sunRetryMessage = "발자국이 흩어졌다. 다시 보여 준다.";
    [TextArea(1, 2)] [SerializeField] private string newSequenceMessage = "길이 바뀌었다. 새 발자국을 처음부터 기억하라.";
    [TextArea(2, 3)] [SerializeField] private string changeMessage = "해가 지고 달이 뜬다.\n밤의 수호자는 지나온 길을 거슬러 돌아간다.";
    [TextArea(1, 2)] [SerializeField] private string moonShowMessage = "고양이 눈이 하나씩 빛난다. 잘 보아라.";
    [TextArea(1, 2)] [SerializeField] private string moonInputMessage = "마지막에 빛난 눈부터 거꾸로 눌러라.";
    [TextArea(1, 2)] [SerializeField] private string moonRetryMessage = "달빛이 흔들렸다. 다시 보여 준다.";
    [TextArea(2, 3)] [SerializeField] private string clearMessage = "아펩을 물리친 고양이가 새벽을 연다.\n고양이 형판을 얻었다.";
    [TextArea(1, 2)] [SerializeField] private string lockedMessage = "신상이 아직 잠들어 있다. 네 개의 시험을 먼저 통과하라.";

    // ================= 읽기 전용 =================
    public int ButtonCount => buttonCount;
    public int SunRounds => sunRounds;
    public bool NoRepeatInRow => noRepeatInRow;
    public bool SunFailRestartsFromRoundOne => sunFailRestartsFromRoundOne;
    public int FailsBeforeNewSequence => failsBeforeNewSequence;

    public float BeforeShowDelay => beforeShowDelay;
    public float ShowGapTime => showGapTime;
    public float PressFlashTime => pressFlashTime;
    public float FailFlashTime => failFlashTime;
    public float PhaseChangeTime => phaseChangeTime;

    public bool TintWithButtonColor => tintWithButtonColor;
    public bool MoonUsesButtonColors => moonUsesButtonColors;
    public Color MoonEyeColor => moonEyeColor;
    public Color OffColor => offColor;
    public Color WrongColor => wrongColor;
    public Color GoldColor => goldColor;
    public Color SunColor => sunColor;
    public Color MoonColor => moonColor;

    public Sprite FootprintOnSprite => footprintOnSprite;
    public Sprite FootprintOffSprite => footprintOffSprite;
    public Sprite EyeOnSprite => eyeOnSprite;
    public Sprite EyeOffSprite => eyeOffSprite;
    public Sprite SunSprite => sunSprite;
    public Sprite MoonSprite => moonSprite;
    public Sprite StatueSprite => statueSprite;
    public Sprite FrameSprite => frameSprite;
    public Sprite ProgressSprite => progressSprite;
    // 석상 그림이 있을 때만 겹치기 모드 사용 (그림 없이 켜면 빛이 석상 전체를 덮음)
    public bool GlowIsFullCanvas => glowIsFullCanvas && statueSprite != null;

    public AudioClip ToneClip => toneClip;
    public AudioClip WrongClip => wrongClip;
    public AudioClip PhaseChangeClip => phaseChangeClip;
    public AudioClip ClearClip => clearClip;
    public float Volume => volume;

    public string StartMessage => startMessage;
    public string SunShowMessage => sunShowMessage;
    public string SunInputMessage => sunInputMessage;
    public string SunRetryMessage => sunRetryMessage;
    public string NewSequenceMessage => newSequenceMessage;
    public string ChangeMessage => changeMessage;
    public string MoonShowMessage => moonShowMessage;
    public string MoonInputMessage => moonInputMessage;
    public string MoonRetryMessage => moonRetryMessage;
    public string ClearMessage => clearMessage;
    public string LockedMessage => lockedMessage;

    // 라운드가 오를수록 짧아지는 켜짐 시간 (round는 1부터)
    public float ShowOnTime(int round) =>
        Mathf.Max(minShowOnTime, showOnTime - speedUpPerRound * Mathf.Max(0, round - 1));

    public Color ButtonColor(int index) =>
        (buttonColors != null && index >= 0 && index < buttonColors.Length) ? buttonColors[index] : goldColor;

    public float Pitch(int index) =>
        (pitches != null && index >= 0 && index < pitches.Length) ? pitches[index] : 1f;
}
