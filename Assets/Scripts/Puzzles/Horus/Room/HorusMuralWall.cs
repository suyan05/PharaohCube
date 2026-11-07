using System;
using System.Collections;
using UnityEngine;

// 입구 복도 벽의 찢긴 벽화 하나 (빌더가 벽화 타일 앞에 조사 상자로 놓음)
// E: 게임 카메라가 벽화로 다가가며 어두워짐 -> 화면이 바뀌어 '플레이어 등 뒤에서 큰 벽화를 올려다보는' 장면 (MuralStage)
//    -> 먼지/빛줄기 속에서 혼잣말 (복선) -> 다시 게임 화면
// 처음 볼 때만 머리 위 '!' 대신 은은한 빛이 깜빡임 (본 뒤엔 꺼짐)
public class HorusMuralWall : MonoBehaviour, IInteractable
{
    [SerializeField] private string muralKey = HorusMuralScenes.Wall1Key;
    [SerializeField] private string title = "찢긴 벽화";
    [SerializeField] private string seenFlag = "F_HO_WALL_1_SEEN";
    [SerializeField] private SpriteRenderer glint;   // 아직 안 본 벽화 표시 (빌더가 채움, 비어도 됨)

    private bool playing;

    private void Update()
    {
        if (glint == null) return;
        bool seen = HorusDirector.HasFlag(seenFlag);
        glint.enabled = !seen;
        if (!seen) glint.color = new Color(1f, 0.85f, 0.5f, 0.35f + Mathf.Sin(Time.time * 2.4f) * 0.25f);
    }

    public string GetPrompt() => $"E: {title} 보기";

    public void Interact(GameObject who)
    {
        try
        {
            if (playing || DialogueBox.IsOpen || Cine.Running || MuralStage.IsPlaying) return;
            Cine.Run(View());
        }
        catch (Exception e)
        {
            Debug.LogError($"[찢긴 벽화] Interact 오류: {e}");
        }
    }

    // 카메라가 벽화로 쑥 다가감(시점 전환) -> 벽화 장면 -> 원래대로
    private IEnumerator View()
    {
        playing = true;
        Cine.Begin(true);
        CineCamera.Focus(transform.position + Vector3.up * 0.6f);
        CineCamera.Zoom(1.6f, 0.7f);
        GameAudio.Play("sfx_dream_shimmer", 0.5f, 0.8f);
        yield return Cine.Wait(0.55f);
        yield return MuralStage.Play(HorusMuralScenes.For(muralKey));
        HorusDirector.SetFlag(seenFlag);
        CineCamera.Release();
        Cine.End();
        playing = false;
    }
}
