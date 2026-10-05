using System;
using UnityEngine;

// OBJ_H10 매의 제단: E키로 H5 오버레이 열기/닫기
public class SkyAlignStation : MonoBehaviour, IInteractable
{
    [Header("연결")]
    [SerializeField] private SkyAlignPuzzle puzzle;
    [SerializeField] private GameObject overlayPanel;

    private bool isOpen;
    private PlayerController2D playerController;
    private Rigidbody2D playerRb;

    private void Start()
    {
        if (puzzle == null || overlayPanel == null)
        {
            Debug.LogError("[제단] Puzzle / Overlay Panel 연결 필요");
            return;
        }
        overlayPanel.SetActive(false);
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape)) CloseOverlay();
    }

    public string GetPrompt()
    {
        return isOpen ? "E: 제단 닫기" : "E: 매의 제단 조사";
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
            Debug.LogError($"[제단] Interact 오류: {e}");
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
}