using UnityEngine;

// OBJ_013 북쪽 중앙방 문: P5 클리어(F_RA_P5_DONE) 후 개방 -> E키로 엔딩 진행
public class NorthDoor : MonoBehaviour, IInteractable
{
    public const string OpenFlag = "F_RA_P5_DONE";

    [Header("연결")]
    [SerializeField] private DemoEnding ending;

    private SpriteRenderer sr;
    private bool isOpen;
    private Color closedColor, openColor;

    private void Start()
    {
        ColorUtility.TryParseHtmlString("#4A3423", out closedColor); // 짙은 갈색 (닫힘)
        ColorUtility.TryParseHtmlString("#E8B23A", out openColor);   // 금색 (열림)

        sr = GetComponent<SpriteRenderer>();
        isOpen = GameManager.Instance.HasFlag(OpenFlag);
        UpdateColor();

        GameManager.Instance.OnFlagChanged += HandleFlag;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
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
        if (!isOpen)
        {
            Debug.Log("[Door] 문이 굳게 닫혀 있다. (P5 클리어 필요)");
            return;
        }
        ending.Play(player);
    }
}