using System;
using System.Collections;
using UnityEngine;

// 스핑크스 머리 (기획서 4.1: 말할 때 눈이 금빛 + 화면 살짝 어두워짐)
// - yield return head.Say(대사들);                        말하는 동안 눈 빛남 + 어두워짐 + 울림 소리
// - yield return head.Ask(질문, 선택지들, 고른 번호 받기);  수수께끼
// 그림: 기본 = Pro_Obj_SphinxHead, 빛남 = Pro_Obj_SphinxHead_EyeGlow (같은 캔버스, 눈 픽셀만 다름)
public class SphinxHead : MonoBehaviour
{
    [SerializeField] private SpriteRenderer target;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite glowSprite;

    [Header("말할 때")]
    [SerializeField, Range(0.2f, 1f)] private float dimTo = 0.65f;
    [SerializeField, Min(0f)] private float dimSeconds = 0.4f;
    [Tooltip("SFX_SPHINX_VOICE (낮은 울림, 비워도 됨)")]
    [SerializeField] private AudioClip voiceSfx;
    [Tooltip("눈빛 연출 (후광, 빛줄기, 광원). 비우면 눈 그림만 바뀜")]
    [SerializeField] private SphinxEyeGlow eyeGlow;

    private void Awake()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();
        if (normalSprite == null && target != null) normalSprite = target.sprite;
    }

    public IEnumerator Say(DialogueLine[] lines)
    {
        BeginSpeak();
        yield return Cine.Say(lines);
        EndSpeak();
    }

    public IEnumerator Ask(DialogueLine question, string[] options, Action<int> chosen)
    {
        bool done = false;
        BeginSpeak();
        try
        {
            DialogueBox.Ask(question, options, i =>
            {
                done = true;
                chosen?.Invoke(i);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"[SphinxHead] 선택지 오류: {e}");
            done = true;
        }

        while (!done && DialogueBox.IsOpen) yield return null;
        EndSpeak();
    }

    private void BeginSpeak()
    {
        SetGlow(true);
        CineLight.Dim(dimTo, dimSeconds);
        CineAudio.Play(voiceSfx);
    }

    private void EndSpeak()
    {
        SetGlow(false);
        CineLight.Restore(dimSeconds);
    }

    private void SetGlow(bool on)
    {
        if (eyeGlow != null) eyeGlow.SetOn(on);
        if (target == null) return;
        Sprite s = on ? glowSprite : normalSprite;
        if (s != null) target.sprite = s;
    }
}
