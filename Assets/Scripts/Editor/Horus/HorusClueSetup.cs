using UnityEditor;
using UnityEngine;

// 호루스 단서 (라의 방 파란 비석 방식, HorusRoomObjects가 부름)
// - ClueStele + CluePanel: E -> 그림과 글이 크게 뜨는 단서 패널, 안 읽은 비석 위에 "!"
// - HorusClueNote: 읽는 순간 고고학 수첩 메모에 남김
// - 퍼즐 비석(HS1~3)은 옆 별 횃불을 켜야 글씨가 보임. 입구 비석/경고 석판/벽화는 바로 읽힘
// - 단서 글은 Assets/Scripts/Data/Horus_Data/Clue_HO_*.asset (Inspector에서 고치면 됨. 메뉴를 다시 누르면 아래 글로 덮어씀)
public static class HorusClueSetup
{
    private const string DataDir = "Assets/Scripts/Data/Horus_Data";
    private const string Frame = "Assets/image/ui/UI_Journal_Frame.png";
    private const string Bubble = "Assets/image/ui/UI_Prompt_New.png";
    public const string Slate = "Assets/image/Test/Object/Slate.png";

    // ================= 단서 패널 (씬에 1개, HorusCanvas 아래 화면 전체) =================
    public static void EnsurePanel()
    {
        if (Object.FindFirstObjectByType<CluePanel>(FindObjectsInactive.Include) != null) return;
        GameObject canvas = HorusArtSetup.Find("HorusCanvas");
        if (canvas == null)
        {
            Debug.LogWarning("[HorusClueSetup] HorusCanvas가 없어서 단서 패널을 못 만듦");
            return;
        }
        var go = new GameObject("Clue_Overlay", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        CluePanel panel = go.AddComponent<CluePanel>();
        var so = new SerializedObject(panel);
        HorusArtSetup.Prop(so, "frameSprite").objectReferenceValue = HorusArtSetup.Art(Frame);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================= 단서 데이터 =================
    public static ClueData Data(string id, string title, string body, string illustration)
    {
        PrologueAssetFinder.EnsureFolder(DataDir);
        string path = $"{DataDir}/Clue_HO_{id}.asset";
        ClueData data = AssetDatabase.LoadAssetAtPath<ClueData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ClueData>();
            AssetDatabase.CreateAsset(data, path);
        }
        Sprite art = string.IsNullOrEmpty(illustration) ? null : HorusArtSetup.Art(illustration);
        var so = new SerializedObject(data);
        HorusArtSetup.Prop(so, "clueId").stringValue = "HO_" + id;
        FillContent(HorusArtSetup.Prop(so, "day"), title, body, art);
        FillContent(HorusArtSetup.Prop(so, "night"), title, body, art); // 라의 방 뒤라 밤 상태여도 같은 글
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return data;
    }

    private static void FillContent(SerializedProperty c, string title, string body, Sprite art)
    {
        if (c == null) return;
        c.FindPropertyRelative("title").stringValue = title;
        c.FindPropertyRelative("body").stringValue = body;
        c.FindPropertyRelative("illustration").objectReferenceValue = art;
        c.FindPropertyRelative("illustrationTint").colorValue = Color.white;
    }

    // ================= 비석 붙이기 =================
    public static void AddStele(GameObject go, ClueData data, WallTorch torch, string flagOnRead, int noteId, string noteText, string layer)
    {
        ClueStele stele = go.GetComponent<ClueStele>();
        if (stele == null) stele = go.AddComponent<ClueStele>();
        var so = new SerializedObject(stele);
        HorusArtSetup.Prop(so, "data").objectReferenceValue = data;
        HorusArtSetup.Prop(so, "newBubble").objectReferenceValue = NewBubble(go.transform, layer);
        HorusArtSetup.Prop(so, "roomSize").vector2Value = Vector2.zero;
        HorusArtSetup.Prop(so, "flagOnRead").stringValue = flagOnRead;
        HorusArtSetup.Prop(so, "needLight").boolValue = torch != null;
        SerializedProperty torches = HorusArtSetup.Prop(so, "roomTorches");
        torches.arraySize = torch != null ? 1 : 0;
        if (torch != null) torches.GetArrayElementAtIndex(0).objectReferenceValue = torch;
        HorusArtSetup.Prop(so, "lensDecodeAtNight").boolValue = false; // 호루스 방엔 렌즈 해독 없음
        SetDarkLine(HorusArtSetup.Prop(so, "darkLines"));
        so.ApplyModifiedPropertiesWithoutUndo();
        AddNote(go, flagOnRead, noteId, noteText);
    }

    private static void SetDarkLine(SerializedProperty lines)
    {
        if (lines == null) return;
        lines.arraySize = 1;
        SerializedProperty e = lines.GetArrayElementAtIndex(0);
        e.FindPropertyRelative("speaker").stringValue = "나";
        e.FindPropertyRelative("text").stringValue = "너무 어두워서 글씨가 보이지 않는다. 옆의 별 횃불부터 밝히자.";
    }

    private static void AddNote(GameObject go, string flag, int noteId, string text)
    {
        HorusClueNote note = go.GetComponent<HorusClueNote>();
        if (note == null) note = go.AddComponent<HorusClueNote>();
        var so = new SerializedObject(note);
        HorusArtSetup.Prop(so, "readFlag").stringValue = flag;
        HorusArtSetup.Prop(so, "noteId").intValue = noteId;
        HorusArtSetup.Prop(so, "noteText").stringValue = text;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 안 읽은 비석 위 "!" (월드 크기 0.5칸, 부모 크기와 상관없이)
    private static SpriteRenderer NewBubble(Transform parent, string layer)
    {
        Transform old = parent.Find("NewBubble");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        Sprite s = HorusArtSetup.Art(Bubble);
        var go = new GameObject("NewBubble", typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        SpriteRenderer parentSr = parent.GetComponent<SpriteRenderer>();
        float top = parentSr != null ? parentSr.bounds.max.y : parent.position.y + 0.6f;
        go.transform.position = new Vector3(parent.position.x, top + 0.35f, 0f);
        float k = s != null ? 0.5f / s.bounds.size.x : 1f;
        go.transform.localScale = Vector3.one * (k / Mathf.Max(parent.lossyScale.x, 0.001f));
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingLayerName = layer;
        sr.sortingOrder = 1;
        return sr;
    }

    // ================= NoteStone(로그만 남던 비석) -> 단서 비석으로 바꾸기 =================
    // 노트 번호, 글, 읽음 플래그는 원래 NoteStone 것을 그대로 씀
    public static void ConvertNoteStone(string objName, string id, string title, string illustration, string layer)
    {
        GameObject go = HorusArtSetup.Find(objName);
        if (go == null) return;
        int noteId = 0;
        string text = "", flag = "";
        NoteStone note = go.GetComponent<NoteStone>();
        if (note == null) return; // 이미 바꾼 비석 (글을 빈 값으로 덮어쓰지 않게)
        var nso = new SerializedObject(note);
        noteId = nso.FindProperty("noteId").intValue;
        text = nso.FindProperty("noteText").stringValue;
        flag = nso.FindProperty("flagOnRead").stringValue;
        Object.DestroyImmediate(note);
        if (string.IsNullOrEmpty(flag)) flag = $"F_HO_CLUE_{id}_READ";
        AddStele(go, Data(id, title, text, illustration), null, flag, noteId, text, layer);
    }
}
