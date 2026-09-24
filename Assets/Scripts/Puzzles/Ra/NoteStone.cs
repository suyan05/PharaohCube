using UnityEngine;

public class NoteStone : MonoBehaviour, IInteractable
{
    [Header("노트 정보 (기획서 7번 표)")]
    [SerializeField] private int noteId = 1;
    [SerializeField, TextArea] private string noteText = "여섯 신의 시험을 통과하라. 빛은 문을 연다.";

    [Header("조사 시 켤 플래그 (비워두면 없음)")]
    [SerializeField] private string flagOnRead = "";

    public string GetPrompt()
    {
        return "E: 비석 읽기";
    }

    public void Interact(GameObject player)
    {
        Debug.Log($"[노트 {noteId}] {noteText}");

        if (!string.IsNullOrEmpty(flagOnRead))
        {
            GameManager.Instance.SetFlag(flagOnRead);
        }
    }
}