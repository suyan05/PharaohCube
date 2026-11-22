using System.Collections;
using UnityEngine;

// 계단 (E: 내려가기 / 올라가기) -> 화면이 어두워졌다가 도착 지점으로 이동
// 허브 수위계 옆 계단 <-> 숨은 방(물 빠진 하층) 계단, 양쪽에 하나씩 붙임
// [추가] playCorridor를 켜면 처음 내려갈 때 옆모습 통로 "페트수코스의 길"을 직접 걸은 뒤 숨은 방에 도착 (SobekVaultScenes)
[RequireComponent(typeof(Collider2D))]
public class SobekStairs : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform destination;
    [SerializeField] private string prompt = "E: 계단 내려가기";
    [Tooltip("이 플래그가 켜져야 쓸 수 있음 (비우면 언제나)")]
    [SerializeField] private string requireFlag = SobekIds.FlagHiddenOpen;
    [TextArea(1, 2)] [SerializeField] private string arriveText = "";
    [Range(0.1f, 2f)] [SerializeField] private float fadeTime = 0.5f;

    [Header("[추가] 처음 내려갈 때 통로 걷기")]
    [Tooltip("켜면 처음 한 번은 옆모습 통로를 걸은 뒤 도착 (다음부터는 바로 이동)")]
    [SerializeField] private bool playCorridor;

    private bool moving;

    public string GetPrompt() => moving || !Ready ? "" : prompt;

    private bool Ready => string.IsNullOrEmpty(requireFlag) || SobekStory.HasFlag(requireFlag);

    public void Interact(GameObject player)
    {
        if (moving || !Ready || destination == null || player == null) return;
        if (playCorridor && !SobekStory.HasFlag(SobekVaultScenes.FlagWalked) && !Cine.Running) { StartCoroutine(Corridor(player)); return; }
        StartCoroutine(Move(player));
    }

    // [추가] 통로를 걸음 -> 화면이 검은 동안 숨은 방으로 옮김 -> 통로 화면이 걷히면 숨은 방
    private IEnumerator Corridor(GameObject player)
    {
        moving = true;
        Debug.Log("[SobekStairs] 처음 내려감 -> 통로 '페트수코스의 길'", this);
        Cine.Begin(true);
        yield return SideWalkStage.Play(SobekVaultScenes.Walk());
        yield return ScreenFader.FadeOut(0.2f);   // 통로를 건너뛰어도 순간 이동이 보이지 않게
        Teleport(player);
        yield return new WaitForFixedUpdate(); // 숨은 방 카메라 영역이 잡힌 뒤
        if (Camera.main != null && Camera.main.TryGetComponent(out CameraFollow2D follow)) follow.SnapToTarget();
        SobekStory.SetFlag(SobekVaultScenes.FlagWalked);
        yield return SideWalkStage.Release();
        yield return ScreenFader.FadeIn(fadeTime);
        yield return Cine.Say(SobekVaultScenes.Arrive);
        Cine.End();
        moving = false;
        Debug.Log("[SobekStairs] 통로 끝 -> 숨은 방 도착", this);
    }

    private void Teleport(GameObject player)
    {
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.position = destination.position; }
        player.transform.position = new Vector3(destination.position.x, destination.position.y, player.transform.position.z);
        Physics2D.SyncTransforms(); // 카메라 영역(CameraRoomZone)이 바로 바뀌게
        if (Camera.main != null && Camera.main.TryGetComponent(out CameraFollow2D cam)) cam.SnapToTarget();
    }

    private IEnumerator Move(GameObject player)
    {
        moving = true;
        PlayerLock.Lock(this);
        yield return ScreenFader.FadeOut(fadeTime);
        Teleport(player);
        yield return new WaitForFixedUpdate(); // [추가] 새 방의 카메라 영역이 잡힌 뒤 화면을 밝힘
        if (Camera.main != null && Camera.main.TryGetComponent(out CameraFollow2D cam)) cam.SnapToTarget();
        yield return ScreenFader.FadeIn(fadeTime);
        PlayerLock.Unlock(this);
        moving = false;
        if (!string.IsNullOrEmpty(arriveText)) AnubisToast.Show(arriveText, 2.5f);
    }

    private void OnDisable()
    {
        if (moving) PlayerLock.Unlock(this);
        if (moving && Cine.Running) Cine.End();
        moving = false;
    }

#if UNITY_EDITOR
    public void EditorSetup(Transform dest, string promptText, string flag, string arrive)
    {
        destination = dest; prompt = promptText; requireFlag = flag; arriveText = arrive;
    }

    public void EditorSetCorridor(bool on) { playCorridor = on; }
#endif
}
