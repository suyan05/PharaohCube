using System;
using UnityEngine;

// OBJ_013 북쪽 중앙방 문: 라의 성소에서 태양 형판을 받은 뒤(F_RA_PLATE_GIVEN) 개방 -> E키로 중앙방
// (밤 P5 클리어 -> 성소 마무리 대사 -> 형판 지급 -> 문 열림)
public class NorthDoor : MonoBehaviour, IInteractable
{
    public const string OpenFlag = "F_RA_PLATE_GIVEN";

    [Header("연결")]
    [SerializeField] private DemoEnding ending;

    private SpriteRenderer sr;
    private bool isOpen;
    private bool subscribed;
    private Color closedColor, openColor;

    private void Start()
    {
        try
        {
            ColorUtility.TryParseHtmlString("#4A3423", out closedColor); // 짙은 갈색 (닫힘)
            ColorUtility.TryParseHtmlString("#E8B23A", out openColor);   // 금색 (열림)
            sr = GetComponent<SpriteRenderer>();

            if (GameManager.Instance == null)
            {
                Debug.LogWarning($"[{name}] GameManager가 없어서 문 상태를 못 읽음 (닫힌 상태로 시작)");
                UpdateColor();
                return;
            }

            isOpen = GameManager.Instance.HasFlag(OpenFlag);
            UpdateColor();
            GameManager.Instance.OnFlagChanged += HandleFlag;
            subscribed = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 북쪽 문 초기화 오류: {e}");
        }
    }

    private void OnDestroy()
    {
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
    }

    private void HandleFlag(string flag)
    {
        if (flag != OpenFlag) return;

        isOpen = true;
        UpdateColor();
        Debug.Log("[Door] 북쪽 중앙방 문이 열렸다");
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
                Debug.Log(RaPhaseState.IsNight
                    ? "[Door] 문이 굳게 닫혀 있다. 라에게서 무언가를 받아야 할 것 같다."
                    : "[Door] 문이 굳게 닫혀 있다. 라의 시련을 모두 마쳐야 할 것 같다.");
                return;
            }

            if (ending == null)
            {
                Debug.LogWarning($"[{name}] Ending 칸이 비어 있음");
                return;
            }
            ending.Play(player);
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 북쪽 문 상호작용 오류: {e}");
        }
    }
}