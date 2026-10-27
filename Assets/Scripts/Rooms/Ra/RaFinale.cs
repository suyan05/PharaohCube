using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 밤 P5까지 다 풀면(F_RA_NIGHT_P5_DONE) 라가 직접 모습을 드러내는 엔딩 (예전 "시현 데모 종료" 대신)
// 1. 퍼즐 화면을 닫고 조작이 풀리면 -> 카메라가 라의 성소로 -> 금빛이 차오르며 흔들림
// 2. 하얗게 번쩍 -> 라 일러스트 전체 화면 + 강렬한 이집트 음악(bgm_ra_finale) + 라의 대사
// 3. 큐브 그림이 나오고 태양 형판이 라의 면에 박힘 -> 아이템 획득 팝업(PLATE_RA)
// 4. 작별 대사 -> 암전 -> 중앙 홀(CentralRoom_Temp)로 이동 -> 중앙 홀에서 돌아온 연출
// - 성소를 직접 눌러도 이 연출이 나옴 (RaShrine이 TryStart를 부름)
// - 씬에 안 놓아도 RaRuntimeBootstrap이 자동으로 붙임
// - ESC 1초 = 건너뛰기 (형판 지급과 이동은 그대로 됨)
public class RaFinale : MonoBehaviour
{
    private static RaFinale instance;

    [Header("플래그")]
    [SerializeField] private string nightClearFlag = "F_RA_NIGHT_P5_DONE";
    [SerializeField] private string plateGivenFlag = "F_RA_PLATE_GIVEN";
    [SerializeField] private string endTalkedFlag = "F_RA_SHRINE_END_TALKED";
    [SerializeField] private string plateItemId = "PLATE_RA";

    [Header("이동 (Build Profiles의 Scene List에 있어야 함)")]
    [SerializeField] private string hallScene = "CentralRoom_Temp";

    [Header("연출")]
    [SerializeField, Min(0f)] private float delay = 0.8f;
    [SerializeField, Min(1f)] private float shrineViewSize = 4.5f;
    [SerializeField] private string music = "bgm_ra_finale";

    [Header("대사")]
    [SerializeField] private RaFinaleLines lines = new RaFinaleLines();

    private RaShrine shrine;
    private bool started;
    private bool subscribed;

    // ================= 시작 조건 =================
    private void Awake()
    {
        instance = this;
    }

    private IEnumerator Start()
    {
        shrine = FindFirstObjectByType<RaShrine>(FindObjectsInactive.Include);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFlagChanged += HandleFlag;
            subscribed = true;
        }
        yield return new WaitForSeconds(1f);
        if (ShouldStart()) StartCoroutine(Play());
    }

    private void OnDestroy()
    {
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
        if (instance == this) instance = null;
    }

    private void HandleFlag(string flag)
    {
        if (flag == nightClearFlag && ShouldStart()) StartCoroutine(Play());
    }

    private bool ShouldStart()
    {
        return !started && HasFlag(nightClearFlag) && !HasFlag(plateGivenFlag);
    }

    // 성소를 직접 눌렀을 때: 엔딩을 틀었으면(또는 이미 도는 중이면) true -> 성소는 예전 대사를 안 틂
    public static bool TryStart()
    {
        if (instance == null) return false;
        if (instance.started) return true;
        if (!instance.ShouldStart()) return false;
        instance.StartCoroutine(instance.Play());
        return true;
    }

    // ================= 흐름 =================
    private IEnumerator Play()
    {
        started = true;
        yield return RaDayToNight.WaitUntilFree();
        yield return new WaitForSeconds(delay);

        TorchLink.SetBurnPaused(this, true); // 연출 중에는 내 횃불이 줄지 않게
        Cine.Begin(true);
        GameAudio.StopMusic(1.5f);
        yield return ShrineGlow();
        yield return Cutscene();
        yield return LeaveToHall();
    }

    private IEnumerator Cutscene()
    {
        RaFinaleCut cut = RaFinaleCut.Create(RaFinaleArt.Main, lines);
        GameAudio.Music(music, 0.3f);
        GameAudio.Play("sfx_rumble", 1f, 0.7f);
        CineCamera.Shake(0.8f, 0.1f);

        yield return cut.Open();
        yield return Cine.Say(lines.appear);
        yield return Cine.Say(lines.gift);
        yield return cut.ShowCube(lines.cubeCaption);
        yield return GrantPlate();
        yield return cut.HideCube();
        yield return Cine.Say(lines.farewell);
    }

    // 아이템 팝업이 닫힐 때까지 기다린 뒤 플래그 (북쪽 문, 업적, 목표 문구가 이걸 봄)
    private IEnumerator GrantPlate()
    {
        bool closed = false;
        ItemPopup.Grant(plateItemId, () => closed = true);
        while (!closed) yield return null;

        SetFlagOnce(plateGivenFlag);
        SetFlagOnce(endTalkedFlag);
        SetFlagOnce(DemoEnding.CentralEnteredFlag);
        Debug.Log("[노트 6] 태양의 형판을 큐브의 첫 면에 장착하라.");
    }

    private IEnumerator LeaveToHall()
    {
        yield return ScreenFader.FadeOut(1.5f);
        TorchLink.SetBurnPaused(this, false);
        Cine.End();
        try
        {
            if (!Application.CanStreamedLevelBeLoaded(hallScene)) Debug.LogWarning($"[RaFinale] '{hallScene}' 씬이 Scene List에 없음 -> Build Profiles에 추가 필요");
            SceneFlow.LoadByNameOrNext(hallScene);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RaFinale] 중앙 홀 이동 오류: {e}");
        }
    }

    // ================= 성소 번쩍 =================
    private IEnumerator ShrineGlow()
    {
        if (shrine == null) yield break;
        CineCamera.Focus(shrine.transform.position + Vector3.up * 0.5f);
        CineCamera.Zoom(shrineViewSize, 1.4f);
        yield return Cine.Wait(1.2f);

        Light2D light = RaGlowFx.PointLight(shrine.transform, Vector3.up * 0.8f, new Color(1f, 0.8f, 0.4f), 6f, 0f);
        GameAudio.Play("sfx_rumble", 0.8f, 0.8f);
        float t = 0f;
        while (t < 1.6f)
        {
            t += Time.deltaTime;
            light.intensity = Mathf.Pow(t / 1.6f, 2f) * 4f;
            CineCamera.Shake(0.1f, 0.03f + t * 0.04f);
            yield return null;
        }
        Destroy(light.gameObject);
    }

    // ================= 도우미 =================
    private static bool HasFlag(string flag)
    {
        return !string.IsNullOrEmpty(flag) && GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
    }

    private static void SetFlagOnce(string flag)
    {
        if (string.IsNullOrEmpty(flag) || GameManager.Instance == null || GameManager.Instance.HasFlag(flag)) return;
        GameManager.Instance.SetFlag(flag);
    }
}
