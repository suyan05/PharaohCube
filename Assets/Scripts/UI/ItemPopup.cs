using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 아이템 획득 팝업 (아트 요청서 5.6, 화면 가운데 760x440)
// - ItemPopup.Grant("TOOL_BRUSH", 끝나면 할 일)  = 인벤토리에 넣고 + 팝업
// - ItemPopup.Show("TOOL_BRUSH", 끝나면 할 일)   = 팝업만 (이미 넣은 경우)
// - 여러 개를 연달아 부르면 순서대로 하나씩 보여줌
// - 0.3초 뒤부터 아무 키나 / 클릭으로 닫힘. 열려 있는 동안 플레이어 잠금
// - 씬에 안 놓아도 처음 부를 때 자동으로 생김 (아트를 넣으려면 씬에 놓고 칸 채우기)
public class ItemPopup : MonoBehaviour
{
    private static ItemPopup instance;

    [Header("아트 (비우면 코드로 그린 틀)")]
    [SerializeField] private Sprite frameSprite;        // Common_UI_ItemPopup_Frame
    [SerializeField] private Sprite frameHiddenSprite;  // Common_UI_ItemPopup_FrameHidden
    [SerializeField] private Sprite iconBaseSprite;     // Common_UI_ItemPopup_IconBase
    [SerializeField] private Sprite hiddenBadgeSprite;  // Common_UI_ItemPopup_HiddenBadge
    [SerializeField] private Sprite raysSprite;         // Common_FX_ItemRays

    [Header("튜닝")]
    [SerializeField] private float minShowTime = 0.3f;
    [SerializeField] private float popTime = 0.2f;
    [SerializeField] private float raysSpeed = 20f;
    [SerializeField] private int sortingOrder = 110;

    private readonly Queue<(ItemDefinition def, Action done)> queue = new Queue<(ItemDefinition, Action)>();
    private GameObject canvasGo;
    private RectTransform panel;
    private Image frame, icon, rays, badge;
    private Text headerText, nameText, descText;
    private Font font;
    private bool showing;

    public static bool IsShowing => instance != null && instance.showing;

    private static ItemPopup Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<ItemPopup>();
            if (instance == null) instance = new GameObject("ItemPopup").AddComponent<ItemPopup>();
            return instance;
        }
    }

    // ================= 밖에서 부르는 함수 =================
    public static void Grant(string itemId, Action onClosed = null)
    {
        try
        {
            if (ItemInventory.Instance != null) ItemInventory.Instance.Grant(itemId);
            else Debug.LogWarning($"[ItemPopup] ItemInventory가 없어서 {itemId}를 못 넣음 (팝업만 표시)");
            Show(itemId, onClosed);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ItemPopup] 아이템 지급 오류: {e}");
            onClosed?.Invoke();
        }
    }

    public static void Show(string itemId, Action onClosed = null)
    {
        ItemDefinition def = ItemCatalog.Main.GetOrFallback(itemId);
        Instance.Enqueue(def, onClosed);
    }

    private void Enqueue(ItemDefinition def, Action done)
    {
        queue.Enqueue((def, done));
        if (!showing) StartCoroutine(ShowQueue());
    }

    // ================= 표시 =================
    private IEnumerator ShowQueue()
    {
        showing = true;
        PlayerLock.Lock(this);
        EnsureBuilt();

        while (queue.Count > 0)
        {
            var (def, done) = queue.Dequeue();
            yield return ShowOne(def);
            done?.Invoke();
        }

        canvasGo.SetActive(false);
        PlayerLock.Unlock(this);
        showing = false;
    }

    private IEnumerator ShowOne(ItemDefinition def)
    {
        Fill(def);
        canvasGo.SetActive(true);

        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            panel.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, Mathf.Clamp01(t / popTime));
            if (rays.enabled) rays.rectTransform.Rotate(0f, 0f, -raysSpeed * Time.unscaledDeltaTime);
            if (t >= minShowTime && (Input.anyKeyDown || Input.GetMouseButtonDown(0))) break;
            yield return null;
        }
        yield return null; // 닫은 키가 다음 팝업/조사로 이어지지 않게 한 프레임 쉼
    }

    private void Fill(ItemDefinition def)
    {
        bool hidden = def.hidden;
        frame.sprite = hidden && frameHiddenSprite != null ? frameHiddenSprite : frameSprite;
        frame.color = frame.sprite != null ? Color.white : Hex(hidden ? "#E8B23A" : "#C9A66B");

        headerText.text = hidden ? "히든!" : "획득!";
        badge.enabled = hidden;
        rays.enabled = hidden;

        icon.sprite = def.icon;
        icon.enabled = def.icon != null;
        nameText.text = def.displayName;
        descText.text = def.description;
    }

    private void OnDisable()
    {
        if (!showing) return;
        PlayerLock.Unlock(this);
        showing = false;
    }

    // ================= 화면 만들기 =================
    private void EnsureBuilt()
    {
        if (canvasGo != null) return;

        font = PuzzleUITheme.GetBodyFont();
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform root = UIBuild.CreateCanvas(transform, "ItemPopupCanvas", sortingOrder, out canvasGo);
        Image dim = UIBuild.Stretch(root, "Dim").gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.5f);
        dim.raycastTarget = true;

        panel = UIBuild.Rect(root, "Panel", Vector2.zero, new Vector2(760f, 440f));
        BuildPanel();
        canvasGo.SetActive(false);
    }

    private void BuildPanel()
    {
        rays = UIBuild.Image(panel, "Rays", new Vector2(0f, 70f), new Vector2(400f, 400f), raysSprite, Hex("#F3D98A"));
        rays.color = raysSprite != null ? Color.white : new Color(0.95f, 0.85f, 0.54f, 0.35f);

        frame = UIBuild.Image(panel, "Frame", Vector2.zero, new Vector2(760f, 440f), frameSprite, Hex("#C9A66B"));
        if (frameSprite != null && frameSprite.border != Vector4.zero) frame.type = Image.Type.Sliced;
        UIBuild.Image(panel, "Inner", Vector2.zero, new Vector2(736f, 416f), null, Hex("#F6F1E7")).enabled = frameSprite == null;

        badge = UIBuild.Image(panel, "HiddenBadge", new Vector2(0f, 220f), new Vector2(220f, 56f), hiddenBadgeSprite, Hex("#E8B23A"));
        headerText = UIBuild.Text(panel, "Header", font, 26, new Vector2(0f, 205f), new Vector2(300f, 40f), Hex("#4A3423"));

        UIBuild.Image(panel, "IconBase", new Vector2(0f, 70f), new Vector2(200f, 200f), iconBaseSprite, Hex("#E8D9B8"));
        icon = UIBuild.Image(panel, "Icon", new Vector2(0f, 70f), new Vector2(192f, 192f), null, Color.white);

        nameText = UIBuild.Text(panel, "Name", font, 36, new Vector2(0f, -95f), new Vector2(640f, 50f), Hex("#4A3423"));
        nameText.fontStyle = FontStyle.Bold;
        descText = UIBuild.Text(panel, "Desc", font, 24, new Vector2(0f, -155f), new Vector2(640f, 50f), Hex("#6B4A2B"));
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}