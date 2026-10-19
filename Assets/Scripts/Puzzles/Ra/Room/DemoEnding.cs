using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 데모 엔딩 (기획서 8)
// 밤 P5 클리어 -> 라의 성소에서 형판 받음(F_RA_PLATE_GIVEN = 형판 등록) -> 북문
// -> 중앙방(큐브 면 0에 형판 그림 장착) -> 시현 데모 종료 -> 다음 방
public class DemoEnding : MonoBehaviour
{
    public const string DemoEndFlag = "F_RA_DEMO_END";

    // 중앙방에 들어간 순간 (RaObjectiveGuide 마지막 단계가 이 플래그로 끝남 -> 목표 문구 숨김)
    public const string CentralEnteredFlag = "F_RA_CENTRAL_ENTERED";

    [Header("연결")]
    [SerializeField] private GameObject endingPanel;

    [Header("기존 큐브 시스템 (비어 있어도 연출은 진행됨)")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private CubeFaceManager cubeFaceManager;

    [Header("형판 등록 (이 플래그가 켜지면 PlayerInventory에 0번 = 라 형판 등록)")]
    [SerializeField] private string plateGivenFlag = "F_RA_PLATE_GIVEN";

    [Header("형판 그림 (태양 형판 아트)")]
    [Tooltip("비우면 예전처럼 면이 금색으로만 바뀜")]
    [SerializeField] private Sprite plateSprite;
    [SerializeField] private string plateLabel = "태양 형판";
    [Tooltip("형판이 위에서 내려와 면에 박히는 시간 (초)")]
    [SerializeField] private float plateDropTime = 0.7f;
    [Tooltip("형판이 처음 나타나는 높이 (면 기준 위쪽 거리)")]
    [SerializeField] private float plateDropHeight = 320f;

    [Header("다음 방")]
    [Tooltip("비우면 Build Profiles Scene List의 다음 번호 씬으로 감 (추천). 이름을 적으면 그 씬으로")]
    [SerializeField] private string nextSceneName = "";

    [Header("연출 시간 (기획서 8 / 10.3)")]
    [SerializeField] private float fadeTime = 0.8f;
    [SerializeField] private float mountTime = 2.0f;

    private static readonly Vector2 FacePos = new Vector2(0, 40);
    private static readonly Vector2 FaceSize = new Vector2(310, 310);

    private CanvasGroup group;
    private GameObject roomRoot;
    private GameObject equipButton;
    private Image faceImage;
    private Text faceLabel;
    private Image plateImage;
    private Text plateLabelText;
    private Image blackCover;
    private Text statusText;
    private Text endText;
    private Text nextHintText;
    private Font font;

    private Color darkBrown, gold, paper, sand, lapis, bgColor;

    private bool playing;
    private bool mounted;
    private bool waitingNext;
    private bool plateRegistered;
    private bool subscribed;

    private void Start()
    {
        try
        {
            SetupColors();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            group = endingPanel.GetComponent<CanvasGroup>();
            if (group == null) group = endingPanel.AddComponent<CanvasGroup>();

            BuildUI();
            endingPanel.SetActive(false);
            SubscribePlateFlag();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Ending] 초기화 오류: {e}");
        }
    }

    private void SetupColors()
    {
        darkBrown = Hex("#4A3423");
        gold = Hex("#E8B23A");
        paper = Hex("#F6F1E7");
        sand = Hex("#E8D9B8");
        lapis = Hex("#1E3A5F");
        bgColor = Hex("#1A120B");
    }

    private void OnDestroy()
    {
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlagChanged;
    }

    private void Update()
    {
        if (!waitingNext) return;

        // 시현 데모 종료 화면에서 Space / Enter / 클릭 -> 다음 방
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            waitingNext = false;
            LoadNextScene();
        }
    }

    // ================= 형판 등록 (성소에서 받은 순간) =================
    private void SubscribePlateFlag()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[Ending] GameManager가 없어서 형판 등록을 못 받음");
            return;
        }

        gm.OnFlagChanged += HandleFlagChanged;
        subscribed = true;
        if (gm.HasFlag(plateGivenFlag)) RegisterPlate(); // 이미 받은 상태로 씬에 들어온 경우
    }

    private void HandleFlagChanged(string flag)
    {
        if (flag == plateGivenFlag) RegisterPlate();
    }

    private void RegisterPlate()
    {
        if (plateRegistered) return;
        plateRegistered = true;

        if (playerInventory != null)
        {
            playerInventory.AddPlate(0); // 0번 = 라 (hasPlate[0] = true)
            Debug.Log("[Ending] 라 형판 등록 (PlayerInventory 0번)");
        }
        else
        {
            Debug.Log("[Ending] PlayerInventory 미연결 - 형판은 ItemInventory(PLATE_RA)에만 기록됨");
        }
    }

    // ================= 북문에서 호출 =================
    public void Play(GameObject player)
    {
        if (playing) return;
        playing = true;

        var controller = player.GetComponent<PlayerController2D>();
        var interaction = player.GetComponent<InteractionSystem>();
        var rb = player.GetComponent<Rigidbody2D>();
        if (controller != null) controller.enabled = false;
        if (interaction != null) interaction.enabled = false; // 중앙방에서 E로 북문이 다시 눌리지 않게
        if (rb != null) rb.linearVelocity = Vector2.zero;

        GameManager gm = GameManager.Instance;
        if (gm != null && !gm.HasFlag(CentralEnteredFlag)) gm.SetFlag(CentralEnteredFlag);

        StartCoroutine(OpenRoutine());
    }

    // 중앙방 전환 (페이드 0.8초)
    private IEnumerator OpenRoutine()
    {
        endingPanel.SetActive(true);
        roomRoot.SetActive(true);
        endText.gameObject.SetActive(false);
        nextHintText.gameObject.SetActive(false);
        SetAlpha(blackCover, 0f);
        statusText.text = "태양 형판을 큐브의 첫 면에 장착하라.";

        float t = 0f;
        while (t < fadeTime)
        {
            group.alpha = t / fadeTime;
            t += Time.deltaTime;
            yield return null;
        }
        group.alpha = 1f;
    }

    // ================= 장착 =================
    private void OnEquipClicked()
    {
        if (mounted) return;
        mounted = true;
        equipButton.SetActive(false);

        MountToCube();
        StartCoroutine(MountRoutine());
    }

    // 기존 CubeFaceManager로 실제 장착 (에러가 나도 연출은 계속 진행)
    private void MountToCube()
    {
        if (cubeFaceManager == null)
        {
            Debug.Log("[Ending] CubeFaceManager 미연결 - 연출만 진행");
            return;
        }

        try
        {
            if (cubeFaceManager.currentFace != 0)
            {
                Debug.Log($"[Ending] 현재 앞면이 {cubeFaceManager.currentFace}번 - 0번(라)이 아님");
            }

            if (cubeFaceManager.CanEquipCurrentFace())
            {
                cubeFaceManager.EquipCurrentFace();
                Debug.Log("[Ending] 큐브 면 0(라) 장착 완료");
            }
            else
            {
                Debug.Log("[Ending] 장착 조건 미충족 (형판 없음 또는 이미 장착) - 연출만 진행");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Ending] 큐브 장착 중 오류, 연출만 진행: {e.Message}");
        }
    }

    // 형판이 내려와 박힘 -> 금빛 펄스 -> 데모 종료 플래그 -> 페이드 아웃 -> 다음 방 안내
    private IEnumerator MountRoutine()
    {
        statusText.text = "태양 형판 장착!";

        if (plateSprite != null) yield return DropPlate();
        else faceImage.color = gold; // 그림이 없으면 예전처럼 금색

        yield return PulseFace();

        if (GameManager.Instance != null) GameManager.Instance.SetFlag(DemoEndFlag);
        yield return FadeToEnd();
    }

    // 위에서 크게 나타나 면 자리로 떨어지며 작아짐 -> 박히는 순간 살짝 튕김
    private IEnumerator DropPlate()
    {
        faceLabel.enabled = false;
        plateImage.enabled = true;
        plateLabelText.enabled = !string.IsNullOrEmpty(plateLabel);

        RectTransform rt = plateImage.rectTransform;
        Vector2 start = FacePos + new Vector2(0f, plateDropHeight);
        float t = 0f;
        while (t < plateDropTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / plateDropTime);
            float ease = p * p; // 점점 빨라지며 떨어짐
            rt.anchoredPosition = Vector2.Lerp(start, FacePos, ease);
            rt.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, ease);
            plateImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(p * 2.5f));
            yield return null;
        }

        rt.anchoredPosition = FacePos;
        plateImage.color = Color.white;
        faceImage.color = darkBrown; // 형판 뒤 빈 면은 어둡게
    }

    // 장착된 면(형판 그림이 있으면 형판)이 금빛으로 맥박치듯 커졌다 작아짐
    private IEnumerator PulseFace()
    {
        RectTransform rt = plateSprite != null ? plateImage.rectTransform : faceImage.rectTransform;
        float t = 0f;
        while (t < mountTime)
        {
            float s = 1f + 0.06f * Mathf.Sin(t * 12f);
            rt.localScale = new Vector3(s, s, 1f);
            t += Time.deltaTime;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private IEnumerator FadeToEnd()
    {
        float t = 0f;
        while (t < fadeTime)
        {
            SetAlpha(blackCover, t / fadeTime);
            t += Time.deltaTime;
            yield return null;
        }
        SetAlpha(blackCover, 1f);

        endText.gameObject.SetActive(true);
        Debug.Log("[Ending] 시현 데모 종료");

        // 방금 누른 클릭이 바로 다음 방으로 넘어가지 않게 잠깐 대기
        yield return new WaitForSeconds(1f);

        nextHintText.gameObject.SetActive(true);
        waitingNext = true;
    }

    // 이름이 비었거나 Scene List에 없으면 다음 번호 씬 (라 = 0번 -> 아누비스 튜토리얼 = 1번)
    private void LoadNextScene()
    {
        try
        {
            SceneFlow.LoadByNameOrNext(nextSceneName);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Ending] 다음 방 이동 오류: {e}");
        }
    }

    // ================= 화면 만들기 =================
    private void BuildUI()
    {
        RectTransform root = endingPanel.GetComponent<RectTransform>();

        Image bg = endingPanel.GetComponent<Image>();
        if (bg != null) bg.color = bgColor;

        // 중앙방 화면
        roomRoot = CreateStretch(root, "CentralRoom");
        RectTransform room = roomRoot.GetComponent<RectTransform>();

        CreateText(room, "중앙방", 48, new Vector2(0, 400), new Vector2(800, 70), paper);
        CreateText(room, "여섯 신의 큐브", 26, new Vector2(0, 340), new Vector2(800, 40), sand);

        BuildFace(room);
        BuildEquipButton(room);

        statusText = CreateText(room, "", 28, new Vector2(0, -330), new Vector2(1200, 50), paper);

        // 페이드 아웃용 검은 화면 + 종료 문구 (맨 위)
        GameObject cover = CreateStretch(root, "BlackCover");
        blackCover = cover.AddComponent<Image>();
        blackCover.color = Color.black;
        blackCover.raycastTarget = false;

        endText = CreateText(root, "시현 데모 종료", 64, new Vector2(0, 40), new Vector2(1200, 120), gold);
        nextHintText = CreateText(root, "Space / 클릭: 다음 방으로", 28, new Vector2(0, -80), new Vector2(1200, 50), sand);
    }

    // 큐브 면 0 + 그 위에 놓일 형판 그림 (장착 전에는 숨김)
    private void BuildFace(RectTransform room)
    {
        CreateRect(room, "FaceFrame", FacePos, new Vector2(340, 340), darkBrown);
        faceImage = CreateRect(room, "Face0", FacePos, FaceSize, lapis);
        faceLabel = CreateText(faceImage.rectTransform, "면 0 - 라", 40, Vector2.zero, FaceSize, paper);

        plateImage = CreateRect(room, "Plate_Ra", FacePos, FaceSize, Color.white);
        plateImage.sprite = plateSprite;
        plateImage.preserveAspect = true;
        plateImage.enabled = false;

        // 형판 아래쪽 이름 띠 위치 (그림 높이의 약 36% 아래)
        Vector2 labelPos = new Vector2(0f, -FaceSize.y * 0.36f);
        plateLabelText = CreateText(plateImage.rectTransform, plateLabel, 24, labelPos,
            new Vector2(FaceSize.x * 0.7f, 36f), Hex("#FFF3C4"));
        plateLabelText.enabled = false;
    }

    private void BuildEquipButton(RectTransform room)
    {
        Image btnImg = CreateRect(room, "Btn_Equip", new Vector2(0, -230), new Vector2(320, 80), gold);
        btnImg.raycastTarget = true;
        Button btn = btnImg.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(OnEquipClicked);
        CreateText(btnImg.rectTransform, "태양 형판 장착", 30, Vector2.zero, new Vector2(320, 80), darkBrown);
        equipButton = btnImg.gameObject;
    }

    // ================= 도우미 함수 =================
    private GameObject CreateStretch(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    private Image CreateRect(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private Text CreateText(RectTransform parent, string text, int fontSize, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static void SetAlpha(Image img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}