using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 호루스의 방 연출 감독 (씬에 1개)
// - 처음 들어오면: 입구 전실 연출 (이시스의 목소리)
// - 본당에 처음 발을 들이면: 신전이 울리고 호루스의 매가 날아와 방을 돌고 첫 목적지에 앉음
// - 성좌/퍼즐을 풀 때마다: 천장 별이 반짝 + 호루스 한마디 + 매가 다음 목적지로 날아가 앉음 (길 안내)
// - H5 클리어: 하늘을 되찾는 엔딩 (형판이 떠오르고 방 전체가 밝아짐 -> 북문)
// 퍼즐 화면이 열려 있거나 대사/연출 중이면 끝날 때까지 기다렸다가 차례로 보여줌
// 실제 장면은 HorusCutscenes에 있음
public class HorusDirector : MonoBehaviour
{
    [Header("그림 (빌더가 채움)")]
    [SerializeField] private Sprite horusPortrait;
    [SerializeField] private Sprite falconSprite;
    [SerializeField] private Sprite plateSprite;
    [SerializeField] private Sprite emoteSprite;

    [Header("이시스 유령 그림 (빌더가 채움, 비어 있으면 빛 덩어리 연출)")]
    [SerializeField] private Sprite isisSprite;
    [SerializeField] private Sprite isisWing;
    [SerializeField] private Sprite featherSprite;
    [SerializeField] private Sprite rippleSprite;

    [Header("매 길잡이 / 꿈 화면 효과 (이시스가 말할 때만 켜짐)")]
    [SerializeField] private HorusFalconGuide falcon;
    [SerializeField] private Volume dreamVolume;

    [Header("퍼즐 화면 (하나라도 켜져 있으면 연출을 미룸)")]
    [SerializeField] private GameObject[] overlays;

    [Header("플래그")]
    [SerializeField] private string introFlag = "F_HO_INTRO_PLAYED";
    [SerializeField] private string awakenFlag = "F_HO_AWAKENED";
    [SerializeField] private string clearFlag = "F_HO_H5_DONE";
    [SerializeField] private string endingFlag = "F_HO_ENDING_PLAYED";

    [Header("본당에 들어왔다고 보는 높이 (y)")]
    [SerializeField] private float awakenY = 0.8f;

    [Header("하늘을 되찾은 뒤 방 밝기")]
    [SerializeField] private float clearIntensity = 0.85f;
    [SerializeField] private Color clearColor = new Color(0.95f, 0.93f, 1f);

    public Sprite HorusPortrait => horusPortrait;
    public Sprite FalconSprite => falconSprite;
    public Sprite PlateSprite => plateSprite;
    public Sprite EmoteSprite => emoteSprite;
    public Sprite IsisSprite => isisSprite;
    public Sprite IsisWing => isisWing;
    public Sprite FeatherSprite => featherSprite;
    public Sprite RippleSprite => rippleSprite;
    public HorusFalconGuide Falcon => falcon;
    public Volume DreamVolume => dreamVolume;
    public float ClearIntensity => clearIntensity;
    public Color ClearColor => clearColor;
    public string IntroFlag => introFlag;
    public string AwakenFlag => awakenFlag;
    public string EndingFlag => endingFlag;

    private readonly List<string> queue = new List<string>();
    private bool subscribed;
    private CluePanel cluePanel;

    private void Start()
    {
        try
        {
            cluePanel = FindFirstObjectByType<CluePanel>(FindObjectsInactive.Include);
            HorusFocus.SetOverlays(overlays);
            if (dreamVolume != null) dreamVolume.weight = 0f;
            if (HasFlag(endingFlag)) RestoreSky();
            if (!HasFlag(introFlag)) queue.Add(HorusCutscenes.Intro);
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnFlagChanged += HandleFlag;
            subscribed = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[호루스 연출] 준비 오류: {e}");
        }
    }

    private void OnDestroy()
    {
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
    }

    private void HandleFlag(string flag)
    {
        if (flag == clearFlag && !HasFlag(endingFlag)) queue.Add(HorusCutscenes.Ending);
        else if (HorusCutscenes.HasReaction(flag)) queue.Add(flag);
        // 치트로 플래그가 한꺼번에 켜지면 마지막 2개만 (엔딩은 항상 남김)
        while (queue.Count > 2 && queue[0] != HorusCutscenes.Ending) queue.RemoveAt(0);
    }

    private void Update()
    {
        if (!CanPlay()) return;
        if (queue.Count > 0)
        {
            string key = queue[0];
            queue.RemoveAt(0);
            Cine.Run(HorusCutscenes.For(this, key));
            return;
        }
        if (ShouldAwaken()) Cine.Run(HorusCutscenes.For(this, HorusCutscenes.Awaken));
    }

    private bool ShouldAwaken()
    {
        if (!HasFlag(introFlag) || HasFlag(awakenFlag)) return false;
        Transform p = CineActor.Player();
        return p != null && p.position.y > awakenY;
    }

    private bool CanPlay()
    {
        if (Cine.Running || DialogueBox.IsOpen || ItemPopup.IsShowing) return false;
        if (cluePanel != null && cluePanel.IsOpen) return false;
        if (overlays == null) return true;
        foreach (GameObject o in overlays) if (o != null && o.activeInHierarchy) return false;
        return true;
    }

    // 엔딩을 이미 본 뒤 다시 들어왔을 때: 밝은 하늘 그대로
    private void RestoreSky()
    {
        var global = HorusFx.GlobalLight();
        if (global == null) return;
        global.intensity = clearIntensity;
        global.color = clearColor;
    }

    public static bool HasFlag(string flag)
    {
        return !string.IsNullOrEmpty(flag) && GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
    }

    public static void SetFlag(string flag)
    {
        if (!string.IsNullOrEmpty(flag) && GameManager.Instance != null && !GameManager.Instance.HasFlag(flag)) GameManager.Instance.SetFlag(flag);
    }
}
