using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// OBJ_007 일식 석탁 (아트 버전)
// E키 -> 오버레이 열기/닫기, 원판 3개를 클릭하면 90도씩 시계 방향 회전
// 정답 판정은 EclipsePuzzle이 그대로 담당하고, 이 스크립트는 화면만 담당
// 컴포넌트 우클릭 -> "P3 UI 자동 생성"으로 오버레이 안의 UI를 한 번에 만들 수 있음
public class EclipseArtStation : MonoBehaviour, IInteractable
{
    [Serializable]
    private class DiskSlot
    {
        public Image image;
        [Tooltip("보드 기준 원판 중심 (0~1, 왼쪽 아래가 0,0)")]
        public Vector2 center;
        [Tooltip("보드 기준 원판 크기 (0~1)")]
        public Vector2 size;
        [Tooltip("자동 생성 때 찾을 스프라이트 파일 이름")]
        public string spriteName;
    }

    [Header("연결 (자동 생성하면 알아서 채워짐)")]
    [SerializeField] private EclipsePuzzle puzzle;
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private Text statusText;
    [SerializeField] private Image completeImage;

    [Header("회전 원판 (배열 순서 = A, B, C)")]
    [SerializeField]
    private DiskSlot[] disks =
    {
        new DiskSlot { center = new Vector2(0.3932f, 0.3750f), size = new Vector2(0.5173f, 0.3450f), spriteName = "P3_Disk_BottomLeft" }, // A
        new DiskSlot { center = new Vector2(0.4316f, 0.6836f), size = new Vector2(0.4170f, 0.2781f), spriteName = "P3_Disk_Top" },        // B
        new DiskSlot { center = new Vector2(0.7265f, 0.5461f), size = new Vector2(0.3248f, 0.2166f), spriteName = "P3_Disk_Right" }       // C
    };

    [Header("튜닝 (기획서 6.2)")]
    [SerializeField] private float hintDelay = 30f;
    [SerializeField] private float rotateAnimTime = 0.25f;
    [SerializeField] private Color hintColor = new Color(1f, 0.55f, 0.55f);
    [SerializeField] private Color lockedColor = new Color(0.55f, 0.55f, 0.55f);

    private const string OverlayName = "P3_Overlay";
    private const string BoardSpriteName = "P3_Board_Base";
    private const string CompleteSpriteName = "P3_Complete";
    private static readonly Vector2 BoardSize = new Vector2(500f, 750f); // 가로:세로 = 2:3 유지
    private static readonly Vector2 BoardPos = new Vector2(0f, 40f);

    private float[] currentZ;
    private bool isOpen;
    private bool hintActive;
    private float hintTimer;
    private PlayerController2D playerController;
    private Rigidbody2D playerRb;

    // ================= 초기화 =================
    private void Start()
    {
        try
        {
            if (!ValidateRefs()) return;

            currentZ = new float[disks.Length];
            for (int i = 0; i < disks.Length; i++)
            {
                PlaceDisk(disks[i]);
                AddClick(disks[i], i);
            }

            overlayPanel.SetActive(false);
            puzzle.OnStateChanged += RefreshVisuals;
            puzzle.OnPigmentApplied += HandlePigmentApplied;
            puzzle.OnSuccess += HandleSuccess;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 초기화 오류: {e}");
        }
    }

    private void OnDestroy()
    {
        if (puzzle == null) return;
        puzzle.OnStateChanged -= RefreshVisuals;
        puzzle.OnPigmentApplied -= HandlePigmentApplied;
        puzzle.OnSuccess -= HandleSuccess;
    }

    private bool ValidateRefs()
    {
        if (puzzle == null || overlayPanel == null || statusText == null)
        {
            Debug.LogError($"[{name}] puzzle / overlayPanel / statusText 중 비어 있는 칸이 있음 (컴포넌트 우클릭 -> P3 UI 자동 생성)");
            return false;
        }

        if (disks == null || disks.Length != EclipsePuzzle.RotatingSlots.Length)
        {
            Debug.LogError($"[{name}] disks는 {EclipsePuzzle.RotatingSlots.Length}개(A, B, C)여야 함");
            return false;
        }

        foreach (DiskSlot d in disks)
        {
            if (d == null || d.image == null)
            {
                Debug.LogError($"[{name}] disks에 Image가 빠진 칸이 있음");
                return false;
            }
        }
        return true;
    }

    // 보드 크기가 바뀌어도 원판이 같은 자리에 오도록 앵커로 배치
    private static void PlaceDisk(DiskSlot d)
    {
        RectTransform rt = d.image.rectTransform;
        Vector2 half = d.size * 0.5f;

        rt.anchorMin = d.center - half;
        rt.anchorMax = d.center + half;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f); // 원판 가운데를 축으로 회전
    }

    private void AddClick(DiskSlot d, int index)
    {
        Button btn = d.image.GetComponent<Button>();
        if (btn == null) btn = d.image.gameObject.AddComponent<Button>();

        btn.transition = Selectable.Transition.None; // 색은 RefreshVisuals가 관리
        btn.onClick.AddListener(() => OnDiskClicked(index));

        d.image.raycastTarget = true;
        // 원판의 투명한 모서리는 클릭 안 되게 (텍스처 Read/Write 필요 -> 자동 생성 때 켜줌)
        d.image.alphaHitTestMinimumThreshold = 0.1f;
    }

    // ================= 매 프레임 =================
    private void Update()
    {
        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseOverlay();
            return;
        }

        AnimateDisks();
        UpdateHint();
    }

    private void AnimateDisks()
    {
        float speed = 90f / Mathf.Max(0.01f, rotateAnimTime);
        for (int i = 0; i < disks.Length; i++)
        {
            currentZ[i] = Mathf.MoveTowardsAngle(currentZ[i], TargetZ(i), speed * Time.deltaTime);
            disks[i].image.rectTransform.localRotation = Quaternion.Euler(0, 0, currentZ[i]);
        }
    }

    // 30초 지나면 어긋난 원판을 붉게 (기획서 6.2)
    private void UpdateHint()
    {
        if (hintActive || !puzzle.PigmentApplied || puzzle.IsCleared) return;

        hintTimer += Time.deltaTime;
        if (hintTimer < hintDelay) return;

        hintActive = true;
        statusText.text = "어긋난 원판의 윤곽이 붉게 빛난다...";
        RefreshVisuals();
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        return isOpen ? "E: 석탁 닫기" : "E: 일식 석탁 조사";
    }

    public void Interact(GameObject player)
    {
        try
        {
            if (isOpen) CloseOverlay();
            else OpenOverlay(player);
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 상호작용 오류: {e}");
        }
    }

    // ================= 열기 / 닫기 =================
    private void OpenOverlay(GameObject player)
    {
        LockPlayer(player);
        overlayPanel.SetActive(true);
        isOpen = true;

        statusText.text = GetOpenMessage();
        puzzle.Open(); // 여기서 청색 안료 자동 사용 시도 (성공하면 문구가 바뀜)

        SnapDisks();
        RefreshVisuals();
    }

    private void CloseOverlay()
    {
        overlayPanel.SetActive(false);
        isOpen = false;
        if (playerController != null) playerController.enabled = true;
    }

    private void LockPlayer(GameObject player)
    {
        playerController = player.GetComponent<PlayerController2D>();
        playerRb = player.GetComponent<Rigidbody2D>();
        if (playerController != null) playerController.enabled = false;
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;
    }

    private string GetOpenMessage()
    {
        if (puzzle.IsCleared) return "이미 복원한 원판이다.";
        if (puzzle.PigmentApplied) return "원판을 돌려 태양 문양을 이어보자.";
        return "밤 문양 조각이 빠져 있다. 무언가로 채워야 할 것 같다.";
    }

    // 열 때는 애니메이션 없이 현재 각도로 바로 맞춤
    private void SnapDisks()
    {
        for (int i = 0; i < disks.Length; i++)
        {
            currentZ[i] = TargetZ(i);
            disks[i].image.rectTransform.localRotation = Quaternion.Euler(0, 0, currentZ[i]);
        }
    }

    // ================= 입력 / 화면 갱신 =================
    private void OnDiskClicked(int index)
    {
        if (!puzzle.PigmentApplied)
        {
            statusText.text = "밤 문양 조각이 빠져 있다. 무언가로 채워야 할 것 같다.";
            return;
        }
        puzzle.RotatePiece(index);
    }

    private void RefreshVisuals()
    {
        for (int i = 0; i < disks.Length; i++)
        {
            disks[i].image.color = GetDiskColor(i);
        }

        if (completeImage != null)
        {
            completeImage.enabled = puzzle.IsCleared;
        }
    }

    private Color GetDiskColor(int index)
    {
        if (!puzzle.PigmentApplied) return lockedColor;
        if (hintActive && !puzzle.IsCorrect(index)) return hintColor;
        return Color.white;
    }

    private void HandlePigmentApplied()
    {
        statusText.text = "청색 안료로 밤 문양을 채웠다. 원판이 움직인다!";
        RefreshVisuals();
    }

    private void HandleSuccess()
    {
        statusText.text = "태양 문양이 완성됐다! 거울 조각 2 + 프리즘 획득";
        RefreshVisuals();
    }

    // UI에서 z가 마이너스면 시계 방향. 어긋난 횟수만큼 시계 방향으로 돌아가 있음
    private float TargetZ(int index)
    {
        return -puzzle.GetOffset(index) * 90f;
    }

    // ================= 에디터 전용: UI 자동 생성 =================
#if UNITY_EDITOR
    [ContextMenu("P3 UI 자동 생성")]
    private void BuildUIInEditor()
    {
        try
        {
            if (!AutoFindSceneRefs()) return;

            RectTransform root = overlayPanel.GetComponent<RectTransform>();
            if (root.Find("Board") != null)
            {
                Debug.LogWarning($"[{name}] {OverlayName} 안에 Board가 이미 있음. 다시 만들려면 Board, StatusText를 지우고 실행");
                return;
            }

            Undo.RecordObject(this, "P3 UI 자동 생성");
            RectTransform board = CreateBoard(root);
            CreateDisks(board);
            completeImage = CreateComplete(board);
            statusText = CreateStatusText(root);

            EditorUtility.SetDirty(this);
            Debug.Log($"[{name}] P3 UI 생성 완료! 씬 저장(Ctrl+S) 잊지 말기");
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] UI 자동 생성 실패: {e}");
        }
    }

    private bool AutoFindSceneRefs()
    {
        if (puzzle == null) puzzle = FindFirstObjectByType<EclipsePuzzle>(FindObjectsInactive.Include);
        if (overlayPanel == null) overlayPanel = GameObject.Find(OverlayName);

        if (puzzle == null)
        {
            Debug.LogError($"[{name}] 씬에서 EclipsePuzzle을 못 찾음 -> Puzzle 칸에 직접 넣어줘");
            return false;
        }
        if (overlayPanel == null)
        {
            Debug.LogError($"[{name}] {OverlayName}을 못 찾음 (꺼져 있으면 켜고 다시 실행) -> 또는 Overlay Panel 칸에 직접 넣어줘");
            return false;
        }
        return true;
    }

    private RectTransform CreateBoard(RectTransform root)
    {
        Image board = CreateImage(root, "Board", LoadSprite(BoardSpriteName));
        RectTransform rt = board.rectTransform;

        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = BoardSize;
        rt.anchoredPosition = BoardPos;
        board.preserveAspect = false; // 켜면 원판 자리가 어긋남
        return rt;
    }

    private void CreateDisks(RectTransform board)
    {
        string[] labels = { "A", "B", "C" };
        for (int i = 0; i < disks.Length; i++)
        {
            Sprite sprite = LoadSprite(disks[i].spriteName);
            EnsureReadable(sprite);

            disks[i].image = CreateImage(board, $"Disk_{labels[i]}", sprite);
            PlaceDisk(disks[i]);
        }
    }

    private Image CreateComplete(RectTransform board)
    {
        Image img = CreateImage(board, "Complete", LoadSprite(CompleteSpriteName));
        RectTransform rt = img.rectTransform;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        img.enabled = false; // 클리어하면 코드가 켜줌
        return img;
    }

    private Text CreateStatusText(RectTransform root)
    {
        var go = new GameObject("StatusText", typeof(RectTransform), typeof(Text));
        Undo.RegisterCreatedObjectUndo(go, "Create StatusText");
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(root, false);
        rt.anchoredPosition = new Vector2(0f, -380f);
        rt.sizeDelta = new Vector2(1200f, 60f);

        var t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 28;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.color = new Color(0.965f, 0.945f, 0.906f); // #F6F1E7
        t.raycastTarget = false;
        return t;
    }

    private static Image CreateImage(RectTransform parent, string objName, Sprite sprite)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, $"Create {objName}");
        go.GetComponent<RectTransform>().SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    // 파일 이름이 정확히 같은 Sprite를 프로젝트에서 찾음
    private static Sprite LoadSprite(string spriteName)
    {
        foreach (string guid in AssetDatabase.FindAssets($"{spriteName} t:Sprite"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != spriteName) continue;

            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;
        }

        Debug.LogWarning($"[EclipseArtStation] 스프라이트 '{spriteName}'를 못 찾음 -> 파일 이름이랑 Texture Type(Sprite) 확인");
        return null;
    }

    // 원판 클릭 판정(alphaHitTest)에 필요한 Read/Write 옵션 켜기
    private static void EnsureReadable(Sprite sprite)
    {
        if (sprite == null) return;

        string path = AssetDatabase.GetAssetPath(sprite);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || importer.isReadable) return;

        importer.isReadable = true;
        importer.SaveAndReimport();
    }
#endif
}