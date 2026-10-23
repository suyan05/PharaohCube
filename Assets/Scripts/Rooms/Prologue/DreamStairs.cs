using System;
using System.Collections;
using UnityEngine;

// 꿈의 지하 계단 (기획서 3.1의 8~9, 스토리보드 컷 8)
// - Open(): 카메라가 계단 비춤 -> 땅울림 + 모래 -> 그림 _Closed -> _Open -> 들어갈 수 있게 됨
// - 열린 계단에 들어가면: 가운데로 걸어감 -> 아래로 내려가며 사라짐 -> 암전
//   -> F_PROLOGUE_CLEAR -> 다음 씬 (Next Scene 비우면 다음 번호 씬 = 라의 방)
// 이 오브젝트의 Collider2D(트리거)가 입구. 열리기 전에는 꺼져 있음
[RequireComponent(typeof(Collider2D))]
public class DreamStairs : MonoBehaviour
{
    [SerializeField] private SpriteRenderer target;
    [Tooltip("Pro_Obj_Stairs_Open")]
    [SerializeField] private Sprite openSprite;
    [Tooltip("계단 안쪽 (내려가기 전에 걸어갈 곳)")]
    [SerializeField] private Vector2 entryPoint = new Vector2(11f, 3f);

    [Header("끝")]
    [SerializeField] private string clearFlag = "F_PROLOGUE_CLEAR";
    [Tooltip("비우면 Build 순서의 다음 씬")]
    [SerializeField] private string nextScene = "";

    [Header("효과 (비워도 됨)")]
    [SerializeField] private AudioClip rumbleSfx;
    [SerializeField] private Sprite[] dustSprites;
    [SerializeField] private Color dustColor = new Color(0.72f, 0.64f, 0.77f, 0.9f);

    private Collider2D trigger;
    private bool open;
    private bool used;

    private void Awake()
    {
        trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
        trigger.enabled = false;
        if (target == null) target = GetComponent<SpriteRenderer>();
    }

    // ================= 열기 =================
    public IEnumerator Open()
    {
        CineCamera.Focus(transform.position + Vector3.up);
        CineCamera.Zoom(4.5f, 0.8f);
        yield return Cine.Wait(0.8f);

        CineAudio.Play(rumbleSfx);
        DreamFX.Ripple(2f, 0.3f);
        Cine.Run(CineCamera.Rumble(1.6f, 0.03f, 0.12f));
        CineFX.Puff(dustSprites, transform.position + Vector3.up * 0.6f, 18, 1.6f, dustColor);
        yield return Cine.Wait(0.8f);

        if (target != null && openSprite != null) target.sprite = openSprite;
        CineFX.Puff(dustSprites, transform.position + Vector3.up * 0.6f, 12, 1f, dustColor);
        yield return Cine.Wait(1f);

        open = true;
        trigger.enabled = true;
    }

    // ================= 내려가기 =================
    private void OnTriggerEnter2D(Collider2D other)
    {
        try
        {
            if (!open || used) return;
            PlayerController2D player = other.GetComponentInParent<PlayerController2D>();
            if (player == null) return;

            used = true;
            StartCoroutine(Descend(player.transform));
        }
        catch (Exception e)
        {
            Debug.LogError($"[DreamStairs] 계단 진입 오류: {e}");
        }
    }

    private IEnumerator Descend(Transform player)
    {
        Cine.Begin(false);
        yield return CineActor.WalkTo(player, entryPoint, 1.8f);
        if (GameManager.Instance != null) GameManager.Instance.SetFlag(clearFlag);

        Cine.Run(CineActor.Sink(player, 1f, 0.7f));
        Cine.Run(DreamFX.HazeTo(1f, 1.1f)); // 꿈에서 깨듯 다시 뿌옇게
        yield return ScreenFader.FadeOut(1.1f);
        yield return CineAudio.StopLoop(0.5f);
        yield return new WaitForSeconds(0.6f);
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
            Debug.LogError($"[DreamStairs] 다음 씬 이동 오류: {e}");
            Cine.End();
            used = false;
        }
    }
}
