using UnityEngine;

// 퍼즐 UI 공통 설정 (폰트, 배경 어둡기, 나중에 버튼/이름표 그림)
// Assets/Resources 폴더에 "PuzzleUITheme" 이름으로 하나 만들어두면 어디서든 자동으로 불러옴
[CreateAssetMenu(fileName = "PuzzleUITheme", menuName = "PharaohCube/Puzzle UI Theme")]
public class PuzzleUITheme : ScriptableObject
{
    private const string ResourcePath = "PuzzleUITheme";
    private const string BuiltinFontName = "LegacyRuntime.ttf";

    [Header("폰트")]
    [SerializeField] private Font bodyFont;   // 본문 (GowunBatang-Regular)
    [SerializeField] private Font titleFont;  // 제목 (GowunBatang-Bold)

    [Header("오버레이 배경")]
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.88f);

    [Header("나중에 아트가 오면 (지금은 비워둬도 됨)")]
    [SerializeField] private Sprite buttonSprite;
    [SerializeField] private Sprite labelPlateSprite;

    [Header("[추가] 비석 해독 화면 판 (Common_UI_Stele_Panel, 비우면 도형으로 그림)")]
    [SerializeField] private Sprite stelePanelSprite;

    public Font BodyFont => bodyFont;
    public Font TitleFont => titleFont;
    public Color BackdropColor => backdropColor;
    public Sprite ButtonSprite => buttonSprite;
    public Sprite LabelPlateSprite => labelPlateSprite;

    private static PuzzleUITheme cached;
    private static bool triedLoad;

    // Resources/PuzzleUITheme 를 한 번만 불러서 재사용
    public static PuzzleUITheme Current
    {
        get
        {
            if (cached != null || triedLoad) return cached;

            triedLoad = true;
            cached = Resources.Load<PuzzleUITheme>(ResourcePath);
            if (cached == null)
            {
                Debug.LogWarning("[PuzzleUITheme] Resources/PuzzleUITheme 이 없음 -> 기본 폰트 사용");
            }
            return cached;
        }
    }

    public static Font GetBodyFont()
    {
        PuzzleUITheme t = Current;
        if (t != null && t.bodyFont != null) return t.bodyFont;
        return Resources.GetBuiltinResource<Font>(BuiltinFontName);
    }

    // [추가] 비석 해독 화면(SteleDecodeUI)이 씬에 없이 실행 중에 만들어져서, 그림은 이 테마(Resources)에서 받아 감
    public static Sprite GetStelePanel()
    {
        PuzzleUITheme t = Current;
        return t != null ? t.stelePanelSprite : null;
    }

    public static Font GetTitleFont()
    {
        PuzzleUITheme t = Current;
        if (t != null && t.titleFont != null) return t.titleFont;
        return GetBodyFont();
    }
}