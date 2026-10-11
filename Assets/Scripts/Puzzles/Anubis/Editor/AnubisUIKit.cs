using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 실전 퍼즐 빌더들이 함께 쓰는 UI 생성 도구 (에디터 전용)
// AnubisPuzzleUIBuilder와 같은 색/모양 규칙 사용
public static class AnubisUIKit
{
    public const string CanvasName = "Canvas_AnubisOverlay";
    public const int UILayer = 5;

    public static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.85f);
    public static readonly Color FrameCol = new Color(0.16f, 0.12f, 0.09f, 0.97f);
    public static readonly Color Gold = new Color(0.91f, 0.70f, 0.23f, 1f);
    public static readonly Color GoldDark = new Color(0.62f, 0.45f, 0.14f, 1f);
    public static readonly Color Sand = new Color(0.91f, 0.85f, 0.72f, 1f);
    public static readonly Color BtnCol = new Color(0.35f, 0.25f, 0.15f, 1f);
    public static readonly Color SlotCol = new Color(0.10f, 0.08f, 0.06f, 1f);
    public static readonly Color TextCol = new Color(0.96f, 0.93f, 0.86f, 1f);
    public static readonly Color NoticeBg = new Color(0.22f, 0.16f, 0.07f, 0.98f);

    public static Transform CanvasT { get; private set; }
    public static Sprite UISprite { get; private set; }
    public static Sprite KnobSprite { get; private set; }
    public static TMP_FontAsset Font { get; private set; }

    // 준비: 캔버스 / 기본 스프라이트 / 한글 폰트
    public static bool Prepare()
    {
        var canvas = GameObject.Find(CanvasName);
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Anubis", $"씬에 {CanvasName}가 없습니다.\nAnubis UI Builder에서 '전체 생성'을 먼저 하세요.", "확인");
            return false;
        }
        CanvasT = canvas.transform;
        UISprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        KnobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        Font = FindAsset<TMP_FontAsset>("MonoplexKR");
        if (Font == null)
        {
            Debug.LogWarning("[AnubisUIKit] MonoplexKR 폰트를 못 찾아 TMP 기본 폰트를 사용합니다 (한글이 깨질 수 있음)");
            Font = TMP_Settings.defaultFontAsset;
        }
        return true;
    }

    public static bool Confirm(string title, string message)
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return EditorUtility.DisplayDialog(title, $"{message}\n\n현재 씬: {scene}\n(튜토리얼 씬이면 '취소'를 누르세요)", "만들기", "취소");
    }

    public static T FindAsset<T>(string nameFilter) where T : Object
    {
        foreach (var guid in AssetDatabase.FindAssets($"{nameFilter} t:{typeof(T).Name}"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) return asset;
        }
        return null;
    }

    // 기존 패널 교체: 그 패널을 열던 제단 목록을 기억해 두고 삭제
    public static GameObject ReplacePanel(string panelName, string title, out List<AnubisAltarStation> stations)
    {
        stations = new List<AnubisAltarStation>();
        var old = CanvasT.Find(panelName);
        if (old != null)
        {
            foreach (var st in Object.FindObjectsByType<AnubisAltarStation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var p = new SerializedObject(st).FindProperty("overlayPanel");
                if (p != null && p.objectReferenceValue == old.gameObject) stations.Add(st);
            }
            Undo.DestroyObjectImmediate(old.gameObject);
        }

        var go = new GameObject(panelName, typeof(RectTransform));
        go.layer = UILayer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(CanvasT, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = go.AddComponent<Image>();
        bg.color = Backdrop;
        bg.raycastTarget = true;

        Img("Frame", rt, V(0, 0), V(1720, 1000), FrameCol, UISprite).type = Image.Type.Sliced;
        Txt("Title", rt, V(0, 440), V(1300, 70), title, 48, Gold);
        return go;
    }

    // 새 패널을 기존 제단에 다시 연결
    public static void Relink(List<AnubisAltarStation> stations, GameObject panel)
    {
        foreach (var st in stations)
        {
            var so = new SerializedObject(st);
            so.FindProperty("overlayPanel").objectReferenceValue = panel;
            so.ApplyModifiedProperties();
            Debug.Log($"[AnubisUIKit] {st.name} ← {panel.name} 재연결", st);
        }
        if (stations.Count == 0)
            Debug.LogWarning($"[AnubisUIKit] {panel.name}를 여는 제단이 없습니다. Anubis UI Builder의 '제단 연결'로 연결하세요.");
    }

    public static void FinishPanel(GameObject root)
    {
        root.SetActive(false);
        Undo.RegisterCreatedObjectUndo(root, "Build " + root.name);
        var nb = CanvasT.Find("Panel_Notebook");
        if (nb != null) nb.SetAsLastSibling(); // 사자의 서는 항상 맨 위
        EditorSceneManager.MarkSceneDirty(root.scene);
    }

    // 퍼즐 패널에 양피지 힌트 상자 부착
    public static void AddHintBox(GameObject panel, string scrollAssetName, Vector2 pos, Vector2 size)
    {
        var box = panel.AddComponent<AnubisScrollHintBox>();
        var scroll = FindAsset<AnubisScrollData>(scrollAssetName);
        var so = new SerializedObject(box);
        so.FindProperty("scroll").objectReferenceValue = scroll;
        so.FindProperty("font").objectReferenceValue = Font;
        so.FindProperty("position").vector2Value = pos;
        so.FindProperty("size").vector2Value = size;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (scroll == null)
            Debug.LogWarning($"[AnubisUIKit] 양피지 데이터 '{scrollAssetName}'를 못 찾음 → {panel.name}의 Anubis Scroll Hint Box에서 직접 연결하세요.");
    }

    public static Vector2 V(float x, float y) => new Vector2(x, y);

    public static RectTransform NewRect(string n, Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.layer = UILayer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static Image Img(string n, Transform parent, Vector2 pos, Vector2 size, Color color, Sprite sprite = null, bool raycast = false)
    {
        var img = NewRect(n, parent, pos, size).gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    public static TextMeshProUGUI Txt(string n, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var t = NewRect(n, parent, pos, size).gameObject.AddComponent<TextMeshProUGUI>();
        if (Font != null) t.font = Font;
        t.text = text;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }

    public static Button Btn(string n, Transform parent, Vector2 pos, Vector2 size, string label, float fontSize = 28, Color? bg = null)
    {
        var img = Img(n, parent, pos, size, bg ?? BtnCol, UISprite, true);
        img.type = Image.Type.Sliced;
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        Txt("Label", img.transform, V(0, 0), size - V(10, 6), label, fontSize, TextCol);
        return b;
    }

    public static Button CloseBtn(Transform parent) => Btn("Btn_Close", parent, V(760, 440), V(180, 64), "닫기 [Esc]", 24);

    public static GameObject Notice(string n, Transform parent, Vector2 pos, Vector2 size, string text, out TextMeshProUGUI tmp)
    {
        var img = Img(n, parent, pos, size, NoticeBg, UISprite);
        img.type = Image.Type.Sliced;
        tmp = Txt("Text", img.transform, V(0, 0), size - V(40, 30), text, 32, Gold);
        img.gameObject.SetActive(false);
        return img.gameObject;
    }

    public static void Wire(Component target, params (string field, Object value)[] pairs)
    {
        var so = new SerializedObject(target);
        foreach (var (field, value) in pairs)
        {
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[AnubisUIKit] {target.GetType().Name}에 '{field}' 필드 없음", target); continue; }
            p.objectReferenceValue = value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void WireArray(Component target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null || !p.isArray) { Debug.LogError($"[AnubisUIKit] {target.GetType().Name}에 배열 '{field}' 없음", target); return; }
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}