using System;
using UnityEngine;

// OBJ_H13 북쪽 중앙방 문: H5 클리어 후 개방 (라의 방 NorthDoor와 같은 구조, 열림 플래그만 Inspector에서 지정)
public class HorusNorthDoor : MonoBehaviour, IInteractable
{
    [Header("이 플래그가 켜지면 문이 열림")]
    [SerializeField] private string openFlag = "F_HO_H5_DONE";

    [Header("연결 (비워두면 들어갈 때 로그만 찍음 - 중앙방 연결은 추후)")]
    [SerializeField] private DemoEnding ending;

    private SpriteRenderer sr;
    private bool isOpen;
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
            else Debug.Log("[호루스 북문] 중앙방 연결 예정 (호루스 형판 장착 엔딩은 추후 구현)");
        }
        catch (Exception e)
        {
            Debug.LogError($"[호루스 북문] Interact 오류: {e}");
        }
    }
}