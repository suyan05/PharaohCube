#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;

// [추가] 메뉴: Tools/UI/한글 글꼴 다시 만들기 (글자 빠짐·깨짐 해결)
// 증상: 안내 상자에 "톱  북  신  해와 달"처럼 글자가 듬성듬성 빠지거나, "J 고고학 수첩"이 이상한 모양으로 깨짐
// 원인: 동적(Dynamic) 글꼴이 1024 크기 그림 4장(멀티 아틀라스)에 글자를 나눠 담고 있는데,
//       여러 사람이 각자 글자를 추가한 글꼴 파일이 Git에서 합쳐지면서 "글자 위치 표"와 "글자 그림"이 서로 안 맞게 됨
// 해결: 글꼴 그림을 4096 한 장으로 바꾸고 전부 비운 뒤, 프로젝트에 쓰인 글자를 다시 그려 넣음
public static class KoreanFontRepair
{
    private const string FontPath = "Assets/Fonts/MonoplexKR/MonoplexKR-Regular SDF.asset";

    [MenuItem("Tools/UI/한글 글꼴 다시 만들기 (글자 빠짐·깨짐 해결)")]
    private static void Repair()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) { EditorUtility.DisplayDialog("글꼴", $"글꼴을 찾지 못했습니다: {FontPath}", "확인"); return; }
        if (!EditorUtility.DisplayDialog("한글 글꼴 다시 만들기",
                "MonoplexKR-Regular SDF 글꼴의 글자 그림을 비우고 다시 만듭니다.\n(팀 공용 글꼴이라 커밋할 때 팀원에게 알려 주세요)\n\nPlay 중이 아닐 때 실행하세요.",
                "진행", "취소")) return;

        // 1) 그림 한 장(4096) + 동적 유지
        var so = new SerializedObject(font);
        Set(so, "m_AtlasWidth", 4096);
        Set(so, "m_AtlasHeight", 4096);
        SerializedProperty multi = so.FindProperty("m_IsMultiAtlasTexturesEnabled");
        if (multi != null) multi.boolValue = false;
        Set(so, "m_AtlasPopulationMode", 1); // Dynamic
        SerializedProperty clearOnBuild = so.FindProperty("m_ClearDynamicDataOnBuild");
        if (clearOnBuild != null) clearOnBuild.boolValue = false; // 빌드에서도 미리 그린 글자 유지
        so.ApplyModifiedPropertiesWithoutUndo();

        // 2) 비우기
        font.ClearFontAssetData(true);

        // 3) 프로젝트에 쓰인 글자 다시 그리기
        string chars = CollectCharacters(out int files);
        bool ok = font.TryAddCharacters(chars, out string missing);
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();

        int atlasCount = font.atlasTextures != null ? font.atlasTextures.Length : 0;
        string msg = $"글자 {chars.Length}개 (파일 {files}개에서 찾음) 다시 그림\n글꼴 그림 {atlasCount}장 (4096)\n" +
                     (ok ? "빠진 글자 없음" : $"못 넣은 글자 {missing?.Length ?? 0}개 (글꼴에 없는 기호일 수 있음)");
        Debug.Log($"[KoreanFontRepair] {msg}\n못 넣은 글자: {missing}");
        EditorUtility.DisplayDialog("한글 글꼴 다시 만들기", msg + "\n\n다른 글자는 게임 중 필요할 때 자동으로 추가됩니다.", "확인");
    }

    private static void Set(SerializedObject so, string prop, int v)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p != null) p.intValue = v;
        else Debug.LogWarning($"[KoreanFontRepair] {prop} 없음 (TMP 버전 차이)");
    }

    // 스크립트·씬·프리팹·에셋에서 한글 + 영어·숫자·기호를 모음
    private static string CollectCharacters(out int fileCount)
    {
        var set = new HashSet<char>();
        for (char c = ' '; c <= '~'; c++) set.Add(c); // 영어·숫자·기본 기호
        foreach (char c in "…·「」『』《》〈〉【】→←↑↓★☆●○■□◆◇♥♡※~！？、。“”‘’%°") set.Add(c);

        var uEscape = new Regex(@"\\u([0-9A-Fa-f]{4})"); // 씬·에셋 파일의 "횃" 형태
        fileCount = 0;
        string[] patterns = { "*.cs", "*.unity", "*.prefab", "*.asset", "*.json", "*.txt", "*.csv" };
        foreach (string pattern in patterns)
        {
            foreach (string path in Directory.GetFiles("Assets", pattern, SearchOption.AllDirectories))
            {
                if (path.Replace('\\', '/') == FontPath) continue;
                string text;
                try { text = File.ReadAllText(path, Encoding.UTF8); }
                catch { continue; }
                fileCount++;
                foreach (char c in text) if (c >= 0xAC00 && c <= 0xD7A3) set.Add(c);
                foreach (Match m in uEscape.Matches(text))
                {
                    char c = (char)System.Convert.ToInt32(m.Groups[1].Value, 16);
                    if (c >= 0xAC00 && c <= 0xD7A3) set.Add(c);
                }
            }
        }
        var sb = new StringBuilder(set.Count);
        foreach (char c in set) sb.Append(c);
        return sb.ToString();
    }
}
#endif
