using System;
using System.Collections;
using UnityEngine;

// OBJ_H13 북쪽 중앙방 문: H5 클리어 후 개방 (라의 방 NorthDoor와 같은 구조, 열림 플래그만 Inspector에서 지정)
public class HorusNorthDoor : MonoBehaviour, IInteractable
{
    [Header("이 플래그가 켜지면 문이 열림")]
    [SerializeField] private string openFlag = "F_HO_H5_DONE";

    [Header("연결 (예비용. 비워두면 아래 '갈 곳'으로 바로 이동 - DemoEnding은 라 형판을 등록하니 호루스엔 안 씀)")]
    [SerializeField] private DemoEnding ending;

    [Header("갈 곳 (비우면 중앙 홀 CentralRoom_Temp)")]
    [SerializeField] private string hallSceneName = "";
    [SerializeField] private float fadeTime = 0.8f;

    private SpriteRenderer sr;
    private bool isOpen;
    private bool leaving;
    private bool subscribed;
    private Color closedColor, openColor;

    private void Start()
    {
        ColorUtility.TryParseHtmlString("#4A3423", out closedColor); // 짙은 갈색 (닫힘)
        ColorUtility.TryParseHtmlString("#E8B23A", out openColor);   // 금색 (열림)
        sr = GetComponent<SpriteRenderer>();
        if (GameManager.Instance == null)
        {
            Debug.LogError("[호루스 북문] GameManager가 없음");
            return;
        }
        isOpen = GameManager.Instance.HasFlag(openFlag);
        UpdateColor();
        GameManager.Instance.OnFlagChanged += HandleFlag;
        subscribed = true;
    }

    private void OnDestroy()
    {
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
    }

    private void HandleFlag(string flag)
    {
        if (flag != openFlag) return;
        isOpen = true;
        UpdateColor();
        Debug.Log("[호루스 북문] 북쪽 중앙방 문이 열렸다");
    }

    private void UpdateColor()
    {
        if (sr != null) sr.color = isOpen ? openColor : closedColor;
    }

    public string GetPrompt() => isOpen ? "E: 중앙방으로 들어가기" : "E: 북쪽 문 조사";

    public void Interact(GameObject player)
    {
        try
        {
            if (!isOpen)
            {
                Debug.Log("[호루스 북문] 문이 굳게 닫혀 있다. 하늘을 되찾아야 열릴 것 같다.");
                return;
            }
            if (ending != null) ending.Play(player);
            else if (!leaving) StartCoroutine(GoToHall(player));
        }
        catch (Exception e)
        {
            Debug.LogError($"[호루스 북문] Interact 오류: {e}");
        }
    }

    // 암전 -> 중앙 홀 (홀에서 HallRoute 호루스 칸이 돌아온 연출을 보여줌)
    private IEnumerator GoToHall(GameObject player)
    {
        leaving = true;
        LockPlayer(player);
        GameAudio.Play("sfx_rumble", 0.7f);
        yield return ScreenFader.FadeOut(fadeTime);
        try
        {
            string scene = string.IsNullOrEmpty(hallSceneName) ? DemoEnding.DefaultHallScene : hallSceneName;
            Debug.Log($"[호루스 북문] 중앙 홀로 이동: {scene}");
            SceneFlow.LoadByNameOrNext(scene);
        }
        catch (Exception e)
        {
            Debug.LogError($"[호루스 북문] 중앙 홀 이동 오류: {e}");
            leaving = false;
        }
    }

    // 암전 중에 움직이거나 E로 문이 다시 눌리지 않게 (DemoEnding과 같은 방식)
    private static void LockPlayer(GameObject player)
    {
        if (player == null) return;
        var controller = player.GetComponent<PlayerController2D>();
        var interaction = player.GetComponent<InteractionSystem>();
        var rb = player.GetComponent<Rigidbody2D>();
        if (controller != null) controller.enabled = false;
        if (interaction != null) interaction.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}