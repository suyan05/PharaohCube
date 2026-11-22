using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 토트의 방 연출 감독 (씬에 1개, 빌더가 놓음)
// - 처음 들어오면: 천창 달빛에서 플레이어로 내려오는 카메라 + 제목 + 케프리
// - 달 시계를 맞추면: 달빛이 밝아지고 북쪽 벽감을 막던 돌판이 올라감 (벽화 C 조각이 빛 속에 나타남) + 달 조각 (2)
// - 주문을 다 읊으면: 지혜의 문이 열리고 토트 일러스트 + 형판 / 우자트 부적
// - 목표 문구는 진행 상황을 보고 자동으로 바꿈
// 퍼즐 화면, 대사, 연출 중이면 끝날 때까지 기다렸다가 차례로 보여줌
public class ThothDirector : MonoBehaviour
{
    private const string KeyIntro = "intro", KeyReveal = "reveal", KeyFinale = "finale";

    [Header("방 물건 (빌더가 채움)")]
    [SerializeField] private SpriteRenderer moonbeam;
    [SerializeField] private Light2D beamLight;
    [SerializeField] private Transform slab;
    [SerializeField] private ThothGate gate;
    [SerializeField] private Transform skylight;   // 인트로 카메라가 내려오는 곳 (달 시계 위)

    [Header("달빛 밝기")]
    [SerializeField] private float beamDim = 0.45f;
    [SerializeField] private float beamBright = 0.95f;
    [SerializeField] private float globalBright = 0.85f;

    private readonly List<string> queue = new List<string>();
    private bool subscribed;
    private string lastGoal;

    private void Start()
    {
        try
        {
            RestoreRoom();
            if (!ThothState.Has(ThothIds.Intro)) queue.Add(KeyIntro);
            if (ThothState.Has(ThothIds.DialDone) && !ThothState.Has(ThothIds.RevealDone)) queue.Add(KeyReveal);
            if (ThothState.Has(ThothIds.SpellDone) && !ThothState.Has(ThothIds.Done)) queue.Add(KeyFinale);
            Cine.Ended += RefreshGoal;
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnFlagChanged += HandleFlag;
            subscribed = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[토트 연출] 준비 오류: {e}");
        }
    }

    private void OnDestroy()
    {
        Cine.Ended -= RefreshGoal;
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
    }

    private void HandleFlag(string flag)
    {
        if (flag == ThothIds.DialDone && !queue.Contains(KeyReveal)) queue.Add(KeyReveal);
        else if (flag == ThothIds.SpellDone && !queue.Contains(KeyFinale)) queue.Add(KeyFinale);
        lastGoal = null;
    }

    private void Update()
    {
        if (ThothState.Busy) return;
        if (queue.Count > 0)
        {
            string key = queue[0];
            queue.RemoveAt(0);
            Cine.Run(Play(key));
            return;
        }
        RefreshGoal();
    }

    private IEnumerator Play(string key)
    {
        if (key == KeyIntro) return ThothCutscenes.Intro(this);
        if (key == KeyReveal) return ThothCutscenes.Reveal(this);
        return ThothCutscenes.Finale(this);
    }

    // ================= 목표 문구 =================
    public static string Goal()
    {
        if (ThothState.Has(ThothIds.SpellDone)) return ThothLines.GoalExit;
        if (ThothState.Has(ThothIds.FrescoDone("C"))) return ThothLines.GoalSpell;
        if (ThothState.Has(ThothIds.DialDone)) return ThothLines.GoalC;
        if (ThothState.Has(ThothIds.DeskDone)) return ThothLines.GoalDial;
        if (ThothState.Has(ThothIds.FrescoDone("B"))) return ThothLines.GoalDesk;
        if (ThothState.Has(ThothIds.FrescoDone("A"))) return ThothLines.GoalB;
        return ThothLines.GoalA;
    }

    private void RefreshGoal()
    {
        if (Cine.Running || PuzzleOverlayFocus.IsActive) return;
        string g = Goal();
        if (g == lastGoal) return;
        lastGoal = g;
        ObjectiveBoxUI.Show(g);
    }

    // ================= 방 상태 (컷신에서도 부름) =================
    public Transform Skylight => skylight != null ? skylight : transform;
    public Transform Slab => slab;
    public ThothGate Gate => gate;

    private void RestoreRoom()
    {
        bool bright = ThothState.Has(ThothIds.RevealDone);
        SetBeam(bright ? 1f : 0f);
        if (bright) OpenSlabNow();
        if (ThothState.Has(ThothIds.Done)) SetGlobal(globalBright);
    }

    // 0 = 처음 달빛, 1 = 달 시계를 맞춘 뒤 밝은 달빛
    public void SetBeam(float t)
    {
        if (moonbeam == null) return;
        Color c = moonbeam.color;
        c.a = Mathf.Lerp(beamDim, beamBright, t);
        moonbeam.color = c;
        moonbeam.transform.localScale = Vector3.one * Mathf.Lerp(1.25f, 1.5f, t);
        if (beamLight != null) beamLight.intensity = Mathf.Lerp(1.1f, 2.2f, t);
    }

    public void OpenSlabNow()
    {
        if (slab == null) return;
        slab.gameObject.SetActive(false);
    }

    public void SetGlobal(float intensity)
    {
        foreach (Light2D l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (l.lightType == Light2D.LightType.Global) l.intensity = intensity;
        }
    }

    public float GlobalBright => globalBright;
}
