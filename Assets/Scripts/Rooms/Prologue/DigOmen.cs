using System;
using UnityEngine;

// 발굴 현장의 복선(떡밥) 조사 지점 - 모래 위 뱀 자국, 반쯤 묻힌 석판
// - E로 조사하면 혼잣말. 한 번 보면 플래그가 남음 (라의 숨은 길에서 "그 자국이랑 똑같다"는 대사와 이어짐)
// - DigAtmosphere가 만들어 붙임 (씬을 손으로 고치지 않아도 됨)
[RequireComponent(typeof(BoxCollider2D))]
public class DigOmen : MonoBehaviour, IInteractable
{
    private string prompt = "E: 살펴보기";
    private string flag = "";
    private DialogueLine[] lines;

    public void Setup(string promptText, string seenFlag, DialogueLine[] omenLines, Vector2 box)
    {
        prompt = promptText;
        flag = seenFlag;
        lines = omenLines;
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = box;
    }

    public string GetPrompt()
    {
        return Cine.Running ? null : prompt;
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (Cine.Running || DialogueBox.IsOpen || lines == null || lines.Length == 0) return;
            DialogueBox.Play(lines);
            GameManager gm = GameManager.Instance;
            if (gm != null && !string.IsNullOrEmpty(flag) && !gm.HasFlag(flag)) gm.SetFlag(flag);
        }
        catch (Exception e)
        {
            Debug.LogError($"[발굴 현장 복선] {name}: {e.Message}");
        }
    }
}
