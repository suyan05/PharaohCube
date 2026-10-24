using TMPro;
using UnityEngine;

// [추가] 시작 화면 / 설정 창 / 일시 정지 메뉴의 모양과 소리 (Resources/MenuTheme.asset)
// Tools/UI/★ 시작 화면·설정 창 만들기 메뉴가 만들어 줌. 아트·사운드가 오면 여기만 바꾸면 됨
[CreateAssetMenu(menuName = "PharaohCube/Menu Theme", fileName = "MenuTheme")]
public class MenuTheme : ScriptableObject
{
    [Header("글꼴 (한글이 들어 있는 TMP 글꼴)")]
    public TMP_FontAsset font;

    [Header("시작 화면")]
    [Tooltip("배경 그림 (비우면 어두운 색)")]
    public Sprite titleBackground;
    [Tooltip("게임 로고 그림 (비우면 아래 제목 글자를 씀)")]
    public Sprite logo;
    [Tooltip("[추가] 시작 화면에서 설정 창을 열 때 뒤에 깔리는 배경 (비우면 시작 화면이 어둡게 비침)")]
    public Sprite settingsBackground;
    public string gameTitle = "PHARAOH CUBE";
    public string gameSubtitle = "파라오의 큐브";
    [Tooltip("새 게임을 누르면 가는 첫 씬 (Build Profiles의 Scene List에 있어야 함)")]
    public string newGameScene = "Prologue_Dig";
    [Tooltip("시작 화면 씬 이름 (일시 정지 메뉴의 '시작 화면으로'가 감)")]
    public string titleScene = "Title";
    public string versionText = "v0.1";

    [Header("판·버튼 그림 (비우면 단색 네모)")]
    public Sprite panelSprite;
    public Sprite buttonSprite;
    public Sprite knobSprite;

    [Header("색")]
    public Color backgroundColor = new Color(0.07f, 0.05f, 0.04f, 1f);
    public Color dimColor = new Color(0f, 0f, 0f, 0.65f);
    public Color panelColor = new Color(0.12f, 0.09f, 0.07f, 0.96f);
    public Color buttonColor = new Color(0.23f, 0.17f, 0.12f, 1f);
    public Color buttonHighlight = new Color(0.42f, 0.31f, 0.18f, 1f);
    public Color buttonPressed = new Color(0.55f, 0.40f, 0.20f, 1f);
    public Color textColor = new Color(0.94f, 0.86f, 0.76f, 1f);   // 크림 #EFDCC2
    public Color accentColor = new Color(0.91f, 0.70f, 0.23f, 1f); // 금 #E8B23A
    public Color disabledText = new Color(0.5f, 0.45f, 0.4f, 1f);

    [Header("소리 (비우면 소리 없음)")]
    public AudioClip titleBgm;
    public AudioClip clickSfx;
    public AudioClip moveSfx;

    private static MenuTheme cached;

    // Resources/MenuTheme.asset (없으면 기본값으로 임시 생성)
    public static MenuTheme Current
    {
        get
        {
            if (cached != null) return cached;
            cached = Resources.Load<MenuTheme>("MenuTheme");
            if (cached == null)
            {
                Debug.LogWarning("[MenuTheme] Resources/MenuTheme.asset 없음 -> 기본값 사용 (Tools/UI/★ 시작 화면·설정 창 만들기 실행)");
                cached = CreateInstance<MenuTheme>();
            }
            if (cached.font == null) cached.font = TMP_Settings.defaultFontAsset;
            return cached;
        }
    }
}
