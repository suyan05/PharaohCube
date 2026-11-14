using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// [추가] 바스테트의 방 스토리 감독 (스토리 기획서 B안: 바스테트의 방 - 아펩의 첫 습격, 밤 3~4시)
// (1) 방 스토리 연출: 벽화 4장을 다 보면(F_BASTET_MURALS_ALL)
//     횃불이 한꺼번에 흔들림 -> 어둠 속 두 눈 -> 바스테트 목소리 3줄 -> 검은 고양이가 여신상 옆에 내려앉음 + 야옹
//     -> 케프리 한 줄 -> 고양이 길잡이 시작 (F_BASTET_ROOM_STORY)
// (2) 메인 클리어: F_BASTET_MAIN_DONE이 켜지면 금빛 번쩍 + 방이 밝아짐 + 고양이가 플레이어 곁으로
//     (여신 마무리 대사는 BastetGoddess가 이 연출이 끝난 뒤 말함 -> "내 고양이도 데려가라")
//     마무리 대사가 끝나면(F_BASTET_GODDESS_END) 고양이가 플레이어를 따라감
// 붙이는 곳: Bastet_Story/BastetStoryDirector (Tools/Bastet/★ 스토리·검은 고양이 세팅 메뉴가 만듦)
public class BastetStoryDirector : MonoBehaviour
{
    [Header("연결 (비우면 자동으로 찾음)")]
    [SerializeField] private BastetCatGuide cat;
    [Tooltip("바스테트 초상화 대사 창 (BastetStoryCanvas/Bastet_Dialogue)")]
    [SerializeField] private RaDialogue bastetVoice;
    [Tooltip("가운데 홀 바스테트 여신상")]
    [SerializeField] private Transform goddess;
    [SerializeField] private CluePanel panel;

    [Header("(1) 방 스토리 연출")]
    [Tooltip("여신상 기준: 어둠 속 두 눈이 빛나는 자리 (나무 그늘)")]
    [SerializeField] private Vector2 eyesOffset = new Vector2(-1.8f, 1.6f);
    [Tooltip("여신상 기준: 고양이가 내려앉는 자리")]
    [SerializeField] private Vector2 catLandOffset = new Vector2(1.4f, -1.0f);
    [Range(2f, 8f)] [SerializeField] private float cameraZoom = 4.5f;
    [Range(0.2f, 3f)] [SerializeField] private float torchShakeSeconds = 1.3f;

    [SerializeField] private RaDialogueLine[] roomStoryLines =
    {
        new RaDialogueLine(RaPortrait.Night, "아버지의 불을 들고 왔구나, 인간. 조심해라. 내 안의 사자는 아직 잠들지 않았다."),
        new RaDialogueLine(RaPortrait.Default, "오늘 밤 아펩은 내 칼을 피해 배를 삼켰다. 내 고양이들까지 이 어둠에 가뒀다. 가만두지 않겠다."),
        new RaDialogueLine(RaPortrait.Glow, "불이 꺼지기 전에 보고, 기억하고, 지켜라. 그것이 이 방의 시험이다.")
    };
    [SerializeField] private DialogueLine[] afterLandLines =
    {
        new DialogueLine("케프리", "고양이는 라의 눈이다. 우리가 못 보는 어둠도 저 눈은 본다.")
    };

    [Header("(2) 메인 클리어")]
    [Tooltip("클리어 순간 전역 조명 밝기 (해와 달이 겹치며 방 전체가 밝아짐)")]
    [Range(0f, 1f)] [SerializeField] private float clearGlobalLight = 0.4f;
    [Range(0.2f, 4f)] [SerializeField] private float clearLightSeconds = 1.6f;
    [SerializeField] private DialogueLine[] followLines =
    {
        new DialogueLine("케프리", "이제 이 홀의 밤도 저 눈이 지켜줄 거다.")
    };

    private bool roomStoryRunning, clearRunning, subscribed;

    // ================= 초기화 =================
    private void Start()
    {
        if (cat == null) cat = FindFirstObjectByType<BastetCatGuide>();
        if (bastetVoice == null) bastetVoice = FindFirstObjectByType<RaDialogue>(FindObjectsInactive.Include);
        if (goddess == null)
        {
            BastetGoddess g = FindFirstObjectByType<BastetGoddess>();
            if (g != null) goddess = g.transform;
        }
        if (panel == null) panel = FindFirstObjectByType<CluePanel>(FindObjectsInactive.Include);
        if (cat == null) Debug.LogError("[BastetStoryDirector] 검은 고양이(BastetCatGuide)가 없음 -> Tools/Bastet/★ 스토리·검은 고양이 세팅 실행", this);
        if (bastetVoice == null) Debug.LogWarning("[BastetStoryDirector] 바스테트 대사 창(RaDialogue)이 없음 -> 공용 대사창으로 대신 말함", this);

        if (GameManager.Instance != null) { GameManager.Instance.OnFlagChanged += HandleFlag; subscribed = true; }
        else Debug.LogWarning("[BastetStoryDirector] GameManager가 없음 -> 스토리 연출이 시작되지 않음", this);

        // 벽화는 다 봤는데 연출을 못 본 채로 다시 들어온 경우
        if (BastetStoryIds.Has(BastetStoryIds.FlagMuralsAll) && !BastetStoryIds.Has(BastetStoryIds.FlagRoomStory)) StartCoroutine(RoomStory());
    }

    private void OnDestroy()
    {
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
        PlayerLock.Unlock(this);
    }

    private void HandleFlag(string flag)
    {
        if (flag == BastetStoryIds.FlagMuralsAll && !BastetStoryIds.Has(BastetStoryIds.FlagRoomStory) && !roomStoryRunning)
            StartCoroutine(RoomStory());
        else if (flag == BastetItemIds.FlagMainDone && !clearRunning)
        {
            // 같은 프레임에 연출을 시작해야 여신 마무리 대사(BastetGoddess)가 이 연출이 끝날 때까지 기다림
            Cine.Begin(false);
            StartCoroutine(MainClear());
        }
        else if (flag == BastetStoryIds.FlagGoddessEnd && cat != null && !BastetStoryIds.Has(BastetStoryIds.FlagCatFollow))
            cat.StartFollow();
    }

    // ================= (1) 방 스토리 연출 =================
    private IEnumerator RoomStory()
    {
        roomStoryRunning = true;
        // 벽화 창, 대사, 다른 연출이 끝날 때까지 기다림
        yield return null;
        while (Busy()) yield return null;
        yield return new WaitForSeconds(0.4f);

        Debug.Log("[BastetStoryDirector] 방 스토리 연출 시작", this);
        if (cat != null) cat.Directed = true;
        Cine.Begin(false);
        PlayerLock.Lock(this);

        Vector3 g = goddess != null ? goddess.position : (CineActor.Player() != null ? CineActor.Player().position : transform.position);
        CineCamera.Focus(g + Vector3.up * 0.8f);
        CineCamera.Zoom(cameraZoom);
        yield return Cine.Wait(0.8f);

        // 횃불이 한꺼번에 흔들림
        yield return ShakeTorches(torchShakeSeconds);

        // 어둠 속에서 두 눈이 빛남
        Vector3 eyesAt = g + (Vector3)eyesOffset;
        if (cat != null) cat.ShowEyesOnly(eyesAt);
        GameAudio.Play("sfx_bastet_eye", 0.6f);
        CineCamera.Focus(eyesAt);
        yield return Cine.Wait(1.0f);

        // 바스테트 목소리
        yield return Voice(roomStoryLines);

        // 검은 고양이가 여신상 옆 나무 그늘로 내려앉음 + 카메라 0.5초 + 야옹
        Vector3 land = g + (Vector3)catLandOffset;
        CineCamera.Focus(land + Vector3.up * 0.5f);
        if (cat != null)
        {
            yield return cat.JumpTo(land, 1.4f);
            cat.Meow();
        }
        yield return Cine.Wait(0.5f);

        // 케프리
        yield return Cine.Say(afterLandLines);

        BastetStoryIds.Set(BastetStoryIds.FlagRoomStory);
        BastetStoryIds.Set(BastetStoryIds.FlagGoddessIntro); // 여신 소개 대사는 이 연출이 대신함 (반복 안 함)
        PlayerLock.Unlock(this);
        Cine.End();
        if (cat != null) cat.ReleaseToGuide(); // 첫 목표로 어둠 속을 먼저 걸어감
        roomStoryRunning = false;
        Debug.Log("[BastetStoryDirector] 방 스토리 연출 끝 -> 검은 고양이 길 안내 시작", this);
    }

    private bool Busy()
    {
        return (panel != null && panel.IsOpen) || DialogueBox.IsOpen || Cine.Running || MuralStage.IsPlaying || ItemPopup.IsShowing
            || (bastetVoice != null && bastetVoice.IsPlaying) || BastetGoddess.Busy;
    }

    // 바스테트 초상화 대사 (없으면 공용 대사창)
    private IEnumerator Voice(RaDialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0) yield break;
        if (bastetVoice == null)
        {
            var fallback = new DialogueLine[lines.Length];
            for (int i = 0; i < lines.Length; i++) fallback[i] = new DialogueLine("바스테트", lines[i].text);
            yield return Cine.Say(fallback);
            yield break;
        }
        bool done = false;
        bastetVoice.Play(lines, () => done = true);
        while (!done && bastetVoice.IsPlaying) yield return null;
        PlayerLock.Lock(this); // RaDialogue가 닫히면서 플레이어 이동을 켜 버림 -> 연출 중이므로 다시 잠금
    }

    // 방의 횃불 빛(Light2D)을 잠깐 흔들었다가 원래대로
    private IEnumerator ShakeTorches(float seconds)
    {
        var lights = new List<Light2D>();
        var original = new List<float>();
        foreach (WallTorch t in FindObjectsByType<WallTorch>(FindObjectsSortMode.None))
        {
            foreach (Light2D l in t.GetComponentsInChildren<Light2D>())
            {
                lights.Add(l);
                original.Add(l.intensity);
            }
        }
        GameAudio.Play("sfx_wind_gust", 0.5f, 0.8f);
        float t0 = 0f;
        while (t0 < seconds && !Cine.Skipping)
        {
            t0 += Time.deltaTime;
            float fade = 1f - t0 / seconds; // 끝으로 갈수록 잦아듦
            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i] == null) continue;
                float flick = Mathf.PerlinNoise(Time.time * 12f, i * 1.7f);
                lights[i].intensity = original[i] * Mathf.Lerp(1f, 0.15f + flick * 0.9f, fade);
            }
            yield return null;
        }
        for (int i = 0; i < lights.Count; i++) if (lights[i] != null) lights[i].intensity = original[i];
    }

    // ================= (2) 메인 클리어 =================
    // HandleFlag에서 Cine.Begin을 먼저 부른 상태로 들어옴
    private IEnumerator MainClear()
    {
        clearRunning = true;
        Debug.Log("[BastetStoryDirector] 메인 클리어 연출", this);
        if (cat != null) cat.Directed = true;

        // 형판 획득 팝업이 닫힐 때까지
        yield return null;
        while (ItemPopup.IsShowing || DialogueBox.IsOpen) yield return null;

        // 해와 달 문양이 겹치며 방 전체가 밝아짐
        GameAudio.Play("sfx_bastet_clear", 0.7f);
        Cine.Run(ScreenTintUI.Flash(new Color(1f, 0.86f, 0.45f, 0.45f), 0.1f, 0.15f, 0.9f));
        yield return BrightenRoom(clearLightSeconds);

        // 검은 고양이가 플레이어 곁으로
        Transform p = CineActor.Player();
        if (cat != null && p != null)
        {
            Vector3 side = p.position + new Vector3(1.1f, -0.2f, 0f);
            CineCamera.Focus(p.position);
            if (Vector2.Distance(cat.Position, side) > 6f) cat.ShowEyesOnly(side + Vector3.right * 2.5f); // 멀면 어둠에서 나타남
            yield return cat.WalkTo(side);
            cat.Meow();
        }
        yield return Cine.Wait(0.6f);

        Cine.End(); // -> BastetGoddess가 마무리 대사 ("내 고양이도 데려가라")
        if (cat != null) cat.Directed = false;
        clearRunning = false;
    }

    // 전역 조명을 천천히 밝게 (해와 달 퍼즐이 밤으로 어둡게 해 둔 상태에서)
    private IEnumerator BrightenRoom(float seconds)
    {
        var globals = new List<Light2D>();
        var from = new List<float>();
        foreach (Light2D l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (l.lightType != Light2D.LightType.Global) continue;
            globals.Add(l);
            from.Add(l.intensity);
        }
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            for (int i = 0; i < globals.Count; i++)
                if (globals[i] != null) globals[i].intensity = Mathf.Lerp(from[i], Mathf.Max(from[i], clearGlobalLight), k);
            yield return null;
        }
    }

    // ================= 테스트 =================
    [ContextMenu("테스트: 방 스토리 연출 바로 보기")]
    private void TestRoomStory()
    {
        if (Application.isPlaying && !roomStoryRunning) StartCoroutine(RoomStory());
    }

    [ContextMenu("테스트: 벽화 4장을 본 것으로 처리")]
    private void TestAllMurals()
    {
        if (!Application.isPlaying) return;
        for (int i = 1; i <= BastetStoryIds.MuralCount; i++) BastetStoryIds.Set(BastetStoryIds.MuralFlag(i));
        BastetStoryIds.Set(BastetStoryIds.FlagMuralsAll);
    }

    [ContextMenu("테스트: 케프리 따라오기 대사")]
    private void TestFollowLine()
    {
        if (Application.isPlaying && !DialogueBox.IsOpen) DialogueBox.Play(followLines);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 g = goddess != null ? goddess.position : transform.position;
        Gizmos.color = new Color(0.85f, 1f, 0.5f);
        Gizmos.DrawWireSphere(g + (Vector3)eyesOffset, 0.3f);
        Gizmos.DrawLine(g + (Vector3)eyesOffset, g + (Vector3)catLandOffset);
        Gizmos.DrawWireSphere(g + (Vector3)catLandOffset, 0.4f);
    }

#if UNITY_EDITOR
    public void EditorSetup(BastetCatGuide c, RaDialogue voice, Transform g, CluePanel p)
    {
        cat = c; bastetVoice = voice; goddess = g; panel = p;
    }
#endif
}
