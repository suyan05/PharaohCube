using System.Collections;
using UnityEngine;

// 계단 (E: 내려가기 / 올라가기) -> 화면이 어두워졌다가 도착 지점으로 이동
// 허브 수위계 옆 계단 <-> 숨은 방(물 빠진 하층) 계단, 양쪽에 하나씩 붙임
[RequireComponent(typeof(Collider2D))]
public class SobekStairs : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform destination;
    [SerializeField] private string prompt = "E: 계단 내려가기";
    [Tooltip("이 플래그가 켜져야 쓸 수 있음 (비우면 언제나)")]
    [SerializeField] private string requireFlag = SobekIds.FlagHiddenOpen;
    [TextArea(1, 2)] [SerializeField] private string arriveText = "";
    [Range(0.1f, 2f)] [SerializeField] private float fadeTime = 0.5f;

    private bool moving;

    public string GetPrompt() => moving || !Ready ? "" : prompt;

    private bool Ready => string.IsNullOrEmpty(requireFlag) || SobekStory.HasFlag(requireFlag);

    public void Interact(GameObject player)
    {
        if (moving || !Ready || destination == null || player == null) return;
        StartCoroutine(Move(player));
    }

    private IEnumerator Move(GameObject player)
    {
        moving = true;
        PlayerLock.Lock(this);
        yield return ScreenFader.FadeOut(fadeTime);
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.position = destination.position; }
        player.transform.position = new Vector3(destination.position.x, destination.position.y, player.transform.position.z);
        if (Camera.main != null && Camera.main.TryGetComponent(out CameraFollow2D cam)) cam.SnapToTarget();
        yield return ScreenFader.FadeIn(fadeTime);
        PlayerLock.Unlock(this);
        moving = false;
        if (!string.IsNullOrEmpty(arriveText)) AnubisToast.Show(arriveText, 2.5f);
    }

    private void OnDisable()
    {
        if (moving) PlayerLock.Unlock(this);
        moving = false;
    }

#if UNITY_EDITOR
    public void EditorSetup(Transform dest, string promptText, string flag, string arrive)
    {
        destination = dest; prompt = promptText; requireFlag = flag; arriveText = arrive;
    }
#endif
}
