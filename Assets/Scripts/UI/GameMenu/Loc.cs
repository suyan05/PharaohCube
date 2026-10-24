using System.Collections.Generic;
using TMPro;
using UnityEngine;

// [추가] 메뉴 글자 번역 (한국어 / English)
// - 시작 화면, 설정 창, 일시 정지 메뉴 글자만 바뀜. 게임 대사·안내는 한국어 그대로
// - 새 문구: Table에 키 한 줄 추가 -> Loc.Get("키") 또는 LocalizedText 컴포넌트의 Key에 입력
public static class Loc
{
    // 키, 한국어, English
    private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
    {
        { "title.continue", new[] { "이어하기", "Continue" } },
        { "title.newgame", new[] { "새 게임", "New Game" } },
        { "title.settings", new[] { "설정", "Settings" } },
        { "title.quit", new[] { "종료", "Quit" } },
        { "title.lastsave", new[] { "마지막 저장: {0}", "Last save: {0}" } },
        { "confirm.newgame", new[] { "저장된 진행이 지워집니다.\n새로 시작할까요?", "Your saved progress will be erased.\nStart a new game?" } },
        { "confirm.quit", new[] { "게임을 종료할까요?", "Quit the game?" } },
        { "confirm.totitle", new[] { "시작 화면으로 돌아갈까요?\n(지금까지의 진행은 저장됩니다)", "Return to the main menu?\n(Your progress will be saved)" } },
        { "common.yes", new[] { "예", "Yes" } },
        { "common.no", new[] { "아니요", "No" } },
        { "common.on", new[] { "켜짐", "On" } },
        { "common.off", new[] { "꺼짐", "Off" } },
        { "pause.title", new[] { "일시 정지", "Paused" } },
        { "pause.resume", new[] { "계속하기", "Resume" } },
        { "pause.settings", new[] { "설정", "Settings" } },
        { "pause.totitle", new[] { "시작 화면으로", "Main Menu" } },
        { "pause.quit", new[] { "게임 종료", "Quit Game" } },
        { "settings.title", new[] { "설정", "Settings" } },
        { "settings.sound", new[] { "소리", "Sound" } },
        { "settings.display", new[] { "화면", "Display" } },
        { "settings.language", new[] { "언어", "Language" } },
        { "settings.master", new[] { "전체 음량", "Master Volume" } },
        { "settings.bgm", new[] { "배경음", "Music" } },
        { "settings.sfx", new[] { "효과음", "Sound Effects" } },
        { "settings.resolution", new[] { "해상도", "Resolution" } },
        { "settings.mode", new[] { "화면 모드", "Display Mode" } },
        { "settings.vsync", new[] { "수직 동기화", "V-Sync" } },
        { "settings.lang", new[] { "언어", "Language" } },
        { "settings.langnote", new[] { "게임 대사는 한국어로 나옵니다.", "In-game dialogue is in Korean only." } },
        { "settings.reset", new[] { "기본값", "Reset" } },
        { "settings.close", new[] { "닫기", "Close" } },
        { "mode.full", new[] { "전체 화면", "Fullscreen" } },
        { "mode.window", new[] { "창 모드", "Windowed" } },
        { "lang.ko", new[] { "한국어", "한국어" } },
        { "lang.en", new[] { "English", "English" } },
        { "hint.back", new[] { "Esc: 뒤로", "Esc: Back" } },
    };

    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (Table.TryGetValue(key, out string[] v))
        {
            int i = (int)GameSettings.Language;
            return i < v.Length && !string.IsNullOrEmpty(v[i]) ? v[i] : v[0];
        }
        Debug.LogWarning($"[Loc] 번역 키 없음: {key}");
        return key;
    }

    public static string Format(string key, object arg) => string.Format(Get(key), arg);
}
