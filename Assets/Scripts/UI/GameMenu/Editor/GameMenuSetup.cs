#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// [추가] 메뉴: Tools/UI/★ 시작 화면·설정 창 만들기
// 1) Assets/Resources/MenuTheme.asset (글꼴·색·버튼 그림·배경음 칸) 만들기 (있으면 빈 칸만 채움)
// 2) Assets/Scenes/Title.unity (카메라 + TitleScreen) 만들기 (있으면 그대로 둠)
// 3) Build Profiles의 Scene List 맨 앞(0번)에 Title 넣기 -> 게임을 켜면 시작 화면부터
// 설정 창·일시 정지 메뉴는 따로 둘 필요 없음 (GameMenuBootstrap이 자동으로 만듦)
public static class GameMenuSetup
{
    private const string ThemePath = "Assets/Resources/MenuTheme.asset";
    private const string ScenePath = "Assets/Scenes/Title.unity";
    private const string FontPath = "Assets/Fonts/MonoplexKR/MonoplexKR-Regular SDF.asset";

    [MenuItem("Tools/UI/★ 시작 화면·설정 창 만들기", priority = 0)]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var log = new List<string>();
        MenuTheme theme = EnsureTheme(log);
        EnsureTitleScene(theme, log);
        EnsureBuildOrder(log);
        AssetDatabase.SaveAssets();
        Debug.Log("[GameMenuSetup] 완료\n- " + string.Join("\n- ", log));
        EditorUtility.DisplayDialog("시작 화면·설정 창", string.Join("\n", log) + "\n\nTitle 씬에서 Play 해 보세요.", "확인");
    }

    private static MenuTheme EnsureTheme(List<string> log)
    {
        MenuTheme t = AssetDatabase.LoadAssetAtPath<MenuTheme>(ThemePath);
        if (t == null)
        {
            Directory.CreateDirectory("Assets/Resources");
            t = ScriptableObject.CreateInstance<MenuTheme>();
            AssetDatabase.CreateAsset(t, ThemePath);
            log.Add($"테마 만듦: {ThemePath}");
        }
        else log.Add($"테마 있음: {ThemePath} (빈 칸만 채움)");

        if (t.font == null) t.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (t.panelSprite == null) t.panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (t.buttonSprite == null) t.buttonSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (t.knobSprite == null) t.knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (t.font == null) log.Add($"경고: 한글 글꼴을 못 찾음 ({FontPath}) -> MenuTheme의 Font 칸에 직접 넣기");
        EditorUtility.SetDirty(t);
        return t;
    }

    private static void EnsureTitleScene(MenuTheme theme, List<string> log)
    {
        if (File.Exists(ScenePath))
        {
            log.Add($"시작 화면 씬 있음: {ScenePath}");
            return;
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = theme.backgroundColor;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<AudioListener>();

        new GameObject("TitleScreen").AddComponent<TitleScreen>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        log.Add($"시작 화면 씬 만듦: {ScenePath}");
    }

    private static void EnsureBuildOrder(List<string> log)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        list.RemoveAll(s => s.path == ScenePath);
        list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
        log.Add("Scene List 0번 = Title (게임을 켜면 시작 화면부터)");
        string first = list.Count > 1 ? Path.GetFileNameWithoutExtension(list[1].path) : "";
        MenuTheme t = AssetDatabase.LoadAssetAtPath<MenuTheme>(ThemePath);
        if (t != null) log.Add($"새 게임 첫 씬 = {t.newGameScene} (Scene List 1번은 {first})");
    }
}
#endif
