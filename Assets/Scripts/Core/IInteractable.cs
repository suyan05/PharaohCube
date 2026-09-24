using UnityEngine;

// E키로 조사 가능한 모든 오브젝트가
public interface IInteractable
{
    // 가까이 갔을 때 보여줄 안내 문구 (예: "E: 비석 읽기")
    string GetPrompt();

    // E키 눌렀을 때 실행
    void Interact(GameObject player);
}