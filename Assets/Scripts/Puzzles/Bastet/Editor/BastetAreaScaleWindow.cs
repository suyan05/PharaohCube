#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// 메뉴: Tools/Bastet/5개 방 크기 조절
// 배율을 고르고 [적용]을 누르면 방 가이드·통로·퍼즐 위치·발자국/석상 간격·봉인석·불빛·비석이 한 번에 줄어듦
// - 그림(석상, 발자국 판) 크기는 그대로, 간격과 방 크기만 바뀜
// - 통로 폭은 2칸 유지 (플레이어가 지나가야 하니까)
// - 이미 칠한 타일은 바뀌지 않음 -> 적용 후 분홍 가이드에 맞춰 다시 칠하기
public class BastetAreaScaleWindow : EditorWindow
{
    private float value = 0.7f;

    [MenuItem("Tools/Bastet/5개 방 크기 조절")]
    private static void Open()
    {
        var w = GetWindow<BastetAreaScaleWindow>(true, "바스테트 5개 방 크기", true);
        w.minSize = new Vector2(340f, 210f);
        var layout = Object.FindFirstObjectByType<BastetAreaLayout>();
        if (layout != null) w.value = layout.Scale;
    }

    private void OnGUI()
    {
        var layout = Object.FindFirstObjectByType<BastetAreaLayout>();
        EditorGUILayout.HelpBox("요청서 배치도(58 x 31칸) 대비 크기입니다.\n그림 크기는 그대로 두고 방 크기와 간격만 줄입니다.\n0.6 아래는 발자국끼리 너무 가까워 잘못 밟기 쉽습니다.", MessageType.Info);

        if (layout == null)
        {
            EditorGUILayout.HelpBox("씬에 Bastet_Area가 없습니다. 먼저 ★ 바스테트 전체 세팅을 실행하세요.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("지금 배율", layout.Scale.ToString("0.00"));
        value = EditorGUILayout.Slider("새 배율", value, 0.5f, 1f);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("100%")) value = 1f;
        if (GUILayout.Button("80%")) value = 0.8f;
        if (GUILayout.Button("70% (추천)")) value = 0.7f;
        if (GUILayout.Button("60%")) value = 0.6f;
        EditorGUILayout.EndHorizontal();

        int w = Mathf.RoundToInt(58 * value), h = Mathf.RoundToInt(31 * value);
        EditorGUILayout.LabelField("예상 전체 크기", $"약 {w} x {h}칸");

        GUILayout.Space(8f);
        if (GUILayout.Button("적용", GUILayout.Height(30f)))
        {
            Undo.RecordObject(layout, "바스테트 크기");
            layout.EditorSetScale(value);
            EditorUtility.SetDirty(layout);
            BastetAreaBuilder.Build(); // 같은 배치를 새 배율로 다시 맞춤 (중복 생성 없음)
            Debug.Log($"[BastetAreaScaleWindow] 배율 {value:0.00} 적용 -> 분홍 가이드대로 타일 다시 칠하고 Ctrl+S");
        }
    }
}
#endif
