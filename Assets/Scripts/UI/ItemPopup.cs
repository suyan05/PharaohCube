using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 아이템 획득 팝업 - [수정] 고고학자가 현장에서 유물 사진을 찍어 수첩에 붙이는 모양으로 바꿈
// - ItemPopup.Grant("TOOL_BRUSH", 끝나면 할 일)  = 인벤토리에 넣고 + 팝업
// - ItemPopup.Show("TOOL_BRUSH", 끝나면 할 일)   = 팝업만 (이미 넣은 경우)
// - 여러 개를 연달아 부르면 순서대로 하나씩 보여줌
// - 연출: 찰칵(셔터 소리) + 하얗게 번쩍 -> 살짝 기울어진 즉석 사진이 톡 떨어짐 -> 빨간 기록 도장이 쾅
// - 닫기: E / Space / Enter / 클릭 (예전엔 아무 키나였음). 열려 있는 동안 플레이어 잠금
// - 씬에 안 놓아도 처음 부를 때 자동으로 생김. 아래 아트 칸은 예전 씬과 맞추려고 남겨둠 (raysSprite, hiddenBadgeSprite만 씀)
public class ItemPopup : MonoBehaviour
{
    private static ItemPopup instance;

    [Header("아트 (비워도 됨)")]
    [SerializeField] private Sprite frameSprite;        // (안 씀) 예전 틀
    [SerializeField] private Sprite frameHiddenSprite;  // (안 씀)
    [SerializeField] private Sprite iconBaseSprite;     // 사진 배경 (비우면 UI_PhotoBg)
    [SerializeField] private Sprite hiddenBadgeSprite;  // 히든 표시
    [SerializeField] private Sprite raysSprite;         // 히든 빛줄기

    [Header("튜닝")]
    [SerializeField] private float minShowTime = 0.45f;
    [SerializeField] private float popTime = 0.22f;
    [SerializeField] private float raysSpeed = 20f;
    [SerializeField] private int sortingOrder = 110;

    private readonly Queue<(ItemDefinition def, Action done)> queue = new Queue<(ItemDefinition, Action)>();
    private GameObject canvasGo;
    private RectTransform card, stamp;
    private Image paper, icon, rays, badge, flash;
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
        GameAudio.Play("sfx_shutter", 0.9f);

        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            Animate(t);
            if (t >= minShowTime && ClosePressed()) break;
            yield return null;
        }
        yield return null; // 닫은 키가 다음 팝업/조사로 이어지지 않게 한 프레임 쉼
    }

    // 번쩍 -> 사진이 톡 -> 도장이 쾅
    private void Animate(float t)
    {
        flash.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - t / 0.28f) * 0.9f);
        float pop = Mathf.Clamp01(t / popTime);
        card.localScale = Vector3.one * Mathf.Lerp(1.18f, 1f, pop * pop * (3f - 2f * pop));
        float s = Mathf.Clamp01((t - 0.32f) / 0.12f);
        stamp.gameObject.SetActive(t >= 0.32f);
        stamp.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, s);
        if (rays.enabled) rays.rectTransform.Rotate(0f, 0f, -raysSpeed * Time.unscaledDeltaTime);
    }

    private static bool ClosePressed()
    {
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetMouseButtonDown(0);
    }

    private void Fill(ItemDefinition def)
    {
        bool hidden = def.hidden;
        paper.color = hidden ? Hex("#F7E3A6") : Hex("#FCF7E8");
        headerText.text = hidden ? "히든 유물 기록" : "유물 기록";
        badge.enabled = hidden && hiddenBadgeSprite != null;
        rays.enabled = hidden;

        icon.sprite = def.icon;
        icon.enabled = def.icon != null;
        nameText.text = def.displayName;
        descText.text = def.description;
        stamp.gameObject.SetActive(false);
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
        dim.color = new Color(0f, 0f, 0f, 0.6f);
        dim.raycastTarget = true;

        card = UIBuild.Rect(root, "Card", new Vector2(0f, 60f), new Vector2(470f, 560f));
        card.localRotation = Quaternion.Euler(0f, 0f, -3f);
        BuildCard();
        BuildCaption(root);
        flash = UIBuild.Image(root, "Flash", Vector2.zero, new Vector2(6000f, 4000f), null, Color.white);
        canvasGo.SetActive(false);
    }

    // 즉석 사진: 그림자 + 흰 종이 + 사진(모래 배경 위 유물) + 테이프 + 이름 + 도장
    private void BuildCard()
    {
        UIBuild.Image(card, "Shadow", new Vector2(12f, -12f), new Vector2(470f, 560f), null, new Color(0f, 0f, 0f, 0.4f));
        paper = UIBuild.Image(card, "Paper", Vector2.zero, new Vector2(470f, 560f), null, Hex("#FCF7E8"));
        Sprite photoBg = iconBaseSprite != null ? iconBaseSprite : UIFrames.Get("UI_PhotoBg");
        Image photo = UIBuild.Image(card, "Photo", new Vector2(0f, 50f), new Vector2(410f, 410f), photoBg, Hex("#E2C48C"));
        photo.preserveAspect = false;

        rays = UIBuild.Image(card, "Rays", new Vector2(0f, 50f), new Vector2(400f, 400f), raysSprite, Hex("#F3D98A"));
        rays.color = raysSprite != null ? Color.white : new Color(1f, 0.92f, 0.6f, 0.35f);
        if (raysSprite == null) rays.sprite = UIBuild.Circle();
        icon = UIBuild.Image(card, "Icon", new Vector2(0f, 56f), new Vector2(256f, 256f), null, Color.white);

        Image tape = UIBuild.Image(card, "Tape", new Vector2(0f, 276f), new Vector2(150f, 48f), UIFrames.Get("UI_Tape"), Hex("#EEE0AA"));
        tape.preserveAspect = false;
        tape.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 5f);
        badge = UIBuild.Image(card, "HiddenBadge", new Vector2(-150f, 236f), new Vector2(120f, 40f), hiddenBadgeSprite, Hex("#E8B23A"));

        nameText = UIBuild.Text(card, "Name", font, 38, new Vector2(0f, -212f), new Vector2(430f, 60f), Hex("#4A3423"));
        nameText.fontStyle = FontStyle.Bold;
        Image stampImg = UIBuild.Image(card, "Stamp", new Vector2(150f, -110f), new Vector2(130f, 130f), UIFrames.Get("UI_Stamp"), Hex("#AA3426"));
        stamp = stampImg.rectTransform;
        stamp.localRotation = Quaternion.Euler(0f, 0f, 14f);
    }

    // 사진 아래 설명 띠 (파피루스 판) + 닫는 법
    private void BuildCaption(RectTransform root)
    {
        Image strip = UIBuild.Image(root, "Caption", new Vector2(0f, -330f), new Vector2(860f, 130f), null, Hex("#F6F1E7"));
        UIFrames.Apply(strip, UIFrames.Papyrus);
        headerText = UIBuild.Text(strip.rectTransform, "Header", font, 22, new Vector2(0f, 34f), new Vector2(800f, 30f), Hex("#A0522D"));
        descText = UIBuild.Text(strip.rectTransform, "Desc", font, 28, new Vector2(0f, -14f), new Vector2(800f, 60f), Hex("#4A3423"));
        Text hint = UIBuild.Text(root, "Hint", font, 22, new Vector2(0f, -430f), new Vector2(600f, 30f), new Color(1f, 1f, 1f, 0.65f));
        hint.text = "E: 수첩에 붙이기";
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
