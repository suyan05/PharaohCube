using System;
using UnityEngine;
using UnityEngine.UI;

// 목표 문구 박스 (아트 요청서 4.2, 화면 왼쪽 위 (40, 40), 640x88)
// - ObjectiveBoxUI.Show("발굴 일지를 확인하자") / ObjectiveBoxUI.Hide()
// - 씬에 안 놓아도 처음 부를 때 자동으로 생김 (아트가 오면 씬에 빈 오브젝트로 놓고 칸 채우기)
// - 라의 방은 기존 RaObjectiveGuide 그대로 둠 (나중에 이걸로 합칠 예정)
public class ObjectiveBoxUI : MonoBehaviour
{
    private static readonly Vector2 BoxSize = new Vector2(640f, 88f);
    private static ObjectiveBoxUI instance;

    [Header("아트 (비우면 코드로 그린 틀)")]
    [SerializeField] private Sprite boxSprite;   // Common_UI_Objective_Box (9-slice 24px)
    [SerializeField] private Sprite iconSprite;  // Common_UI_Objective_Icon_Normal
    [SerializeField] private int sortingOrder = 60;

    private GameObject canvasGo;
    private Text label;

    private static ObjectiveBoxUI Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<ObjectiveBoxUI>();
            if (instance == null) instance = new GameObject("ObjectiveBoxUI").AddComponent<ObjectiveBoxUI>();
            return instance;
        }
    }

    // ================= 밖에서 부르는 함수 =================
    public static void Show(string text)
    {
        try
        {
            Instance.ShowInternal(text);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ObjectiveBoxUI] 목표 표시 오류: {e}");
        }
    }

    public static void Hide()
    {
        if (instance != null && instance.canvasGo != null) instance.canvasGo.SetActive(false);
    }

    private void ShowInternal(string text)
    {
        EnsureBuilt();
        label.text = text ?? "";
        canvasGo.SetActive(!string.IsNullOrEmpty(text));
    }

    // ================= 화면 만들기 =================
    private void EnsureBuilt()
    {
        if (canvasGo != null) return;

        Font font = PuzzleUITheme.GetBodyFont();
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform root = UIBuild.CreateCanvas(transform, "ObjectiveCanvas", sortingOrder, out canvasGo);
        Image box = BuildBox(root);
        BuildIcon(box.rectTransform);

        label = UIBuild.Text(box.rectTransform, "Label", font, 28, new Vector2(40f, 0f), new Vector2(520f, 72f), Hex("#4A3423"));
        label.alignment = TextAnchor.MiddleLeft;
        canvasGo.SetActive(false);
    }

    private Image BuildBox(RectTransform root)
    {
        Image box = UIBuild.Image(root, "Box", Vector2.zero, BoxSize, boxSprite, Hex("#F6F1E7"));
        box.preserveAspect = false;
        if (boxSprite != null && boxSprite.border != Vector4.zero) box.type = Image.Type.Sliced;
        if (boxSprite == null)
        {
            Outline line = box.gameObject.AddComponent<Outline>();
            line.effectColor = Hex("#4A3423");
            line.effectDistance = new Vector2(3f, -3f);
        }

        // 화면 왼쪽 위 기준으로 40px 안쪽
        RectTransform rt = box.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(40f, -40f);
        return box;
    }

    private void BuildIcon(RectTransform box)
    {
        Sprite sprite = iconSprite != null ? iconSprite : UIBuild.Circle();
        Vector2 pos = new Vector2(-BoxSize.x * 0.5f + 44f, 0f);
        Image icon = UIBuild.Image(box, "Icon", pos, new Vector2(56f, 56f), sprite, Color.white);
        if (iconSprite == null) icon.color = Hex("#E8B23A");
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
