using System;
using System.Collections.Generic;
using UnityEngine;

// 토트의 방 (달의 서고) 이름표: 씬, 플래그, 아이템
// 진행: 벽화 A(서쪽) -> 벽화 B(동쪽) -> 서기관 책상(거짓 기록) -> 달 시계 -> 벽화 C(북쪽 벽감) -> 글자판 주문 -> 지혜의 문
// 숨은 방: 달 조각 3개 (복도 책장 / 달 시계 보상 / 동쪽 먼지 구석) -> 동쪽 벽의 홈 -> '토트의 책'
public static class ThothIds
{
    public const string Scene = "Puzzle_Thoth_Tilemap";
    public const string HallScene = "CentralRoom_Temp";

    // ================= 플래그 =================
    public const string Intro = "F_TH_INTRO";
    public const string DeskDone = "F_TH_DESK_DONE";
    public const string DialDone = "F_TH_DIAL_DONE";
    public const string RevealDone = "F_TH_DIAL_REVEALED";   // 달빛 연출을 봄 (벽감이 열림)
    public const string SpellDone = "F_TH_SPELL_DONE";
    public const string Done = "F_TH_DONE";                  // 형판을 받음 = 토트의 방 끝 (중앙 홀 경로)
    public const string Vault = "F_TH_HIDDEN_VAULT";

    public static string FrescoDone(string fresco) => "F_TH_F" + fresco + "_DONE";
    public static string WallPieces(string fresco) => "F_TH_F" + fresco + "_WALL";   // 벽화 앞 6조각을 주움
    public static string Piece(string fresco, int i) => "F_TH_P_" + fresco + i;       // 흩어진 조각을 주움
    public static string Placed(string fresco, int i) => "F_TH_PP_" + fresco + i;     // 판에 맞춰 넣음

    // ================= 아이템 =================
    public const string Notes = "ITEM_TH_NOTES";
    public const string Plate = "PLATE_THOTH";
    public const string Amulet = "ITEM_WEDJAT_AMULET";        // HorusItemIds.WedjatAmulet와 같은 값 (호루스 매의 회랑을 엶)
    public const string Tablet = "ITEM_THOTH_TABLET";         // 숨은 방 보상 (수첩 숨은 유물 목록과 같은 ID)
    public static string MoonFrag(int n) => "ITEM_TH_MOON_" + n;
    public static string PieceItem(string fresco) => "ITEM_TH_PIECE_" + fresco; // 팝업 표시용 (인벤토리엔 안 넣음)

    public static readonly string[] Frescoes = { "A", "B", "C" };
}

// 토트의 방 공용 도우미 (플래그, 아이템, 그림 불러오기, "지금 E를 받아도 되나")
public static class ThothState
{
    private const string ArtRoot = "Thoth/";
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static bool Has(string flag)
    {
        GameManager gm = GameManager.Instance;
        return !string.IsNullOrEmpty(flag) && gm != null && gm.HasFlag(flag);
    }

    public static void Set(string flag)
    {
        try
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) Debug.LogWarning($"[토트] GameManager가 없어서 플래그 저장 못 함: {flag}");
            else if (!gm.HasFlag(flag)) gm.SetFlag(flag);
        }
        catch (Exception e)
        {
            Debug.LogError($"[토트] 플래그 저장 오류 ({flag}): {e.Message}");
        }
    }

    public static bool HasItem(string id) => ItemInventory.Instance != null && ItemInventory.Instance.Has(id);

    public static int MoonFragCount()
    {
        int n = 0;
        for (int i = 1; i <= 3; i++) if (HasItem(ThothIds.MoonFrag(i))) n++;
        return n;
    }

    // Resources/Thoth/<path> 그림 (예: "World/TH_Desk", "UI/UI_Moon_3", "Fresco/ThFA_p0")
    public static Sprite Art(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        Sprite s;
        if (cache.TryGetValue(path, out s) && s != null) return s;
        s = Resources.Load<Sprite>(ArtRoot + path);
        if (s == null) Debug.LogWarning($"[토트] 그림 없음: Resources/{ArtRoot}{path}");
        cache[path] = s;
        return s;
    }

    // 대사, 연출, 퍼즐 화면, 팝업 중이거나 그게 방금 닫힌 프레임이면 E를 무시 (닫는 E가 다시 여는 일 방지)
    public static bool Busy
    {
        get
        {
            if (Cine.Running || DialogueBox.IsOpen || ItemPopup.IsShowing || MuralStage.IsPlaying) return true;
            if (PuzzleOverlayFocus.IsActive || PuzzleOverlayFocus.ClosedThisFrame) return true;
            return DialogueBox.ClosedFrame == Time.frameCount || ThothPanel.ClosedFrame == Time.frameCount;
        }
    }

    // 벽화 하나의 조각을 몇 개 가졌나 (벽 앞 6개 + 흩어진 것)
    public static bool OwnsPiece(string fresco, int i)
    {
        if (ThothFrescoData.IsScattered(fresco, i)) return Has(ThothIds.Piece(fresco, i));
        return Has(ThothIds.WallPieces(fresco));
    }

    public static int OwnedCount(string fresco)
    {
        int n = 0;
        for (int i = 0; i < ThothFrescoData.PieceCount; i++) if (OwnsPiece(fresco, i)) n++;
        return n;
    }

    public static DialogueLine[] Lines(params string[] speakerAndText)
    {
        var list = new List<DialogueLine>();
        for (int i = 0; i + 1 < speakerAndText.Length; i += 2) list.Add(new DialogueLine(speakerAndText[i], speakerAndText[i + 1]));
        return list.ToArray();
    }
}
