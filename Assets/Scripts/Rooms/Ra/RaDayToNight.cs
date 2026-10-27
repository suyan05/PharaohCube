using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 낮 시련을 다 풀면(F_RA_P5_DONE) 성소로 걸어갈 필요 없이 라가 먼저 말을 검
// 1. 제단 화면을 닫고 조작이 풀리면 -> 검은 바 + 카메라가 라의 성소로
// 2. 성소가 금빛으로 번쩍 + 흔들림 -> 라의 대화 자동 시작 (시련 통과 대사)
// 3. 대화가 끝나면 라의 성소가 밤을 엶 (F_RA_NIGHT_OPEN) -> RaTorchManager가 횃불을 모두 끔 -> 라의 밤 대사
// - 이미 밤이면 아무것도 안 함. 씬에 다시 들어왔는데 낮 클리어 상태로 멈춰 있으면 그때도 자동 시작
public class RaDayToNight : MonoBehaviour
{
    [SerializeField] private string dayClearFlag = "F_RA_P5_DONE";
    [SerializeField, Min(0f)] private float delay = 0.8f;
    [SerializeField, Min(1f)] private float shrineViewSize = 4.5f;

    private RaShrine shrine;
    private bool started;
    private bool subscribed;

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
    }

    private void HandleFlag(string flag)
    {
        if (flag == dayClearFlag && ShouldStart()) StartCoroutine(Play());
    }

    private bool ShouldStart()
    {
        GameManager gm = GameManager.Instance;
        return !started && shrine != null && gm != null && gm.HasFlag(dayClearFlag) && !RaPhaseState.IsNight;
    }

    private IEnumerator Play()
    {
        started = true;
        yield return WaitUntilFree();
        yield return new WaitForSeconds(delay);

        Transform player = CineActor.Player();
        Cine.Begin(false);
        CineCamera.Focus(shrine.transform.position + Vector3.up * 0.5f);
        CineCamera.Zoom(shrineViewSize, 1.4f);
        yield return Cine.Wait(1.4f);
        yield return Flash();
        Cine.End();

        if (player != null) shrine.Interact(player.gameObject); // 라의 대화 (끝나면 성소가 밤을 엶)
    }

    // 성소 위로 금빛이 차오르며 번쩍
    private IEnumerator Flash()
    {
        Light2D light = RaGlowFx.PointLight(shrine.transform, Vector3.up * 0.8f, new Color(1f, 0.8f, 0.4f), 5f, 0f);
        float t = 0f;
        while (t < 1.2f)
        {
            t += Time.deltaTime;
            light.intensity = Mathf.Sin(t / 1.2f * Mathf.PI) * 2.5f;
            yield return null;
        }
        Destroy(light.gameObject);
        StartCoroutine(ScreenTintUI.Flash(new Color(1f, 0.85f, 0.5f, 0.45f), 0.08f, 0.1f, 0.5f));
        CineCamera.Shake(0.5f, 0.08f);
        GameAudio.Play("sfx_rumble", 0.9f);
        yield return Cine.Wait(0.7f);
    }

    // 제단 화면, 대사, 다른 연출이 다 끝나고 플레이어가 움직일 수 있을 때까지 (RaFinale도 같이 씀)
    public static IEnumerator WaitUntilFree()
    {
        PlayerController2D pc = FindFirstObjectByType<PlayerController2D>();
        while (Cine.Running || DialogueBox.IsOpen || ItemPopup.IsShowing || PuzzleOverlayFocus.IsActive || (pc != null && !pc.enabled))
        {
            yield return null;
        }
    }
}
