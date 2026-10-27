using System;
using System.Collections;
using UnityEngine;

// 다른 씬으로 가는 문 (E로 조사)
// - Required Item이 없으면 Locked Lines (예: 횃불이 없으면 "너무 어둡다")
// - 있으면: 문 앞까지 걸어감 -> 안쪽으로 들어가며 사라짐 -> 암전 -> Next Scene (비우면 Build 순서의 다음 씬)
[RequireComponent(typeof(Collider2D))]
public class SceneDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private string promptText = "들어가기";
    [SerializeField] private string requiredItem = "";
    [SerializeField] private DialogueLine[] lockedLines = { new DialogueLine("나", "아직은 들어갈 수 없다.") };
    [SerializeField] private DialogueLine[] enterLines;
    [Tooltip("문 안쪽 (걸어 들어갈 곳)")]
    [SerializeField] private Vector2 walkInto;
    [SerializeField] private string nextScene = "";
    [SerializeField] private string setFlagOnEnter = "";

    private bool used;

    public string GetPrompt()
    {
        return used ? null : $"E: {promptText}";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (used || Cine.Running || DialogueBox.IsOpen || ItemPopup.IsShowing) return;
            if (DialogueBox.ClosedFrame == Time.frameCount) return;

            if (!HasItem()) DialogueBox.Play(lockedLines);
            else StartCoroutine(Enter(who != null ? who.transform : CineActor.Player()));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SceneDoor] 문 오류: {e}");
        }
    }

    // 중앙 홀: 신전을 하나 끝낼 때마다 같은 북쪽 문이 다음 신전으로 이어짐 (HallTempDirector가 부름)
    public void Retarget(string prompt, string scene)
    {
        if (!string.IsNullOrEmpty(prompt)) promptText = prompt;
        if (!string.IsNullOrEmpty(scene)) nextScene = scene;
    }

    private bool HasItem()
    {
        if (string.IsNullOrEmpty(requiredItem)) return true;
        return ItemInventory.Instance != null && ItemInventory.Instance.Has(requiredItem);
    }

    private IEnumerator Enter(Transform player)
    {
        used = true;
        Cine.Begin(false);
        yield return Cine.Say(enterLines);
        yield return CineActor.WalkTo(player, walkInto, 2f);

        if (!string.IsNullOrEmpty(setFlagOnEnter) && GameManager.Instance != null) GameManager.Instance.SetFlag(setFlagOnEnter);
        Cine.Run(CineActor.Fade(player, 1f, 0f, 0.6f));
        yield return ScreenFader.FadeOut(0.9f);
        yield return CineAudio.StopLoop(0.3f);
        LoadNext();
    }

    private void LoadNext()
    {
        try
        {
            SceneFlow.LoadByNameOrNext(nextScene);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SceneDoor] 씬 이동 오류: {e}");
            Cine.End();
            used = false;
        }
    }
}
