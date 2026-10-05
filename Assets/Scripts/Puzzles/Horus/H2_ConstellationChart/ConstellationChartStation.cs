using System;
using UnityEngine;
using UnityEngine.UI;

// H2 매의 성도판 (동쪽 벽): E키로 오버레이 열기/닫기 (EclipseStation과 같은 구조)
public class ConstellationChartStation : MonoBehaviour, IInteractable
{
    [Header("연결")]
    [SerializeField] private ConstellationChartPuzzle puzzle;
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text statusText;
    [SerializeField] private Button undoButton;

    private bool isOpen;
    private PlayerController2D playerController;
    private Rigidbody2D playerRb;

    private void Start()
    {
        if (puzzle == null || overlayPanel == null)
        {
            Debug.LogError("[성도판] Puzzle / Overlay Panel 연결 필요");
            return;
        }
        overlayPanel.SetActive(false);
        puzzle.OnMessage += ShowStatus;
        puzzle.OnTitleChanged += ShowTitle;
        if (undoButton != null) undoButton.onClick.AddListener(puzzle.Undo);
    }

    private void OnDestroy()
    {
        if (puzzle == null) return;
        puzzle.OnMessage -= ShowStatus;
        puzzle.OnTitleChanged -= ShowTitle;
        if (undoButton != null) undoButton.onClick.RemoveListener(puzzle.Undo);
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape)) CloseOverlay();
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        return isOpen ? "E: 성도판 닫기" : "E: 매의 성도 조사";
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
            Debug.LogError($"[성도판] Interact 오류: {e}");
        }
    }

    private void OpenOverlay(GameObject player)
    {
        playerController = player.GetComponent<PlayerController2D>();
        playerRb = player.GetComponent<Rigidbody2D>();
        if (playerController != null) playerController.enabled = false;
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;

        overlayPanel.SetActive(true);
        isOpen = true;
        puzzle.Open();
    }

    private void CloseOverlay()
    {
        overlayPanel.SetActive(false);
        isOpen = false;
        if (playerController != null) playerController.enabled = true;
    }

    private void ShowStatus(string text)
    {
        if (statusText != null) statusText.text = text;
    }

    private void ShowTitle(string text)
    {
        if (titleText != null) titleText.text = text;
    }
}