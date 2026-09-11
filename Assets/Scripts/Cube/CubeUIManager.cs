using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CubeUIManager : MonoBehaviour
{
    // 싱글톤
    public static CubeUIManager Instance { get; private set; }

    [Header("UI Root")]
    [SerializeField] private GameObject cubeUIPanel;

    [Header("Face Info")]
    [SerializeField] private TextMeshProUGUI txtFaceName;
    [SerializeField] private TextMeshProUGUI txtEngraving;
    [SerializeField] private Button btnEquip;

    [Header("Navigation Buttons")]
    [SerializeField] private Button btnUp;
    [SerializeField] private Button btnDown;
    [SerializeField] private Button btnLeft;
    [SerializeField] private Button btnRight;
    [SerializeField] private Button btnClose;

    [Header("Managers")]
    [SerializeField] private CubeFaceManager cubeFaceManager;
    [SerializeField] private CubeFaceState cubeFaceState;

    private static readonly string[] FaceGodNames = { "라 (태양의 신)", "아누비스 (저승의 신)", "토트 (지식의 신)", "바스테트 (고양이의 신)", "소베크 (나일의 신)", "호루스 (매의 신)" };
    private static readonly string[] HieroglyphWords = { "태양", "질서", "왕", "고양이", "물", "하늘" };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        BindButtons();
    }

    //TODO(강태호) : 실제 면 확대 UI 여는 코드 작성 예정
    private void Start()
    {
        if (cubeFaceManager != null)
            cubeFaceManager.OnFaceChanged += HandleFaceChanged;

        if (cubeFaceState != null)
            cubeFaceState.OnFaceActivated += HandleFaceActivated;

        // 시작 시 패널 숨김
        if (cubeUIPanel != null)
            cubeUIPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (cubeFaceManager != null)
            cubeFaceManager.OnFaceChanged -= HandleFaceChanged;

        if (cubeFaceState != null)
            cubeFaceState.OnFaceActivated -= HandleFaceActivated;
    }

    private void BindButtons()
    {
        btnUp.onClick.AddListener(() => cubeFaceManager.MoveUp());
        btnDown.onClick.AddListener(() => cubeFaceManager.MoveDown());
        btnLeft.onClick.AddListener(() => cubeFaceManager.MoveLeft());
        btnRight.onClick.AddListener(() => cubeFaceManager.MoveRight());

        btnEquip.onClick.AddListener(() => cubeFaceManager.EquipCurrentFace());
        btnClose.onClick.AddListener(CloseUI);
    }

    public void OpenUI()
    {
        Debug.Log("[CubeUIManager] 큐브 UI 열림"); // 디버깅용 (선택 사항)
        cubeUIPanel.SetActive(true);
        RefreshFaceUI(cubeFaceManager.currentFace);
    }

    public void CloseUI()
    {
        cubeUIPanel.SetActive(false);
    }

    private void HandleFaceChanged(int faceIndex) => RefreshFaceUI(faceIndex);
    private void HandleFaceActivated(int faceIndex) => RefreshFaceUI(cubeFaceManager.currentFace);

    private void RefreshFaceUI(int faceIndex)
    {
        if (faceIndex < 0 || faceIndex >= FaceGodNames.Length) return;

        txtFaceName.text = FaceGodNames[faceIndex];

        bool isActivated = cubeFaceState.faceActivated[faceIndex];
        txtEngraving.text = isActivated ? $"각인됨: [{HieroglyphWords[faceIndex]}]" : "미각인 (형판 필요)";
        btnEquip.interactable = cubeFaceManager.CanEquipCurrentFace();
    }
}
