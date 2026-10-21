using UnityEngine;
using UnityEngine.UI;

// 길게 누르기 링 (아트 요청서 5.4, 96x96, 조사 대상 머리 위)
// - HoldRingUI.Show(월드 위치, 0~1 진행도) 를 누르는 동안 매 프레임 부름, 끝나면 Hide()
// - 12시부터 시계 방향으로 채워짐
// - 씬에 안 놓아도 처음 부를 때 자동으로 생김
public class HoldRingUI : MonoBehaviour
{
    private static HoldRingUI instance;

    [Header("아트 (비우면 코드로 그린 원)")]
    [SerializeField] private Sprite ringBgSprite;    // Common_UI_Hold_Ring_BG
    [SerializeField] private Sprite ringFillSprite;  // Common_UI_Hold_Ring_Fill
    [SerializeField] private Vector2 size = new Vector2(96f, 96f);
    [Tooltip("조사 대상 위로 띄울 높이 (유닛)")]
    [SerializeField] private float worldOffsetY = 1.2f;
    [SerializeField] private int sortingOrder = 80;

    private GameObject canvasGo;
    private RectTransform ring;
    private Image fill;

    private static HoldRingUI Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<HoldRingUI>();
            if (instance == null) instance = new GameObject("HoldRingUI").AddComponent<HoldRingUI>();
            return instance;
        }
    }

    public static void Show(Vector3 worldPos, float progress)
    {
        Instance.ShowInternal(worldPos, progress);
    }

    public static void Hide()
    {
        if (instance != null && instance.canvasGo != null) instance.canvasGo.SetActive(false);
    }

    private void ShowInternal(Vector3 worldPos, float progress)
    {
        EnsureBuilt();
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(worldPos + new Vector3(0f, worldOffsetY, 0f));
        ring.position = screen;
        fill.fillAmount = Mathf.Clamp01(progress);
        if (!canvasGo.activeSelf) canvasGo.SetActive(true);
    }

    private void EnsureBuilt()
    {
        if (canvasGo != null) return;

        RectTransform root = UIBuild.CreateCanvas(transform, "HoldRingCanvas", sortingOrder, out canvasGo);
        ring = UIBuild.Rect(root, "Ring", Vector2.zero, size);
        ring.anchorMin = ring.anchorMax = Vector2.zero; // 화면 좌표로 옮기기 위해 왼쪽 아래 기준

        Image bg = UIBuild.Image(ring, "BG", Vector2.zero, size, ringBgSprite != null ? ringBgSprite : UIBuild.Circle(), Color.white);
        if (ringBgSprite == null) bg.color = new Color(0.17f, 0.13f, 0.09f, 0.8f);

        fill = UIBuild.Image(ring, "Fill", Vector2.zero, size, ringFillSprite != null ? ringFillSprite : UIBuild.Circle(), Color.white);
        if (ringFillSprite == null) fill.color = new Color(0.91f, 0.7f, 0.23f, 0.9f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = true;

        canvasGo.SetActive(false);
    }
}