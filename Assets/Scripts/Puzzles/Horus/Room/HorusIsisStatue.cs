using System;
using UnityEngine;

// 입구 전실의 이시스 여신상 (빌더가 놓음)
// E: 이시스가 짧게 나타나 벽화로 기억을 보여 줌 (HorusDirector.RequestIsis -> HorusCutscenes.Isis)
// 라/바스테트 방처럼 "조사하면 신과 대화"가 되도록 들어오자마자 나오던 연출을 여기로 옮김
public class HorusIsisStatue : MonoBehaviour, IInteractable
{
    [SerializeField] private HorusDirector director;
    [SerializeField] private string seenFlag = "F_HO_INTRO_PLAYED";

    private void Start()
    {
        try
        {
            if (director == null) director = FindFirstObjectByType<HorusDirector>();
            if (director == null) Debug.LogWarning("[이시스 석상] HorusDirector가 씬에 없음 -> 조사해도 연출이 안 나옴");
        }
        catch (Exception e)
        {
            Debug.LogError($"[이시스 석상] 준비 오류: {e}");
        }
    }

    public string GetPrompt()
    {
        return HorusDirector.HasFlag(seenFlag) ? "E: 이시스의 기억 다시 보기" : "E: 이시스 여신상 조사";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (DialogueBox.IsOpen || Cine.Running || MuralStage.IsPlaying) return;
            if (director == null)
            {
                HorusWorld.Say("날개를 펼친 여신상이다. 눈가에 물기가 맺혀 있다.");
                return;
            }
            director.RequestIsis();
        }
        catch (Exception e)
        {
            Debug.LogError($"[이시스 석상] Interact 오류: {e}");
        }
    }
}
