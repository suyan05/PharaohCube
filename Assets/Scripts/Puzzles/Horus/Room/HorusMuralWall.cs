using System;
using System.Collections;
using UnityEngine;

// 입구 복도 벽의 찢긴 벽화 하나 (빌더가 벽화 타일 앞에 조사 상자로 놓음)
// E: 게임 카메라가 벽화로 다가가며 어두워짐 -> 화면이 바뀌어 '플레이어 등 뒤에서 큰 벽화를 올려다보는' 장면 (MuralStage)
//    -> 먼지/빛줄기 속에서 벽화 글 한두 줄 -> 다시 게임 화면
// [수정] 최종 스토리(B안): 방마다 벽화 4장 (HUD n/4). 조사하면 F_HO_MURAL_{번호}
//        4장을 다 보면 HorusDirector가 방 스토리 연출(호루스 목소리 + 매 착지 + 케프리)을 틂
// 처음 볼 때만 은은한 빛이 깜빡임 (본 뒤엔 꺼짐)
public class HorusMuralWall : MonoBehaviour, IInteractable
{
    public const int Count = 4;
    public const string FlagPrefix = "F_HO_MURAL_";

    [SerializeField] private int index = 1;          // 1~4
    [SerializeField] private string muralKey = HorusMuralScenes.Wall1Key;
    [SerializeField] private SpriteRenderer glint;   // 아직 안 본 벽화 표시 (빌더가 채움, 비어도 됨)

    private bool playing;

    private string SeenFlag => FlagPrefix + index;
    private string Title => index >= 1 && index <= HorusMuralScenes.WallTitles.Length ? HorusMuralScenes.WallTitles[index - 1] : "찢긴 벽화";

    public static int SeenCount()
    {
        int n = 0;
        for (int i = 1; i <= Count; i++) if (HorusDirector.HasFlag(FlagPrefix + i)) n++;
        return n;
    }

    private void Update()
    {
        if (glint == null) return;
        bool seen = HorusDirector.HasFlag(SeenFlag);
        glint.enabled = !seen;
        if (!seen) glint.color = new Color(1f, 0.85f, 0.5f, 0.35f + Mathf.Sin(Time.time * 2.4f) * 0.25f);
    }

    public string GetPrompt() => $"E: 벽화 '{Title}' 보기";

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
        yield return MuralStage.Play(WithCount(HorusMuralScenes.For(muralKey)));
        HorusDirector.SetFlag(SeenFlag);
        CineCamera.Release();
        Cine.End();
        playing = false;
    }

    // 자막 이름 칸: '벽화 이름 (n/4)' - 이번에 처음 보는 거면 하나 더 센 숫자
    private MuralShot[] WithCount(MuralShot[] shots)
    {
        int n = SeenCount() + (HorusDirector.HasFlag(SeenFlag) ? 0 : 1);
        foreach (MuralShot s in shots)
            foreach (DialogueLine l in s.lines) l.speaker = $"{Title} ({n}/{Count})";
        return shots;
    }
}
