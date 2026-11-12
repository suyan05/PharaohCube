using System;
using UnityEngine;

// 이야기 비석 (퍼즐 단서가 아니라 신화 한 줄 / 복선). E: 대사창으로 읽음 + 플래그(수첩 '밤의 기록'용)
// 최종 스토리(B안) 호루스 비석 3: "심판의 날, 매 머리의 호루스가 죽은 자의 손을 잡고..." (히든 '호루스의 가호' 복선)
public class HorusStoryStele : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "E: 비석 읽기";
    [SerializeField] private string readFlag = "F_HO_STELE_3";
    [SerializeField] private DialogueLine[] lines = { new DialogueLine("비석", "...") };

    public string GetPrompt() => prompt;

    public void Interact(GameObject who)
    {
        try
        {
            if (DialogueBox.IsOpen || Cine.Running || MuralStage.IsPlaying) return;
            DialogueBox.Play(lines, () => HorusDirector.SetFlag(readFlag));
        }
        catch (Exception e)
        {
            Debug.LogError($"[이야기 비석] Interact 오류: {e}");
        }
    }

#if UNITY_EDITOR
    public void EditorSetup(string flag, DialogueLine[] text)
    {
        readFlag = flag;
        lines = text;
    }
#endif
}
