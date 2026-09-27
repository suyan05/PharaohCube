using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 데모 엔딩 (기획서 8)
// P5 클리어 -> 형판 등록 -> 북문 -> 중앙방(큐브 면 0 장착) -> 시현 데모 종료
public class DemoEnding : MonoBehaviour
{
    public const string DemoEndFlag = "F_RA_DEMO_END";

    [Header("연결")]
    [SerializeField] private SunCyclePuzzle p5;
    [SerializeField] private GameObject endingPanel;

    [Header("기존 큐브 시스템 (비어 있어도 연출은 진행됨)")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private CubeFaceManager cubeFaceManager;

    [Header("연출 시간 (기획서 8 / 10.3)")]
    [SerializeField] private float fadeTime = 0.8f;
    [SerializeField] private float mountTime = 2.0f;

    private CanvasGroup group;
    private GameObject roomRoot;
    private GameObject equipButton;
    private Image faceImage;
    private Image blackCover;
    private Text statusText;
    private Text endText;
    private Font font;

    private Color darkBrown, gold, paper, sand, lapis, bgColor;

    private bool playing;
    private bool mounted;

    private void Start()
    {
        darkBrown = Hex("#4A3423");
        gold = Hex("#E8B23A");
        paper = Hex("#F6F1E7");
        sand = Hex("#E8D9B8");
        lapis = Hex("#1E3A5F");
        bgColor = Hex("#1A120B");
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        group = endingPanel.GetComponent<CanvasGroup>();
        if (group == null) group = endingPanel.AddComponent<CanvasGroup>();

        BuildUI();
        endingPanel.SetActive(false);

        p5.OnSuccess += HandleP5Cleared;
    }

    private void OnDestroy()
    {
        if (p5 != null) p5.OnSuccess -= HandleP5Cleared;
    }

    // ================= P5 클리어 -> 형판 등록 =================
    private void HandleP5Cleared()
    {
        if (playerInventory != null)
        {
            playerInventory.AddPlate(0); // 0번 = 라 (hasPlate[0] = true)
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
        var rb = player.GetComponent<Rigidbody2D>();
        if (controller != null) controller.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        StartCoroutine(OpenRoutine());
    }

    // 중앙방 전환 (페이드 0.8초)
    private IEnumerator OpenRoutine()
    {
        endingPanel.SetActive(true);
        roomRoot.SetActive(true);
        endText.gameObject.SetActive(false);
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

    // 장착 연출 (금빛 펄스 2초) -> 데모 종료 플래그 -> 페이드 아웃
    private IEnumerator MountRoutine()
    {
        statusText.text = "태양 형판 장착!";
        faceImage.color = gold;

        RectTransform rt = faceImage.rectTransform;
        float t = 0f;
        while (t < mountTime)
        {
            float s = 1f + 0.06f * Mathf.Sin(t * 12f);
            rt.localScale = new Vector3(s, s, 1f);
            t += Time.deltaTime;
            yield return null;
        }
        rt.localScale = Vector3.one;

        GameManager.Instance.SetFlag(DemoEndFlag);

        t = 0f;
        while (t < fadeTime)
        {
            SetAlpha(blackCover, t / fadeTime);
            t += Time.deltaTime;
            yield return null;
        }
        SetAlpha(blackCover, 1f);

        endText.gameObject.SetActive(true);
        Debug.Log("[Ending] 시현 데모 종료");
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

        CreateRect(room, "FaceFrame", new Vector2(0, 40), new Vector2(340, 340), darkBrown);
        faceImage = CreateRect(room, "Face0", new Vector2(0, 40), new Vector2(310, 310), lapis);
        CreateText(faceImage.rectTransform, "면 0 - 라", 40, Vector2.zero, new Vector2(310, 310), paper);

        Image btnImg = CreateRect(room, "Btn_Equip", new Vector2(0, -230), new Vector2(320, 80), gold);
        btnImg.raycastTarget = true;
        Button btn = btnImg.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(OnEquipClicked);
        CreateText(btnImg.rectTransform, "태양 형판 장착", 30, Vector2.zero, new Vector2(320, 80), darkBrown);
        equipButton = btnImg.gameObject;

        statusText = CreateText(room, "", 28, new Vector2(0, -330), new Vector2(1200, 50), paper);

        // 페이드 아웃용 검은 화면 + 종료 문구 (맨 위)
        GameObject cover = CreateStretch(root, "BlackCover");
        blackCover = cover.AddComponent<Image>();
        blackCover.color = Color.black;
        blackCover.raycastTarget = false;

        endText = CreateText(root, "시현 데모 종료", 64, Vector2.zero, new Vector2(1200, 120), gold);
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