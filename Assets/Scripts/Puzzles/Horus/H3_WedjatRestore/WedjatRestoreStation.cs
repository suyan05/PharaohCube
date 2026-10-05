using System;
using UnityEngine;

// OBJ_H07 우자트 복원 석탁: E키로 H3 오버레이 열기/닫기
public class WedjatRestoreStation : MonoBehaviour, IInteractable
{
    [Header("연결")]
    [SerializeField] private WedjatRestorePuzzle puzzle;
    [SerializeField] private GameObject overlayPanel;

    private bool isOpen;
    private PlayerController2D playerController;
    private Rigidbody2D playerRb;

    private void Start()
    {
        if (puzzle == null || overlayPanel == null)
        {
            Debug.LogError("[석탁] Puzzle / Overlay Panel 연결 필요");
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
        return isOpen ? "E: 석탁 닫기" : "E: 우자트 석탁 조사";
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
            Debug.LogError($"[석탁] Interact 오류: {e}");
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
        puzzle.Close();
        if (playerController != null) playerController.enabled = true;
    }
}