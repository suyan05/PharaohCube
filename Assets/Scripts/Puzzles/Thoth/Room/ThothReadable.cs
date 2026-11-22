using System;
using System.Collections;
using UnityEngine;

// 읽기만 하는 물건 (비석, 책장, 두루마리 더미) - E: 대사
// lines는 "화자, 글, 화자, 글..." 순서. key가 있으면 ThothLines에서 가져옴 (stele)
public class ThothReadable : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "E: 살펴보기";
    [SerializeField] private string key = "";
    [SerializeField] private string[] lines = new string[0];

    private bool busy;

    public string GetPrompt() => prompt;

    public void Interact(GameObject who)
    {
        try
        {
            if (busy || ThothState.Busy) return;
            DialogueLine[] l = key == "stele" ? ThothLines.SteleLines : ThothState.Lines(lines);
            if (l.Length > 0) Cine.Run(Read(l));
        }
        catch (Exception e)
        {
            Debug.LogError($"[토트 읽기] Interact 오류 ({name}): {e}");
        }
    }

    private IEnumerator Read(DialogueLine[] l)
    {
        busy = true;
        Cine.Begin(false);
        GameAudio.Play("sfx_th_paper", 0.5f);
        yield return Cine.Say(l);
        Cine.End();
        busy = false;
    }
}
