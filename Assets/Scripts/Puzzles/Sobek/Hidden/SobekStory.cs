using System;
using System.Collections.Generic;
using UnityEngine;

// 소베크 방 대사 도우미 (팀 DialogueBox: 초상화 + 이름 + 글)
// - 씬에 하나 두고 초상화만 연결 (Tools/Sobek 메뉴가 자동으로 붙임)
// - 다른 스크립트는 SobekStory.Say("케프리", "...", "나", "...") 처럼 "이름, 글" 짝으로 부름
public class SobekStory : MonoBehaviour
{
    public const string Sobek = "소베크";
    public const string Khepri = "케프리";
    public const string Me = "나";

    [Header("초상화 (비우면 글만)")]
    [SerializeField] private Sprite sobekPortrait;
    [SerializeField] private Sprite khepriPortrait;
    [SerializeField] private Sprite mePortrait;

    private static SobekStory instance;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private static SobekStory Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<SobekStory>();
            return instance;
        }
    }

    // ("이름", "글", "이름", "글", ...) 짝으로 받아 대사창에 차례로 띄움
    public static void Say(Action finished, params string[] speakerTextPairs)
    {
        var lines = new List<DialogueLine>();
        for (int i = 0; i + 1 < speakerTextPairs.Length; i += 2)
        {
            var line = new DialogueLine(speakerTextPairs[i], speakerTextPairs[i + 1]);
            line.portrait = PortraitOf(speakerTextPairs[i]);
            lines.Add(line);
        }
        if (lines.Count == 0) { finished?.Invoke(); return; }
        DialogueBox.Play(lines.ToArray(), finished);
    }

    public static void Say(params string[] speakerTextPairs) => Say(null, speakerTextPairs);

    public static bool Busy => DialogueBox.IsOpen || DialogueBox.ClosedFrame == Time.frameCount;

    private static Sprite PortraitOf(string speaker)
    {
        SobekStory s = Instance;
        if (s == null) return null;
        if (speaker == Sobek) return s.sobekPortrait;
        if (speaker == Khepri) return s.khepriPortrait;
        if (speaker == Me) return s.mePortrait;
        return null;
    }

    public static bool HasFlag(string f) => !string.IsNullOrEmpty(f) && GameManager.Instance != null && GameManager.Instance.HasFlag(f);

    public static void SetFlag(string f)
    {
        if (!string.IsNullOrEmpty(f) && GameManager.Instance != null && !GameManager.Instance.HasFlag(f)) GameManager.Instance.SetFlag(f);
    }

#if UNITY_EDITOR
    public void EditorSetup(Sprite sobek, Sprite khepri) { sobekPortrait = sobek; khepriPortrait = khepri; }
#endif
}
